// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"

bool
CrashInfo::Initialize()
{
    m_ppid = 0;
    m_tgid = 0;

    kern_return_t result = ::task_for_pid(mach_task_self(), m_pid, &m_task);
    if (result != KERN_SUCCESS)
    {
        fprintf(stderr, "task_for_pid(%d) FAILED %x %s\n", m_pid, result, mach_error_string(result));
        return false;
    }
    m_auxvEntries.push_back(elf_aux_entry { AT_BASE, { 0 } });
    m_auxvEntries.push_back(elf_aux_entry { AT_NULL, { 0 } });
    return true;
}

void
CrashInfo::CleanupAndResumeProcess()
{
    // Resume all the threads suspended in EnumerateAndSuspendThreads
    for (ThreadInfo* thread : m_threads)
    {
        ::thread_resume(thread->Port());
    }
}

static
kern_return_t
SuspendMachThread(thread_act_t thread, int tid)
{
    kern_return_t result;

    while (true)
    {
        result = thread_suspend(thread);
        if (result != KERN_SUCCESS)
        {
            fprintf(stderr, "thread_suspend(%d) FAILED %x %s\n", tid, result, mach_error_string(result));
            break;
        }

        // Ensure that if the thread was running in the kernel, the kernel operation
        // is safely aborted so that it can be restarted later.
        result = thread_abort_safely(thread);
        if (result == KERN_SUCCESS)
        {
            break;
        }
        else
        {
            TRACE("thread_abort_safely(%d) FAILED %x %s\n", tid, result, mach_error_string(result));
        }
        // The thread was running in the kernel executing a non-atomic operation
        // that cannot be restarted, so we need to resume the thread and retry
        result = thread_resume(thread);
        if (result != KERN_SUCCESS)
        {
            fprintf(stderr, "thread_resume(%d) FAILED %x %s\n", tid, result, mach_error_string(result));
            break;
        }
    }

    return result;
}

//
// Suspends all the threads and creating a list of them. Should be the before gathering any info about the process.
//
bool
CrashInfo::EnumerateAndSuspendThreads()
{
    thread_act_port_array_t threadList;
    mach_msg_type_number_t threadCount;

    kern_return_t result = ::task_threads(Task(), &threadList, &threadCount);
    if (result != KERN_SUCCESS)
    {
        fprintf(stderr, "task_threads(%d) FAILED %x %s\n", m_pid, result, mach_error_string(result));
        return false;
    }

    for (int i = 0; i < threadCount; i++)
    {
        thread_identifier_info_data_t tident;
        mach_msg_type_number_t tident_count = THREAD_IDENTIFIER_INFO_COUNT;
        int tid;

        result = ::thread_info(threadList[i], THREAD_IDENTIFIER_INFO, (thread_info_t)&tident, &tident_count);
        if (result != KERN_SUCCESS)
        {
            TRACE("%d thread_info(%x) FAILED %x %s\n", i, threadList[i], result, mach_error_string(result));
            tid = (int)threadList[i];
        }
        else
        {
            tid = tident.thread_id;
        }

        result = SuspendMachThread(threadList[i], tid);
        if (result != KERN_SUCCESS)
        {
            return false;
        }
        // Add to the list of threads
        ThreadInfo* thread = new ThreadInfo(*this, tid, threadList[i]);
        m_threads.push_back(thread);
    }

    return true;
}

uint32_t
ConvertProtectionFlags(vm_prot_t prot)
{
    uint32_t regionFlags = 0;
    if (prot & VM_PROT_READ) {
        regionFlags |= PF_R;
    }
    if (prot & VM_PROT_WRITE) {
        regionFlags |= PF_W;
    }
    if (prot & VM_PROT_EXECUTE) {
        regionFlags |= PF_X;
    }
    return regionFlags;
}

bool
CrashInfo::EnumerateMemoryRegions()
{
    vm_region_submap_info_data_64_t info;
    mach_vm_address_t address = 1;
    mach_vm_size_t size = 0;
    uint32_t depth = 0;

    // First enumerate and add all the regions
    while (address > 0 && address < MACH_VM_MAX_ADDRESS)
    {
        mach_msg_type_number_t count = VM_REGION_SUBMAP_INFO_COUNT_64;
        kern_return_t result = ::mach_vm_region_recurse(Task(), &address, &size, &depth, (vm_region_recurse_info_t)&info, &count);
        if (result != KERN_SUCCESS) {
            fprintf(stderr, "mach_vm_region_recurse for address %016llx %08llx FAILED %x %s\n", address, size, result, mach_error_string(result));
            return false;
        }
        TRACE("%016llx - %016llx (%06llx) %08llx %s %d %d %d %c%c%c\n",
            address,
            address + size,
            size / PAGE_SIZE,
            info.offset,
            info.is_submap ? "sub" : "   ",
            info.user_wired_count,
            info.share_mode,
            depth,
            (info.protection & VM_PROT_READ) ? 'r' : '-',
            (info.protection & VM_PROT_WRITE) ? 'w' : '-',
            (info.protection & VM_PROT_EXECUTE) ? 'x' : '-');

        if (info.is_submap) {
            depth++;
        }
        else
        {
            if (info.share_mode != SM_EMPTY && (info.protection & (VM_PROT_READ | VM_PROT_WRITE | VM_PROT_EXECUTE)) != 0)
            {
                MemoryRegion memoryRegion(ConvertProtectionFlags(info.protection), address, address + size, info.offset);
                m_allMemoryRegions.insert(memoryRegion);
            }
            address += size;
        }
    }

    // Now find all the modules and add them to the module list
    for (const MemoryRegion& region : m_allMemoryRegions)
    {
        bool found;
        if (!TryFindDyLinker(region.StartAddress(), region.Size(), &found)) {
            return false;
        }
        if (found) {
            break;
        }
    }

    // Filter out the module regions from the memory regions gathered
    for (const MemoryRegion& region : m_allMemoryRegions)
    {
        std::set<MemoryRegion>::iterator found = m_moduleMappings.find(region);
        if (found == m_moduleMappings.end())
        {
            m_otherMappings.insert(region);
        }
        else
        {
            // Skip any region that is fully contained in a module region
            if (!found->Contains(region))
            {
                TRACE("Region:   ");
                region.Trace();

                // Now add all the gaps in "region" left by the module regions
                uint64_t previousEndAddress = region.StartAddress();

                for (; found != m_moduleMappings.end(); found++)
                {
                    if (region.Contains(*found))
                    {
                        MemoryRegion gap(region.Flags(), previousEndAddress, found->StartAddress(), region.Offset(), std::string());
                        if (gap.Size() > 0)
                        {
                            TRACE("     Gap: ");
                            gap.Trace();
                            m_otherMappings.insert(gap);
                        }
                        previousEndAddress = found->EndAddress();
                    }
                }

                MemoryRegion endgap(region.Flags(), previousEndAddress, region.EndAddress(), region.Offset(), std::string());
                if (endgap.Size() > 0)
                {
                    TRACE("   EndGap:");
                    endgap.Trace();
                    m_otherMappings.insert(endgap);
                }
            }
        }
    }
    return true;
}

bool
CrashInfo::TryFindDyLinker(mach_vm_address_t address, mach_vm_size_t size, bool* found)
{
    bool result = true;
    *found = false;

    if (size > sizeof(mach_header_64))
    {
        mach_header_64* header = nullptr;
        mach_msg_type_number_t read = 0;
        kern_return_t kresult = ::vm_read(Task(), address, sizeof(mach_header_64), (vm_offset_t*)&header, &read);
        if (kresult == KERN_SUCCESS)
        {
            if (header->magic == MH_MAGIC_64)
            {
                TRACE("TryFindDyLinker: found module header at %016llx %08llx ncmds %d sizeofcmds %08x type %02x\n",
                    address,
                    size,
                    header->ncmds,
                    header->sizeofcmds,
                    header->filetype);

                if (header->filetype == MH_DYLINKER)
                {
                    TRACE("TryFindDyLinker: found dylinker\n");
                    *found = true;

                    // Enumerate all the modules in dyld's image cache. VisitModule is called for every module found.
                    result = EnumerateModules(address, header);
                }
            }
        }
        if (header != nullptr)
        {
            ::vm_deallocate(Task(), (vm_address_t)header, sizeof(mach_header_64));
        }
    }

    return result;
}

void CrashInfo::VisitModule(MachOModule& module)
{
    // Get the process name from the executable module file type
    if (m_name.empty() && module.Header().filetype == MH_EXECUTE)
    {
        size_t last = module.Name().rfind(DIRECTORY_SEPARATOR_STR_A);
        if (last != std::string::npos) {
            last++;
        }
        else {
            last = 0;
        }
        m_name = module.Name().substr(last);
    }
    // Save the runtime module path
    if (m_coreclrPath.empty())
    {
        size_t last = module.Name().rfind(MAKEDLLNAME_A("coreclr"));
        if (last != std::string::npos) {
            m_coreclrPath = module.Name().substr(0, last);
        }
    }
    // VisitSegment is called for each segment of the module
    module.EnumerateSegments();
}

void CrashInfo::VisitSegment(MachOModule& module, const segment_command_64& segment)
{
    if (segment.initprot != 0)
    {
        // The __LINKEDIT segment contains the raw data used by dynamic linker, such as symbol,
        // string and relocation table entries. More importantly, the same __LINKEDIT segment
        // can be shared by multiple modules so we need to skip them to prevent overlapping
        // module regions.
        if (strcmp(segment.segname, SEG_LINKEDIT) != 0)
        {
            uint32_t regionFlags = ConvertProtectionFlags(segment.initprot);
            uint64_t offset = segment.fileoff;
            uint64_t start = segment.vmaddr + module.LoadBias();
            uint64_t end = start + segment.vmsize;

            // Round to page boundary
            start = start & PAGE_MASK;
            _ASSERTE(start > 0);

            // Round up to page boundary
            end = (end + (PAGE_SIZE - 1)) & PAGE_MASK;
            _ASSERTE(end > 0);

            // Add module memory region if not already on the list
            MemoryRegion moduleRegion(regionFlags, start, end, offset, module.Name());
            const auto& found = m_moduleMappings.find(moduleRegion);
            if (found == m_moduleMappings.end())
            {
                TRACE("VisitSegment: ");
                moduleRegion.Trace();

                // Add this module segment to the module mappings list
                m_moduleMappings.insert(moduleRegion);

                // Add module segment ip to base address lookup
                MemoryRegion addressRegion(0, start, end, module.BaseAddress());
                m_moduleAddresses.insert(addressRegion);
            }
            else
            {
                TRACE("VisitSegment: WARNING: ");
                moduleRegion.Trace();
                TRACE("       is overlapping: ");
                found->Trace();
            }
        }
    }
}

void
CrashInfo::VisitSection(MachOModule& module, const section_64& section)
{
    // Add the unwind and eh frame info to the dump
    if ((strcmp(section.sectname, "__unwind_info") == 0) || (strcmp(section.sectname, "__eh_frame") == 0))
    {
        InsertMemoryRegion(section.addr + module.LoadBias(), section.size);
    }
}

//
// Get the memory region flags for a start address
//
uint32_t
CrashInfo::GetMemoryRegionFlags(uint64_t start)
{
    MemoryRegion search(0, start, start + PAGE_SIZE);
    const MemoryRegion* region = SearchMemoryRegions(m_allMemoryRegions, search);
    if (region != nullptr) {
        return region->Flags();
    }
    TRACE("GetMemoryRegionFlags: %016llx FAILED\n", start);
    return PF_R | PF_W | PF_X;
}

//
// Read raw memory
//
bool
CrashInfo::ReadProcessMemory(void* address, void* buffer, size_t size, size_t* read)
{
    assert(buffer != nullptr);
    assert(read != nullptr);

    // vm_read_overwrite usually requires that the address be page-aligned
    // and the size be a multiple of the page size.  We can't differentiate
    // between the cases in which that's required and those in which it
    // isn't, so we do it all the time.
    int* addressAligned = (int*)((SIZE_T)address & ~(PAGE_SIZE - 1));
    ssize_t offset = ((SIZE_T)address & (PAGE_SIZE - 1));
    char *data = (char*)alloca(PAGE_SIZE);
    ssize_t numberOfBytesRead = 0;
    ssize_t bytesToRead;

    while (size > 0)
    {
        vm_size_t bytesRead;
        
        bytesToRead = PAGE_SIZE - offset;
        if (bytesToRead > size)
        {
            bytesToRead = size;
        }
        bytesRead = PAGE_SIZE;
        kern_return_t result = ::vm_read_overwrite(Task(), (vm_address_t)addressAligned, PAGE_SIZE, (vm_address_t)data, &bytesRead);
        if (result != KERN_SUCCESS || bytesRead != PAGE_SIZE)
        {
            TRACE_VERBOSE("vm_read_overwrite failed for %d bytes from %p: %x %s\n", PAGE_SIZE, (char *)addressAligned, result, mach_error_string(result));
            *read = 0;
            return false;
        }
        memcpy((LPSTR)buffer + numberOfBytesRead, data + offset, bytesToRead);
        addressAligned = (int*)((char*)addressAligned + PAGE_SIZE);
        numberOfBytesRead += bytesToRead;
        size -= bytesToRead;
        offset = 0;
    }
    *read = numberOfBytesRead;
    return true;
}

// For src/inc/llvm/ELF.h
Elf64_Ehdr::Elf64_Ehdr()
{
}

