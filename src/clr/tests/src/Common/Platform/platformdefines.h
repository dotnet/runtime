// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


#include <stdio.h>
#include <stdlib.h>
#include <string.h>

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


typedef unsigned error_t;
typedef HANDLE THREAD_ID;

#define DLL_EXPORT __declspec(dllexport)

#else // !WINDOWS
#include <pthread.h>

typedef char16_t WCHAR;
typedef unsigned long DWORD;
typedef int BOOL;
typedef WCHAR *LPWSTR, *PWSTR;
typedef const WCHAR *LPCWSTR, *PCWSTR;

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
#else
#define __stdcall
#define _cdecl
#endif
#endif

#if __GNUC__ >= 4
#define DLL_EXPORT __attribute__ ((visibility ("default")))
#else
#define DLL_EXPORT
#endif

LPWSTR HackyConvertToWSTR(char* pszInput);

#define FS_SEPERATOR L("/")
#define PATH_DELIMITER L(":")
#define L(t) HackyConvertToWSTR(t)
#define MAX_PATH 260

typedef pthread_t THREAD_ID;
typedef void* (*MacWorker)(void*);
typedef DWORD (*LPTHREAD_START_ROUTINE)(void*);
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
#endif

//
// Method declarations
//
error_t TP_scpy_s(LPWSTR strDestination, size_t sizeInWords, LPCWSTR strSource);
error_t TP_scat_s(LPWSTR strDestination, size_t sizeInWords, LPCWSTR strSource);
int TP_slen(LPWSTR str);
int TP_scmp_s(LPSTR str1, LPSTR str2);
int TP_wcmp_s(LPWSTR str1, LPWSTR str2);
error_t TP_getenv_s(size_t* pReturnValue, LPWSTR buffer, size_t sizeInWords, LPCWSTR varname);
error_t TP_putenv_s(LPTSTR name, LPTSTR value);
void TP_ZeroMemory(LPVOID buffer, size_t sizeInBytes);
error_t TP_itow_s(int num, LPWSTR buffer, size_t sizeInCharacters, int radix);
LPWSTR TP_sstr(LPWSTR str, LPWSTR searchStr);
LPSTR  HackyConvertToSTR(LPWSTR pwszInput);
DWORD TP_CreateThread(THREAD_ID* tThread, LPTHREAD_START_ROUTINE worker,  LPVOID lpParameter);
void TP_JoinThread(THREAD_ID tThread);
void TP_DebugBreak();
DWORD TP_GetFullPathName(LPWSTR fileName, DWORD nBufferLength, LPWSTR lpBuffer);

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
#define wcsstr TP_sstr
#define strcmp TP_scmp_s
#define wcscmp TP_wcmp_s
#endif

#endif

