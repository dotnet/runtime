// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __XPLAT_H__
#define __XPLAT_H__

// common headers
#include <stdio.h>
#include <memory.h>
#include <stdlib.h>

// This macro is used to standardize the wide character string literals between UNIX and Windows.
// Unix L"" is UTF32, and on windows it's UTF16.  Because of built-in assumptions on the size
// of string literals, it's important to match behaviour between Unix and Windows.  Unix will be defined
// as u"" (char16_t)
#ifdef _WIN32
#define W(str)  L##str
#else // !_WIN32
#define W(str)  u##str
#endif //_WIN32


//  include 
#ifdef _WIN32
	#include <windows.h>
	#include <wchar.h>
	#include <tchar.h>
#else
	#include "types.h"
#endif


// dllexport
#if defined _WIN32
#define DLL_EXPORT __declspec(dllexport)
#else //!_Win32
#if __GNUC__ >= 4    
#define DLL_EXPORT __attribute__ ((visibility ("default")))
#else
#define DLL_EXPORT
#endif

#endif //_WIN32


#define WINAPI   _cdecl
#ifndef __stdcall
#if __i386__
#define __stdcall __attribute__((stdcall))
#define _cdecl __attribute__((cdecl))
#else
#define __stdcall
#define _cdecl
#endif
#endif





// Ensure that both UNICODE and _UNICODE are set.
#ifdef UNICODE
#ifndef _UNICODE
#define _UNICODE
#endif
#else
#ifdef _UNICODE
#define UNICODE
#endif
#endif


// redirected functions
#ifdef UNICODE
#define _tcslen	wcslen
#define _tcsncmp wcsncmp
#else
#define _tcslen strlen
#define _tcsncmp strncmp
#endif // UNICODE



// redirected types not-windows only
#ifndef  _WIN32

typedef union tagCY {
	struct {
		unsigned long Lo;
		long          Hi;
	};
	long int64;
} CY, CURRENCY;

#define CoTaskMemAlloc(p) malloc(p)
#define CoTaskMemFree(p) free(p)

// function implementation
size_t strncpy_s(char* strDest, size_t numberOfElements, const char *strSource, size_t count)
{
	return snprintf(strDest, count, "%s", strSource);
}

size_t strcpy_s(char *dest, size_t n, char const *src)
{
	return snprintf(dest, n, "%s", src);
}

void SysFreeString(char* str)
{
	free(str);
}


char* SysAllocString( const char* str)
{
	size_t nz = strlen(str);
	char *cArr = (char*) malloc(nz);
	memcpy(cArr, str, nz);
	return cArr;
}


size_t wcslen(const WCHAR *str)
{
	int len;
	if (!str) return 0;
	len = 0;
	while ('\0' != *(str + len)) len++;
	return len;
}

WCHAR* SysAllocString(const WCHAR* str)
{
	size_t nz = wcslen(str);
	nz *= 2;
	WCHAR *cArr = (WCHAR*)malloc(nz);
	memcpy(cArr, str, nz);
	return cArr;
}



int wcsncpy_s(LPWSTR strDestination, size_t size1, LPCWSTR strSource, size_t size2)
{
	int cnt;
	// copy sizeInBytes bytes of strSource into strDestination
	if (NULL == strDestination || NULL == strSource) return 1;

	cnt = 0;
	while (cnt < size1 && '\0' != strSource[cnt])
	{
		strDestination[cnt] = strSource[cnt];
		cnt++;
	}
	strDestination[cnt] = '\0';
	return 0;
}

int wcsncpy_s(LPWSTR strDestination, size_t size1, LPCWSTR strSource)
{
	return wcsncpy_s(strDestination, size1, strSource, 0);

}

int wmemcmp(LPWSTR str1, LPWSTR str2,size_t len)
{
	// < 0 str1 less than str2
	// 0  str1 identical to str2
	// > 0 str1 greater than str2

	if (NULL == str1 && NULL != str2) return -1;
	if (NULL != str1 && NULL == str2) return 1;
	if (NULL == str1 && NULL == str2) return 0;

	while (*str1 == *str2 && '\0' != *str1 && '\0' != *str2 && len--!= 0)
	{
		str1++;
		str2++;
	}

	if ('\0' == *str1 && '\0' == *str2) return 0;

	if ('\0' != *str1) return -1;
	if ('\0' != *str2) return 1;

	return (*str1 > *str2) ? 1 : -1;
}


#endif //!_Win32

#endif // __XPLAT_H__
