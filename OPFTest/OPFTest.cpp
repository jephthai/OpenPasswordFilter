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
// OPFTest.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <WinSock2.h>
#include <Windows.h>
#include <stdio.h>
#include <ws2tcpip.h>
#include <SubAuth.h>

#pragma comment(lib, "Ws2_32.lib")

typedef int(__stdcall *PasswordFilter_t)(PUNICODE_STRING, PUNICODE_STRING, PUNICODE_STRING, int);

int _tmain(int argc, _TCHAR* argv[])
{
	WSADATA wsa;
	PUNICODE_STRING pwd;

	pwd = (PUNICODE_STRING) malloc(sizeof(PUNICODE_STRING));

	printf("\n");
	printf("----------------------------------------------------------------------\n");
	printf("Test program for OpenPasswordFilter.dll (Josh Stone, yakovdk@gmail.com)\n");
	printf("----------------------------------------------------------------------\n");
	printf("\n");
	if (argc < 2) {
		printf("usage: opftest <password>\n");
		return 2;
	}

	printf("Initializing winsock...");
	if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0) {
		return 3;
	}
	printf("done.\n");

	PasswordFilter_t PasswordFilter;
	printf("Loading library...");
	HMODULE lib = LoadLibrary(L"OpenPasswordFilter.dll");
	if (lib != NULL) {
		printf("success (%08x).\n", lib);
		printf("Getting PasswordFilter() address...");
		PasswordFilter = (PasswordFilter_t)GetProcAddress(lib, "PasswordFilter");
		if (PasswordFilter != NULL) {
			printf("success (%08x).\n", PasswordFilter);
			if (PasswordFilter == NULL) {
				return 1;
			}
			wprintf(L"Testing password %s...", argv[1]);
			pwd->Length = wcslen(argv[1]) * sizeof(WCHAR);
			pwd->MaximumLength = wcslen(argv[1]) * sizeof(WCHAR);
			pwd->Buffer = argv[1];
			if (PasswordFilter(pwd, pwd, pwd, 1)) {
				printf("success.\n");
			}
			else {
				printf("failure.\n");
			}
		}
		else {
			printf("failed (%d).\n", GetLastError());
		}
	}
	else {
		printf("failed (%d).\n", GetLastError());
	}
	return 0;
}

