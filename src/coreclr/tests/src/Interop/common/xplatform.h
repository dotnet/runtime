// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __XPLAT_H__
#define __XPLAT_H__

#ifdef _MSC_VER
// Our tests don't care about secure CRT
#define _CRT_SECURE_NO_WARNINGS 1
#endif

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

    #ifndef snprintf
    #define snprintf _snprintf
    #endif //snprintf

#else
    #include "types.h"

#endif
#include <wchar.h>

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

// Calling conventions
#ifndef _WIN32

#define STDMETHODCALLTYPE

#if __i386__
#define __stdcall __attribute__((stdcall))
#define __cdecl __attribute__((cdecl))
#else
#define __stdcall
#define __cdecl
#endif
#endif //!_WIN32

// Ensure that both UNICODE and _UNICODE are set.
#ifndef _UNICODE
    #define _UNICODE
#endif
#ifndef UNICODE
    #define UNICODE
#endif

void *CoreClrAlloc(size_t cb)
{
#ifdef _WIN32
    return ::CoTaskMemAlloc(cb);
#else
    return ::malloc(cb);
#endif
}

void CoreClrFree(void *p)
{
#ifdef _WIN32
    return ::CoTaskMemFree(p);
#else
    return ::free(p);
#endif
}

// redirected types not-windows only
#ifndef  _WIN32

class IUnknown
{
public:
    virtual int QueryInterface(void* riid,void** ppvObject) = 0;
    virtual unsigned long AddRef() = 0;
    virtual unsigned long Release() = 0;
};

// function implementation
size_t strncpy_s(char* strDest, size_t numberOfElements, const char *strSource, size_t count)
{
    // NOTE: Need to pass count + 1 since strncpy_s does not count null,
    // while snprintf does. 
    return snprintf(strDest, count + 1, "%s", strSource);
}

size_t strcpy_s(char *dest, size_t n, char const *src)
{
    return snprintf(dest, n, "%s", src);
}

size_t wcslen(const WCHAR *str)
{
    size_t len = 0;
    while ('\0' != *(str + len)) len++;
    return len;
}

int wcsncpy_s(LPWSTR strDestination, size_t size1, LPCWSTR strSource, size_t size2)
{
    // copy sizeInBytes bytes of strSource into strDestination
    if (NULL == strDestination || NULL == strSource) return 1;

    int cnt = 0;
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

int wcsncmp(LPCWSTR str1, LPCWSTR str2,size_t len)
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

int wmemcmp(LPCWSTR str1, LPCWSTR str2,size_t len)
{
    return wcsncmp(str1, str2, len);
}

#endif //!_Win32

#endif // __XPLAT_H__
