// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"

#if defined(__arm__) || defined(__aarch64__) || defined(__loongarch64)
long g_pageSize = 0;
#endif

//
// The Linux/MacOS create dump code
//
bool
CreateDump(const char* dumpPathTemplate, int pid, const char* dumpType, MINIDUMP_TYPE minidumpType, bool createDump, bool crashReport, int crashThread, int signal)
{
    ReleaseHolder<CrashInfo> crashInfo = new CrashInfo(pid, crashReport, crashThread, signal);
    DumpWriter dumpWriter(*crashInfo);
    std::string dumpPath;
    bool result = false;

    // Initialize PAGE_SIZE
#if defined(__arm__) || defined(__aarch64__) || defined(__loongarch64)
    g_pageSize = sysconf(_SC_PAGESIZE);
#endif
    TRACE("PAGE_SIZE %d\n", PAGE_SIZE);

    // Initialize the crash info 
    if (!crashInfo->Initialize())
    {
        goto exit;
    }
    printf_status("Gathering state for process %d %s\n", pid, crashInfo->Name().c_str());

    if (signal != 0 || crashThread != 0)
    {
        printf_status("Crashing thread %08x signal %08x\n", crashThread, signal);
    }

    // Suspend all the threads in the target process and build the list of threads
    if (!crashInfo->EnumerateAndSuspendThreads())
    {
        goto exit;
    }
    // Gather all the info about the process, threads (registers, etc.) and memory regions
    if (!crashInfo->GatherCrashInfo(minidumpType))
    {
        goto exit;
    }
    // Format the dump pattern template now that the process name on MacOS has been obtained
    if (!FormatDumpName(dumpPath, dumpPathTemplate, crashInfo->Name().c_str(), pid))
    {
        goto exit;
    }
    // Write the crash report json file if enabled
    if (crashReport)
    {
        CrashReportWriter crashReportWriter(*crashInfo);
        crashReportWriter.WriteCrashReport(dumpPath);
    }
    if (createDump)
    {
        // Gather all the useful memory regions from the DAC
        if (!crashInfo->EnumerateMemoryRegionsWithDAC(minidumpType))
        {
            goto exit;
        }
        // Join all adjacent memory regions
        crashInfo->CombineMemoryRegions();
    
        printf_status("Writing %s to file %s\n", dumpType, dumpPath.c_str());

        // Write the actual dump file
        if (!dumpWriter.OpenDump(dumpPath.c_str()))
        {
            goto exit;
        }
        if (!dumpWriter.WriteDump())
        {
            printf_error("Writing dump FAILED\n");

            // Delete the partial dump file on error
            remove(dumpPath.c_str());
            goto exit;
        }
    }
    result = true;
exit:
    if (kill(pid, 0) == 0)
    {
        printf_status("Target process is alive\n");
    }
    else
    {
        int err = errno;
        if (err == ESRCH)
        {
            printf_error("Target process terminated\n");
        }
        else
        {
            printf_error("kill(%d, 0) FAILED %s (%d)\n", pid, strerror(err), err);
        }
    }
    crashInfo->CleanupAndResumeProcess();
    return result;
}
