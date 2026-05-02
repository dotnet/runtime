// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//*****************************************************************************
// WinWrap.cpp
//
//*****************************************************************************

#include "stdafx.h"                     // Precompiled header key.
#include "winwrap.h"                    // Header for macros and functions.
#include "utilcode.h"
#include "holder.h"

#ifdef HOST_UNIX

// Command line splitting matching Win32 CreateProcessW semantics:
// 1) Whitespace splits arguments (per isspace())
// 2) Double quotes group text (whitespace inside quotes doesn't split)
// 3) \" is an escaped double quote (produces literal " in output)
// 4) Backslash followed by anything other than " is literal (kept as-is)
// 5) Bare double quotes are stripped from output
// Returns heap-allocated argv array (caller frees each element and the array).
static char** SplitCommandLine(const char* cmd, int* out_argc)
{
    int capacity = 8;
    int count = 0;
    char** argv = (char**)malloc(capacity * sizeof(char*));
    if (argv == NULL)
        return NULL;

    const char* p = cmd;
    while (*p != '\0')
    {
        // Skip whitespace
        while (*p != '\0' && isspace((unsigned char)*p))
            p++;
        if (*p == '\0')
            break;

        // Find the end of this argument (first pass: determine boundaries)
        const char* arg_start = p;
        bool in_quotes = false;
        while (*p != '\0')
        {
            if (!in_quotes && isspace((unsigned char)*p))
                break;

            if (*p == '"')
            {
                // Check for escaped quote: \"
                if (p > arg_start && *(p - 1) == '\\')
                {
                    // This is an escaped quote, not a real quote toggle
                    p++;
                    continue;
                }
                in_quotes = !in_quotes;
                p++;
            }
            else
            {
                p++;
            }
        }

        // Second pass: copy the argument, stripping bare quotes and handling \"
        size_t arg_len = (size_t)(p - arg_start);
        char* buf = (char*)malloc(arg_len + 1);
        if (buf == NULL)
        {
            for (int i = 0; i < count; i++) free(argv[i]);
            free(argv);
            return NULL;
        }

        size_t j = 0;
        const char* s = arg_start;
        while (s < p)
        {
            if (*s == '"')
            {
                // Skip bare double quotes (they're grouping characters)
                s++;
            }
            else if (*s == '\\' && (s + 1) < p && *(s + 1) == '"')
            {
                // Escaped double quote: \" -> produce literal "
                buf[j++] = '"';
                s += 2;
            }
            else
            {
                buf[j++] = *s++;
            }
        }
        buf[j] = '\0';

        if (count + 2 > capacity)
        {
            capacity *= 2;
            argv = (char**)realloc(argv, capacity * sizeof(char*));
        }
        argv[count++] = buf;
    }

    argv[count] = NULL;
    *out_argc = count;
    return argv;
}

#endif // HOST_UNIX

// The only purpose of this function is to make a local copy of lpCommandLine.
// Because windows implementation of CreateProcessW can actually change lpCommandLine,
// but we'd like to keep it const.
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
    )
{
#ifdef HOST_UNIX
    // Only caller on Unix is LaunchCreateDump (excep.cpp); other parameters are ignored.
    (void)lpApplicationName;
    (void)lpProcessAttributes;
    (void)lpThreadAttributes;
    (void)dwCreationFlags;
    (void)lpEnvironment;
    (void)lpCurrentDirectory;
    (void)lpStartupInfo;
    _ASSERTE(lpCurrentDirectory == NULL);
    _ASSERTE(lpProcessInformation != NULL);

    if (lpCommandLine == NULL)
    {
        SetLastError(ERROR_INVALID_PARAMETER);
        return FALSE;
    }

    int commandLineLength = WideCharToMultiByte(CP_UTF8, 0, lpCommandLine, -1, NULL, 0, NULL, NULL);
    if (commandLineLength == 0)
    {
        SetLastError(ERROR_INVALID_PARAMETER);
        return FALSE;
    }

    NewArrayHolder<char> commandLineUtf8 = new (nothrow) char[commandLineLength];
    if (commandLineUtf8 == NULL)
    {
        SetLastError(ERROR_OUTOFMEMORY);
        return FALSE;
    }

    if (WideCharToMultiByte(CP_UTF8, 0, lpCommandLine, -1, commandLineUtf8, commandLineLength, NULL, NULL) == 0)
    {
        SetLastError(ERROR_INVALID_PARAMETER);
        return FALSE;
    }

    pid_t pid = fork();
    if (pid < 0)
    {
        SetLastError(ERROR_INTERNAL_ERROR);
        return FALSE;
    }

    if (pid == 0)
    {
        int argc = 0;
        char** argv = SplitCommandLine(commandLineUtf8.GetValue(), &argc);
        if (argv == NULL || argc == 0)
        {
            _exit(127);
        }

        execvp(argv[0], argv);
        _exit(127);
    }

    HANDLE processHandle = OpenProcess(PROCESS_ALL_ACCESS, bInheritHandles, static_cast<DWORD>(pid));
    if (processHandle == NULL)
    {
        return FALSE;
    }

    lpProcessInformation->hProcess = processHandle;
    lpProcessInformation->hThread = NULL;
    lpProcessInformation->dwProcessId = static_cast<DWORD>(pid);
    lpProcessInformation->dwThreadId_PAL_Undefined = 0;

    return TRUE;
#else
    BOOL fResult;
    DWORD err;
    {
        size_t commandLineLength = u16_strlen(lpCommandLine) + 1;
        NewArrayHolder<WCHAR> nonConstCommandLine(new (nothrow) WCHAR[commandLineLength]);
        if (nonConstCommandLine == NULL)
        {
            SetLastError(ERROR_OUTOFMEMORY);
            return 0;
        }

        memcpy(nonConstCommandLine, lpCommandLine, commandLineLength * sizeof(WCHAR));

        fResult = CreateProcessW(lpApplicationName,
                                   nonConstCommandLine,
                                   lpProcessAttributes,
                                   lpThreadAttributes,
                                   bInheritHandles,
                                   dwCreationFlags,
                                   lpEnvironment,
                                   (LPWSTR)lpCurrentDirectory,
                                   lpStartupInfo,
                                   lpProcessInformation);

        // At the end of the current scope, the last error code will be overwritten by the destructor of
        // NewArrayHolder. So we save the error code here, and restore it after the end of the current scope.
        err = GetLastError();
    }

    SetLastError(err);
    return fResult;
#endif
}

#ifndef HOST_UNIX


#include "psapi.h"
#include "winnls.h"

//********** Globals. *********************************************************
static volatile ULONG g_dwMaxDBCSCharByteSize = 0;

// Detect Unicode support of the operating system, and initialize globals
DWORD GetMaxDBCSCharByteSize()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;

    if (g_dwMaxDBCSCharByteSize == 0)
    {
        DWORD dwMaxDBCSCharByteSize;

        CPINFO cpInfo;
        if (GetCPInfo(CP_ACP, &cpInfo))
            dwMaxDBCSCharByteSize = cpInfo.MaxCharSize;
        else
            dwMaxDBCSCharByteSize = 2;

        g_dwMaxDBCSCharByteSize = dwMaxDBCSCharByteSize;
    }

    return g_dwMaxDBCSCharByteSize;
}

typedef HRESULT(WINAPI *pfnSetThreadDescription)(HANDLE hThread, PCWSTR lpThreadDescription);
extern pfnSetThreadDescription g_pfnSetThreadDescription;

// Dummy method if windows version does not support it
HRESULT SetThreadDescriptionDummy(HANDLE hThread, PCWSTR lpThreadDescription)
{
    return NOERROR;
}

HRESULT WINAPI InitializeSetThreadDescription(HANDLE hThread, PCWSTR lpThreadDescription)
{
    HMODULE hKernel32 = WszLoadLibrary(W("kernel32.dll"));

    pfnSetThreadDescription pLocal = NULL;
    if (hKernel32 != NULL)
    {
        // store to thread local variable to prevent data race
        pLocal = (pfnSetThreadDescription)GetProcAddress(hKernel32, "SetThreadDescription");
    }

    if (pLocal == NULL) // method is only available with Windows 10 Creators Update or later
    {
        g_pfnSetThreadDescription = SetThreadDescriptionDummy;
    }
    else
    {
        g_pfnSetThreadDescription = pLocal;
    }

    return g_pfnSetThreadDescription(hThread, lpThreadDescription);
}

pfnSetThreadDescription g_pfnSetThreadDescription = &InitializeSetThreadDescription;

// Set unmanaged thread name which will show up in ETW and Debuggers which know how to read this data.
HRESULT SetThreadName(HANDLE hThread, PCWSTR lpThreadDescription)
{
    return g_pfnSetThreadDescription(hThread, lpThreadDescription);
}

#else //!HOST_UNIX

HRESULT SetThreadName(HANDLE hThread, PCWSTR lpThreadDescription)
{
    return SetThreadDescription(hThread, lpThreadDescription);
}

#endif //!HOST_UNIX
