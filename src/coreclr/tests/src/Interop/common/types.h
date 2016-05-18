// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _INTEROP_TYPES__H
#define _INTEROP_TYPES__H

#define INT_MIN	   (-2147483647 - 1)

typedef char16_t WCHAR;
typedef unsigned long DWORD;
typedef int BOOL;
typedef WCHAR *LPWSTR, *PWSTR;
typedef const WCHAR *LPCWSTR, *PCWSTR;

#ifdef UNICODE
typedef WCHAR TCHAR;
#else // ANSI
typedef char TCHAR;
#endif // UNICODE

typedef char* LPSTR;
typedef const char* LPCSTR;
typedef TCHAR* LPTSTR;
typedef const TCHAR* LPCTSTR;
typedef void* FARPROC;
typedef void* HMODULE;
typedef void* ULONG_PTR;
typedef unsigned error_t;
typedef void* LPVOID;
typedef char BYTE;
typedef WCHAR OLECHAR;

typedef unsigned int UINT_PTR;

typedef unsigned long long ULONG64;
typedef double DOUBLE;
typedef float FLOAT;
typedef signed long long LONG64, *PLONG64;
typedef int INT, *LPINT;
typedef unsigned int UINT;
typedef char CHAR, *PCHAR;
typedef unsigned short USHORT;
typedef signed short SHORT;
typedef unsigned short WORD, *PWORD, *LPWORD;

typedef int*  DWORD_PTR;

#ifndef TRUE
#define TRUE 1
#endif

#ifndef FALSE
#define FALSE 0
#endif

#endif //_INTEROP_TYPES__H