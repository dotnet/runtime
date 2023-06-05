// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"

#if defined(__arm__) || defined(__aarch64__) || defined(__loongarch64) || defined(__riscv)
long g_pageSize = 0;
#endif

//
// The Linux/MacOS create dump code
//
bool
CreateDump(const CreateDumpOptions& options)
{
    ReleaseHolder<CrashInfo> crashInfo = new CrashInfo(options);
    DumpWriter dumpWriter(*crashInfo);
    std::string dumpPath;
    bool result = false;

    // Initialize PAGE_SIZE
#if defined(__arm__) || defined(__aarch64__) || defined(__loongarch64) || defined(__riscv)
    g_pageSize = sysconf(_SC_PAGESIZE);
#endif
    TRACE("PAGE_SIZE %d\n", PAGE_SIZE);

    if (options.CrashReport && (options.AppModel == AppModelType::SingleFile || options.AppModel == AppModelType::NativeAOT))
    {
        printf_error("The app model does not support crash report generation\n");
        goto exit;
    }

    // Initialize the crash info 
    if (!crashInfo->Initialize())
    {
        goto exit;
    }
    printf_status("Gathering state for process %d %s\n", options.Pid, crashInfo->Name().c_str());

    if (options.Signal != 0 || options.CrashThread != 0)
    {
        printf_status("Crashing thread %04x signal %d (%04x)\n", options.CrashThread, options.Signal, options.Signal);
    }

    // Suspend all the threads in the target process and build the list of threads
    if (!crashInfo->EnumerateAndSuspendThreads())
    {
        goto exit;
    }
    // Gather all the info about the process, threads (registers, etc.) and memory regions
    if (!crashInfo->GatherCrashInfo(options.DumpType))
    {
        goto exit;
    }
    // Format the dump pattern template now that the process name on MacOS has been obtained
    if (!FormatDumpName(dumpPath, options.DumpPathTemplate, crashInfo->Name().c_str(), options.Pid))
    {
        goto exit;
    }
    // Write the crash report json file if enabled
    if (options.CrashReport)
    {
        CrashReportWriter crashReportWriter(*crashInfo);
        crashReportWriter.WriteCrashReport(dumpPath);
    }
    if (options.CreateDump)
    {
        // Gather all the useful memory regions from the DAC
        if (!crashInfo->EnumerateMemoryRegionsWithDAC(options.DumpType))
        {
            goto exit;
        }
        // Join all adjacent memory regions
        crashInfo->CombineMemoryRegions();
    
        printf_status("Writing %s to file %s\n", GetDumpTypeString(options.DumpType), dumpPath.c_str());

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
    if (kill(options.Pid, 0) == 0)
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
            printf_error("kill(%d, 0) FAILED %s (%d)\n", options.Pid, strerror(err), err);
        }
    }
    crashInfo->CleanupAndResumeProcess();
    return result;
}
