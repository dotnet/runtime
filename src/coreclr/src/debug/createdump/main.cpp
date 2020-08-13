// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"

#ifdef HOST_WINDOWS
#define DEFAULT_DUMP_PATH "%TEMP%\\"
#define DEFAULT_DUMP_TEMPLATE "dump.%p.dmp"
#else
#define DEFAULT_DUMP_PATH "/tmp/"
#define DEFAULT_DUMP_TEMPLATE "coredump.%p"
#endif

const char* g_help = "createdump [options] pid\n"
"-f, --name - dump path and file name. The default is '" DEFAULT_DUMP_PATH DEFAULT_DUMP_TEMPLATE "'. These specifiers are substituted with following values:\n"
"   %p  PID of dumped process.\n"
"   %e  The process executable filename.\n"
"   %h  Hostname return by gethostname().\n"
"   %t  Time of dump, expressed as seconds since the Epoch, 1970-01-01 00:00:00 +0000 (UTC).\n"
"-n, --normal - create minidump.\n"
"-h, --withheap - create minidump with heap (default).\n"
"-t, --triage - create triage minidump.\n"
"-u, --full - create full core dump.\n"
"-d, --diag - enable diagnostic messages.\n";

bool g_diagnostics = false;

//
// Main entry point
//
int __cdecl main(const int argc, const char* argv[])
{
    MINIDUMP_TYPE minidumpType = (MINIDUMP_TYPE)(MiniDumpWithPrivateReadWriteMemory |
                                                 MiniDumpWithDataSegs |
                                                 MiniDumpWithHandleData |
                                                 MiniDumpWithUnloadedModules |
                                                 MiniDumpWithFullMemoryInfo |
                                                 MiniDumpWithThreadInfo |
                                                 MiniDumpWithTokenInformation);
    const char* dumpType = "minidump with heap";
    const char* dumpPathTemplate = nullptr;
    int exitCode = 0;
    int pid = 0;

#ifdef __APPLE__
    char* enabled = getenv("COMPlus_DbgEnableElfDumpOnMacOS");
    if (enabled == nullptr || strcmp(enabled, "1") != 0)
    {
        fprintf(stderr, "MachO coredumps are not supported. To enable ELF coredumps on MacOS, set the COMPlus_DbgEnableElfDumpOnMacOS environment variable to 1.\n");
        return -1;
    }
#endif

#ifdef HOST_UNIX
    exitCode = PAL_InitializeDLL();
    if (exitCode != 0)
    {
        fprintf(stderr, "PAL initialization FAILED %d\n", exitCode);
        return exitCode;
    }
#endif

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
                dumpType = "minidump";
                minidumpType = (MINIDUMP_TYPE)(MiniDumpNormal |
                                               MiniDumpWithThreadInfo);
            }
            else if ((strcmp(*argv, "-h") == 0) || (strcmp(*argv, "--withheap") == 0))
            {
                dumpType = "minidump with heap";
                minidumpType = (MINIDUMP_TYPE)(MiniDumpWithPrivateReadWriteMemory |
                                               MiniDumpWithDataSegs |
                                               MiniDumpWithHandleData |
                                               MiniDumpWithUnloadedModules |
                                               MiniDumpWithFullMemoryInfo |
                                               MiniDumpWithThreadInfo |
                                               MiniDumpWithTokenInformation);
            }
            else if ((strcmp(*argv, "-t") == 0) || (strcmp(*argv, "--triage") == 0))
            {
                dumpType = "triage minidump";
                minidumpType = (MINIDUMP_TYPE)(MiniDumpFilterTriage |
                                               MiniDumpWithThreadInfo);
            }
            else if ((strcmp(*argv, "-u") == 0) || (strcmp(*argv, "--full") == 0))
            {
                dumpType = "full dump";
                minidumpType = (MINIDUMP_TYPE)(MiniDumpWithFullMemory |
                                               MiniDumpWithDataSegs |
                                               MiniDumpWithHandleData |
                                               MiniDumpWithUnloadedModules |
                                               MiniDumpWithFullMemoryInfo |
                                               MiniDumpWithThreadInfo |
                                               MiniDumpWithTokenInformation);
            }
            else if ((strcmp(*argv, "-d") == 0) || (strcmp(*argv, "--diag") == 0))
            {
                g_diagnostics = true;
            }
            else {
                pid = atoi(*argv);
            }
            argv++;
        }
    }

    if (pid != 0)
    {
        ArrayHolder<char> tmpPath = new char[MAX_LONGPATH];

        if (dumpPathTemplate == nullptr)
        {
            if (::GetTempPathA(MAX_LONGPATH, tmpPath) == 0)
            {
                fprintf(stderr, "GetTempPath failed (0x%08x)", ::GetLastError());
                return ::GetLastError();
            }
            exitCode = strcat_s(tmpPath, MAX_LONGPATH, DEFAULT_DUMP_TEMPLATE);
            if (exitCode != 0)
            {
                fprintf(stderr, "strcat_s failed (%d)", exitCode);
                return exitCode;
            }
            dumpPathTemplate = tmpPath;
        }

        if (CreateDump(dumpPathTemplate, pid, dumpType, minidumpType))
        {
            printf("Dump successfully written\n");
        }
        else
        {
            exitCode = -1;
        }

        fflush(stdout);
        fflush(stderr);
    }
    else
    {
        // if no pid or invalid command line option
        fprintf(stderr, "%s", g_help);
        exitCode = -1;
    }
#ifdef HOST_UNIX
    PAL_TerminateEx(exitCode);
#endif
    return exitCode;
}

void
trace_printf(const char* format, ...)
{
    if (g_diagnostics)
    { 
        va_list args;
        va_start(args, format);
        vfprintf(stdout, format, args);
        fflush(stdout);
        va_end(args);
    }
}

