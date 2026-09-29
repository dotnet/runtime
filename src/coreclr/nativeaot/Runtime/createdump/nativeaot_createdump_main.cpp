// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdumpcore.h"
#include "dumpwriter.h"
#include <time.h>

// Exported symbol so PalCreateDump.cpp can detect that this library is linked.
bool g_createdumpLinked = true;

// Regions is a sorted array of non-overlapping memory regions
static size_t FindDumpRegionInsertionIndex(const DynamicArray<MemoryRegion>* regions, uint64_t startAddress)
{
    size_t low = 0;
    size_t high = regions->Count();

    while (low < high)
    {
        size_t middle = low + ((high - low) / 2);
        if ((*regions)[middle].StartAddress() < startAddress)
        {
            low = middle + 1;
        }
        else
        {
            high = middle;
        }
    }

    return low;
}

static bool FindDumpRegionOverlap(
    void* container,
    uint64_t startAddress,
    uint64_t endAddress,
    MemoryRegion* result)
{
    DynamicArray<MemoryRegion>* regions = static_cast<DynamicArray<MemoryRegion>*>(container);
    size_t index = FindDumpRegionInsertionIndex(regions, startAddress);

    if (index > 0 && (*regions)[index - 1].EndAddress() > startAddress)
    {
        *result = (*regions)[index - 1];
        return true;
    }

    if (index == regions->Count() || (*regions)[index].StartAddress() >= endAddress)
    {
        return false;
    }

    *result = (*regions)[index];
    return true;
}

static bool InsertDumpRegion(void* container, const MemoryRegion* region)
{
    DynamicArray<MemoryRegion>* regions = static_cast<DynamicArray<MemoryRegion>*>(container);
    size_t index = FindDumpRegionInsertionIndex(regions, region->StartAddress());

    if ((index > 0 && (*regions)[index - 1].EndAddress() > region->StartAddress()) ||
        (index < regions->Count() && (*regions)[index].StartAddress() < region->EndAddress()))
    {
        return false;
    }

    if (!regions->Add(*region))
    {
        return false;
    }

    for (size_t destination = regions->Count() - 1; destination > index; destination--)
    {
        (*regions)[destination] = (*regions)[destination - 1];
    }
    (*regions)[index] = *region;
    return true;
}

void print_trace_timestamp()
{
    struct timespec timestamp;
    if (clock_gettime(CLOCK_MONOTONIC, &timestamp) == 0)
    {
        uint64_t milliseconds = static_cast<uint64_t>(timestamp.tv_sec) * 1000 + static_cast<uint64_t>(timestamp.tv_nsec) / 1000000;
        fprintf(g_stdout, "%08" PRIx64 " ", milliseconds);
    }
}

bool GetDefaultDumpPath(char* buffer, size_t bufferSize)
{
    strncpy(buffer, DEFAULT_DUMP_PATH DEFAULT_DUMP_TEMPLATE, bufferSize);
    buffer[bufferSize - 1] = '\0';
    return true;
}

// This method is a simplified version of the original CreateDump function in createdumpunix.cpp.
// If changes are made to this function, consider updating the other one.
bool LinkedCreateDump(const CreateDumpOptions* options)
{
    assert(options->CreateDump);

    if (!ValidateDumpOptions(options))
    {
        return false;
    }

    bool result = false;
    bool processInitialized = false;
    DynamicArray<MemoryRegion> dumpRegions;
    DynamicArray<MemoryRegion> combinedRegions;
    DumpRegionStore regionStore{ &dumpRegions, &FindDumpRegionOverlap, &InsertDumpRegion };
    ProcessInfo processInfo(*options);

    if (!processInfo.Initialize())
    {
        return false;
    }

    processInitialized = true;

    printf_status("Gathering state for process %d %s\n", options->Pid, processInfo.Name());

    if (options->Signal != 0 || options->CrashThread != 0)
    {
        printf_status("Crashing thread %04x signal %d (%04x)\n", options->CrashThread, options->Signal, options->Signal);
    }

    // Suspend all the threads in the target process and build the list of threads
    if (!processInfo.EnumerateAndSuspendThreads())
    {
        goto exit;
    }

    // Gather all the info about the process, threads (registers, etc.) and memory regions
    if (!processInfo.GatherCrashInfo(regionStore))
    {
        goto exit;
    }

    // Add the special (fake) memory region for the special diagnostics info. Use constructor that doesn't assert PAGE_SIZE alignment.
    if (!AddSpecialDiagInfoRegion(regionStore))
    {
        goto exit;
    }

    // Determine which memory regions should be included in the dump based on the dump type
    if (!processInfo.SelectDumpRegions(regionStore, options->DumpType))
    {
        goto exit;
    }

    char dumpPath[MAX_LONGPATH + 1];
    // Format the dump pattern template
    if (!FormatDumpName(dumpPath, MAX_LONGPATH, options->DumpPathTemplate, processInfo.Name(), options->Pid))
    {
        goto exit;
    }

    if (!CombineMemoryRegions(
            dumpRegions,
            combinedRegions,
            [&combinedRegions](const MemoryRegion& region)
            {
                assert(combinedRegions.empty() || combinedRegions[combinedRegions.Count() - 1] < region);
                return combinedRegions.Add(region);
            }))
    {
        goto exit;
    }
    dumpRegions = Move(combinedRegions);

    printf_status("Writing %s to file %s\n", GetDumpTypeString(options->DumpType), dumpPath);

    processInfo.CalculateRuntimeBaseAddress();

    {
        DumpWriter dumpWriter(processInfo, processInfo.ModuleMappings(), dumpRegions);

        // Write the actual dump file
        if (!dumpWriter.OpenAndWriteDump(dumpPath))
        {
            goto exit;
        }
    }

    result = true;

exit:
    LogProcessStatus(options->Pid);
    if (processInitialized)
    {
        processInfo.CleanupAndResumeProcess();
    }

    return result;
}

int nativeaot_createdump_main(int argc, const char* argv[])
{
    CreateDumpOptions options{};
    int exitCode = ParseCreateDumpOptions(argc, (char**)argv, &options);
    if (exitCode != 0)
    {
        return exitCode;
    }

    char defaultDumpPath[MAX_LONGPATH];
    if (options.DumpPathTemplate == nullptr)
    {
        if (!GetDefaultDumpPath(defaultDumpPath, MAX_LONGPATH))
        {
            printf_error("Could not get default dump path\n");
            return -1;
        }
        options.DumpPathTemplate = defaultDumpPath;
    }

    if (LinkedCreateDump(&options))
    {
        printf_status("Dump successfully written\n");
    }
    else
    {
        printf_error("Failure writing dump\n");
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