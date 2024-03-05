// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"

#ifndef PT_ARM_EXIDX
#define PT_ARM_EXIDX   0x70000001      /* See llvm ELF.h */
#endif

extern CrashInfo* g_crashInfo;
extern uint8_t g_debugHeaderCookie[4];

int g_readProcessMemoryErrno = 0;

bool GetStatus(pid_t pid, pid_t* ppid, pid_t* tgid, std::string* name);

bool
CrashInfo::Initialize()
{
    char memPath[128];
    int chars = snprintf(memPath, sizeof(memPath), "/proc/%u/mem", m_pid);
    if (chars <= 0 || (size_t)chars >= sizeof(memPath))
    {
        printf_error("snprintf failed building /proc/<pid>/mem name\n");
        return false;
    }

    m_fdMem = open(memPath, O_RDONLY);
    if (m_fdMem == -1)
    {
        int err = errno;
        const char* message = "Problem accessing memory";
        if (err == EPERM || err == EACCES)
        {
            message = "The process or container does not have permissions or access";
        }
        else if (err == ENOENT)
        {
            message = "Invalid process id";
        }
        printf_error("%s: open(%s) FAILED %s (%d)\n", message, memPath, strerror(err), err);
        return false;
    }

    CLRConfigNoCache disablePagemapUse = CLRConfigNoCache::Get("DbgDisablePagemapUse", /*noprefix*/ false, &getenv);
    DWORD val = 0;
    if (disablePagemapUse.IsSet() && disablePagemapUse.TryAsInteger(10, val) && val == 0)
    {
        TRACE("DbgDisablePagemapUse detected - pagemap file checking is enabled\n");
        char pagemapPath[128];
        chars = snprintf(pagemapPath, sizeof(pagemapPath), "/proc/%u/pagemap", m_pid);
        if (chars <= 0 || (size_t)chars >= sizeof(pagemapPath))
        {
            printf_error("snprintf failed building /proc/<pid>/pagemap name\n");
            return false;
        }
        m_fdPagemap = open(pagemapPath, O_RDONLY);
        if (m_fdPagemap == -1)
        {
            TRACE("open(%s) FAILED %d (%s), will fallback to dumping all memory regions without checking if they are committed\n", pagemapPath, errno, strerror(errno));
        }
    }
    else
    {
        m_fdPagemap = -1;
    }

    // Get the process info
    if (!GetStatus(m_pid, &m_ppid, &m_tgid, &m_name))
    {
        return false;
    }

    m_canUseProcVmReadSyscall = true;
    return true;
}

void
CrashInfo::CleanupAndResumeProcess()
{
    // Resume all the threads suspended in EnumerateAndSuspendThreads
    for (ThreadInfo* thread : m_threads)
    {
        if (ptrace(PTRACE_DETACH, thread->Tid(), nullptr, nullptr) != -1)
        {
            int waitStatus;
            waitpid(thread->Tid(), &waitStatus, __WALL);
        }
    }
    if (m_fdMem != -1)
    {
        close(m_fdMem);
        m_fdMem = -1;
    }
    if (m_fdPagemap != -1)
    {
        close(m_fdPagemap);
        m_fdPagemap = -1;
    }
}

//
// Suspends all the threads and creating a list of them. Should be the before gathering any info about the process.
//
bool
CrashInfo::EnumerateAndSuspendThreads()
{
    char taskPath[128];
    int chars = snprintf(taskPath, sizeof(taskPath), "/proc/%u/task", m_pid);
    if (chars <= 0 || (size_t)chars >= sizeof(taskPath))
    {
        printf_error("snprintf failed building /proc/<pid>/task\n");
        return false;
    }

    DIR* taskDir = opendir(taskPath);
    if (taskDir == nullptr)
    {
        printf_error("Problem enumerating threads: opendir(%s) FAILED %s (%d)\n", taskPath, strerror(errno), errno);
        return false;
    }

    struct dirent* entry;
    while ((entry = readdir(taskDir)) != nullptr)
    {
        pid_t tid = static_cast<pid_t>(strtol(entry->d_name, nullptr, 10));
        if (tid != 0)
        {
            //  Reference: http://stackoverflow.com/questions/18577956/how-to-use-ptrace-to-get-a-consistent-view-of-multiple-threads
            if (ptrace(PTRACE_ATTACH, tid, nullptr, nullptr) != -1)
            {
                int waitStatus;
                waitpid(tid, &waitStatus, __WALL);
            }
            else
            {
                printf_error("Problem suspending threads: ptrace(ATTACH, %d) FAILED %s (%d)\n", tid, strerror(errno), errno);
                closedir(taskDir);
                return false;
            }
            // Add to the list of threads
            ThreadInfo* thread = new ThreadInfo(*this, tid);
            m_threads.push_back(thread);
        }
    }

    closedir(taskDir);
    return true;
}

//
// Get the auxv entries to use and add to the core dump
//
bool
CrashInfo::GetAuxvEntries()
{
    char auxvPath[128];
    int chars = snprintf(auxvPath, sizeof(auxvPath), "/proc/%u/auxv", m_pid);
    if (chars <= 0 || (size_t)chars >= sizeof(auxvPath))
    {
        printf_error("snprintf failed building /proc/<pid>/auxv\n");
        return false;
    }
    int fd = open(auxvPath, O_RDONLY, 0);
    if (fd == -1)
    {
        printf_error("Problem reading aux info: open(%s) FAILED %s (%d)\n", auxvPath, strerror(errno), errno);
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
CrashInfo::EnumerateMemoryRegions()
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
    int chars = snprintf(mapPath, sizeof(mapPath), "/proc/%u/maps", m_pid);
    if (chars <= 0 || (size_t)chars >= sizeof(mapPath))
    {
        printf_error("snprintf failed building /proc/<pid>/maps\n");
        return false;
    }
    FILE* mapsFile = fopen(mapPath, "rb");
    if (mapsFile == nullptr)
    {
        printf_error("Problem reading maps file: fopen(%s) FAILED %s (%d)\n", mapPath, strerror(errno), errno);
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

        int c = sscanf(line, "%" PRIx64 "-%" PRIx64 " %m[-rwxsp] %" PRIx64 " %*[:0-9a-f] %*d %m[^\n]\n", &start, &end, &permissions, &offset, &moduleName);
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
            ModuleRegion moduleRegion(regionFlags, start, end, offset, moduleName);

            if (moduleName != nullptr && *moduleName == '/')
            {
                // Don't add files that don't exists anymore especially /memfd:doublemapper.
                size_t last = moduleRegion.FileName().rfind(" (deleted)");
                if (last == std::string::npos)
                {
                    m_moduleMappings.insert(moduleRegion);
                    m_cbModuleMappings += moduleRegion.Size();
                }
                else
                {
                    m_otherMappings.insert(moduleRegion);
                }
            }
            else
            {
                m_otherMappings.insert(moduleRegion);
            }
            if (linuxGateAddress != nullptr && reinterpret_cast<void*>(start) == linuxGateAddress)
            {
                InsertMemoryRegion(moduleRegion);
            }
            free(moduleName);
            free(permissions);
        }
    }

    if (g_diagnostics)
    {
        TRACE("Module mappings (%06llx):\n", m_cbModuleMappings / PAGE_SIZE);
        for (const ModuleRegion& region : m_moduleMappings)
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
    return EnumerateElfInfo(phdrAddr, phnum);
}

//
// Add all the necessary ELF headers to the core dump
//
void
CrashInfo::VisitModule(uint64_t baseAddress, std::string& moduleName)
{
    if (baseAddress == 0 || baseAddress == m_auxvValues[AT_SYSINFO_EHDR]) {
        return;
    }
    // For reasons unknown the main app singlefile module name is empty in the DSO. This replaces
    // it with the one found in the /proc/<pid>/maps.
    if (moduleName.empty())
    {
        ModuleRegion search(0, baseAddress, baseAddress + PAGE_SIZE);
        const ModuleRegion* region = SearchModuleRegions(search);
        if (region != nullptr)
        {
            moduleName = region->FileName();
            TRACE("VisitModule using module name from mappings '%s'\n", moduleName.c_str());
        }
    }
    AddModuleInfo(false, baseAddress, nullptr, moduleName);
    if (m_coreclrPath.empty())
    {
        size_t last = moduleName.rfind(DIRECTORY_SEPARATOR_STR_A MAKEDLLNAME_A("coreclr"));
        if (last != std::string::npos)
        {
            m_coreclrPath = moduleName.substr(0, last + 1);
            m_runtimeBaseAddress = baseAddress;

            // Now populate the elfreader with the runtime module info and
            // lookup the DAC table symbol to ensure that all the memory
            // necessary is in the core dump.
            if (PopulateForSymbolLookup(baseAddress))
            {
                uint64_t symbolOffset;
                if (!TryLookupSymbol(DACCESS_TABLE_SYMBOL, &symbolOffset))
                {
                    TRACE("TryLookupSymbol(" DACCESS_TABLE_SYMBOL ") FAILED\n");
                }
            }
        }
        else if (m_appModel == AppModelType::SingleFile)
        {
            if (PopulateForSymbolLookup(baseAddress))
            {
                uint64_t symbolOffset;
                if (TryLookupSymbol("DotNetRuntimeInfo", &symbolOffset))
                {
                    m_coreclrPath = GetDirectory(moduleName);
                    m_runtimeBaseAddress = baseAddress;

                    // explicit initialization for old gcc support; instead of just runtimeInfo { }
                    RuntimeInfo runtimeInfo { .Signature = { }, .Version = 0, .RuntimeModuleIndex = { }, .DacModuleIndex = { }, .DbiModuleIndex = { }, .RuntimeVersion = { } };
                    if (ReadMemory(baseAddress + symbolOffset, &runtimeInfo, sizeof(RuntimeInfo)))
                    {
                        if (strcmp(runtimeInfo.Signature, RUNTIME_INFO_SIGNATURE) == 0)
                        {
                            TRACE("Found valid single-file runtime info\n");
                        }
                    }
                }
            }
        }
        else if (m_appModel == AppModelType::NativeAOT)
        {
            if (PopulateForSymbolLookup(baseAddress))
            {
                uint64_t symbolOffset;
                if (TryLookupSymbol("DotNetRuntimeDebugHeader", &symbolOffset))
                {
                    m_coreclrPath = GetDirectory(moduleName);
                    m_runtimeBaseAddress = baseAddress;

                    uint8_t cookie[sizeof(g_debugHeaderCookie)];
                    if (ReadMemory(baseAddress + symbolOffset, cookie, sizeof(cookie)))
                    {
                        if (memcmp(cookie, g_debugHeaderCookie, sizeof(g_debugHeaderCookie)) == 0)
                        {
                            TRACE("Found valid NativeAOT runtime module\n");
                        }
                    }
                }
            }
        }
    }
    EnumerateProgramHeaders(baseAddress);
}

// Helper for PAL_GetUnwindInfoSize. Reads memory directly without adding it to the memory region list.
BOOL
ReadMemoryAdapter(PVOID address, PVOID buffer, SIZE_T size)
{
    size_t read = 0;
    return g_crashInfo->ReadProcessMemory(CONVERT_FROM_SIGN_EXTENDED(address), buffer, size, &read);
}

//
// Called for each program header adding the build id note, unwind frame
// region and module addresses to the crash info.
//
void
CrashInfo::VisitProgramHeader(uint64_t loadbias, uint64_t baseAddress, Phdr* phdr)
{
    switch (phdr->p_type)
    {
    case PT_DYNAMIC:
    case PT_NOTE:
#if defined(TARGET_ARM)
    case PT_ARM_EXIDX:
#endif
        if (phdr->p_vaddr != 0 && phdr->p_memsz != 0)
        {
            InsertMemoryRegion(loadbias + phdr->p_vaddr, phdr->p_memsz);
        }
        break;

    case PT_GNU_EH_FRAME:
        if (phdr->p_vaddr != 0 && phdr->p_memsz != 0)
        {
            uint64_t ehFrameHdrStart = loadbias + phdr->p_vaddr;
            uint64_t ehFrameHdrSize = phdr->p_memsz;
            TRACE("VisitProgramHeader: ehFrameHdrStart %016llx ehFrameHdrSize %08llx\n", ehFrameHdrStart, ehFrameHdrSize);
            InsertMemoryRegion(ehFrameHdrStart, ehFrameHdrSize);

            if (m_appModel != AppModelType::NativeAOT)
            {
                ULONG64 ehFrameStart;
                ULONG64 ehFrameSize;
                if (PAL_GetUnwindInfoSize(baseAddress, ehFrameHdrStart, ReadMemoryAdapter, &ehFrameStart, &ehFrameSize))
                {
                    TRACE("VisitProgramHeader: ehFrameStart %016llx ehFrameSize %08llx\n", ehFrameStart, ehFrameSize);
                    if (ehFrameStart != 0 && ehFrameSize != 0)
                    {
                        InsertMemoryRegion(ehFrameStart, ehFrameSize);
                    }
                }
                else
                {
                    TRACE("VisitProgramHeader: PAL_GetUnwindInfoSize FAILED\n");
                }
            }
        }
        break;

    case PT_LOAD:
        AddModuleAddressRange(loadbias + phdr->p_vaddr, loadbias + phdr->p_vaddr + phdr->p_memsz, baseAddress);
        break;
    }
}

//
// Get the memory region flags for a start address
//
uint32_t
CrashInfo::GetMemoryRegionFlags(uint64_t start)
{
    assert(start == CONVERT_FROM_SIGN_EXTENDED(start));

    ModuleRegion search(0, start, start + PAGE_SIZE, 0);
    const ModuleRegion* moduleRegion = SearchModuleRegions(search);
    if (moduleRegion != nullptr) {
        return moduleRegion->Flags();
    }
    const MemoryRegion* region = SearchMemoryRegions(m_otherMappings, search);
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
    *read = 0;

#ifdef HAVE_PROCESS_VM_READV
    if (m_canUseProcVmReadSyscall)
    {
        iovec local{ buffer, size };
        iovec remote{ (void*)address, size };
        *read = process_vm_readv(m_pid, &local, 1, &remote, 1, 0);
    }

    if (!m_canUseProcVmReadSyscall || (*read == (size_t)-1 && (errno == EPERM || errno == ENOSYS)))
#endif
    {
        // If we've failed, avoid going through expensive syscalls
        // After all, the use of process_vm_readv is largely as a
        // performance optimization.
        m_canUseProcVmReadSyscall = false;
        assert(m_fdMem != -1);
        *read = pread(m_fdMem, buffer, size, (off_t)address);
    }

    if (*read == (size_t)-1)
    {
        // Preserve errno for the ELF dump writer call
        g_readProcessMemoryErrno = errno;
        TRACE_VERBOSE("ReadProcessMemory FAILED addr: %" PRIA PRIx " size: %zu error: %s (%d)\n", address, size, strerror(g_readProcessMemoryErrno), g_readProcessMemoryErrno);
        return false;
    }
    return true;
}

//
// Get the process or thread status
//
bool
GetStatus(pid_t pid, pid_t* ppid, pid_t* tgid, std::string* name)
{
    char statusPath[128];
    int chars = snprintf(statusPath, sizeof(statusPath), "/proc/%d/status", pid);
    if (chars <= 0 || (size_t)chars >= sizeof(statusPath))
    {
        printf_error("snprintf failed building /proc/<pid>/status\n");
        return false;
    }

    FILE *statusFile = fopen(statusPath, "rb");
    if (statusFile == nullptr)
    {
        printf_error("GetStatus fopen(%s) FAILED %s (%d)\n", statusPath, strerror(errno), errno);
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
                *name = line + 6;
            }
        }
    }

    free(line);
    fclose(statusFile);
    return true;
}

void
ModuleInfo::LoadModule()
{
    if (m_module == nullptr)
    {
        m_module = dlopen(m_moduleName.c_str(), RTLD_LAZY);
        if (m_module != nullptr)
        {
            m_localBaseAddress = ((struct link_map*)m_module)->l_addr;
        }
        else
        {
            TRACE("LoadModule: dlopen(%s) FAILED %s\n", m_moduleName.c_str(), dlerror());
        }
    }
}
