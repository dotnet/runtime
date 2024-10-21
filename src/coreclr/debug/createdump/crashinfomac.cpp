// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"

extern uint8_t g_debugHeaderCookie[4];

int g_readProcessMemoryResult = KERN_SUCCESS;

bool
CrashInfo::Initialize()
{
    m_ppid = 0;
    m_tgid = 0;

    kern_return_t result = ::task_for_pid(mach_task_self(), m_pid, &m_task);
    if (result != KERN_SUCCESS)
    {
        // Regardless of the reason (invalid process id or invalid signing/entitlements) it always returns KERN_FAILURE (5)
        printf_error("Invalid process id: task_for_pid(%d) FAILED %s (%x)\n", m_pid, mach_error_string(result), result);
        printf_error("This failure may be because createdump or the application is not properly signed and entitled.\n");
        return false;
    }
    return true;
}

void
CrashInfo::CleanupAndResumeProcess()
{
    // Resume all the threads suspended in EnumerateAndSuspendThreads
    ::task_resume(Task());
}

//
// Suspends all the threads and creating a list of them. Should be the before gathering any info about the process.
//
bool
CrashInfo::EnumerateAndSuspendThreads()
{
    thread_act_port_array_t threadList;
    mach_msg_type_number_t threadCount;

    kern_return_t result = ::task_suspend(Task());
    if (result != KERN_SUCCESS)
    {
        printf_error("Problem suspending process: task_suspend(%d) FAILED %s (%x)\n", m_pid, mach_error_string(result), result);
        return false;
    }

    result = ::task_threads(Task(), &threadList, &threadCount);
    if (result != KERN_SUCCESS)
    {
        printf_error("Problem enumerating threads: task_threads(%d) FAILED %s (%x)\n", m_pid, mach_error_string(result), result);
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
            TRACE("%d thread_info(%x) FAILED %s (%x)\n", i, threadList[i], mach_error_string(result), result);
            tid = (int)threadList[i];
        }
        else
        {
            tid = tident.thread_id;
        }

        // Add to the list of threads
        ThreadInfo* thread = new ThreadInfo(*this, tid, threadList[i]);
        m_threads.push_back(thread);
    }

    result = ::vm_deallocate(mach_task_self(), reinterpret_cast<vm_address_t>(threadList), threadCount * sizeof(thread_act_t));
    if (result != KERN_SUCCESS)
    {
        TRACE("vm_deallocate FAILED %x %s\n", result, mach_error_string(result));
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
    uint64_t cbAllMemoryRegions = 0;
    uint32_t depth = 0;

    // First enumerate and add all the regions
    while (address > 0 && address < MACH_VM_MAX_ADDRESS)
    {
        mach_msg_type_number_t count = VM_REGION_SUBMAP_INFO_COUNT_64;
        kern_return_t result = ::mach_vm_region_recurse(Task(), &address, &size, &depth, (vm_region_recurse_info_t)&info, &count);
        if (result != KERN_SUCCESS) {
            // Iteration can be ended on a KERN_INVALID_ADDRESS
            // Allow other kernel errors to continue too so we can get at least part of a dump
            TRACE("mach_vm_region_recurse for address %016llx %08llx FAILED %s (%x)\n", address, size, mach_error_string(result), result);
            break;
        }
        TRACE_VERBOSE("%016llx - %016llx (%06llx, %06llx) %08llx %s %d %d %c%c%c %02x\n",
            address,
            address + size,
            size / PAGE_SIZE,
            info.pages_resident,
            info.offset,
            info.is_submap ? "sub" : "   ",
            depth,
            info.share_mode,
            (info.protection & VM_PROT_READ) ? 'r' : '-',
            (info.protection & VM_PROT_WRITE) ? 'w' : '-',
            (info.protection & VM_PROT_EXECUTE) ? 'x' : '-',
            info.protection);

        if (info.is_submap) {
            depth++;
        }
        else
        {
            if (info.share_mode != SM_EMPTY && (info.protection & (VM_PROT_READ | VM_PROT_WRITE | VM_PROT_EXECUTE)) != 0)
            {
                MemoryRegion memoryRegion(ConvertProtectionFlags(info.protection), address, address + size, info.offset);
                m_allMemoryRegions.insert(memoryRegion);
                cbAllMemoryRegions += memoryRegion.Size();
            }
            address += size;
        }
    }

    // Get the dylinker info and enumerate all the modules
    struct task_dyld_info dyld_info;
    mach_msg_type_number_t count = TASK_DYLD_INFO_COUNT;
    kern_return_t result = ::task_info(Task(), TASK_DYLD_INFO, (task_info_t)&dyld_info, &count);
    if (result != KERN_SUCCESS)
    {
        TRACE("EnumerateMemoryRegions: task_info(TASK_DYLD_INFO) FAILED %x %s\n", result, mach_error_string(result));
        return false;
    }

    // Enumerate all the modules in dyld's image cache. VisitModule is called for every module found.
    if (!EnumerateModules(dyld_info.all_image_info_addr))
    {
        return false;
    }

    TRACE("EnumerateMemoryRegions: cbAllMemoryRegions %06llx native cbModuleMappings %06llx\n", cbAllMemoryRegions / PAGE_SIZE, m_cbModuleMappings / PAGE_SIZE);
    return true;
}

void
CrashInfo::InitializeOtherMappings()
{
    uint64_t cbOtherMappings = 0;

    // Filter out the module regions from the memory regions gathered. The m_moduleMappings list needs
    // to include all the native and managed module regions.
    for (const MemoryRegion& region : m_allMemoryRegions)
    {
        std::set<ModuleRegion>::iterator found = m_moduleMappings.find(ModuleRegion(region));
        if (found == m_moduleMappings.end())
        {
            m_otherMappings.insert(region);
            cbOtherMappings += region.Size();
        }
        else
        {
            // Skip any region that is fully contained in a module region
            if (!found->Contains(region))
            {
                region.Trace("Region:   ");

                // Now add all the gaps in "region" left by the module regions
                uint64_t previousEndAddress = region.StartAddress();

                for (; found != m_moduleMappings.end(); found++)
                {
                    if (region.Contains(*found))
                    {
                        MemoryRegion gap(region.Flags(), previousEndAddress, found->StartAddress(), region.Offset());
                        if (gap.Size() > 0)
                        {
                            gap.Trace("     Gap: ");
                            m_otherMappings.insert(gap);
                            cbOtherMappings += gap.Size();
                        }
                        previousEndAddress = found->EndAddress();
                    }
                }

                MemoryRegion endgap(region.Flags(), previousEndAddress, region.EndAddress(), region.Offset());
                if (endgap.Size() > 0)
                {
                    endgap.Trace("  EndGap: ");
                    m_otherMappings.insert(endgap);
                    cbOtherMappings += endgap.Size();
                }
            }
        }
    }
    TRACE("OtherMappings: %06llx\n", cbOtherMappings / PAGE_SIZE);
}

void CrashInfo::VisitModule(MachOModule& module)
{
    AddModuleInfo(false, module.BaseAddress(), nullptr, module.Name());

    // Get the process name from the executable module file type
    if (m_name.empty() && module.Header().filetype == MH_EXECUTE)
    {
        m_name = GetFileName(module.Name());
    }
    // Save the runtime module path
    if (m_coreclrPath.empty())
    {
        size_t last = module.Name().rfind(DIRECTORY_SEPARATOR_STR_A MAKEDLLNAME_A("coreclr"));
        if (last != std::string::npos)
        {
            m_coreclrPath = module.Name().substr(0, last + 1);
            m_runtimeBaseAddress = module.BaseAddress();

            uint64_t symbolOffset;
            if (!module.TryLookupSymbol(DACCESS_TABLE_SYMBOL, &symbolOffset))
            {
                TRACE("TryLookupSymbol(" DACCESS_TABLE_SYMBOL ") FAILED\n");
            }
        }
        else if (m_appModel == AppModelType::SingleFile)
        {
            uint64_t symbolOffset;
            if (module.TryLookupSymbol("DotNetRuntimeInfo", &symbolOffset))
            {
                m_coreclrPath = GetDirectory(module.Name());
                m_runtimeBaseAddress = module.BaseAddress();

                RuntimeInfo runtimeInfo { };
                if (ReadMemory(module.BaseAddress() + symbolOffset, &runtimeInfo, sizeof(RuntimeInfo)))
                {
                    if (strcmp(runtimeInfo.Signature, RUNTIME_INFO_SIGNATURE) == 0)
                    {
                        TRACE("Found valid single-file runtime info\n");
                    }
                }
            }
        }
        else if (m_appModel == AppModelType::NativeAOT)
        {
            uint64_t symbolOffset;
            if (module.TryLookupSymbol("DotNetRuntimeDebugHeader", &symbolOffset))
            {
                m_coreclrPath = GetDirectory(module.Name());
                m_runtimeBaseAddress = module.BaseAddress();

                uint8_t cookie[sizeof(g_debugHeaderCookie)];
                if (ReadMemory(module.BaseAddress() + symbolOffset, cookie, sizeof(cookie)))
                {
                    if (memcmp(cookie, g_debugHeaderCookie, sizeof(g_debugHeaderCookie)) == 0)
                    {
                        TRACE("Found valid NativeAOT runtime module\n");
                    }
                }
            }
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

            // Add this module segment to the set used by the thread unwinding to lookup the module base address for an ip.
            AddModuleAddressRange(start, end, module.BaseAddress());

            // Round to page boundary
            start = start & PAGE_MASK;
            assert(start > 0);

            // Round up to page boundary
            end = (end + (PAGE_SIZE - 1)) & PAGE_MASK;
            assert(end > 0);

            // Add module memory region if not already on the list
            ModuleRegion newModule(regionFlags, start, end, offset, module.Name());
            std::set<ModuleRegion>::iterator existingModule = m_moduleMappings.find(newModule);
            if (existingModule == m_moduleMappings.end())
            {
                if (g_diagnosticsVerbose)
                {
                    newModule.Trace("VisitSegment: ");
                }
                // Add this module segment to the module mappings list
                m_moduleMappings.insert(newModule);
                m_cbModuleMappings += newModule.Size();
            }
            else
            {
                // Skip the new module region if it is fully contained in an existing module region
                if (!existingModule->Contains(newModule))
                {
                    if (g_diagnosticsVerbose)
                    {
                        newModule.Trace("VisitSegment: ");
                        existingModule->Trace(" overlapping: ");
                    }
                    uint64_t numberPages = newModule.SizeInPages();
                    for (size_t p = 0; p < numberPages; p++, start += PAGE_SIZE, offset += PAGE_SIZE)
                    {
                        ModuleRegion gap(newModule.Flags(), start, start + PAGE_SIZE, offset, newModule.FileName());

                        const auto& found = m_moduleMappings.find(gap);
                        if (found != m_moduleMappings.end())
                        {
                            if (g_diagnosticsVerbose)
                            {
                                gap.Trace("VisitSegment: *");
                            }
                            m_moduleMappings.insert(gap);
                            m_cbModuleMappings += gap.Size();
                        }
                    }
                }
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
    assert(start == CONVERT_FROM_SIGN_EXTENDED(start));

    MemoryRegion search(0, start, start + PAGE_SIZE, 0);
    const MemoryRegion* region = SearchMemoryRegions(m_allMemoryRegions, search);
    if (region != nullptr) {
        return region->Flags();
    }
    TRACE_VERBOSE("GetMemoryRegionFlags: %016llx FAILED\n", start);
    return PF_R | PF_W | PF_X;
}

//
// Read raw memory
//
bool
CrashInfo::ReadProcessMemory(uint64_t address, void* buffer, size_t size, size_t* read)
{
    assert(buffer != nullptr);
    assert(read != nullptr);

    // vm_read_overwrite usually requires that the address be page-aligned
    // and the size be a multiple of the page size.  We can't differentiate
    // between the cases in which that's required and those in which it
    // isn't, so we do it all the time.
    vm_address_t addressAligned = (vm_address_t)address & ~(PAGE_SIZE - 1);
    ssize_t offset = (ssize_t)address & (PAGE_SIZE - 1);
    char *data = (char*)alloca(PAGE_SIZE);
    ssize_t numberOfBytesRead = 0;
    ssize_t bytesLeft = size;

    while (bytesLeft > 0)
    {
        vm_size_t bytesRead = PAGE_SIZE;
        kern_return_t result = ::vm_read_overwrite(Task(), addressAligned, PAGE_SIZE, (vm_address_t)data, &bytesRead);
        if (result != KERN_SUCCESS || bytesRead != PAGE_SIZE)
        {
            g_readProcessMemoryResult = result;
            TRACE_VERBOSE("ReadProcessMemory(%p %d): vm_read_overwrite failed bytesLeft %d bytesRead %d from %p: %s (%x)\n",
                (void*)address, size, bytesLeft, bytesRead, (void*)addressAligned, mach_error_string(result), result);
            break;
        }
        ssize_t bytesToCopy = PAGE_SIZE - offset;
        if (bytesToCopy > bytesLeft)
        {
            bytesToCopy = bytesLeft;
        }
        memcpy((LPSTR)buffer + numberOfBytesRead, data + offset, bytesToCopy);
        addressAligned = addressAligned + PAGE_SIZE;
        numberOfBytesRead += bytesToCopy;
        bytesLeft -= bytesToCopy;
        offset = 0;
    }
    *read = numberOfBytesRead;
    return size == 0 || numberOfBytesRead > 0;
}

const struct dyld_all_image_infos* g_image_infos = nullptr;

void
ModuleInfo::LoadModule()
{
    if (m_module == nullptr)
    {
        m_module = dlopen(m_moduleName.c_str(), RTLD_LAZY);
        if (m_module != nullptr)
        {
            if (g_image_infos == nullptr)
            {
                struct task_dyld_info dyld_info;
                mach_msg_type_number_t count = TASK_DYLD_INFO_COUNT;
                kern_return_t result = task_info(mach_task_self_, TASK_DYLD_INFO, (task_info_t)&dyld_info, &count);
                if (result == KERN_SUCCESS)
                {
                    g_image_infos = (const struct dyld_all_image_infos*)dyld_info.all_image_info_addr;
                }
                else
                {
                    TRACE("LoadModule: task_info(self) FAILED %x %s\n", result, mach_error_string(result));
                }
            }
            if (g_image_infos != nullptr)
            {
                for (int i = 0; i < g_image_infos->infoArrayCount; ++i)
                {
                    const struct dyld_image_info* image = g_image_infos->infoArray + i;
                    if (strcasecmp(image->imageFilePath, m_moduleName.c_str()) == 0)
                    {
                        m_localBaseAddress = (uint64_t)image->imageLoadAddress;
                        break;
                    }
                }
                if (m_localBaseAddress == 0)
                {
                    TRACE("LoadModule: local base address not found for %s\n", m_moduleName.c_str());
                }
            }
        }
        else
        {
            TRACE("LoadModule: dlopen(%s) FAILED %s\n", m_moduleName.c_str(), dlerror());
        }
    }
}
