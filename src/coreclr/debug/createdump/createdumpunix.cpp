// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"
#include <minipal/ospagesize.h>

//
// The Linux/MacOS create dump code
//
// There is a simplified version of this CreateDump function available in nativeaot_createdump_main.cpp.
// If changes are made to this function, consider updating the other one.
bool
CreateDump(const CreateDumpOptions& options)
{
    ProcessInfo processInfo(options);
    ReleaseHolder<CrashInfo> crashInfo{ new CrashInfo(options, processInfo) };
    std::string dumpPath;
    bool processInitialized = false;
    bool result = false;

    // Initialize PAGE_SIZE
#ifdef CREATEDUMP_RUNTIME_PAGE_SIZE
    g_pageSize = minipal_getpagesize();
#endif
    TRACE("PAGE_SIZE %lu\n", (unsigned long)PAGE_SIZE);

    if (!ValidateDumpOptions(&options))
    {
        goto exit;
    }

    if (!processInfo.Initialize())
    {
        goto exit;
    }

    processInitialized = true;

    printf_status("Gathering state for process %d %s\n", options.Pid, crashInfo->Name());

    if (options.Signal != 0 || options.CrashThread != 0)
    {
        printf_status("Crashing thread %04x signal %d (%04x)\n", options.CrashThread, options.Signal, options.Signal);
    }

    // Suspend all the threads in the target process and build the list of threads
    if (!processInfo.EnumerateAndSuspendThreads())
    {
        goto exit;
    }

    // The following three steps gather all the info about the process, threads (registers, etc.) and memory regions
    if (!processInfo.GatherCrashInfo(crashInfo->GetDumpRegionStore()))
    {
        goto exit;
    }
    if (!crashInfo->PopulateFromProcessInfo())
    {
        goto exit;
    }
    if (!crashInfo->GatherCrashInfo(options.DumpType))
    {
        goto exit;
    }

    // Add the special (fake) memory region for the special diagnostics info. Use constructor that doesn't assert PAGE_SIZE alignment.
    if (!AddSpecialDiagInfoRegion(crashInfo->GetDumpRegionStore()))
    {
        goto exit;
    }

    // Determine which memory regions should be included in the dump based on the dump type
    if (!processInfo.SelectDumpRegions(crashInfo->GetDumpRegionStore(), options.DumpType))
    {
        goto exit;
    }

    if (options.DumpType != DumpType::Full)
    {
        crashInfo->AddThreadStacks();
    }

    char pathName[MAX_DUMP_PATH];
    // Format the dump pattern template now that the process name on MacOS has been obtained
    if (!FormatDumpName(pathName, sizeof(pathName), options.DumpPathTemplate, crashInfo->Name(), options.Pid))
    {
        goto exit;
    }

    dumpPath = pathName;
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

#ifdef __APPLE__
        DumpWriter dumpWriter(*crashInfo);
#else
        DynamicArray<ModuleRegion> moduleMappings;
        DynamicArray<MemoryRegion> dumpRegions;
        if (!crashInfo->CopyDumpWriterRegions(moduleMappings, dumpRegions))
        {
            goto exit;
        }
        DumpWriter dumpWriter(processInfo, moduleMappings, dumpRegions);
#endif
        // Write the actual dump file
        if (!dumpWriter.OpenAndWriteDump(dumpPath.c_str()))
        {
            goto exit;
        }
    }
    result = true;
exit:
    LogProcessStatus(options.Pid);
    crashInfo->CleanupAndResumeProcess();
    if (processInitialized)
    {
        processInfo.CleanupAndResumeProcess();
    }

    return result;
}
