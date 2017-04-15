// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "createdump.h"

bool g_diagnostics = true;

//
// Create a minidump using the DAC's enum memory regions interface
//
static bool 
CreateDump(const char* pszExePath, const char* dumpPathTemplate, pid_t pid, MINIDUMP_TYPE minidumpType)
{
    DataTarget* dataTarget = new DataTarget(pid);
    CrashInfo* crashInfo = new CrashInfo(pid, *dataTarget);
    DumpWriter* dumpWriter = new DumpWriter(*dataTarget, *crashInfo);
    ArrayHolder<char> dumpPath = new char[MAX_LONGPATH];
    bool result = false;

    // Suspend all the threads in the target process and build the list of threads
    if (!crashInfo->EnumerateAndSuspendThreads())
    {
        goto exit;
    }
    // The initialize the data target's ReadVirtual support (opens /proc/$pid/mem)
    if (!dataTarget->Initialize(crashInfo))
    {
        goto exit;
    }
    // Gather all the info about the process, threads (registers, etc.) and memory regions
    if (!crashInfo->GatherCrashInfo(pszExePath, minidumpType))
    {
        goto exit;
    }
    snprintf(dumpPath, MAX_LONGPATH, dumpPathTemplate, pid);
    if (!dumpWriter->OpenDump(dumpPath))
    {
        goto exit;
    }
    if (!dumpWriter->WriteDump())
    {
        goto exit;
    }
    result = true;
exit:
    dumpWriter->Release();
    crashInfo->ResumeThreads();
    crashInfo->Release();
    dataTarget->Release();
    return result;
}

//
// main entry point
//
int __cdecl main(const int argc, const char* argv[])
{
    const char* dumpPathTemplate = "/tmp/coredump.%d";

    char* diagnostics = getenv("COMPlus_CreateDumpDiagnostics");
    g_diagnostics = diagnostics != nullptr && strcmp(diagnostics, "1") == 0;

    int exitCode = PAL_InitializeDLL();
    if (exitCode != 0)
    {
        fprintf(stderr, "PAL_Initialize FAILED %d\n", exitCode);
        return exitCode;
    }
    pid_t pid;
    if (argc < 2)
    {
        fprintf(stderr, "Not enough arguments\n");
        exitCode = -1;
        goto exit;
    }
    pid = _atoi64(argv[1]);

    if (!CreateDump(argv[0], dumpPathTemplate, pid, MiniDumpNormal)) 
    {
        exitCode = -1;
        goto exit;
    }
exit:
    PAL_TerminateEx(exitCode);
    return exitCode;
}
