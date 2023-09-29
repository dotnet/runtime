// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//*****************************************************************************
// WinWrap.h
//
// This file contains wrapper functions for Win32 API's that take strings.
//
// The Common Language Runtime internally uses UNICODE as the internal state
// and string format.  This file will undef the mapping macros so that one
// cannot mistakingly call a method that isn't going to work.  Instead, you
// have to call the correct wrapper API.
//
//*****************************************************************************

#ifndef __WIN_WRAP_H__
#define __WIN_WRAP_H__

//********** Macros. **********************************************************
#if !defined(WIN32_LEAN_AND_MEAN)
#define WIN32_LEAN_AND_MEAN
#endif

//
// WinCE uniformly uses cdecl calling convention on x86. __stdcall is defined as __cdecl in SDK.
// STDCALL macro is meant to be used where we have hard dependency on __stdcall calling convention
// - the unification with __cdecl does not apply to STDCALL.
//
#define STDCALL _stdcall

//********** Includes. ********************************************************

#include <crtwrap.h>
#include <windows.h>
#include <wincrypt.h>
#include <specstrings.h>

#include "longfilepathwrappers.h"

#if defined(_PREFAST_) || defined(SOURCE_FORMATTING)
//
// For PREFAST we don't want the C_ASSERT to be expanded since it always
// involves the comparison of two constants which causes PREfast warning 326
//
#undef C_ASSERT
#define C_ASSERT(expr)
#endif

#include "palclr.h"

#if !defined(__TODO_PORT_TO_WRAPPERS__)
//*****************************************************************************
// Undefine all of the windows wrappers so you can't use them.
//*****************************************************************************

// winbase.h
#undef GetBinaryType
#undef GetShortPathName
#undef GetEnvironmentStrings
#undef FreeEnvironmentStrings
#undef FormatMessage
#undef lstrcmp
#undef lstrcmpi
#undef lstrcpyn
#undef lstrlen
#undef CreateMutex
#undef OpenMutex
#undef CreateEvent
#undef OpenEvent
#undef CreateSemaphore
#undef OpenSemaphore
#undef CreateWaitableTimer
#undef CreateFileMapping
#undef GetModuleFileName
#undef GetModuleHandle
#undef GetModuleHandleEx
#undef CreateProcess
#undef GetCommandLine
#undef GetEnvironmentVariable
#undef SetEnvironmentVariable
#undef ExpandEnvironmentStrings
#undef OutputDebugString
#undef FindResource
#undef FindResourceEx
#undef BeginUpdateResource
#undef UpdateResource
#undef EndUpdateResource
#undef GetPrivateProfileInt
#undef GetSystemDirectory
#undef GetTempPath
#undef GetTempFileName
#undef GetFullPathName
#undef CreateFile
#undef GetFileAttributes
#undef GetFileAttributesEx
#undef FindFirstFileEx
#undef FindFirstFile
#undef FindNextFile
#undef CopyFile
#undef CopyFileEx
#undef MoveFile
#undef CreateHardLink
#undef CreateNamedPipe
#undef WaitNamedPipe
#undef LookupPrivilegeValue
#undef GetVersionEx

// winuser.h
#undef MAKEINTRESOURCE
#undef GetUserObjectInformation
#undef GetMessage

#undef SendMessage
#undef CharLower
#undef MessageBox
#undef GetClassName
#undef LoadString
#undef GetCalendarInfo
#undef GetDateFormat
#undef GetTimeFormat
#undef LCMapString

#endif // !defined(__TODO_PORT_TO_WRAPPERS__)

//
// NT supports the wide entry points.  So we redefine the wrappers right back
// to the *W entry points as macros.  This way no client code needs a wrapper on NT.
//

// winbase.h
#define WszFormatMessage   FormatMessageW
#define WszCreateMutex CreateMutexW
#define WszOpenMutex OpenMutexW
#define WszCreateEvent CreateEventW
#define WszOpenEvent OpenEventW
#define WszCreateWaitableTimer CreateWaitableTimerW
#define WszCreateFileMapping CreateFileMappingW
#define WszGetModuleHandle GetModuleHandleW
#define WszGetModuleHandleEx GetModuleHandleExW
#define WszGetCommandLine GetCommandLineW
#define WszSetEnvironmentVariable SetEnvironmentVariableW
#define WszExpandEnvironmentStrings ExpandEnvironmentStringsW
#define WszOutputDebugString OutputDebugStringW
#define WszFindResource FindResourceW
#define WszFindResourceEx FindResourceExW
#define WszBeginUpdateResource BeginUpdateResourceW
#define WszUpdateResource UpdateResourceW
#define WszEndUpdateResource EndUpdateResourceW
#define WszGetPrivateProfileInt GetPrivateProfileIntW
#define WszGetSystemDirectory GetSystemDirectoryW
#define WszCreateNamedPipe CreateNamedPipeW
#define WszWaitNamedPipe WaitNamedPipeW
#define WszLookupPrivilegeValue LookupPrivilegeValueW

// winuser.h
#define WszMAKEINTRESOURCE MAKEINTRESOURCEW
#define WszGetUserObjectInformation GetUserObjectInformationW
#define WszGetMessage GetMessageW
#define WszSendMessage SendMessageW
#define WszCharLower CharLowerW
#define WszGetClassName GetClassNameW
#define WszLoadString LoadStringW
#define WszRegOpenKeyEx RegOpenKeyExW
#define WszRegOpenKey(hKey, wszSubKey, phkRes) RegOpenKeyExW(hKey, wszSubKey, 0, KEY_ALL_ACCESS, phkRes)
#define WszRegQueryValue RegQueryValueW
#define WszRegQueryValueEx RegQueryValueExW

#define WszRegQueryInfoKey RegQueryInfoKeyW
#define WszRegEnumValue RegEnumValueW
#define WszRegEnumKeyEx RegEnumKeyExW
#define WszGetCalendarInfo GetCalendarInfoW
#define WszGetDateFormat GetDateFormatW
#define WszGetTimeFormat GetTimeFormatW
#define WszLCMapString LCMapStringW
#define WszMultiByteToWideChar MultiByteToWideChar
#define WszWideCharToMultiByte WideCharToMultiByte
#define WszCreateSemaphore(_secattr, _count, _maxcount, _name) CreateSemaphoreExW((_secattr), (_count), (_maxcount), (_name), 0, MAXIMUM_ALLOWED | SYNCHRONIZE | SEMAPHORE_MODIFY_STATE)

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

//Can not use extended syntax
#define WszGetFullPathName     GetFullPathNameW

//Long Files will not work on these till redstone
#define WszGetTempPath         GetTempPathWrapper

//APIS which have a buffer as an out parameter
#define WszGetEnvironmentVariable GetEnvironmentVariableWrapper
#define WszSearchPath          SearchPathWrapper
#define WszGetModuleFileName   GetModuleFileNameWrapper

//*****************************************************************************
// Prototypes for API's.
//*****************************************************************************

extern DWORD g_dwMaxDBCSCharByteSize;

void EnsureCharSetInfoInitialized();

inline DWORD GetMaxDBCSCharByteSize()
{
    // contract.h not visible here
    __annotation(W("WRAPPER ") W("GetMaxDBCSCharByteSize"));
#ifndef HOST_UNIX
    EnsureCharSetInfoInitialized();

    _ASSERTE(g_dwMaxDBCSCharByteSize != 0);
    return (g_dwMaxDBCSCharByteSize);
#else // HOST_UNIX
    return 3;
#endif // HOST_UNIX
}

#ifndef Wsz_mbstowcs
#define Wsz_mbstowcs(szOut, szIn, iSize) WszMultiByteToWideChar(CP_ACP, 0, szIn, -1, szOut, iSize)
#endif

#ifndef Wsz_wcstombs
#define Wsz_wcstombs(szOut, szIn, iSize) WszWideCharToMultiByte(CP_ACP, 0, szIn, -1, szOut, iSize, 0, 0)
#endif

// For all platforms:

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

#if defined(HOST_X86) && defined(_MSC_VER)

//
// Windows SDK does not use intrinsics on x86. Redefine the interlocked operations to use intrinsics.
//

#include "intrin.h"

#define InterlockedIncrement            _InterlockedIncrement
#define InterlockedDecrement            _InterlockedDecrement
#define InterlockedExchange             _InterlockedExchange
#define InterlockedCompareExchange      _InterlockedCompareExchange
#define InterlockedExchangeAdd          _InterlockedExchangeAdd
#define InterlockedCompareExchange64    _InterlockedCompareExchange64
#define InterlockedAnd                  _InterlockedAnd
#define InterlockedOr                   _InterlockedOr

//
// There is no _InterlockedCompareExchangePointer intrinsic in VC++ for x86.
// winbase.h #defines InterlockedCompareExchangePointer as __InlineInterlockedCompareExchangePointer,
// which calls the Win32 InterlockedCompareExchange, not the intrinsic _InterlockedCompareExchange.
// We want the intrinsic, so we #undef the Windows version of this API, and define our own.
//
#ifdef InterlockedCompareExchangePointer
#undef InterlockedCompareExchangePointer
#endif

FORCEINLINE
PVOID
InterlockedCompareExchangePointer (
    __inout  PVOID volatile *Destination,
    _In_opt_ PVOID ExChange,
    _In_opt_ PVOID Comperand
    )
{
    return((PVOID)(LONG_PTR)_InterlockedCompareExchange((LONG volatile *)Destination, (LONG)(LONG_PTR)ExChange, (LONG)(LONG_PTR)Comperand));
}

#endif // HOST_X86 && _MSC_VER

#if defined(HOST_X86) & !defined(InterlockedIncrement64)

// Interlockedxxx64 that do not have intrinsics are only supported on Windows Server 2003
// or higher for X86 so define our own portable implementation

#undef InterlockedIncrement64
#define InterlockedIncrement64          __InterlockedIncrement64
#undef InterlockedDecrement64
#define InterlockedDecrement64          __InterlockedDecrement64
#undef InterlockedExchange64
#define InterlockedExchange64           __InterlockedExchange64
#undef InterlockedExchangeAdd64
#define InterlockedExchangeAdd64        __InterlockedExchangeAdd64

__forceinline LONGLONG __InterlockedIncrement64(LONGLONG volatile *Addend)
{
    LONGLONG Old;

    do {
        Old = *Addend;
    } while (InterlockedCompareExchange64(Addend,
                                          Old + 1,
                                          Old) != Old);

    return Old + 1;
}

__forceinline LONGLONG __InterlockedDecrement64(LONGLONG volatile *Addend)
{
    LONGLONG Old;

    do {
        Old = *Addend;
    } while (InterlockedCompareExchange64(Addend,
                                          Old - 1,
                                          Old) != Old);

    return Old - 1;
}

__forceinline LONGLONG __InterlockedExchange64(LONGLONG volatile * Target, LONGLONG Value)
{
    LONGLONG Old;

    do {
        Old = *Target;
    } while (InterlockedCompareExchange64(Target,
                                          Value,
                                          Old) != Old);

    return Old;
}

__forceinline LONGLONG __InterlockedExchangeAdd64(LONGLONG volatile * Addend, LONGLONG Value)
{
    LONGLONG Old;

    do {
        Old = *Addend;
    } while (InterlockedCompareExchange64(Addend,
                                          Old + Value,
                                          Old) != Old);

    return Old;
}

#endif // HOST_X86

#endif  // __WIN_WRAP_H__
