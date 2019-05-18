// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "createdump.h"

// This is for the PAL_VirtualUnwindOutOfProc read memory adapter.
CrashInfo* g_crashInfo;

CrashInfo::CrashInfo(pid_t pid, ICLRDataTarget* dataTarget, bool sos) :
    m_ref(1),
    m_pid(pid),
    m_ppid(-1),
    m_name(nullptr),
    m_sos(sos),
    m_dataTarget(dataTarget)
{
    g_crashInfo = this;
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
// Suspends all the threads and creating a list of them. Should be the first before
// gather any info about the process.
//
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

//
// Gather all the necessary crash dump info.
//
bool
CrashInfo::GatherCrashInfo(MINIDUMP_TYPE minidumpType)
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

    for (const MemoryRegion& region : m_moduleAddresses)
    {
        region.Trace();
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
    // Add all the heap read/write memory regions (m_otherMappings contains the heaps). On Alpine
    // the heap regions are marked RWX instead of just RW.
    else if (minidumpType & MiniDumpWithPrivateReadWriteMemory)
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
    // Gather all the useful memory regions from the DAC
    if (!EnumerateMemoryRegionsWithDAC(minidumpType))
    {
        return false;
    }
    if ((minidumpType & MiniDumpWithFullMemory) == 0)
    {
        // Add the thread's stack and some code memory to core
        for (ThreadInfo* thread : m_threads)
        {
            // Add the thread's stack
            thread->GetThreadStack(*this);
        }
        // All the regions added so far has been backed by memory. Now add the rest of
        // mappings so the debuggers like lldb see that an address is code (PF_X) even
        // if it isn't actually in the core dump.
        for (const MemoryRegion& region : m_moduleMappings)
        {
            assert(!region.IsBackedByMemory());
            InsertMemoryRegion(region);
        }
        for (const MemoryRegion& region : m_otherMappings)
        {
            assert(!region.IsBackedByMemory());
            InsertMemoryRegion(region);
        }
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

//
// Get the auxv entries to use and add to the core dump
//
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
            TRACE("AUXV: %" PRIu " = %" PRIxA "\n", auxvEntry.a_type, auxvEntry.a_un.a_val);
            result = true;
        }
    }

    close(fd);
    return result;
}

//
// Get the module mappings for the core dump NT_FILE notes
//
bool
CrashInfo::EnumerateModuleMappings()
{
    // Here we read /proc/<pid>/maps file in order to parse it and figure out what it says
    // about a library we are looking for. This file looks something like this:
    //
    // [address]          [perms] [offset] [dev] [inode] [pathname] - HEADER is not preset in an actual file
    //
    // 35b1800000-35b1820000 r-xp 00000000 08:02 135522  /usr/lib64/ld-2.15.so
    // 35b1a1f000-35b1a20000 r--p 0001f000 08:02 135522  /usr/lib64/ld-2.15.so
    // 35b1a20000-35b1a21000 rw-p 00020000 08:02 135522  /usr/lib64/ld-2.15.so
    // 35b1a21000-35b1a22000 rw-p 00000000 00:00 0       [heap]
    // 35b1c00000-35b1dac000 r-xp 00000000 08:02 135870  /usr/lib64/libc-2.15.so
    // 35b1dac000-35b1fac000 ---p 001ac000 08:02 135870  /usr/lib64/libc-2.15.so
    // 35b1fac000-35b1fb0000 r--p 001ac000 08:02 135870  /usr/lib64/libc-2.15.so
    // 35b1fb0000-35b1fb2000 rw-p 001b0000 08:02 135870  /usr/lib64/libc-2.15.so
    char* line = nullptr;
    size_t lineLen = 0;
    int count = 0;
    ssize_t read;

    // Making something like: /proc/123/maps
    char mapPath[128];
    int chars = snprintf(mapPath, sizeof(mapPath), "/proc/%d/maps", m_pid);
    assert(chars > 0 && (size_t)chars <= sizeof(mapPath));

    FILE* mapsFile = fopen(mapPath, "r");
    if (mapsFile == nullptr)
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

        int c = sscanf(line, "%" PRIx64 "-%" PRIx64 " %m[-rwxsp] %" PRIx64 " %*[:0-9a-f] %*d %ms\n", &start, &end, &permissions, &offset, &moduleName);
        if (c == 4 || c == 5)
        {
            // r = read
            // w = write
            // x = execute
            // s = shared
            // p = private (copy on write)
            uint32_t regionFlags = 0;
            if (strchr(permissions, 'r')) {
                regionFlags |= PF_R;
            }
            if (strchr(permissions, 'w')) {
                regionFlags |= PF_W;
            }
            if (strchr(permissions, 'x')) {
                regionFlags |= PF_X;
            }
            if (strchr(permissions, 's')) {
                regionFlags |= MEMORY_REGION_FLAG_SHARED;
            }
            if (strchr(permissions, 'p')) {
                regionFlags |= MEMORY_REGION_FLAG_PRIVATE;
            }
            MemoryRegion memoryRegion(regionFlags, start, end, offset, moduleName);

            if (moduleName != nullptr && *moduleName == '/')
            {
                if (m_coreclrPath.empty())
                {
                    std::string coreclrPath;
                    coreclrPath.append(moduleName);
                    size_t last = coreclrPath.rfind(MAKEDLLNAME_A("coreclr"));
                    if (last != std::string::npos) {
                        m_coreclrPath = coreclrPath.substr(0, last);
                    }
                }
                m_moduleMappings.insert(memoryRegion);
            }
            else
            {
                m_otherMappings.insert(memoryRegion);
            }
            if (linuxGateAddress != nullptr && reinterpret_cast<void*>(start) == linuxGateAddress)
            {
                InsertMemoryBackedRegion(memoryRegion);
            }
            free(permissions);
        }
    }

    if (g_diagnostics)
    {
        TRACE("Module mappings:\n");
        for (const MemoryRegion& region : m_moduleMappings)
        {
            region.Trace();
        }
        TRACE("Other mappings:\n");
        for (const MemoryRegion& region : m_otherMappings)
        {
            region.Trace();
        }
    }

    free(line); // We didn't allocate line, but as per contract of getline we should free it
    fclose(mapsFile);

    return true;
}

//
// All the shared (native) module info to the core dump
//
bool
CrashInfo::GetDSOInfo()
{
    Phdr* phdrAddr = reinterpret_cast<Phdr*>(m_auxvValues[AT_PHDR]);
    int phnum = m_auxvValues[AT_PHNUM];
    assert(m_auxvValues[AT_PHENT] == sizeof(Phdr));
    assert(phnum != PN_XNUM);

    if (phnum <= 0 || phdrAddr == nullptr) {
        return false;
    }
    uint64_t baseAddress = (uint64_t)phdrAddr - sizeof(Ehdr);
    ElfW(Dyn)* dynamicAddr = nullptr;

    TRACE("DSO: base %" PRIA PRIx64 " phdr %p phnum %d\n", baseAddress, phdrAddr, phnum);

    // Enumerate program headers searching for the PT_DYNAMIC header, etc.
    if (!EnumerateProgramHeaders(phdrAddr, phnum, baseAddress, &dynamicAddr))
    {
        return false;
    }
    if (dynamicAddr == nullptr) {
        return false;
    }

    // Search for dynamic debug (DT_DEBUG) entry
    struct r_debug* rdebugAddr = nullptr;
    for (;;) {
        ElfW(Dyn) dyn;
        if (!ReadMemory(dynamicAddr, &dyn, sizeof(dyn))) {
            fprintf(stderr, "ReadMemory(%p, %" PRIx ") dyn FAILED\n", dynamicAddr, sizeof(dyn));
            return false;
        }
        TRACE("DSO: dyn %p tag %" PRId " (%" PRIx ") d_ptr %" PRIxA "\n", dynamicAddr, dyn.d_tag, dyn.d_tag, dyn.d_un.d_ptr);
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
        fprintf(stderr, "ReadMemory(%p, %" PRIx ") r_debug FAILED\n", rdebugAddr, sizeof(debugEntry));
        return false;
    }

    // Add the DSO link_map entries
    ArrayHolder<char> moduleName = new char[PATH_MAX];
    for (struct link_map* linkMapAddr = debugEntry.r_map; linkMapAddr != nullptr;) {
        struct link_map map;
        if (!ReadMemory(linkMapAddr, &map, sizeof(map))) {
            fprintf(stderr, "ReadMemory(%p, %" PRIx ") link_map FAILED\n", linkMapAddr, sizeof(map));
            return false;
        }
        // Read the module's name and make sure the memory is added to the core dump
        int i = 0;
        if (map.l_name != nullptr) {
            for (; i < PATH_MAX; i++)
            {
                if (!ReadMemory(map.l_name + i, &moduleName[i], 1)) {
                    TRACE("DSO: ReadMemory link_map name %p + %d FAILED\n", map.l_name, i);
                    break;
                }
                if (moduleName[i] == '\0') {
                    break;
                }
            }
        }
        moduleName[i] = '\0';
        TRACE("\nDSO: link_map entry %p l_ld %p l_addr (Ehdr) %" PRIx " %s\n", linkMapAddr, map.l_ld, map.l_addr, (char*)moduleName);

        // Read the ELF header and info adding it to the core dump
        if (!GetELFInfo(map.l_addr)) {
            return false;
        }
        linkMapAddr = map.l_next;
    }

    return true;
}

//
// Add all the necessary ELF headers to the core dump
//
bool
CrashInfo::GetELFInfo(uint64_t baseAddress)
{
    if (baseAddress == 0 || baseAddress == m_auxvValues[AT_SYSINFO_EHDR] || baseAddress == m_auxvValues[AT_BASE]) {
        return true;
    }
    Ehdr ehdr;
    if (!ReadMemory((void*)baseAddress, &ehdr, sizeof(ehdr))) {
        TRACE("ReadMemory(%p, %" PRIx ") ehdr FAILED\n", (void*)baseAddress, sizeof(ehdr));
        return true;
    }
    int phnum = ehdr.e_phnum;
    assert(phnum != PN_XNUM);
    assert(ehdr.e_phentsize == sizeof(Phdr));
#ifdef BIT64
    assert(ehdr.e_ident[EI_CLASS] == ELFCLASS64);
#else
    assert(ehdr.e_ident[EI_CLASS] == ELFCLASS32);
#endif
    assert(ehdr.e_ident[EI_DATA] == ELFDATA2LSB);

    TRACE("ELF: type %d mach 0x%x ver %d flags 0x%x phnum %d phoff %" PRIxA " phentsize 0x%02x shnum %d shoff %" PRIxA " shentsize 0x%02x shstrndx %d\n",
        ehdr.e_type, ehdr.e_machine, ehdr.e_version, ehdr.e_flags, phnum, ehdr.e_phoff, ehdr.e_phentsize, ehdr.e_shnum, ehdr.e_shoff, ehdr.e_shentsize, ehdr.e_shstrndx);

    if (ehdr.e_phoff != 0 && phnum > 0)
    {
        Phdr* phdrAddr = reinterpret_cast<Phdr*>(baseAddress + ehdr.e_phoff);

        if (!EnumerateProgramHeaders(phdrAddr, phnum, baseAddress, nullptr))
        {
            return false;
        }
    }

    return true;
}

//
// Enumerate the program headers adding the build id note, unwind frame
// region and module addresses to the crash info.
//
bool
CrashInfo::EnumerateProgramHeaders(Phdr* phdrAddr, int phnum, uint64_t baseAddress, ElfW(Dyn)** pdynamicAddr)
{
    uint64_t loadbias = baseAddress;

    for (int i = 0; i < phnum; i++)
    {
        Phdr ph;
        if (!ReadMemory(phdrAddr + i, &ph, sizeof(ph))) {
            fprintf(stderr, "ReadMemory(%p, %" PRIx ") phdr FAILED\n", phdrAddr + i, sizeof(ph));
            return false;
        }
        if (ph.p_type == PT_LOAD && ph.p_offset == 0)
        {
            loadbias -= ph.p_vaddr;
            TRACE("PHDR: loadbias %" PRIA PRIx64 "\n", loadbias);
            break;
        }
    }

    for (int i = 0; i < phnum; i++)
    {
        Phdr ph;
        if (!ReadMemory(phdrAddr + i, &ph, sizeof(ph))) {
            fprintf(stderr, "ReadMemory(%p, %" PRIx ") phdr FAILED\n", phdrAddr + i, sizeof(ph));
            return false;
        }
        TRACE("PHDR: %p type %d (%x) vaddr %" PRIxA " memsz %" PRIxA " paddr %" PRIxA " filesz %" PRIxA " offset %" PRIxA " align %" PRIxA "\n",
            phdrAddr + i, ph.p_type, ph.p_type, ph.p_vaddr, ph.p_memsz, ph.p_paddr, ph.p_filesz, ph.p_offset, ph.p_align);

        switch (ph.p_type)
        {
        case PT_DYNAMIC:
            if (pdynamicAddr != nullptr)
            {
                *pdynamicAddr = reinterpret_cast<ElfW(Dyn)*>(loadbias + ph.p_vaddr);
                break;
            }
            // fall into InsertMemoryRegion

        case PT_NOTE:
        case PT_GNU_EH_FRAME:
            if (ph.p_vaddr != 0 && ph.p_memsz != 0) {
                InsertMemoryRegion(loadbias + ph.p_vaddr, ph.p_memsz);
            }
            break;

        case PT_LOAD:
            MemoryRegion region(0, loadbias + ph.p_vaddr, loadbias + ph.p_vaddr + ph.p_memsz, baseAddress);
            m_moduleAddresses.insert(region);
            break;
        }
    }

    return true;
}

//
// Enumerate all the memory regions using the DAC memory region support given a minidump type
//
bool
CrashInfo::EnumerateMemoryRegionsWithDAC(MINIDUMP_TYPE minidumpType)
{
    PFN_CLRDataCreateInstance pfnCLRDataCreateInstance = nullptr;
    ICLRDataEnumMemoryRegions* pClrDataEnumRegions = nullptr;
    IXCLRDataProcess* pClrDataProcess = nullptr;
    HMODULE hdac = nullptr;
    HRESULT hr = S_OK;
    bool result = false;

    if (!m_coreclrPath.empty())
    {
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
            hr = pfnCLRDataCreateInstance(__uuidof(ICLRDataEnumMemoryRegions), m_dataTarget, (void**)&pClrDataEnumRegions);
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
        hr = pfnCLRDataCreateInstance(__uuidof(IXCLRDataProcess), m_dataTarget, (void**)&pClrDataProcess);
        if (FAILED(hr))
        {
            fprintf(stderr, "CLRDataCreateInstance(IXCLRDataProcess) FAILED %08x\n", hr);
            goto exit;
        }
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
            TRACE("MODULE: %" PRIA PRIx64 " dyn %d inmem %d file %d pe %" PRIA PRIx64 " pdb %" PRIA PRIx64, moduleData.LoadedPEAddress, moduleData.IsDynamic,
                moduleData.IsInMemory, moduleData.IsFileLayout, moduleData.PEFile, moduleData.InMemoryPdbAddress);

            if (!moduleData.IsDynamic && moduleData.LoadedPEAddress != 0)
            {
                ArrayHolder<WCHAR> wszUnicodeName = new WCHAR[MAX_LONGPATH + 1];
                if (SUCCEEDED(hr = pClrDataModule->GetFileName(MAX_LONGPATH, nullptr, wszUnicodeName)))
                {
                    // If the module file name isn't empty
                    if (wszUnicodeName[0] != 0) {
                        char* pszName = (char*)malloc(MAX_LONGPATH + 1);
                        if (pszName == nullptr) {
                            fprintf(stderr, "Allocating module name FAILED\n");
                            result = false;
                            break;
                        }
                        sprintf_s(pszName, MAX_LONGPATH, "%S", (WCHAR*)wszUnicodeName);
                        TRACE(" %s\n", pszName);

                        // Change the module mapping name
                        ReplaceModuleMapping(moduleData.LoadedPEAddress, pszName);
                    }
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
        if (!thread->UnwindThread(*this, pClrDataProcess)) {
            return false;
        }
    }
    return true;
}

//
// Replace an existing module mapping with one with a different name.
//
void
CrashInfo::ReplaceModuleMapping(CLRDATA_ADDRESS baseAddress, const char* pszName)
{
    // Add or change the module mapping for this PE image. The managed assembly images are
    // already in the module mappings list but in .NET 2.0 they have the name "/dev/zero".
    MemoryRegion region(PF_R | PF_W | PF_X, (ULONG_PTR)baseAddress, (ULONG_PTR)(baseAddress + PAGE_SIZE), 0, pszName);
    const auto& found = m_moduleMappings.find(region);
    if (found == m_moduleMappings.end())
    {
        m_moduleMappings.insert(region);

        if (g_diagnostics) {
            TRACE("MODULE: ADD ");
            region.Trace();
        }
    }
    else
    {
        // Create the new memory region with the managed assembly name.
        MemoryRegion newRegion(*found, pszName);

        // Remove and cleanup the old one
        m_moduleMappings.erase(found);
        const_cast<MemoryRegion&>(*found).Cleanup();

        // Add the new memory region
        m_moduleMappings.insert(newRegion);

        if (g_diagnostics) {
            TRACE("MODULE: REPLACE ");
            newRegion.Trace();
        }
    }
}

//
// Returns the module base address for the IP or 0.
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
// Get the memory region flags for a start address
//
uint32_t
CrashInfo::GetMemoryRegionFlags(uint64_t start)
{
    MemoryRegion search(0, start, start + PAGE_SIZE);
    const MemoryRegion* region = SearchMemoryRegions(m_moduleMappings, search);
    if (region != nullptr) {
        return region->Flags();
    }
    region = SearchMemoryRegions(m_otherMappings, search);
    if (region != nullptr) {
        return region->Flags();
    }
    TRACE("GetMemoryRegionFlags: FAILED\n");
    return PF_R | PF_W | PF_X;
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
            uint32_t read;

            if (FAILED(m_dataTarget->ReadVirtual(start, buffer, 1, &read)))
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

    if (g_diagnostics)
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

//
// Get the process or thread status
//
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
            *ppid = atoll(line + 6);
        }
        else if (strncmp("Tgid:\t", line, 6) == 0)
        {
            *tgid = atoll(line + 6);
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
