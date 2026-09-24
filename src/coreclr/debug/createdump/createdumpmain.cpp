// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"
#include "minipal/time.h"

bool linkedCreateDump = false;
uint64_t g_ticksPerMS = 0;
uint64_t g_startTime = 0;

bool GetDefaultDumpPath(char* buffer, size_t bufferSize)
{
    if (GetTempPathWrapper(bufferSize, buffer) == 0)
    {
        return false;
    }
    int exitCode = strcat_s(buffer, bufferSize, DEFAULT_DUMP_TEMPLATE);
    if (exitCode != 0)
    {
        printf_error("strcat_s failed (%d)", exitCode);
        return false;
    }
    return true;
}

//
// Common entry point
//
int createdump_main(const int argc, const char* argv[])
{
#ifdef HOST_UNIX
    CLRConfigNoCache waitForAttach = CLRConfigNoCache::Get("CreateDumpWaitForAttach", /*noprefix*/ false, &getenv);
    DWORD value = 0;
    if (waitForAttach.IsSet() && waitForAttach.TryAsInteger(10, value) && value == 1)
    {
        fprintf(stderr, "[createdump] waiting for attach %u: ", getpid());
        fgetc(stdin);
    }
#endif

    CreateDumpOptions options;
    int exitCode = ParseCreateDumpOptions(argc, (char**)argv, &options);
    if (exitCode != 0)
    {
        return exitCode;
    }

    char defaultDumpPath[MAX_LONGPATH];
    if (options.DumpPathTemplate == NULL)
    {
        if (!GetDefaultDumpPath(defaultDumpPath, MAX_LONGPATH))
        {
            printf_error("Could not get default dump path\n");
            return -1;
        }
        options.DumpPathTemplate = defaultDumpPath;
    }

    g_ticksPerMS = minipal_hires_tick_frequency() / 1000UL;
    g_startTime = minipal_hires_ticks();
    TRACE("TickFrequency: %" PRIu64 " ticks per ms\n", g_ticksPerMS);

    if (CreateDump(options))
    {
        printf_status("Dump successfully written in %" PRIu64 "ms\n", (minipal_hires_ticks() - g_startTime) / g_ticksPerMS);
    }
    else
    {
        printf_error("Failure took %" PRIu64 "ms\n", (minipal_hires_ticks() - g_startTime) / g_ticksPerMS);
        exitCode = -1;
    }

    fflush(stderr);
    fflush(g_stdout);

    if (g_logfile != nullptr)
    {
        fflush(g_logfile);
        fclose(g_logfile);
    }
    return exitCode;
}

const char*
GetDumpTypeString(DumpType dumpType)
{
    switch (dumpType)
    {
        case DumpType::Mini:
            return "minidump";
        case DumpType::Heap:
            return "minidump with heap";
        case DumpType::Triage:
            return "triage minidump";
        case DumpType::Full:
            return "full dump";
        default:
            return "unknown";
    }
}

MINIDUMP_TYPE
GetMiniDumpType(DumpType dumpType)
{
    switch (dumpType)
    {
        case DumpType::Mini:
            return (MINIDUMP_TYPE)(MiniDumpNormal |
                                   MiniDumpWithDataSegs |
                                   MiniDumpWithHandleData |
                                   MiniDumpWithThreadInfo);
        case DumpType::Heap:
            return (MINIDUMP_TYPE)(MiniDumpWithPrivateReadWriteMemory |
                                   MiniDumpWithDataSegs |
                                   MiniDumpWithHandleData |
                                   MiniDumpWithUnloadedModules |
                                   MiniDumpWithFullMemoryInfo |
                                   MiniDumpWithThreadInfo |
                                   MiniDumpWithTokenInformation);
        case DumpType::Triage:
            return (MINIDUMP_TYPE)(MiniDumpFilterTriage |
                                   MiniDumpIgnoreInaccessibleMemory |
                                   MiniDumpWithoutOptionalData |
                                   MiniDumpWithProcessThreadData |
                                   MiniDumpFilterModulePaths |
                                   MiniDumpWithUnloadedModules |
                                   MiniDumpFilterMemory |
                                   MiniDumpWithHandleData);
        case DumpType::Full:
        default:
            return (MINIDUMP_TYPE)(MiniDumpWithFullMemory |
                                   MiniDumpWithDataSegs |
                                   MiniDumpWithHandleData |
                                   MiniDumpWithUnloadedModules |
                                   MiniDumpWithFullMemoryInfo |
                                   MiniDumpWithThreadInfo |
                                   MiniDumpWithTokenInformation);
    }
}

#ifdef HOST_UNIX

void
CrashInfo::Trace(const char* format, ...)
{
    if (g_diagnostics)
    {
        va_list args;
        va_start(args, format);
        trace_prefix(format, args);
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
        trace_prefix(format, args);
        va_end(args);
    }
}

void initialize_static_createdump()
{
    PAL_SetCreateDumpCallback(createdump_main);
}

#endif // HOST_UNIX
