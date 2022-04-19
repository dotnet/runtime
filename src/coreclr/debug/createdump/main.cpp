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
"-l, --logtofile - file path and name to log diagnostic messages.\n"
#ifdef HOST_UNIX
"--crashreport - write crash report file (dump file path + .crashreport.json).\n"
"--crashthread <id> - the thread id of the crashing thread.\n"
"--signal <code> - the signal code of the crash.\n"
"--singlefile - enable single-file app check.\n"
#endif
;

FILE *g_logfile = nullptr;
FILE *g_stdout = stdout;
FILE *g_stderr = stderr;
bool g_diagnostics = false;
bool g_diagnosticsVerbose = false;
#ifdef HOST_UNIX
bool g_checkForSingleFile = false;
uint64_t g_ticksPerMS = 0;
uint64_t GetTickFrequency();
#endif

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
    bool crashReport = false;
    int signal = 0;
    int crashThread = 0;
    int exitCode = 0;
    int pid = 0;

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
                dumpPathTemplate = *++argv;
            }
            else if ((strcmp(*argv, "-n") == 0) || (strcmp(*argv, "--normal") == 0))
            {
                dumpType = "minidump";
                minidumpType = (MINIDUMP_TYPE)(MiniDumpNormal |
                                               MiniDumpWithDataSegs |
                                               MiniDumpWithHandleData |
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
                dumpType = "full dump";
                minidumpType = (MINIDUMP_TYPE)(MiniDumpWithFullMemory |
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
                crashReport = true;
            }
            else if (strcmp(*argv, "--crashthread") == 0)
            {
                crashThread = atoi(*++argv);
            }
            else if (strcmp(*argv, "--signal") == 0)
            {
                signal = atoi(*++argv);
            }
            else if (strcmp(*argv, "--singlefile") == 0)
            {
                g_checkForSingleFile = true;
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
            else if ((strcmp(*argv, "-l") == 0) || (strcmp(*argv, "--logtofile") == 0))
            {
                const char* logFilePath = *++argv;
                g_logfile = fopen(logFilePath, "w");
                if (g_logfile == nullptr)
                {
                    printf_error("Can not create log file %s: %d %s\n", logFilePath, errno, strerror(errno));
                    return errno;
                }
                g_stdout = g_logfile;
                g_stderr = g_logfile;
            }
            else {
                pid = atoi(*argv);
            }
            argv++;
        }
    }

#ifdef HOST_UNIX
    g_ticksPerMS = GetTickFrequency() / 1000000UL;
    TRACE("TickFrequency: %d ticks per ms\n", g_ticksPerMS);
#endif

    if (pid != 0)
    {
        ArrayHolder<char> tmpPath = new char[MAX_LONGPATH];

        if (dumpPathTemplate == nullptr)
        {
            if (::GetTempPathA(MAX_LONGPATH, tmpPath) == 0)
            {
                printf_error("GetTempPath failed (0x%08x)", ::GetLastError());
                return ::GetLastError();
            }
            exitCode = strcat_s(tmpPath, MAX_LONGPATH, DEFAULT_DUMP_TEMPLATE);
            if (exitCode != 0)
            {
                printf_error("strcat_s failed (%d)", exitCode);
                return exitCode;
            }
            dumpPathTemplate = tmpPath;
        }

        if (CreateDump(dumpPathTemplate, pid, dumpType, minidumpType, crashReport, crashThread, signal))
        {
            printf_status("Dump successfully written\n");
        }
        else
        {
            exitCode = -1;
        }

        fflush(g_stdout);
        fflush(g_stderr);

        if (g_logfile != nullptr)
        {
            fclose(g_logfile);
        }
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
    if (g_logfile == nullptr)
    {
        fprintf(g_stdout, "[createdump] ");
    }
    vfprintf(g_stdout, format, args);
    fflush(g_stdout);
    va_end(args);
}

void
printf_error(const char* format, ...)
{
    va_list args;
    va_start(args, format);
    if (g_logfile == nullptr)
    {
        fprintf(g_stderr, "[createdump] ");
    }
    vfprintf(g_stderr, format, args);
    fflush(g_stderr);
    va_end(args);
}

#ifdef HOST_UNIX

uint64_t
GetTickFrequency()
{
    LARGE_INTEGER ret;
    ZeroMemory(&ret, sizeof(LARGE_INTEGER));
    QueryPerformanceFrequency(&ret);
    return ret.QuadPart;
}

static uint64_t
GetTimeStamp()
{
    LARGE_INTEGER ret;
    ZeroMemory(&ret, sizeof(LARGE_INTEGER));
    QueryPerformanceCounter(&ret);
    return ret.QuadPart / g_ticksPerMS;
}

static void
trace_prefix()
{
    // Only add this prefix if logging to the console
    if (g_logfile == nullptr)
    {
        fprintf(g_stdout, "[createdump] ");
    }
    fprintf(g_stdout, "%08" PRIx64 " ", GetTimeStamp());
}

void
trace_printf(const char* format, ...)
{
    if (g_diagnostics)
    {
        va_list args;
        va_start(args, format);
        trace_prefix();
        vfprintf(g_stdout, format, args);
        fflush(g_stdout);
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
        trace_prefix();
        vfprintf(g_stdout, format, args);
        fflush(g_stdout);
        va_end(args);
    }
}

void
CrashInfo::Trace(const char* format, ...)
{
    if (g_diagnostics)
    {
        va_list args;
        va_start(args, format);
        trace_prefix();
        vfprintf(g_stdout, format, args);
        fflush(g_stdout);
        va_end(args);
    }
}

void
CrashInfo::TraceVerbose(const char* format, ...)
{
    if (g_diagnosticsVerbose)
    {
        va_list args;
        va_start(args, format);
        trace_prefix();
        vfprintf(g_stdout, format, args);
        fflush(g_stdout);
        va_end(args);
    }
}

#endif // HOST_UNIX
