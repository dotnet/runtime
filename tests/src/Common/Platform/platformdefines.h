// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <cstdint>

#ifndef _PLATFORMDEFINES__H
#define _PLATFORMDEFINES__H

//
// types and constants
//
#ifdef WINDOWS
#include <windows.h>

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
typedef int BOOL;
typedef WCHAR *LPWSTR, *PWSTR;
typedef const WCHAR *LPCWSTR, *PCWSTR;

typedef int HRESULT;
#define LONGLONG long long
#define ULONGLONG unsigned LONGLONG
typedef unsigned int ULONG, *PULONG;
#define S_OK                    0x0
#define SUCCEEDED(_hr)          ((HRESULT)(_hr) >= 0)
#define FAILED(_hr)             ((HRESULT)(_hr) < 0)

#ifdef ULONG_MAX
#undef ULONG_MAX
#endif
#define ULONG_MAX     0xffffffffUL
#define CCH_BSTRMAX 0x7FFFFFFF  // 4 + (0x7ffffffb + 1 ) * 2 ==> 0xFFFFFFFC
#define CB_BSTRMAX 0xFFFFFFFa   // 4 + (0xfffffff6 + 2) ==> 0xFFFFFFFC

#ifdef RC_INVOKED
#define _HRESULT_TYPEDEF_(_sc) _sc
#else // RC_INVOKED
#define _HRESULT_TYPEDEF_(_sc) ((HRESULT)_sc)
#endif // RC_INVOKED
#define E_INVALIDARG                     _HRESULT_TYPEDEF_(0x80070057L)
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

typedef pthread_t THREAD_ID;
typedef void* (*MacWorker)(void*);
typedef DWORD __stdcall (*LPTHREAD_START_ROUTINE)(void*);
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
typedef int error_t;
typedef void* LPVOID;
typedef unsigned char BYTE;
typedef WCHAR OLECHAR;
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

typedef WCHAR* BSTR;
BSTR TP_SysAllocStringByteLen(LPCSTR psz, size_t len);
void TP_SysFreeString(BSTR bstr);
size_t TP_SysStringByteLen(BSTR bstr);
BSTR TP_SysAllocStringLen(LPCWSTR psz, size_t len);
BSTR TP_SysAllocString(LPCWSTR psz);
DWORD TP_SysStringLen(BSTR bstr);

//
// Method redirects
//
#ifdef WINDOWS
#define TP_LoadLibrary(l) LoadLibrary(l)
#define TP_LoadLibraryW(l) LoadLibraryW(l)
#define TP_LoadLibraryA(l) LoadLibraryA(l)
#define TP_GetProcAddress(m,e) GetProcAddress(m,e)
#define TP_CoTaskMemAlloc(t) CoTaskMemAlloc(t)
#define TP_CoTaskMemFree(t) CoTaskMemFree(t)
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
#define TP_CoTaskMemAlloc(t) malloc(t)
#define TP_CoTaskMemFree(t) free(t)
#define TP_rand arc4random
#define TP_srand srandom
#define wcscpy_s TP_scpy_s
#define wcscat_s TP_scat_s
#define GetFullPathNameW(fname,buflen,buf,filepart)  TP_GetFullPathName(fname,buflen,buf)
#define wcslen TP_slen
#define _wgetenv_s TP_getenv_s
#define _putenv_s TP_putenv_s
#define ZeroMemory TP_ZeroMemory
#define _itow_s TP_itow_s
#define _itoa_s TP_itoa_s
#define wcsstr TP_sstr
#define strcmp TP_scmp_s
#define wcscmp TP_wcmp_s
#endif

#endif

