// This file is part of OpenPasswordFilter.
// 
// OpenPasswordFilter is free software; you can redistribute it and / or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// OpenPasswordFilter is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with OpenPasswordFilter; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111 - 1307  USA
//
// --------
//
// dllmain.cpp -- this is the code for OpenPasswordFilter's DLL that will be used
// by LSASS to check the validity of incoming user password requests.  This is a
// very simple password filter; all it does is connect to the local OPFService.exe
// instance (on 127.0.0.1:5999) and send off the password.  
//
// Note that this software "fails open", which means that if anything goes wrong
// we assume that the password is OK.  This has the disadvantage that in unforeseen
// circumstances a user may still be able to set a password that is not allowed.
// That said, it has the advantage that if something breaks people can actually
// change passwords (often a nice feature when things go wrong).  
//
// Author:  Josh Stone
// Contact: yakovdk@gmail.com
// Date:    2015-07-19
//

#include "stdafx.h"
#include <Windows.h>
#include <WinSock2.h>
#include <ws2tcpip.h>
#include <stdlib.h>
#include <stdio.h>
#include <SubAuth.h>
#include <process.h>
#include <codecvt>

#pragma comment(lib, "Ws2_32.lib")

using namespace std;

struct PasswordFilterAccount {
	PUNICODE_STRING AccountName;
	PUNICODE_STRING FullName;
	PUNICODE_STRING Password;
};

bool bPasswordOk = true;

//
// make sure all data is sent through the socket
//
int sendall(SOCKET s, const char *buf, int *len) {
	int total = 0;        // how many bytes we've sent
	int bytesleft = *len; // how many we have left to send
	int n;

	while (total < *len) {
		n = send(s, buf + total, bytesleft, 0);
		if (n == -1) { break; }
		total += n;
		bytesleft -= n;
	}

	*len = total; // return number actually sent here

	return n == -1 ? -1 : 0; // return -1 onm failure, 0 on success
}

// Regular DLL boilerplate

BOOL __stdcall APIENTRY DllMain(HMODULE hModule, DWORD  ul_reason_for_call, LPVOID lpReserved) {
	WSADATA wsa;
	FILE *f = NULL;

	switch (ul_reason_for_call) {
	case DLL_PROCESS_ATTACH:
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
	return TRUE;
}

//
// We don't have any setup to do here, since the core business logic
// for evaluating passwords lives in the separate OPFService.exe 
// project.  So, we can just say we've initialized immediately.
//

extern "C" __declspec(dllexport) BOOLEAN __stdcall InitializeChangeNotify(void) {
	return TRUE;
}

extern "C" __declspec(dllexport) int __stdcall 
PasswordChangeNotify(PUNICODE_STRING *UserName, 
                     ULONG RelativeId, 
                     PUNICODE_STRING *NewPassword) {
	return 0;
}

//
// Assuming that a socket connection has been successfully accomplished
// with the password filter service, this function will handle the
// query for the user's password and determine whether it is an approved
// password or not.  The server will respond with "true" or "false", 
// though for simplicity here I just check the first character. 
// 
// Here is a sample query:
//
//    <connect>
//    client:   test\n
//    client:   Username\n
//    client:   Password1\n
//    server:   false\n
//    <disconnect>
//
void askServer(SOCKET sock, PUNICODE_STRING AccountName, PUNICODE_STRING Password) {
	using convert_type = std::codecvt_utf8<wchar_t>;
	std::wstring_convert<convert_type, wchar_t> converter;
	char rcBuffer[1024];
	char *preamble = "test\n"; //command that is used to start password testing
	int i;
	int len;

	i = send(sock, preamble, (int)strlen(preamble), 0); //send test command
	if (i != SOCKET_ERROR) {
		std::wstring wPassword(Password->Buffer, Password->Length / sizeof(WCHAR));
		wPassword.push_back('\n');

		std::string sPassword = converter.to_bytes(wPassword);

		const char * cPassword = sPassword.c_str();
		len = static_cast<int>(sPassword.size());
		i = sendall(sock, cPassword, &len);

		//i = send(sock, sPassword.c_str(), sPassword.size(), 0);

		if (i != SOCKET_ERROR) {
			i = recv(sock, rcBuffer, sizeof(rcBuffer), 0);//read response
			if (i > 0 && rcBuffer[0] == 'f') {
				bPasswordOk = FALSE;
			}
		}
		else {
			//report error
		}
	}
	else {
		//report error
	}
}

//
// In this function, we establish a TCP connection to 127.0.0.1:5999 and determine
// whether the indicated password is acceptable according to the filter service.
// The service is a C# program also in this solution, titled "OPFService".
//
unsigned int __stdcall CreateSocket(void *v) {
	//the account object
	PasswordFilterAccount *pfAccount = static_cast<PasswordFilterAccount*>(v);

	SOCKET sock = INVALID_SOCKET;
	struct addrinfo *result = NULL;
	struct addrinfo *ptr = NULL;
	struct addrinfo hints;
	bPasswordOk = TRUE; // set fail open

	int i;

	ZeroMemory(&hints, sizeof(hints));
	hints.ai_family = AF_UNSPEC;
	hints.ai_socktype = SOCK_STREAM;
	hints.ai_protocol = IPPROTO_TCP;

	// This butt-ugly loop is straight out of Microsoft's reference example
	// for a TCP client.  It's not my style, but how can the reference be
	// wrong? ;-)
	i = getaddrinfo("127.0.0.1", "5999", &hints, &result);
	if (i == 0) {
		for (ptr = result; ptr != NULL; ptr = ptr->ai_next) {
			sock = socket(ptr->ai_family, ptr->ai_socktype, ptr->ai_protocol);
			if (sock == INVALID_SOCKET) {
				break;
			}
			i = connect(sock, ptr->ai_addr, (int)ptr->ai_addrlen);
			if (i == SOCKET_ERROR) {
				closesocket(sock);
				sock = INVALID_SOCKET;
				continue;
			}
			break;
		}

		if (sock != INVALID_SOCKET) {
			askServer(sock, pfAccount->AccountName, pfAccount->Password);
			closesocket(sock);
		}
	}

	return bPasswordOk;
}

extern "C" __declspec(dllexport) BOOLEAN __stdcall PasswordFilter(PUNICODE_STRING AccountName,
																  PUNICODE_STRING FullName,
																  PUNICODE_STRING Password,
																  BOOLEAN SetOperation) {

	//build the account struct
	PasswordFilterAccount *pfAccount = new PasswordFilterAccount();
	pfAccount->AccountName = AccountName;
	pfAccount->Password = Password;

	//start an asynchronous thread to be able to kill the thread if it exceeds the timout
	HANDLE pfHandle = (HANDLE)_beginthreadex(0, 0, CreateSocket, (LPVOID *)pfAccount, 0, 0);

	DWORD dWaitFor = WaitForSingleObject(pfHandle, 30000); //do not exceed the timeout
	if (dWaitFor == WAIT_TIMEOUT) {
		//timeout exceeded
	}
	else if (dWaitFor == WAIT_OBJECT_0) {
		//here is where we want to be
	}
	else {
		//WAIT_ABANDONED
		//WAIT_FAILED
	}

	if (pfHandle != INVALID_HANDLE_VALUE && pfHandle != 0) {
		if (CloseHandle(pfHandle)) {
			pfHandle = INVALID_HANDLE_VALUE;
		}
	}

	return bPasswordOk;
}
