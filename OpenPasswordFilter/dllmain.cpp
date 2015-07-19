// This file is part of OpenPasswordFilter.
// 
// OpenPasswordFilter is free software; you can redistribute it and / or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// Foobar is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Foobar; if not, write to the Free Software
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

#pragma comment(lib, "Ws2_32.lib")

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
//    client:   Password1\n
//    server:   false\n
//    <disconnect>
//

BOOLEAN askServer(SOCKET sock, PUNICODE_STRING Password) {
	char buffer[1024];
	char *preamble = "test\n";
	int i;

	i = send(sock, preamble, (int)strlen(preamble), 0);
	if (i != SOCKET_ERROR) {
		int length = Password->Length / sizeof(WCHAR);
		if (length + 2 < sizeof(buffer)) {
			i = wcstombs(buffer, Password->Buffer, length);
			buffer[i] = '\n';
			buffer[i + 1] = '\0';
			i = send(sock, buffer, (int)strlen(buffer), 0);
			if (i != SOCKET_ERROR) {
				i = recv(sock, buffer, sizeof(buffer), 0);
				if (i > 0 && buffer[0] == 'f') {
					return FALSE;
				}
			}
		}
	}

	return TRUE;
}

//
// In this function, we establish a TCP connection to 127.0.0.1:5999 and determine
// whether the indicated password is acceptable according to the filter service.
// The service is a C# program also in this solution, titled "OPFService".
//

extern "C" __declspec(dllexport) BOOLEAN __stdcall PasswordFilter(PUNICODE_STRING AccountName, 
	                                                              PUNICODE_STRING FullName, 
																  PUNICODE_STRING Password, 
																  BOOLEAN SetOperation) {
	SOCKET sock = INVALID_SOCKET;
	struct addrinfo *result = NULL;
	struct addrinfo *ptr = NULL;
	struct addrinfo hints;
	BOOLEAN retval = TRUE;

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
			i = connect(sock, ptr->ai_addr, (int)ptr->ai_addr);
			if (i == SOCKET_ERROR) {
				closesocket(sock);
				sock = INVALID_SOCKET;
				continue;
			}
			break;
		}

		if (sock != INVALID_SOCKET) {
			retval = askServer(sock, Password);
			closesocket(sock);
		}
	}

	// MS documentation suggests doing this.  I honestly don't know why LSA
	// doesn't just do this for you after we return.  But, I'll do what the
	// docs say...

	SecureZeroMemory(Password->Buffer, Password->Length);
	return retval;
}

