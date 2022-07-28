// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"
#include <psapi.h>

// The Windows SDK (winternl.h) we use doesn't have the necessary field (InheritedFromUniqueProcessId)
typedef struct _PROCESS_BASIC_INFORMATION_ {
    NTSTATUS ExitStatus;
    PPEB PebBaseAddress;
    ULONG_PTR AffinityMask;
    KPRIORITY BasePriority;
    ULONG_PTR UniqueProcessId;
    ULONG_PTR InheritedFromUniqueProcessId;
} PROCESS_BASIC_INFORMATION_;

//
// The Windows create dump code
//
bool
CreateDump(const char* dumpPathTemplate, int pid, const char* dumpType, MINIDUMP_TYPE minidumpType, bool crashReport, int crashThread, int signal)
{
    HANDLE hFile = INVALID_HANDLE_VALUE;
    HANDLE hProcess = NULL;
    bool result = false;

    ArrayHolder<char> pszName = new char[MAX_LONGPATH + 1];
    std::string dumpPath;

    // On Windows, createdump is restricted for security reasons to only the .NET process (parent process) that launched createdump
    PROCESS_BASIC_INFORMATION_ processInformation;
    NTSTATUS status = NtQueryInformationProcess(GetCurrentProcess(), PROCESSINFOCLASS::ProcessBasicInformation, &processInformation, sizeof(processInformation), NULL);
    if (status != 0)
    {
        printf_error("Failed to get parent process id status %d\n", status);
        goto exit;
    }
    pid = (int)processInformation.InheritedFromUniqueProcessId;

    hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, FALSE, pid);
    if (hProcess == NULL)
    {
        printf_error("Invalid process id '%d' - %s\n", pid, GetLastErrorString().c_str());
        goto exit;
    }
    if (GetModuleBaseNameA(hProcess, NULL, pszName, MAX_LONGPATH) <= 0)
    {
        printf_error("Get process name FAILED - %s\n", GetLastErrorString().c_str());
        goto exit;
    }
    if (!FormatDumpName(dumpPath, dumpPathTemplate, pszName, pid))
    {
        goto exit;
    }
    printf_status("Writing %s for process %d to file %s\n", dumpType, pid, dumpPath.c_str());

    hFile = CreateFileA(dumpPath.c_str(), GENERIC_READ | GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    if (hFile == INVALID_HANDLE_VALUE)
    {
        printf_error("Invalid dump path '%s' - %s\n", dumpPath.c_str(), GetLastErrorString().c_str());
        goto exit;
    }

    // Retry the write dump on ERROR_PARTIAL_COPY
    for (int i = 0; i < 5; i++)
    {
        if (MiniDumpWriteDump(hProcess, pid, hFile, minidumpType, NULL, NULL, NULL))
        {
            result = true;
            break;
        }
        else
        {
            int err = GetLastError();
            if (err != ERROR_PARTIAL_COPY)
            {
                printf_error("MiniDumpWriteDump - %s\n", GetLastErrorString().c_str());
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
