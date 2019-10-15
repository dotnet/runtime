// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "createdump.h"

const char* g_help = "createdump [options] pid\n" 
"-f, --name - dump path and file name. The pid can be placed in the name with %d. The default is '/tmp/coredump.%d'\n"
"-n, --normal - create minidump.\n"
"-h, --withheap - create minidump with heap (default).\n" 
"-t, --triage - create triage minidump.\n" 
"-u, --full - create full core dump.\n" 
"-d, --diag - enable diagnostic messages.\n";

bool CreateDumpCommon(const char* dumpPathTemplate, MINIDUMP_TYPE minidumpType, CrashInfo* crashInfo);

//
// Main entry point
//
int __cdecl main(const int argc, const char* argv[])
{
    MINIDUMP_TYPE minidumpType = MiniDumpWithPrivateReadWriteMemory;
    const char* dumpPathTemplate = nullptr;
    pid_t pid = 0;

    int exitCode = PAL_InitializeDLL();
    if (exitCode != 0)
    {
        fprintf(stderr, "PAL initialization FAILED %d\n", exitCode);
        return exitCode;
    }


    // Parse the command line options and target pid
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
            else if ((strcmp(*argv, "-t") == 0) || (strcmp(*argv, "--triage") == 0))
            {
                minidumpType = MiniDumpFilterTriage;
            }
            else if ((strcmp(*argv, "-u") == 0) || (strcmp(*argv, "--full") == 0))
            {
                minidumpType = MiniDumpWithFullMemory;
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

    if (pid != 0)
    { 
        if (dumpPathTemplate == nullptr)
        {
            char tmpPath[MAX_LONGPATH];
            if (::GetTempPathA(MAX_LONGPATH, tmpPath) == 0)
            {
                fprintf(stderr, "GetTempPath failed (0x%08x)", ::GetLastError());
                return ::GetLastError();
            }
            exitCode = strcat_s(tmpPath, MAX_LONGPATH, "coredump.%d");
            if (exitCode != 0)
            {
                fprintf(stderr, "strcat_s failed (%d)", exitCode);
                return exitCode;
            }
            dumpPathTemplate = tmpPath;
        }
        ReleaseHolder<DumpDataTarget> dataTarget = new DumpDataTarget(pid);
        ReleaseHolder<CrashInfo> crashInfo = new CrashInfo(pid, dataTarget, false);

        // The initialize the data target's ReadVirtual support (opens /proc/$pid/mem)
        if (dataTarget->Initialize(crashInfo))
        {
            if (!CreateDumpCommon(dumpPathTemplate, minidumpType, crashInfo))
            {
                exitCode = -1;
            }
        }
        else 
        {
            exitCode = -1;
        }
    }
    else
    {
        // if no pid or invalid command line option
        fprintf(stderr, "%s", g_help);
        exitCode = -1;
    }
    PAL_TerminateEx(exitCode);
    return exitCode;
}
