// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


#include <stdio.h>
#include <memory.h>
#include <stdlib.h>
#include <string.h>
#include <cstdint>

#ifndef _PLATFORMDEFINES__H
#define _PLATFORMDEFINES__H

#ifdef _MSC_VER
// Our tests don't care about secure CRT
#define _CRT_SECURE_NO_WARNINGS 1
#endif


// Ensure that both UNICODE and _UNICODE are set.
#ifndef _UNICODE
#define _UNICODE
#endif
#ifndef UNICODE
#define UNICODE
#endif

#include <wchar.h>
#define static_assert_no_msg(x) static_assert((x), #x)

//
// types and constants
//
#ifdef WINDOWS

#define NOMINMAX

#include <windows.h>
#include <combaseapi.h>

#define FS_SEPERATOR L"\\"
#define PATH_DELIMITER L";"
#define L(t) L##t
#define W(str)  L##str

typedef unsigned error_t;
typedef HANDLE THREAD_ID;

#define DLL_EXPORT __declspec(dllexport)

#else // !WINDOWS
#include <pthread.h>

typedef char16_t WCHAR;
typedef unsigned int DWORD;

#ifdef OBJC_TESTS
// The Objective-C headers define the BOOL type to be unsigned char or bool.
// As a result, we can't redefine it here. So instead, define WINBOOL to be int-sized.
typedef int WINBOOL;
#else
typedef int BOOL;
#endif
typedef WCHAR *LPWSTR, *PWSTR;
typedef const WCHAR *LPCWSTR, *PCWSTR;

typedef int HRESULT;
#define LONGLONG long long
#define ULONGLONG unsigned LONGLONG
typedef unsigned int ULONG, *PULONG;
#define S_OK                    0x0
#define SUCCEEDED(_hr)          ((HRESULT)(_hr) >= 0)
#define FAILED(_hr)             ((HRESULT)(_hr) < 0)

#define CCH_BSTRMAX 0x7FFFFFFF  // 4 + (0x7ffffffb + 1 ) * 2 ==> 0xFFFFFFFC
#define CB_BSTRMAX 0xFFFFFFFa   // 4 + (0xfffffff6 + 2) ==> 0xFFFFFFFC

#ifdef RC_INVOKED
#define _HRESULT_TYPEDEF_(_sc) _sc
#else // RC_INVOKED
#define _HRESULT_TYPEDEF_(_sc) ((HRESULT)_sc)
#endif // RC_INVOKED
#define E_INVALIDARG                     _HRESULT_TYPEDEF_(0x80070057L)

#ifdef HOST_64BIT
#define __int64     long
#else // HOST_64BIT
#define __int64     long long
#endif // HOST_64BIT

#define UInt32x32To64(a, b) ((unsigned __int64)((ULONG)(a)) * (unsigned __int64)((ULONG)(b)))

#define ARRAYSIZE(x) (sizeof(x)/sizeof(*x))

#ifndef TRUE
#define TRUE 1
#endif

#ifndef FALSE
#define FALSE 0
#endif

#ifndef WINAPI
#define WINAPI  __stdcall
#endif

#ifndef STDMETHODCALLTYPE
#define STDMETHODCALLTYPE
#endif

#ifndef STDMETHODVCALLTYPE
#define STDMETHODVCALLTYPE
#endif

#ifndef _MSC_VER
#if __i386__
#define __stdcall __attribute__((stdcall))
#define _cdecl __attribute__((cdecl))
#define __cdecl __attribute__((cdecl))
#else
#define __stdcall
#define _cdecl
#define __cdecl
#endif
#endif

#if __GNUC__ >= 4
#define DLL_EXPORT __attribute__ ((visibility ("default")))
#else
#define DLL_EXPORT
#endif

LPWSTR HackyConvertToWSTR(const char* pszInput);

#define FS_SEPERATOR L("/")
#define PATH_DELIMITER L(":")
#define L(t) HackyConvertToWSTR(t)
#define W(str)  u##str
#define MAX_PATH 260
#define __FUNCTIONW__ HackyConvertToWSTR(__func__)

typedef pthread_t THREAD_ID;
typedef void* (*MacWorker)(void*);
typedef DWORD __stdcall (*LPTHREAD_START_ROUTINE)(void*);
typedef WCHAR TCHAR;
typedef char* LPSTR;
typedef const char* LPCSTR;
typedef TCHAR* LPTSTR;
typedef const TCHAR* LPCTSTR;
typedef void* FARPROC;
typedef void* HANDLE;
typedef HANDLE HMODULE;
typedef void* ULONG_PTR;
typedef int error_t;
typedef void* LPVOID;
typedef unsigned char BYTE;
typedef WCHAR OLECHAR;
typedef double DATE;          
typedef DWORD LCID;
#endif

typedef ULONG_PTR DWORD_PTR;

//
// Method declarations
//
error_t TP_scpy_s(LPWSTR strDestination, size_t sizeInWords, LPCWSTR strSource);
error_t TP_scat_s(LPWSTR strDestination, size_t sizeInWords, LPCWSTR strSource);
size_t TP_slen(LPCWSTR str);
int TP_scmp_s(LPCSTR str1, LPCSTR str2);
int TP_wcmp_s(LPCWSTR str1, LPCWSTR str2);
error_t TP_getenv_s(size_t* pReturnValue, LPWSTR buffer, size_t sizeInWords, LPCWSTR varname);
error_t TP_putenv_s(LPTSTR name, LPTSTR value);
void TP_ZeroMemory(LPVOID buffer, size_t sizeInBytes);
error_t TP_itow_s(int num, LPWSTR buffer, size_t sizeInCharacters, int radix);
error_t TP_itoa_s(int num, LPSTR buffer, size_t sizeInCharacters, int radix);
LPWSTR TP_sstr(LPWSTR str, LPWSTR searchStr);
LPSTR  HackyConvertToSTR(LPWSTR pwszInput);
DWORD TP_CreateThread(THREAD_ID* tThread, LPTHREAD_START_ROUTINE worker,  LPVOID lpParameter);
void TP_JoinThread(THREAD_ID tThread);
void TP_DebugBreak();
DWORD TP_GetFullPathName(LPWSTR fileName, DWORD nBufferLength, LPWSTR lpBuffer);


size_t TP_strncpy_s(char* strDest, size_t numberOfElements, const char *strSource, size_t count);
size_t TP_strcpy_s(char *dest, size_t n, char const *src);
int    TP_wcsncpy_s(LPWSTR strDestination, size_t size1, LPCWSTR strSource, size_t size2);
int    TP_wcsncmp(LPCWSTR str1, LPCWSTR str2,size_t len);
int    TP_wmemcmp(LPCWSTR str1, LPCWSTR str2,size_t len);

typedef WCHAR* BSTR;

BSTR CoreClrBStrAlloc(LPCSTR psz, size_t len);
BSTR CoreClrBStrAlloc(LPCWSTR psz, size_t len);

inline void *CoreClrBStrAlloc(size_t cb)
{
    // A null is automatically applied in the SysAllocStringByteLen API.
    // Remove a single OLECHAR for the implied null.
    // https://docs.microsoft.com/en-us/previous-versions/windows/desktop/api/oleauto/nf-oleauto-sysallocstringbytelen
    if (cb >= sizeof(OLECHAR))
        cb -= sizeof(OLECHAR);

    return CoreClrBStrAlloc((LPCSTR)nullptr, cb);
}

void CoreClrBStrFree(BSTR bstr);

inline void CoreClrBStrFree(void* p)
{
    CoreClrBStrFree((BSTR)p);
}

size_t TP_SysStringByteLen(BSTR bstr);
BSTR TP_SysAllocString(LPCWSTR psz);
size_t TP_SysStringLen(BSTR bstr);


inline void *CoreClrAlloc(size_t cb)
{
#ifdef WINDOWS
    return ::CoTaskMemAlloc(cb);
#else
    return ::malloc(cb);
#endif
}

inline void CoreClrFree(void *p)
{
#ifdef WINDOWS
    return ::CoTaskMemFree(p);
#else
    return ::free(p);
#endif
}

//
// Method redirects
//
#ifdef WINDOWS
#define TP_LoadLibrary(l) LoadLibrary(l)
#define TP_LoadLibraryW(l) LoadLibraryW(l)
#define TP_LoadLibraryA(l) LoadLibraryA(l)
#define TP_GetProcAddress(m,e) GetProcAddress(m,e)
#define TP_DebugBreak() DebugBreak()
#define TP_rand rand
#define TP_srand srand
#else
#define fopen_s(FILEHANDLE, FILENAME, MODE) *(FILEHANDLE) = fopen(FILENAME, MODE)
#define _fsopen(FILENAME, MODE, ACCESS) fopen(FILENAME, MODE)
#define GetCurrentDirectory(BUFSIZ, BUF) getcwd(BUF, BUFSIZ)
#define DeleteFile unlink
#define GlobalFree free
#define sprintf_s snprintf
#define fwscanf_s fwscanf
#define strcat_s(DST,SIZ,SRC) strlcat(DST,SRC,SIZ)
#define TP_LoadLibrary(l) dlopen(l, 0)
#define TP_LoadLibraryW(l) dlopen(l, 0)
#define TP_LoadLibraryA(l) dlopen(l, 0)
#define TP_GetProcAddress(m,e) dlsym(m,e)
#define TP_rand arc4random
#define TP_srand srandom
#define GetFullPathNameW(fname,buflen,buf,filepart)  TP_GetFullPathName(fname,buflen,buf)
#define ZeroMemory TP_ZeroMemory
#define _itow_s TP_itow_s
#define _itoa_s TP_itoa_s
#define strcmp TP_scmp_s
#define strncpy_s TP_strncpy_s
#define strcpy_s TP_strcpy_s
#endif

#endif

