// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump_sentinel.h"
#include "createdumpcore.h"
#include "dumpwriter.h"
#include <time.h>

// Exported symbol so PalCreateDump.cpp can detect that this library is linked.
bool g_createdumpLinked = true;

// DumpRegionOperations requires sorted, non-overlapping regions. Use the shared
// array instead of std::set so the linked writer needs only NativeAOT's C++ shim,
// no dependency on the C++ standard library.
static size_t FindDumpRegionInsertionIndex(const DynamicArray<MemoryRegion>& regions, uint64_t startAddress)
{
    size_t low = 0;
    size_t high = regions.size();
    while (low < high)
    {
        size_t middle = low + (high - low) / 2;
        if (regions[middle].StartAddress() < startAddress)
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

static bool FindDumpRegionOverlap(void* context, uint64_t startAddress, uint64_t endAddress, MemoryRegion& result)
{
    DynamicArray<MemoryRegion>& regions = *static_cast<DynamicArray<MemoryRegion>*>(context);
    size_t index = FindDumpRegionInsertionIndex(regions, startAddress);
    if (index > 0 && regions[index - 1].EndAddress() > startAddress)
    {
        result = regions[index - 1];
        return true;
    }
    if (index < regions.size() && regions[index].StartAddress() < endAddress)
    {
        result = regions[index];
        return true;
    }

    return false;
}

static bool InsertDumpRegion(void* context, const MemoryRegion& region)
{
    DynamicArray<MemoryRegion>& regions = *static_cast<DynamicArray<MemoryRegion>*>(context);
    size_t index = FindDumpRegionInsertionIndex(regions, region.StartAddress());
    if ((index > 0 && regions[index - 1].EndAddress() > region.StartAddress()) ||
        (index < regions.size() && regions[index].StartAddress() < region.EndAddress()))
    {
        return false;
    }
    if (!regions.Add(region))
    {
        return false;
    }
    for (size_t destination = regions.size() - 1; destination > index; destination--)
    {
        regions[destination] = regions[destination - 1];
    }
    regions[index] = region;
    return true;
}

void print_trace_timestamp()
{
    struct timespec timestamp;
    if (clock_gettime(CLOCK_MONOTONIC, &timestamp) == 0)
    {
        uint64_t milliseconds = static_cast<uint64_t>(timestamp.tv_sec) * 1000 +
            static_cast<uint64_t>(timestamp.tv_nsec) / 1000000;
        fprintf(g_stdout, "%08" PRIx64 " ", milliseconds);
    }
}

bool GetDefaultDumpPath(char* buffer, size_t bufferSize)
{
    const char* tempDirectory = getenv("TMPDIR");
    if (tempDirectory == nullptr)
    {
        tempDirectory = DEFAULT_DUMP_PATH;
    }
    int length = snprintf(buffer, bufferSize, "%s%s", tempDirectory, DEFAULT_DUMP_TEMPLATE);
    if (tempDirectory[0] == '\0' || length < 0 || static_cast<size_t>(length) >= bufferSize)
    {
        printf_error("Could not get default dump path\n");
        return false;
    }

    return true;
}

// This method is a simplified version of the original CreateDump function in createdumpunix.cpp.
// If changes are made to this function, consider updating the other one.
bool LinkedCreateDump(const CreateDumpOptions& options)
{
    assert(options.CreateDump);
    if (!ValidateDumpOptions(&options))
    {
        return false;
    }

    DynamicArray<MemoryRegion> dumpRegions;
    DumpRegionOperations regionOperations(&dumpRegions, FindDumpRegionOverlap, InsertDumpRegion);
    ProcessInfo processInfo(options);
    if (!processInfo.Initialize())
    {
        return false;
    }

    bool result = false;
    printf_status("Gathering state for process %d %s\n", options.Pid, processInfo.Name());
    if (options.Signal != 0 || options.CrashThread != 0)
    {
        printf_status("Crashing thread %04x signal %d (%04x)\n", options.CrashThread, options.Signal, options.Signal);
    }

    // Suspend all the threads in the target process and build the list of threads
    if (!processInfo.EnumerateAndSuspendThreads())
    {
        goto exit;
    }

    // Gather all the info about the process, threads (registers, etc.) and memory regions
    if (!processInfo.GatherProcessState(regionOperations))
    {
        goto exit;
    }

    // Add the special (fake) memory region for the special diagnostics info. Use constructor that doesn't assert PAGE_SIZE alignment.
    if (!AddSpecialDiagInfoRegion(regionOperations))
    {
        goto exit;
    }

    // Determine which memory regions should be included in the dump based on the dump type
    if (!processInfo.SelectDumpRegions(regionOperations, options.DumpType))
    {
        goto exit;
    }

    char dumpPath[MAX_DUMP_PATH];
    // Format the dump pattern template
    if (!FormatDumpName(dumpPath, sizeof(dumpPath), options.DumpPathTemplate, processInfo.Name(), options.Pid))
    {
        goto exit;
    }

    if (!CombineMemoryRegions(dumpRegions, [](DynamicArray<MemoryRegion>& regions, const MemoryRegion& region)
        {
            return regions.Add(region);
        }))
    {
        goto exit;
    }

    processInfo.CalculateRuntimeBaseAddress();
    printf_status("Writing %s to file %s\n", GetDumpTypeString(options.DumpType), dumpPath);
    {
        DumpWriter dumpWriter(processInfo);
        if (!dumpWriter.OpenDump(dumpPath))
        {
            goto exit;
        }
        if (!dumpWriter.WriteDump(processInfo.ModuleMappings(), dumpRegions))
        {
            printf_error("Writing dump FAILED\n");
            remove(dumpPath);
            goto exit;
        }
    }

    result = true;
exit:
    LogProcessStatus(options.Pid);
    processInfo.CleanupAndResumeProcess();
    return result;
}

int nativeaot_createdump_main(int argc, char* argv[])
{
    CreateDumpOptions options{};
    int exitCode = ParseCreateDumpOptions(argc, argv, &options);
    if (exitCode == 0)
    {
        // The shared parser also accepts CoreCLR options. This implementation has
        // no DAC or crash-report writer, even when invoked directly via the sentinel.
        options.AppModel = AppModelType::NativeAOT;
        char defaultDumpPath[MAX_LONGPATH];
        if (options.DumpPathTemplate == nullptr)
        {
            if (!GetDefaultDumpPath(defaultDumpPath, sizeof(defaultDumpPath)))
            {
                exitCode = -1;
            }
            else
            {
                options.DumpPathTemplate = defaultDumpPath;
            }
        }
        if (exitCode == 0)
        {
            if (LinkedCreateDump(options))
            {
                printf_status("Dump successfully written\n");
            }
            else
            {
                printf_error("Failure writing dump\n");
                exitCode = -1;
            }
        }
    }

    if (g_logfile != nullptr)
    {
        fclose(g_logfile);
    }

    return exitCode;
}
