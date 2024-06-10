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
"-d, --diag - enable diagnostic messages.\n"
"-v, --verbose - enable verbose diagnostic messages.\n"
#ifdef HOST_UNIX
"--crashreport - write crash report file.\n"
"--crashreportonly - write crash report file only (no dump).\n"
"--crashthread <id> - the thread id of the crashing thread.\n"
"--signal <code> - the signal code of the crash.\n"
#endif
;

bool g_diagnostics = false;
bool g_diagnosticsVerbose = false;

//
// Main entry point
//
int __cdecl main(const int argc, const char* argv[])
{
    CreateDumpOptions options;
    options.MinidumpType = (MINIDUMP_TYPE)(MiniDumpWithPrivateReadWriteMemory |
                                           MiniDumpWithDataSegs |
                                           MiniDumpWithHandleData |
                                           MiniDumpWithUnloadedModules |
                                           MiniDumpWithFullMemoryInfo |
                                           MiniDumpWithThreadInfo |
                                           MiniDumpWithTokenInformation);
    options.DumpType = "minidump with heap";
    options.DumpPathTemplate = nullptr;
    options.CrashReport = false;
    options.CreateDump = true;
    options.Signal = 0;
    options.CrashThread = 0;
    options.Pid = 0;
#if defined(HOST_UNIX)
    options.SignalCode = 0;
    options.SignalErrno = 0;
    options.SignalAddress = nullptr;
#endif
    int exitCode = 0;

#ifdef HOST_UNIX
    exitCode = PAL_InitializeDLL();
    if (exitCode != 0)
    {
        printf_error("PAL initialization FAILED %d\n", exitCode);
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
                options.DumpPathTemplate = *++argv;
            }
            else if ((strcmp(*argv, "-n") == 0) || (strcmp(*argv, "--normal") == 0))
            {
                options.DumpType = "minidump";
                options.MinidumpType = (MINIDUMP_TYPE)(MiniDumpNormal |
                                                       MiniDumpWithDataSegs |
                                                       MiniDumpWithHandleData |
                                                       MiniDumpWithThreadInfo);
            }
            else if ((strcmp(*argv, "-h") == 0) || (strcmp(*argv, "--withheap") == 0))
            {
                options.DumpType = "minidump with heap";
                options.MinidumpType = (MINIDUMP_TYPE)(MiniDumpWithPrivateReadWriteMemory |
                                                       MiniDumpWithDataSegs |
                                                       MiniDumpWithHandleData |
                                                       MiniDumpWithUnloadedModules |
                                                       MiniDumpWithFullMemoryInfo |
                                                       MiniDumpWithThreadInfo |
                                                       MiniDumpWithTokenInformation);
            }
            else if ((strcmp(*argv, "-t") == 0) || (strcmp(*argv, "--triage") == 0))
            {
                options.DumpType = "triage minidump";
                options.MinidumpType = (MINIDUMP_TYPE)(MiniDumpFilterTriage |
                                                       MiniDumpIgnoreInaccessibleMemory |
                                                       MiniDumpWithoutOptionalData |
                                                       MiniDumpWithProcessThreadData |
                                                       MiniDumpFilterModulePaths |
                                                       MiniDumpWithUnloadedModules |
                                                       MiniDumpFilterMemory |
                                                       MiniDumpWithHandleData);
            }
            else if ((strcmp(*argv, "-u") == 0) || (strcmp(*argv, "--full") == 0))
            {
                options.DumpType = "full dump";
                options.MinidumpType = (MINIDUMP_TYPE)(MiniDumpWithFullMemory |
                                                       MiniDumpWithDataSegs |
                                                       MiniDumpWithHandleData |
                                                       MiniDumpWithUnloadedModules |
                                                       MiniDumpWithFullMemoryInfo |
                                                       MiniDumpWithThreadInfo |
                                                       MiniDumpWithTokenInformation);
            }
#ifdef HOST_UNIX
            else if (strcmp(*argv, "--crashreport") == 0)
            {
                options.CrashReport = true;
            }
            else if (strcmp(*argv, "--crashreportonly") == 0)
            {
                options.CrashReport = true;
                options.CreateDump = false;
            }
            else if (strcmp(*argv, "--crashthread") == 0)
            {
                options.CrashThread = atoi(*++argv);
            }
            else if (strcmp(*argv, "--signal") == 0)
            {
                options.Signal = atoi(*++argv);
            }
            else if (strcmp(*argv, "--code") == 0)
            {
                options.SignalCode = atoi(*++argv);
            }
            else if (strcmp(*argv, "--errno") == 0)
            {
                options.SignalErrno = atoi(*++argv);
            }
            else if (strcmp(*argv, "--address") == 0)
            {
                options.SignalAddress = (void*)atoll(*++argv);
            }
#endif
            else if ((strcmp(*argv, "-d") == 0) || (strcmp(*argv, "--diag") == 0))
            {
                g_diagnostics = true;
            }
            else if ((strcmp(*argv, "-v") == 0) || (strcmp(*argv, "--verbose") == 0))
            {
                g_diagnostics = true;
                g_diagnosticsVerbose = true;
            }
            else {
                options.Pid = atoi(*argv);
            }
            argv++;
        }
    }

    if (options.Pid != 0)
    {
        ArrayHolder<char> tmpPath = new char[MAX_LONGPATH];

        if (options.DumpPathTemplate == nullptr)
        {
            if (::GetTempPathWrapper(MAX_LONGPATH, tmpPath) == 0)
            {
                printf_error("GetTempPath failed (0x%08x)\n", ::GetLastError());
                return ::GetLastError();
            }
            exitCode = strcat_s(tmpPath, MAX_LONGPATH, DEFAULT_DUMP_TEMPLATE);
            if (exitCode != 0)
            {
                printf_error("strcat_s failed (%d)\n", exitCode);
                return exitCode;
            }
            options.DumpPathTemplate = tmpPath;
        }

        if (CreateDump(options))
        {
            printf_status("Dump successfully written\n");
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
        printf_error("%s", g_help);
        exitCode = -1;
    }
#ifdef HOST_UNIX
    PAL_TerminateEx(exitCode);
#endif
    return exitCode;
}

void
printf_status(const char* format, ...)
{
    va_list args;
    va_start(args, format);
    fprintf(stdout, "[createdump] ");
    vfprintf(stdout, format, args);
    fflush(stdout);
    va_end(args);
}

void
printf_error(const char* format, ...)
{
    va_list args;
    va_start(args, format);
    fprintf(stderr, "[createdump] ");
    vfprintf(stderr, format, args);
    fflush(stderr);
    va_end(args);
}

void
trace_printf(const char* format, ...)
{
    if (g_diagnostics)
    {
        va_list args;
        va_start(args, format);
        fprintf(stdout, "[createdump] ");
        vfprintf(stdout, format, args);
        fflush(stdout);
        va_end(args);
    }
}

void
trace_verbose_printf(const char* format, ...)
{
    if (g_diagnosticsVerbose)
    {
        va_list args;
        va_start(args, format);
        fprintf(stdout, "[createdump] ");
        vfprintf(stdout, format, args);
        fflush(stdout);
        va_end(args);
    }
}
