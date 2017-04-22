// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "createdump.h"

const char* g_help = "createdump [options] pid\n" 
"-f, --name - dump path and file name. The pid can be placed in the name with %d. The default is '/tmp/coredump.%d'\n"
"-n, --normal - create minidump (default).\n"
"-h, --withheap - create minidump with heap.\n" 
"-m, --micro - create triage minidump.\n" 
"-d, --diag - enable diagnostic messages.\n";

bool g_diagnostics = false;

//
// Create a minidump using the DAC's enum memory regions interface
//
static bool 
CreateDump(const char* programPath, const char* dumpPathTemplate, pid_t pid, MINIDUMP_TYPE minidumpType)
{
    DataTarget* dataTarget = new DataTarget(pid);
    CrashInfo* crashInfo = new CrashInfo(pid, *dataTarget);
    DumpWriter* dumpWriter = new DumpWriter(*dataTarget, *crashInfo);
    ArrayHolder<char> dumpPath = new char[MAX_LONGPATH];
    const char* dumpType = "minidump";
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
    if (!crashInfo->GatherCrashInfo(programPath, minidumpType))
    {
        goto exit;
    }
    snprintf(dumpPath, MAX_LONGPATH, dumpPathTemplate, pid);
    if (!dumpWriter->OpenDump(dumpPath))
    {
        goto exit;
    }
    switch (minidumpType)
    {
        case MiniDumpWithPrivateReadWriteMemory:
            dumpType = "minidump with heap";
            break;

        case MiniDumpFilterTriage:
            dumpType = "triage minidump";
            break;

        default:
            break;
    }
    printf("Writing %s to file %s\n", dumpType, (char*)dumpPath);
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
    MINIDUMP_TYPE minidumpType = MiniDumpWithPrivateReadWriteMemory;
    const char* dumpPathTemplate = "/tmp/coredump.%d";
    const char* programPath = nullptr;
    pid_t pid = 0;

    int exitCode = PAL_InitializeDLL();
    if (exitCode != 0)
    {
        fprintf(stderr, "PAL initialization FAILED %d\n", exitCode);
        return exitCode;
    }
    programPath = *argv;
    argv++;

    for (int i = 1; i < argc; i++)
    {
        if (*argv != nullptr)
        {
            if ((strcmp(*argv, "-f") == 0) || (strcmp(*argv, "--name") == 0))
            {
                dumpPathTemplate = *++argv;
            }
            else if ((strcmp(*argv, "-n") == 0) || (strcmp(*argv, "--normal") == 0))
            {
                minidumpType = MiniDumpNormal;
            }
            else if ((strcmp(*argv, "-h") == 0) || (strcmp(*argv, "--withheap") == 0))
            {
                minidumpType = MiniDumpWithPrivateReadWriteMemory;
            }
            else if ((strcmp(*argv, "-m") == 0) || (strcmp(*argv, "--micro") == 0))
            {
                minidumpType = MiniDumpFilterTriage;
            }
            else if ((strcmp(*argv, "-d") == 0) || (strcmp(*argv, "--diag") == 0))
            {
                g_diagnostics = true;
            }
            else {
                pid = atoll(*argv);
            }
            argv++;
        }
    }
    // if no pid or invalid command line option
    if (pid == 0)
    {
        fprintf(stderr, "%s", g_help);
        exitCode = -1;
    }
    else if (!CreateDump(programPath, dumpPathTemplate, pid, minidumpType)) 
    {
        exitCode = -1;
    }
    PAL_TerminateEx(exitCode);
    return exitCode;
}
