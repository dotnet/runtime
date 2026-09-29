// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"
#include <minipal/ospagesize.h>

//
// The Linux/MacOS create dump code
//
// There is a simplified version of the original CreateDump function available in nativeaot_createdump_main.cpp.
// Consider updating the original CreateDump if changes are made to the simplified version.
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

    // Initialize the crash info
    if (!crashInfo->Initialize())
    {
        goto exit;
    }
    printf_status("Gathering state for process %d %s\n", options.Pid, crashInfo->Name());

    if (options.Signal != 0 || options.CrashThread != 0)
    {
        printf_status("Crashing thread %04x signal %d (%04x)\n", options.CrashThread, options.Signal, options.Signal);
    }

    if (!processInfo.EnumerateAndSuspendThreads())
    {
        goto exit;
    }
    if (!processInfo.GatherCrashInfo(crashInfo->GetDumpRegionStore()))
    {
        goto exit;
    }
    if (!crashInfo->PopulateFromProcessInfo())
    {
        goto exit;
    }
    // Gather external-only DAC, unwind, and managed module information.
    if (!crashInfo->GatherCrashInfo(options.DumpType))
    {
        goto exit;
    }

    if (!AddSpecialDiagInfoRegion(crashInfo->GetDumpRegionStore()))
    {
        goto exit;
    }

    if (!processInfo.SelectDumpRegions(crashInfo->GetDumpRegionStore(), options.DumpType))
    {
        goto exit;
    }

    if (options.DumpType != DumpType::Full)
    {
        crashInfo->AddThreadStacks();
    }

    char pathName[MAX_LONGPATH + 1];
    // Format the dump pattern template now that the process name on MacOS has been obtained
    if (!FormatDumpName(pathName, MAX_LONGPATH, options.DumpPathTemplate, crashInfo->Name(), options.Pid))
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
