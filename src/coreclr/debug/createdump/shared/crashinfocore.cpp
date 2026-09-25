// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdumpcore.h"

bool ProcessInfo::PageCanBeRead(uint64_t start)
{
    uint8_t buffer[1];
    size_t read;
    return ReadProcessMemory(start, buffer, 1, &read);
}

//
// Check the page is really used by the application before adding it to the dump
// On some kernels reading a region from createdump results in committing this region in the parent application
// That leads to OOM in container environment and unnecesserally increses the size of the dump file
// However this is an optimization: if it fails we still try to add the page to the dump
//
bool ProcessInfo::PageMappedToPhysicalMemory(uint64_t start)
{
    #if !defined(__linux__)
        // this check has not been implemented yet for other unix systems
        return true;
    #else
        // https://www.kernel.org/doc/Documentation/vm/pagemap.txt
        if (m_fdPagemap == -1)
        {
            // Weren't able to open pagemap file, so don't run this check
            // Expected on kernels 4.0 and 4.1 as we need CAP_SYS_ADMIN to open /proc/pid/pagemap
            // On kernels after 4.2 we only need PTRACE_MODE_READ_FSCREDS as we are ok with zeroed PFNs
            return true;
        }

        uint64_t pagemapOffset = (start / PAGE_SIZE) * sizeof(uint64_t);
        uint64_t seekResult = lseek(m_fdPagemap, (off_t) pagemapOffset, SEEK_SET);
        if (seekResult != pagemapOffset)
        {
            int seekErrno = errno;
            TRACE("Seeking in pagemap file FAILED, addr: %" PRIA PRIx64 ", pagemap offset: %" PRIA PRIx64 ", ERRNO %d: %s\n", start, pagemapOffset, seekErrno, strerror(seekErrno));
            return true;
        }
        uint64_t value;
        size_t readResult = read(m_fdPagemap, (void*)&value, sizeof(value));
        if (readResult == (size_t) -1)
        {
            int readErrno = errno;
            TRACE("Reading of pagemap file FAILED, addr: %" PRIA PRIx64 ", pagemap offset: %" PRIA PRIx64 ", size: %zu, ERRNO %d: %s\n", start, pagemapOffset, sizeof(value), readErrno, strerror(readErrno));
            return true;
        }

        bool is_page_present = (value & ((uint64_t)1 << 63)) != 0;
        bool is_page_swapped = (value & ((uint64_t)1 << 62)) != 0;
        TRACE_VERBOSE("Pagemap value for %" PRIA PRIx64 ", pagemap offset %" PRIA PRIx64 " is %" PRIA PRIx64 " -> %s\n", start, pagemapOffset, value, is_page_present ? "in memory" : (is_page_swapped ? "in swap" : "NOT in memory"));
        return is_page_present || is_page_swapped;
    #endif
}

bool ProcessInfo::SelectDumpRegions(DumpRegionStore& regionStore, DumpType dumpType)
{
    // If full memory dump, include everything regardless of permissions
    if (dumpType == DumpType::Full)
    {
        for (const MemoryRegion& region : m_moduleMappings)
        {
            if (InsertMemoryRegion(regionStore, region) < 0)
                return false;
        }
        for (const MemoryRegion& region : m_otherMappings)
        {
            // Don't add uncommitted pages to the full dump
            if ((region.Permissions() & (PF_R | PF_W | PF_X)) != 0)
            {
                if (InsertMemoryRegion(regionStore, region) < 0)
                    return false;
            }
        }
    }
    else
    {
        // Add all the heap read/write memory regions (m_otherMappings contains the heaps). On Alpine
        // the heap regions are marked RWX instead of just RW.
        if (dumpType == DumpType::Heap)
        {
            for (const MemoryRegion& region : m_otherMappings)
            {
                uint32_t permissions = region.Permissions();
#ifdef __APPLE__
                if (permissions == (PF_R | PF_W))
#else
                if (permissions == (PF_R | PF_W) || permissions == (PF_R | PF_W | PF_X))
#endif
                {
                    if (InsertMemoryRegion(regionStore, region) < 0)
                        return false;
                }
            }
        }
    }

    return true;
}

bool ProcessInfo::AddMapping(const MemoryRegion& region)
{
    return m_otherMappings.Add(region);
}

bool ProcessInfo::AddMapping(const ModuleRegion& region)
{
    ModuleRegion mapping(static_cast<const MemoryRegion&>(region));
    if (*region.FileName() != '\0' && !mapping.SetFileName(region.FileName()))
    {
        return false;
    }
    return m_moduleMappings.Add(Move(mapping));
}

bool AddSpecialDiagInfoRegion(DumpRegionStore& regionStore)
{
    // Add the special (fake) memory region for the special diagnostics info. Use constructor that doesn't assert PAGE_SIZE alignment.
    MemoryRegion special(PF_R, SpecialDiagInfoAddress, SpecialDiagInfoAddress + SpecialDiagInfoSize, /* offset */ 0);
    return regionStore.Insert(&special);
}

//
// Add a memory region to the list. Returns the number of pages actually added.
//
int ProcessInfo::InsertMemoryRegion(DumpRegionStore& regionStore, const MemoryRegion& memoryRegion)
{
    // Check if the new region overlaps with the previously added ones
    MemoryRegion conflictingRegion;
    bool hasConflict = regionStore.FindOverlap(memoryRegion.StartAddress(), memoryRegion.EndAddress(), &conflictingRegion);
    if (hasConflict && conflictingRegion.Contains(memoryRegion))
    {
        // The region is contained in the one we added before
        // Nothing to do
        return 0;
    }
    uint64_t pageStart = memoryRegion.StartAddress();
    uint64_t numberPages = memoryRegion.Size() / m_pageSize;
    uint64_t subRegionStart, subRegionEnd;
    int pagesAdded = 0;
    subRegionStart = subRegionEnd = pageStart;
    for (size_t p = 0; p < numberPages; p++, pageStart += m_pageSize)
    {
        MemoryRegion pageRegion(memoryRegion.Flags(), pageStart, pageStart + m_pageSize);
        // avoid searching for conflicts if we know we don't have one
        bool pageHasConflicts = hasConflict && 
                                regionStore.FindOverlap(pageRegion.StartAddress(), pageRegion.EndAddress(), &conflictingRegion);
        // avoid validating the page if it conflicts: we won't add it in any case
        bool pageIsValid = !pageHasConflicts && PageMappedToPhysicalMemory(pageStart) && PageCanBeRead(pageStart);

        if (pageIsValid)
        {
            subRegionEnd = pageRegion.EndAddress();
            pagesAdded++;
        }
        else
        {
            // the next page is not valid thus sub-region is complete
            if (subRegionStart != subRegionEnd)
            {
                MemoryRegion subRegion(memoryRegion.Flags(), subRegionStart, subRegionEnd);
                if (!regionStore.Insert(&subRegion))
                {
                    return -1;
                }
            }
            subRegionStart = subRegionEnd = pageStart + m_pageSize;
        }
    }

    // add the last sub-region if it's not empty
    if (subRegionStart != subRegionEnd)
    {
        MemoryRegion subRegion(memoryRegion.Flags(), subRegionStart, subRegionEnd);
        if (!regionStore.Insert(&subRegion))
        {
            return -1;
        }
    }

    return pagesAdded;
}

bool ProcessInfo::GatherCrashInfo(DumpRegionStore& regionStore)
{
    for (ThreadSnapshot& thread : m_threads)
    {
        if (!thread.Initialize())
        {
            return false;
        }
    }
#ifndef __APPLE__
    if (!GetAuxvEntries())
    {
        return false;
    }
    if (!EnumerateMemoryRegions(regionStore))
    {
        return false;
    }
#endif
    return true;
}