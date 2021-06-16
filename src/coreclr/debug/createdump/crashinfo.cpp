// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"

// This is for the PAL_VirtualUnwindOutOfProc read memory adapter.
CrashInfo* g_crashInfo;

CrashInfo::CrashInfo(pid_t pid) :
    m_ref(1),
    m_pid(pid),
    m_ppid(-1)
{
    g_crashInfo = this;
#ifdef __APPLE__
    m_task = 0;
#else
    m_auxvValues.fill(0);
    m_fd = -1;
#endif
}

CrashInfo::~CrashInfo()
{
    // Clean up the threads
    for (ThreadInfo* thread : m_threads)
    {
        delete thread;
    }
    m_threads.clear();
#ifdef __APPLE__
    if (m_task != 0)
    {
        kern_return_t result = ::mach_port_deallocate(mach_task_self(), m_task);
        if (result != KERN_SUCCESS)
        {
            fprintf(stderr, "~CrashInfo: mach_port_deallocate FAILED %x %s\n", result, mach_error_string(result));
        }
    }
#endif
}

STDMETHODIMP
CrashInfo::QueryInterface(
    ___in REFIID InterfaceId,
    ___out PVOID* Interface)
{
    if (InterfaceId == IID_IUnknown ||
        InterfaceId == IID_ICLRDataEnumMemoryRegionsCallback)
    {
        *Interface = (ICLRDataEnumMemoryRegionsCallback*)this;
        AddRef();
        return S_OK;
    }
    else
    {
        *Interface = nullptr;
        return E_NOINTERFACE;
    }
}

STDMETHODIMP_(ULONG)
CrashInfo::AddRef()
{
    LONG ref = InterlockedIncrement(&m_ref);
    return ref;
}

STDMETHODIMP_(ULONG)
CrashInfo::Release()
{
    LONG ref = InterlockedDecrement(&m_ref);
    if (ref == 0)
    {
        delete this;
    }
    return ref;
}

HRESULT STDMETHODCALLTYPE
CrashInfo::EnumMemoryRegion(
    /* [in] */ CLRDATA_ADDRESS address,
    /* [in] */ ULONG32 size)
{
    InsertMemoryRegion((ULONG_PTR)address, size);
    return S_OK;
}

//
// Gather all the necessary crash dump info.
//
bool
CrashInfo::GatherCrashInfo(MINIDUMP_TYPE minidumpType)
{
    // Get the info about the threads (registers, etc.)
    for (ThreadInfo* thread : m_threads)
    {
        if (!thread->Initialize())
        {
            return false;
        }
    }
#ifdef __APPLE__
    if (!EnumerateMemoryRegions())
    {
        return false;
    }
#else
    // Get the auxv data
    if (!GetAuxvEntries())
    {
        return false;
    }
    // Gather all the module memory mappings (from /dev/$pid/maps)
    if (!EnumerateModuleMappings())
    {
        return false;
    }
    // Get shared module debug info
    if (!GetDSOInfo())
    {
        return false;
    }
#endif
    if (g_diagnosticsVerbose)
    {
        TRACE_VERBOSE("Module addresses:\n");
        for (const MemoryRegion& region : m_moduleAddresses)
        {
            region.Trace();
        }
    }
    // If full memory dump, include everything regardless of permissions
    if (minidumpType & MiniDumpWithFullMemory)
    {
        for (const MemoryRegion& region : m_moduleMappings)
        {
            InsertMemoryBackedRegion(region);
        }
        for (const MemoryRegion& region : m_otherMappings)
        {
            // Don't add uncommitted pages to the full dump
            if ((region.Permissions() & (PF_R | PF_W | PF_X)) != 0)
            {
                InsertMemoryBackedRegion(region);
            }
        }
    }
    else
    {
        // Add all the heap read/write memory regions (m_otherMappings contains the heaps). On Alpine
        // the heap regions are marked RWX instead of just RW.
        if (minidumpType & MiniDumpWithPrivateReadWriteMemory)
        {
            for (const MemoryRegion& region : m_otherMappings)
            {
                uint32_t permissions = region.Permissions();
                if (permissions == (PF_R | PF_W) || permissions == (PF_R | PF_W | PF_X))
                {
                    InsertMemoryBackedRegion(region);
                }
            }
        }
        // Add the thread's stack and some code memory to core
        for (ThreadInfo* thread : m_threads)
        {
            // Add the thread's stack
            thread->GetThreadStack();
        }
    }
    // Gather all the useful memory regions from the DAC
    if (!EnumerateMemoryRegionsWithDAC(minidumpType))
    {
        return false;
    }
    // Join all adjacent memory regions
    CombineMemoryRegions();
    return true;
}

//
// Enumerate all the memory regions using the DAC memory region support given a minidump type
//
bool
CrashInfo::EnumerateMemoryRegionsWithDAC(MINIDUMP_TYPE minidumpType)
{
    ReleaseHolder<DumpDataTarget> dataTarget = new DumpDataTarget(*this);
    PFN_CLRDataCreateInstance pfnCLRDataCreateInstance = nullptr;
    ICLRDataEnumMemoryRegions* pClrDataEnumRegions = nullptr;
    IXCLRDataProcess* pClrDataProcess = nullptr;
    HMODULE hdac = nullptr;
    HRESULT hr = S_OK;
    bool result = false;

    if (!m_coreclrPath.empty())
    {
        TRACE("EnumerateMemoryRegionsWithDAC: Memory enumeration STARTED\n");

        // We assume that the DAC is in the same location as the libcoreclr.so module
        std::string dacPath;
        dacPath.append(m_coreclrPath);
        dacPath.append(MAKEDLLNAME_A("mscordaccore"));

        // Load and initialize the DAC
        hdac = LoadLibraryA(dacPath.c_str());
        if (hdac == nullptr)
        {
            fprintf(stderr, "LoadLibraryA(%s) FAILED %d\n", dacPath.c_str(), GetLastError());
            goto exit;
        }
        pfnCLRDataCreateInstance = (PFN_CLRDataCreateInstance)GetProcAddress(hdac, "CLRDataCreateInstance");
        if (pfnCLRDataCreateInstance == nullptr)
        {
            fprintf(stderr, "GetProcAddress(CLRDataCreateInstance) FAILED %d\n", GetLastError());
            goto exit;
        }
        if ((minidumpType & MiniDumpWithFullMemory) == 0)
        {
            hr = pfnCLRDataCreateInstance(__uuidof(ICLRDataEnumMemoryRegions), dataTarget, (void**)&pClrDataEnumRegions);
            if (FAILED(hr))
            {
                fprintf(stderr, "CLRDataCreateInstance(ICLRDataEnumMemoryRegions) FAILED %08x\n", hr);
                goto exit;
            }
            // Calls CrashInfo::EnumMemoryRegion for each memory region found by the DAC
            hr = pClrDataEnumRegions->EnumMemoryRegions(this, minidumpType, CLRDATA_ENUM_MEM_DEFAULT);
            if (FAILED(hr))
            {
                fprintf(stderr, "EnumMemoryRegions FAILED %08x\n", hr);
                goto exit;
            }
        }
        hr = pfnCLRDataCreateInstance(__uuidof(IXCLRDataProcess), dataTarget, (void**)&pClrDataProcess);
        if (FAILED(hr))
        {
            fprintf(stderr, "CLRDataCreateInstance(IXCLRDataProcess) FAILED %08x\n", hr);
            goto exit;
        }
        TRACE("EnumerateMemoryRegionsWithDAC: Memory enumeration FINISHED\n");
        if (!EnumerateManagedModules(pClrDataProcess))
        {
            goto exit;
        }
    }
    else {
        TRACE("EnumerateMemoryRegionsWithDAC: coreclr not found; not using DAC\n");
    }
    if (!UnwindAllThreads(pClrDataProcess))
    {
        goto exit;
    }
    result = true;
exit:
    if (pClrDataEnumRegions != nullptr)
    {
        pClrDataEnumRegions->Release();
    }
    if (pClrDataProcess != nullptr)
    {
        pClrDataProcess->Release();
    }
    if (hdac != nullptr)
    {
        FreeLibrary(hdac);
    }
    return result;
}

//
// Enumerate all the managed modules and replace the module mapping with the module name found.
//
bool
CrashInfo::EnumerateManagedModules(IXCLRDataProcess* pClrDataProcess)
{
    CLRDATA_ENUM enumModules = 0;
    bool result = true;
    HRESULT hr = S_OK;

    if (FAILED(hr = pClrDataProcess->StartEnumModules(&enumModules))) {
        fprintf(stderr, "StartEnumModules FAILED %08x\n", hr);
        return false;
    }

    while (true)
    {
        ReleaseHolder<IXCLRDataModule> pClrDataModule;
        if ((hr = pClrDataProcess->EnumModule(&enumModules, &pClrDataModule)) != S_OK) {
            break;
        }

        // Skip any dynamic modules. The Request call below on some DACs crashes on dynamic modules.
        ULONG32 flags;
        if ((hr = pClrDataModule->GetFlags(&flags)) != S_OK) {
            TRACE("MODULE: GetFlags FAILED %08x\n", hr);
            continue;
        }
        if (flags & CLRDATA_MODULE_IS_DYNAMIC) {
            TRACE("MODULE: Skipping dynamic module\n");
            continue;
        }

        DacpGetModuleData moduleData;
        if (SUCCEEDED(hr = moduleData.Request(pClrDataModule.GetPtr())))
        {
            TRACE("MODULE: %" PRIA PRIx64 " dyn %d inmem %d file %d pe %" PRIA PRIx64 " pdb %" PRIA PRIx64, (uint64_t)moduleData.LoadedPEAddress, moduleData.IsDynamic,
                moduleData.IsInMemory, moduleData.IsFileLayout, (uint64_t)moduleData.PEFile, (uint64_t)moduleData.InMemoryPdbAddress);

            if (!moduleData.IsDynamic && moduleData.LoadedPEAddress != 0)
            {
                ArrayHolder<WCHAR> wszUnicodeName = new (std::nothrow) WCHAR[MAX_LONGPATH + 1];
                if (wszUnicodeName == nullptr)
                {
                    fprintf(stderr, "Allocating unicode module name FAILED\n");
                    result = false;
                    break;
                }
                if (SUCCEEDED(hr = pClrDataModule->GetFileName(MAX_LONGPATH, nullptr, wszUnicodeName)))
                {
                    ArrayHolder<char> pszName = new (std::nothrow) char[MAX_LONGPATH + 1];
                    if (pszName == nullptr)
                    {
                        fprintf(stderr, "Allocating ascii module name FAILED\n");
                        result = false;
                        break;
                    }
                    sprintf_s(pszName.GetPtr(), MAX_LONGPATH, "%S", (WCHAR*)wszUnicodeName);
                    TRACE(" %s\n", pszName.GetPtr());

                    // Change the module mapping name
                    ReplaceModuleMapping(moduleData.LoadedPEAddress, moduleData.LoadedPESize, std::string(pszName.GetPtr()));
                }
                else {
                    TRACE("\nModule.GetFileName FAILED %08x\n", hr);
                }
            }
            else {
                TRACE("\n");
            }
        }
        else {
            TRACE("moduleData.Request FAILED %08x\n", hr);
        }
    }

    if (enumModules != 0) {
        pClrDataProcess->EndEnumModules(enumModules);
    }

    return result;
}

//
// Unwind all the native threads to ensure that the dwarf unwind info is added to the core dump.
//
bool
CrashInfo::UnwindAllThreads(IXCLRDataProcess* pClrDataProcess)
{
    // For each native and managed thread
    for (ThreadInfo* thread : m_threads)
    {
        if (!thread->UnwindThread(pClrDataProcess)) {
            return false;
        }
    }
    return true;
}

//
// Replace an existing module mapping with one with a different name.
//
void
CrashInfo::ReplaceModuleMapping(CLRDATA_ADDRESS baseAddress, ULONG64 size, const std::string& pszName)
{
    uint64_t start = (uint64_t)baseAddress;
    uint64_t end = ((baseAddress + size) + (PAGE_SIZE - 1)) & PAGE_MASK;
    uint32_t flags = GetMemoryRegionFlags(start);

    // Make sure that the page containing the PE header for the managed asseblies is in the dump
    // especially on MacOS where they are added artificially.
    MemoryRegion header(flags | MEMORY_REGION_FLAG_MEMORY_BACKED, start, start + PAGE_SIZE);
    InsertMemoryRegion(header);

    // Add or change the module mapping for this PE image. The managed assembly images may already
    // be in the module mappings list but they may not have the full assembly name (like in .NET 2.0
    // they have the name "/dev/zero"). On MacOS, the managed assembly modules have not been added.
    const auto& found = m_moduleMappings.find(header);
    if (found == m_moduleMappings.end())
    {
        // On MacOS the assemblies are always added.
        MemoryRegion newRegion(flags, start, end, 0, pszName);
        m_moduleMappings.insert(newRegion);

        if (g_diagnostics) {
            TRACE("MODULE: ADD ");
            newRegion.Trace();
        }
    }
    else if (found->FileName().compare(pszName) != 0)
    {
        // Create the new memory region with the managed assembly name.
        MemoryRegion newRegion(*found, pszName);

        // Remove and cleanup the old one
        m_moduleMappings.erase(found);

        // Add the new memory region
        m_moduleMappings.insert(newRegion);

        if (g_diagnostics) {
            TRACE("MODULE: REPLACE ");
            newRegion.Trace();
        }
    }
}

//
// Returns the module base address for the IP or 0. Used by the thread unwind code.
//
uint64_t CrashInfo::GetBaseAddress(uint64_t ip)
{
    MemoryRegion search(0, ip, ip, 0);
    const MemoryRegion* found = SearchMemoryRegions(m_moduleAddresses, search);
    if (found == nullptr) {
        return 0;
    }
    // The memory region Offset() is the base address of the module
    return found->Offset();
}

//
// ReadMemory from target and add to memory regions list
//
bool
CrashInfo::ReadMemory(void* address, void* buffer, size_t size)
{
    size_t read = 0;
    if (!ReadProcessMemory(address, buffer, size, &read))
    {
        return false;
    }
    assert(read == size);
    InsertMemoryRegion(reinterpret_cast<uint64_t>(address), size);
    return true;
}

//
// Add this memory chunk to the list of regions to be
// written to the core dump.
//
void
CrashInfo::InsertMemoryRegion(uint64_t address, size_t size)
{
    assert(size < UINT_MAX);

    // Round to page boundary
    uint64_t start = address & PAGE_MASK;
    assert(start > 0);

    // Round up to page boundary
    uint64_t end = ((address + size) + (PAGE_SIZE - 1)) & PAGE_MASK;
    assert(end > 0);

    InsertMemoryRegion(MemoryRegion(GetMemoryRegionFlags(start) | MEMORY_REGION_FLAG_MEMORY_BACKED, start, end));
}

//
// Adds a memory backed flagged copy of the memory region. The file name is not preserved.
//
void
CrashInfo::InsertMemoryBackedRegion(const MemoryRegion& region)
{
    InsertMemoryRegion(MemoryRegion(region, region.Flags() | MEMORY_REGION_FLAG_MEMORY_BACKED));
}

//
// Add a memory region to the list
//
void
CrashInfo::InsertMemoryRegion(const MemoryRegion& region)
{
    // First check if the full memory region can be added without conflicts and is fully valid.
    const auto& found = m_memoryRegions.find(region);
    if (found == m_memoryRegions.end())
    {
        // If the region is valid, add the full memory region
        if (ValidRegion(region)) {
            m_memoryRegions.insert(region);
            return;
        }
    }
    else
    {
        // If the memory region is wholly contained in region found and both have the
        // same backed by memory state, we're done.
        if (found->Contains(region) && (found->IsBackedByMemory() == region.IsBackedByMemory())) {
            return;
        }
    }
    // Either part of the region was invalid, part of it hasn't been added or the backed
    // by memory state is different.
    uint64_t start = region.StartAddress();

    // The region overlaps/conflicts with one already in the set so add one page at a
    // time to avoid the overlapping pages.
    uint64_t numberPages = region.Size() / PAGE_SIZE;

    for (size_t p = 0; p < numberPages; p++, start += PAGE_SIZE)
    {
        MemoryRegion memoryRegionPage(region.Flags(), start, start + PAGE_SIZE);

        const auto& found = m_memoryRegions.find(memoryRegionPage);
        if (found == m_memoryRegions.end())
        {
            // All the single pages added here will be combined in CombineMemoryRegions()
            if (ValidRegion(memoryRegionPage)) {
                m_memoryRegions.insert(memoryRegionPage);
            }
        }
        else {
            assert(found->IsBackedByMemory() || !region.IsBackedByMemory());
        }
    }
}

//
// Validates a memory region
//
bool
CrashInfo::ValidRegion(const MemoryRegion& region)
{
    if (region.IsBackedByMemory())
    {
        uint64_t start = region.StartAddress();

        uint64_t numberPages = region.Size() / PAGE_SIZE;
        for (size_t p = 0; p < numberPages; p++, start += PAGE_SIZE)
        {
            BYTE buffer[1];
            size_t read;

            if (!ReadProcessMemory((void*)start, buffer, 1, &read))
            {
                return false;
            }
        }
    }
    return true;
}

//
// Combine any adjacent memory regions into one
//
void
CrashInfo::CombineMemoryRegions()
{
    TRACE("CombineMemoryRegions: STARTED\n");
    assert(!m_memoryRegions.empty());
    std::set<MemoryRegion> memoryRegionsNew;

    // MEMORY_REGION_FLAG_SHARED and MEMORY_REGION_FLAG_PRIVATE are internal flags that
    // don't affect the core dump so ignore them when comparing the flags.
    uint32_t flags = m_memoryRegions.begin()->Flags() & (MEMORY_REGION_FLAG_MEMORY_BACKED | MEMORY_REGION_FLAG_PERMISSIONS_MASK);
    uint64_t start = m_memoryRegions.begin()->StartAddress();
    uint64_t end = start;

    for (const MemoryRegion& region : m_memoryRegions)
    {
        // To combine a region it needs to be contiguous, same permissions and memory backed flag.
        if ((end == region.StartAddress()) &&
            (flags == (region.Flags() & (MEMORY_REGION_FLAG_MEMORY_BACKED | MEMORY_REGION_FLAG_PERMISSIONS_MASK))))
        {
            end = region.EndAddress();
        }
        else
        {
            MemoryRegion memoryRegion(flags, start, end);
            assert(memoryRegionsNew.find(memoryRegion) == memoryRegionsNew.end());
            memoryRegionsNew.insert(memoryRegion);

            flags = region.Flags() & (MEMORY_REGION_FLAG_MEMORY_BACKED | MEMORY_REGION_FLAG_PERMISSIONS_MASK);
            start = region.StartAddress();
            end = region.EndAddress();
        }
    }

    assert(start != end);
    MemoryRegion memoryRegion(flags, start, end);
    assert(memoryRegionsNew.find(memoryRegion) == memoryRegionsNew.end());
    memoryRegionsNew.insert(memoryRegion);

    m_memoryRegions = memoryRegionsNew;

    TRACE("CombineMemoryRegions: FINISHED\n");

    if (g_diagnosticsVerbose)
    {
        TRACE("Memory Regions:\n");
        for (const MemoryRegion& region : m_memoryRegions)
        {
            region.Trace();
        }
    }
}

//
// Searches for a memory region given an address.
//
const MemoryRegion*
CrashInfo::SearchMemoryRegions(const std::set<MemoryRegion>& regions, const MemoryRegion& search)
{
    std::set<MemoryRegion>::iterator found = regions.find(search);
    for (; found != regions.end(); found++)
    {
        if (search.StartAddress() >= found->StartAddress() && search.StartAddress() < found->EndAddress())
        {
            return &*found;
        }
    }
    return nullptr;
}

void
CrashInfo::Trace(const char* format, ...)
{
    if (g_diagnostics)
    {
        va_list args;
        va_start(args, format);
        vfprintf(stdout, format, args);
        fflush(stdout);
        va_end(args);
    }
}

void
CrashInfo::TraceVerbose(const char* format, ...)
{
    if (g_diagnosticsVerbose)
    {
        va_list args;
        va_start(args, format);
        vfprintf(stdout, format, args);
        fflush(stdout);
        va_end(args);
    }
}
