// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "createdump.h"

CrashInfo::CrashInfo(pid_t pid, ICLRDataTarget* dataTarget, bool sos) :
    m_ref(1),
    m_pid(pid),
    m_ppid(-1),
    m_name(nullptr),
    m_sos(sos),
    m_dataTarget(dataTarget)
{
    dataTarget->AddRef();
    m_auxvValues.fill(0);
}

CrashInfo::~CrashInfo()
{
    if (m_name != nullptr)
    {
        free(m_name);
    }
    // Clean up the threads
    for (ThreadInfo* thread : m_threads)
    {
        delete thread;
    }
    m_threads.clear();

    // Module and other mappings have a file name to clean up.
    for (const MemoryRegion& region : m_moduleMappings)
    {
        const_cast<MemoryRegion&>(region).Cleanup();
    }
    m_moduleMappings.clear();
    for (const MemoryRegion& region : m_otherMappings)
    {
        const_cast<MemoryRegion&>(region).Cleanup();
    }
    m_otherMappings.clear();
    m_dataTarget->Release();
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
        *Interface = NULL;
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
    InsertMemoryRegion(address, size);
    return S_OK;
}

bool 
CrashInfo::EnumerateAndSuspendThreads()
{
    char taskPath[128];
    snprintf(taskPath, sizeof(taskPath), "/proc/%d/task", m_pid);

    DIR* taskDir = opendir(taskPath);
    if (taskDir == nullptr)
    {
        fprintf(stderr, "opendir(%s) FAILED %s\n", taskPath, strerror(errno));
        return false;
    }

    struct dirent* entry;
    while ((entry = readdir(taskDir)) != nullptr)
    {
        pid_t tid = static_cast<pid_t>(strtol(entry->d_name, nullptr, 10));
        if (tid != 0)
        {
            // Don't suspend the threads if running under sos
            if (!m_sos)
            {
                //  Reference: http://stackoverflow.com/questions/18577956/how-to-use-ptrace-to-get-a-consistent-view-of-multiple-threads
                if (ptrace(PTRACE_ATTACH, tid, nullptr, nullptr) != -1)
                {
                    int waitStatus;
                    waitpid(tid, &waitStatus, __WALL);
                }
                else
                {
                    fprintf(stderr, "ptrace(ATTACH, %d) FAILED %s\n", tid, strerror(errno));
                    closedir(taskDir);
                    return false;
                }
            }
            // Add to the list of threads
            ThreadInfo* thread = new ThreadInfo(tid);
            m_threads.push_back(thread);
        }
    }

    closedir(taskDir);
    return true;
}

bool
CrashInfo::GatherCrashInfo(const char* programPath, MINIDUMP_TYPE minidumpType)
{
    // Get the process info
    if (!GetStatus(m_pid, &m_ppid, &m_tgid, &m_name))
    {
        return false;
    }
    // Get the info about the threads (registers, etc.)
    for (ThreadInfo* thread : m_threads)
    {
        if (!thread->Initialize(m_sos ? m_dataTarget : nullptr))
        {
            return false;
        }
    }
    // Get the auxv data
    if (!GetAuxvEntries())
    {
        return false;
    }
    // Get shared module debug info
    if (!GetDSOInfo())
    {
        return false;
    }
    // Gather all the module memory mappings (from /dev/$pid/maps)
    if (!EnumerateModuleMappings())
    {
        return false;
    }
    // Gather all the useful memory regions from the DAC
    if (!EnumerateMemoryRegionsWithDAC(programPath, minidumpType))
    {
        return false;
    }
    // Add the thread's stack and some code memory to core
    for (ThreadInfo* thread : m_threads)
    {
        uint64_t start;
        size_t size;

        // Add the thread's stack and some of the code 
        thread->GetThreadStack(*this, &start, &size); 
        InsertMemoryRegion(start, size);

        thread->GetThreadCode(&start, &size);
        InsertMemoryRegion(start, size);
    }
    // Join all adjacent memory regions
    CombineMemoryRegions();
    return true;
}

void
CrashInfo::ResumeThreads()
{
    if (!m_sos)
    {
        for (ThreadInfo* thread : m_threads)
        {
            thread->ResumeThread();
        }
    }
}

bool 
CrashInfo::GetAuxvEntries()
{
    char auxvPath[128];
    snprintf(auxvPath, sizeof(auxvPath), "/proc/%d/auxv", m_pid);

    int fd = open(auxvPath, O_RDONLY, 0);
    if (fd == -1)
    {
        fprintf(stderr, "open(%s) FAILED %s\n", auxvPath, strerror(errno));
        return false;
    }
    bool result = false;
    elf_aux_entry auxvEntry;

    while (read(fd, &auxvEntry, sizeof(elf_aux_entry)) == sizeof(elf_aux_entry)) 
    {
        m_auxvEntries.push_back(auxvEntry);
        if (auxvEntry.a_type == AT_NULL) 
        {
            break;
        }
        if (auxvEntry.a_type < AT_MAX) 
        {
            m_auxvValues[auxvEntry.a_type] = auxvEntry.a_un.a_val;
            TRACE("AUXV: %lu = %016lx\n", auxvEntry.a_type, auxvEntry.a_un.a_val);
            result = true;
        }
    }

    close(fd);
    return result;
}

bool
CrashInfo::EnumerateModuleMappings()
{
    // Here we read /proc/<pid>/maps file in order to parse it and figure out what it says 
    // about a library we are looking for. This file looks something like this:
    //
    // [address]      [perms] [offset] [dev] [inode]     [pathname] - HEADER is not preset in an actual file
    //
    // 35b1800000-35b1820000 r-xp 00000000 08:02 135522  /usr/lib64/ld-2.15.so
    // 35b1a1f000-35b1a20000 r--p 0001f000 08:02 135522  /usr/lib64/ld-2.15.so
    // 35b1a20000-35b1a21000 rw-p 00020000 08:02 135522  /usr/lib64/ld-2.15.so
    // 35b1a21000-35b1a22000 rw-p 00000000 00:00 0       [heap]
    // 35b1c00000-35b1dac000 r-xp 00000000 08:02 135870  /usr/lib64/libc-2.15.so
    // 35b1dac000-35b1fac000 ---p 001ac000 08:02 135870  /usr/lib64/libc-2.15.so
    // 35b1fac000-35b1fb0000 r--p 001ac000 08:02 135870  /usr/lib64/libc-2.15.so
    // 35b1fb0000-35b1fb2000 rw-p 001b0000 08:02 135870  /usr/lib64/libc-2.15.so
    char* line = NULL;
    size_t lineLen = 0;
    int count = 0;
    ssize_t read;

    // Making something like: /proc/123/maps
    char mapPath[128];
    int chars = snprintf(mapPath, sizeof(mapPath), "/proc/%d/maps", m_pid);
    assert(chars > 0 && chars <= sizeof(mapPath));

    FILE* mapsFile = fopen(mapPath, "r");
    if (mapsFile == NULL)
    {
        fprintf(stderr, "fopen(%s) FAILED %s\n", mapPath, strerror(errno));
        return false;
    }
    // linuxGateAddress is the beginning of the kernel's mapping of
    // linux-gate.so in the process.  It doesn't actually show up in the
    // maps list as a filename, but it can be found using the AT_SYSINFO_EHDR
    // aux vector entry, which gives the information necessary to special
    // case its entry when creating the list of mappings.
    // See http://www.trilithium.com/johan/2005/08/linux-gate/ for more
    // information.
    const void* linuxGateAddress = (const void*)m_auxvValues[AT_SYSINFO_EHDR];

    // Reading maps file line by line 
    while ((read = getline(&line, &lineLen, mapsFile)) != -1)
    {
        uint64_t start, end, offset;
        char* permissions = nullptr;
        char* moduleName = nullptr;

        int c = 0;
        if ((c = sscanf(line, "%lx-%lx %m[-rwxsp] %lx %*[:0-9a-f] %*d %ms\n", &start, &end, &permissions, &offset, &moduleName)) == 5)
        {
            if (linuxGateAddress != nullptr && reinterpret_cast<void*>(start) == linuxGateAddress)
            {
                InsertMemoryRegion(start, end - start);
                free(moduleName);
            }
            else {
                uint32_t permissionFlags = 0;
                if (strchr(permissions, 'r')) {
                    permissionFlags |= PF_R;
                }
                if (strchr(permissions, 'w')) {
                    permissionFlags |= PF_W;
                }
                if (strchr(permissions, 'x')) {
                    permissionFlags |= PF_X;
                }
                MemoryRegion memoryRegion(permissionFlags, start, end, offset, moduleName);

                if (moduleName != nullptr && *moduleName == '/') {
                    m_moduleMappings.insert(memoryRegion);
                }
                else {
                    m_otherMappings.insert(memoryRegion);
                }
            }
            free(permissions);
        }
    }

    if (g_diagnostics)
    {
        TRACE("Module mappings:\n");
        for (const MemoryRegion& region : m_moduleMappings)
        {
            region.Print();
        }
        TRACE("Other mappings:\n");
        for (const MemoryRegion& region : m_otherMappings)
        {
            region.Print();
        }
    }

    free(line); // We didn't allocate line, but as per contract of getline we should free it
    fclose(mapsFile);

    return true;
}

bool
CrashInfo::EnumerateMemoryRegionsWithDAC(const char* programPath, MINIDUMP_TYPE minidumpType)
{
    PFN_CLRDataCreateInstance pfnCLRDataCreateInstance = nullptr;
    ICLRDataEnumMemoryRegions *clrDataEnumRegions = nullptr;
    HMODULE hdac = nullptr;
    HRESULT hr = S_OK;
    bool result = false;

    // We assume that the DAC is in the same location as this createdump exe
    std::string dacPath;
    dacPath.append(programPath);
    dacPath.append("/");
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
    hr = pfnCLRDataCreateInstance(__uuidof(ICLRDataEnumMemoryRegions), m_dataTarget, (void**)&clrDataEnumRegions);
    if (FAILED(hr))
    {
        fprintf(stderr, "CLRDataCreateInstance(ICLRDataEnumMemoryRegions) FAILED %08x\n", hr);
        goto exit;
    }
    // Calls CrashInfo::EnumMemoryRegion for each memory region found by the DAC
    hr = clrDataEnumRegions->EnumMemoryRegions(this, minidumpType, CLRDATA_ENUM_MEM_DEFAULT);
    if (FAILED(hr))
    {
        fprintf(stderr, "EnumMemoryRegions FAILED %08x\n", hr);
        goto exit;
    }
    result = true;
exit:
    if (clrDataEnumRegions != nullptr)
    {
        clrDataEnumRegions->Release();
    }
    if (hdac != nullptr)
    {
        FreeLibrary(hdac);
    }
    return result;
}

bool
CrashInfo::GetDSOInfo()
{
    Phdr* phdrAddr = reinterpret_cast<Phdr*>(m_auxvValues[AT_PHDR]);
    int phnum = m_auxvValues[AT_PHNUM];
    assert(m_auxvValues[AT_PHENT] == sizeof(Phdr));

    if (phnum <= 0 || phdrAddr == nullptr) {
        return false;
    }
    TRACE("DSO: phdr %p phnum %d\n", phdrAddr, phnum);

    // Search for the program PT_DYNAMIC header 
    ElfW(Dyn)* dynamicAddr = nullptr;
    for (int i = 0; i < phnum; i++, phdrAddr++)
    {
        Phdr ph;
        if (!ReadMemory(phdrAddr, &ph, sizeof(ph))) {
            return false;
        }
        TRACE("DSO: phdr %p type %d (%x) vaddr %016lx memsz %016lx offset %016lx\n", 
            phdrAddr, ph.p_type, ph.p_type, ph.p_vaddr, ph.p_memsz, ph.p_offset);

        if (ph.p_type == PT_DYNAMIC) 
        {
            dynamicAddr = reinterpret_cast<ElfW(Dyn)*>(ph.p_vaddr);
        }
        else if (ph.p_type == PT_GNU_EH_FRAME)
        {
            if (ph.p_vaddr != 0 && ph.p_memsz != 0)
            {
                InsertMemoryRegion(ph.p_vaddr, ph.p_memsz);
            }
        }
    }

    if (dynamicAddr == nullptr) {
        return false;
    }

    // Search for dynamic debug (DT_DEBUG) entry
    struct r_debug* rdebugAddr = nullptr;
    for (;;) {
        ElfW(Dyn) dyn;
        if (!ReadMemory(dynamicAddr, &dyn, sizeof(dyn))) {
            return false;
        }
        TRACE("DSO: dyn %p tag %ld (%lx) d_ptr %016lx\n", dynamicAddr, dyn.d_tag, dyn.d_tag, dyn.d_un.d_ptr);
        if (dyn.d_tag == DT_DEBUG) {
            rdebugAddr = reinterpret_cast<struct r_debug*>(dyn.d_un.d_ptr);
        }
        else if (dyn.d_tag == DT_NULL) {
            break;
        }
        dynamicAddr++;
    }

    // Add the DSO r_debug entry
    TRACE("DSO: rdebugAddr %p\n", rdebugAddr);
    struct r_debug debugEntry;
    if (!ReadMemory(rdebugAddr, &debugEntry, sizeof(debugEntry))) {
        return false;
    }

    // Add the DSO link_map entries
    for (struct link_map* linkMapAddr = debugEntry.r_map; linkMapAddr != nullptr;) {
        struct link_map map;
        if (!ReadMemory(linkMapAddr, &map, sizeof(map))) {
            return false;
        }
        char moduleName[257] = { 0 };
        if (map.l_name != nullptr) {
            if (!ReadMemory(map.l_name, &moduleName, sizeof(moduleName) - 1)) {
                return false;
            }
        }
        TRACE("DSO: link_map entry %p l_ld %p l_addr %lx %s\n", linkMapAddr, map.l_ld, map.l_addr, moduleName);
        linkMapAddr = map.l_next;
    }

    return true;
}

//
// ReadMemory from target and add to memory regions list
//
bool
CrashInfo::ReadMemory(void* address, void* buffer, size_t size)
{
    uint32_t read = 0;
    if (FAILED(m_dataTarget->ReadVirtual(reinterpret_cast<CLRDATA_ADDRESS>(address), reinterpret_cast<PBYTE>(buffer), size, &read)))
    {
        return false;
    }
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
    // Round to page boundary
    uint64_t start = address & PAGE_MASK;
    assert(start > 0);

    // Round up to page boundary
    uint64_t end = ((address + size) + (PAGE_SIZE - 1)) & PAGE_MASK;
    assert(end > 0);

    MemoryRegion memoryRegionFull(start, end);

    // First check if the full memory region can be added without conflicts
    const auto& found = m_memoryRegions.find(memoryRegionFull);
    if (found == m_memoryRegions.end())
    {
        // Add full memory region
        m_memoryRegions.insert(memoryRegionFull);
    }
    else
    {
        // The memory region is not wholely contained in region found
        if (!found->Contains(memoryRegionFull))
        {
            // The region overlaps/conflicts with one already in the set so 
            // add one page at a time to avoid the overlapping pages.
            uint64_t numberPages = (end - start) >> PAGE_SHIFT;

            for (int p = 0; p < numberPages; p++, start += PAGE_SIZE)
            {
                MemoryRegion memoryRegion(start, start + PAGE_SIZE);

                const auto& found = m_memoryRegions.find(memoryRegion);
                if (found == m_memoryRegions.end())
                {
                    m_memoryRegions.insert(memoryRegion);
                }
            }
        }
    }
}

//
// Combine any adjacent memory regions into one
//
void
CrashInfo::CombineMemoryRegions()
{
    assert(!m_memoryRegions.empty());

    std::set<MemoryRegion> memoryRegionsNew;

    uint64_t start = m_memoryRegions.begin()->StartAddress();
    uint64_t end = start;

    for (const MemoryRegion& region : m_memoryRegions)
    {
        if (end == region.StartAddress())
        {
            end = region.EndAddress();
        }
        else
        {
            MemoryRegion memoryRegion(start, end);
            assert(memoryRegionsNew.find(memoryRegion) == memoryRegionsNew.end());
            memoryRegionsNew.insert(memoryRegion);

            start = region.StartAddress();
            end = region.EndAddress();
        }
    }

    assert(start != end);
    MemoryRegion memoryRegion(start, end);
    assert(memoryRegionsNew.find(memoryRegion) == memoryRegionsNew.end());
    memoryRegionsNew.insert(memoryRegion);

    m_memoryRegions = memoryRegionsNew;

    if (g_diagnostics)
    {
        TRACE("Memory Regions:\n");
        for (const MemoryRegion& region : m_memoryRegions)
        {
            region.Print();
        }
    }
}

bool
CrashInfo::GetStatus(pid_t pid, pid_t* ppid, pid_t* tgid, char** name)
{
    char statusPath[128];
    snprintf(statusPath, sizeof(statusPath), "/proc/%d/status", pid);

    FILE *statusFile = fopen(statusPath, "r");
    if (statusFile == nullptr)
    {
        fprintf(stderr, "GetStatus fopen(%s) FAILED\n", statusPath);
        return false;
    }

    *ppid = -1;

    char *line = nullptr;
    size_t lineLen = 0;
    ssize_t read;
    while ((read = getline(&line, &lineLen, statusFile)) != -1)
    {
        if (strncmp("PPid:\t", line, 6) == 0)
        {
            *ppid = _atoi64(line + 6);
        }
        else if (strncmp("Tgid:\t", line, 6) == 0)
        {
            *tgid = _atoi64(line + 6);
        }
        else if (strncmp("Name:\t", line, 6) == 0)
        {
            if (name != nullptr)
            {
                char* n = strchr(line + 6, '\n');
                if (n != nullptr) 
                {
                    *n = '\0';
                }
                *name = strdup(line + 6);
            }
        }
    }

    free(line);
    fclose(statusFile);
    return true;
}
