// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "shared/createdumpcore.h"

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

// Exported symbol so PalCreateDump.cpp can detect that this library is linked.
bool g_createdumpLinked = true;

void print_trace_timestamp()
{

}

// This method is a simplified version of the original CreateDump function.
// Consider updating the original CreateDump if changes are made to this simplified version.
bool LinkedCreateDump(const CreateDumpOptions* options)
{
    asserte(options->CreateDump);

    if (!ValidateDumpOptions(options))
    {
        return false;
    }

    bool result = false;
    bool initialized = false;
    DynamicArray<MemoryRegion> dumpRegions;
    DynamicArray<MemoryRegion> combinedRegions;
    DumpRegionStore regionStore{ &dumpRegions, &FindDumpRegionOverlap, &InsertDumpRegion };
    ProcessInfo processInfo(*options);

    if (!processInfo.Initialize())
    {
        return false;
    }

    printf_status("Gathering state for process %d %s\n", options->Pid, processInfo.Name());

    if (options->Signal != 0 || options->CrashThread != 0)
    {
        printf_status("Crashing thread %04x signal %d (%04x)\n", options->CrashThread, options->Signal, options->Signal);
    }

    initialized = true;

    if (!processInfo.EnumerateAndSuspendThreads())
    {
        goto exit;
    }

    if (!processInfo.GatherCrashInfo(regionStore))
    {
        goto exit;
    }

    if (!AddSpecialDiagInfoRegion(regionStore))
    {
        goto exit;
    }

    if (!processInfo.SelectDumpRegions(regionStore, options->DumpType))
    {
        goto exit;
    }

    char dumpPath[MAX_LONGPATH + 1];
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

    printf_status("Writing %s to file %s\n", GetDumpTypeString(options.DumpType), dumpPath);

    processInfo.CalculateRuntimeBaseAddress();
    DumpWriter dumpWriter(processInfo, processInfo.ModuleMappings(), dumpRegions);
    // Write the actual dump file
    if (!dumpWriter.OpenDump(dumpPath))
    {
        goto exit;
    }
    if (!dumpWriter.WriteDump())
    {
        printf_error("Writing dump FAILED\n");

        // Delete the partial dump file on error
        remove(dumpPath);
        goto exit;
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
    if (initialized)
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
    if (options.DumpPathTemplate == NULL)
    {
        if (!GetDefaultDumpPath(defaultDumpPath, MAX_LONGPATH))
        {
            printf_error("Could not get default dump path\n");
            return -1;
        }
        options.DumpPathTemplate = defaultDumpPath;
    }

    bool result = LinkedCreateDump(&options);
    return result ? 0 : 1;
}