// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//*****************************************************************************
// WinWrap.h
//
// This file contains wrapper functions for Win32 API's.
//
//*****************************************************************************

#ifndef __WIN_WRAP_H__
#define __WIN_WRAP_H__

// STDCALL macro is meant to be used where we have hard dependency on __stdcall calling convention on x86.
#define STDCALL _stdcall

//********** Includes. ********************************************************

#include <crtwrap.h>
#include <windows.h>
#include <wincrypt.h>
#include <specstrings.h>

#include "longfilepathwrappers.h"

#include "palclr.h"

#undef CreateSemaphore
#define CreateSemaphore(_secattr, _count, _maxcount, _name) CreateSemaphoreExW((_secattr), (_count), (_maxcount), (_name), 0, MAXIMUM_ALLOWED | SYNCHRONIZE | SEMAPHORE_MODIFY_STATE)

#undef GetFileVersionInfo
#define GetFileVersionInfo(_filename, _handle, _len, _data) GetFileVersionInfoEx(0, (_filename), (_handle), (_len), (_data))
#undef GetFileVersionInfoSize
#define GetFileVersionInfoSize(_filename, _handle) GetFileVersionInfoSizeEx(0, (_filename), (_handle))

#ifndef _T
#define _T(str) W(str)
#endif

//File and Directory Functions which need special handling for LongFile Names
//Note only the functions which are currently used are defined
#ifdef HOST_WINDOWS
#define WszLoadLibrary         LoadLibraryExWrapper
#define WszCreateFile          CreateFileWrapper
#define WszGetFileAttributesEx GetFileAttributesExWrapper
#else // HOST_WINDOWS
#define WszLoadLibrary         LoadLibraryExW
#define WszCreateFile          CreateFileW
#define WszGetFileAttributesEx GetFileAttributesExW
#endif // HOST_WINDOWS

//APIS which have a buffer as an out parameter
#define WszGetEnvironmentVariable GetEnvironmentVariableWrapper
#define WszSearchPath          SearchPathWrapper
#define WszGetModuleFileName   GetModuleFileNameWrapper

#ifdef HOST_WINDOWS
DWORD GetMaxDBCSCharByteSize();
#else
#define GetMaxDBCSCharByteSize() 3 // UTF8
#endif

BOOL
WszCreateProcess(
    LPCWSTR lpApplicationName,
    LPCWSTR lpCommandLine,
    LPSECURITY_ATTRIBUTES lpProcessAttributes,
    LPSECURITY_ATTRIBUTES lpThreadAttributes,
    BOOL bInheritHandles,
    DWORD dwCreationFlags,
    LPVOID lpEnvironment,
    LPCWSTR lpCurrentDirectory,
    LPSTARTUPINFOW lpStartupInfo,
    LPPROCESS_INFORMATION lpProcessInformation
    );

#endif  // __WIN_WRAP_H__
