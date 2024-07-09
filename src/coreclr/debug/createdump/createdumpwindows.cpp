// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"
#include "psapi.h"

//
// The Windows create dump code
//
bool
CreateDump(const CreateDumpOptions& options)
{
    HANDLE hFile = INVALID_HANDLE_VALUE;
    HANDLE hProcess = NULL;
    bool result = false;

    ArrayHolder<char> pszName = new char[MAX_LONGPATH + 1];
    std::string dumpPath;

    hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, FALSE, options.Pid);
    if (hProcess == NULL)
    {
        printf_error("Invalid process id '%d' error %d\n", options.Pid, GetLastError());
        goto exit;
    }
    if (GetModuleBaseNameA(hProcess, NULL, pszName, MAX_LONGPATH) <= 0)
    {
        printf_error("Get process name FAILED %d\n", GetLastError());
        goto exit;
    }
    if (!FormatDumpName(dumpPath, options.DumpPathTemplate, pszName, options.Pid))
    {
        goto exit;
    }
    printf_status("Writing %s to file %s\n", options.DumpType, dumpPath.c_str());

    hFile = CreateFileA(dumpPath.c_str(), GENERIC_READ | GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    if (hFile == INVALID_HANDLE_VALUE)
    {
        printf_error("Invalid dump path '%s' error %d\n", dumpPath.c_str(), GetLastError());
        goto exit;
    }

    // Retry the write dump on ERROR_PARTIAL_COPY
    for (int i = 0; i < 5; i++)
    {
        if (MiniDumpWriteDump(hProcess, options.Pid, hFile, options.MinidumpType, NULL, NULL, NULL))
        {
            result = true;
            break;
        }
        else
        {
            int err = GetLastError();
            if (err != HRESULT_FROM_WIN32(ERROR_PARTIAL_COPY))
            {
                printf_error("Write dump FAILED 0x%08x\n", err);
                break;
            }
        }
    }

exit:
    if (hProcess != NULL)
    {
        CloseHandle(hProcess);
    }

    if (hFile != INVALID_HANDLE_VALUE)
    {
        CloseHandle(hFile);
    }

    return result;
}

typedef DWORD(WINAPI *pfnGetTempPathA)(DWORD nBufferLength, LPSTR  lpBuffer);

static volatile pfnGetTempPathA
g_pfnGetTempPathA = nullptr;


DWORD
GetTempPathWrapper(
    IN DWORD nBufferLength,
    OUT LPSTR lpBuffer)
{
    if (g_pfnGetTempPathA == nullptr)
    {
        HMODULE hKernel32 = LoadLibraryExW(L"kernel32.dll", NULL, LOAD_LIBRARY_SEARCH_SYSTEM32);

        pfnGetTempPathA pLocalGetTempPathA = NULL;
        if (hKernel32 != NULL)
        {
            // store to thread local variable to prevent data race
            pLocalGetTempPathA = (pfnGetTempPathA)::GetProcAddress(hKernel32, "GetTempPath2A");
        }

        if (pLocalGetTempPathA == NULL) // method is only available with Windows 10 Creators Update or later
        {
            g_pfnGetTempPathA = &GetTempPathA;
        }
        else
        {
            g_pfnGetTempPathA = pLocalGetTempPathA;
        }
    }

    return g_pfnGetTempPathA(nBufferLength, lpBuffer);
}