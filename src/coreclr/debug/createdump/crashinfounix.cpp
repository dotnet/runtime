// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"

bool GetStatus(pid_t pid, pid_t* ppid, pid_t* tgid, std::string* name);

bool
CrashInfo::Initialize()
{
    char memPath[128];
    _snprintf_s(memPath, sizeof(memPath), sizeof(memPath), "/proc/%lu/mem", m_pid);

    m_fd = open(memPath, O_RDONLY);
    if (m_fd == -1)
    {
        fprintf(stderr, "open(%s) FAILED %d (%s)\n", memPath, errno, strerror(errno));
        return false;
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
    if (m_fd != -1)
    {
        close(m_fd);
        m_fd = -1;
    }
}

//
// Suspends all the threads and creating a list of them. Should be the before gathering any info about the process.
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
            MemoryRegion memoryRegion(regionFlags, start, end, offset, std::string(moduleName != nullptr ? moduleName : ""));

            if (moduleName != nullptr && *moduleName == '/')
            {
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
            free(moduleName);
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
    AddModuleInfo(false, baseAddress, nullptr, moduleName);
    if (m_coreclrPath.empty())
    {
        size_t last = moduleName.rfind(DIRECTORY_SEPARATOR_STR_A MAKEDLLNAME_A("coreclr"));
        if (last != std::string::npos) {
            m_coreclrPath = moduleName.substr(0, last + 1);

            // Now populate the elfreader with the runtime module info and
            // lookup the DAC table symbol to ensure that all the memory
            // necessary is in the core dump.
            if (PopulateForSymbolLookup(baseAddress)) {
                uint64_t symbolOffset;
                if (!TryLookupSymbol("g_dacTable", &symbolOffset))
                {
                    TRACE("TryLookupSymbol(g_dacTable) FAILED\n");
                }
            }
        }
    }
    EnumerateProgramHeaders(baseAddress);
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
    case PT_GNU_EH_FRAME:
        if (phdr->p_vaddr != 0 && phdr->p_memsz != 0) {
            InsertMemoryRegion(loadbias + phdr->p_vaddr, phdr->p_memsz);
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
// Read raw memory
//
bool
CrashInfo::ReadProcessMemory(void* address, void* buffer, size_t size, size_t* read)
{
    assert(buffer != nullptr);
    assert(read != nullptr);
    *read = 0;

#ifdef HAVE_PROCESS_VM_READV
    if (m_canUseProcVmReadSyscall)
    {
        iovec local{ buffer, size };
        iovec remote{ address, size };
        *read = process_vm_readv(m_pid, &local, 1, &remote, 1, 0);
    }

    if (!m_canUseProcVmReadSyscall || (*read == (size_t)-1 && errno == EPERM))
#endif
    {
        // If we've failed, avoid going through expensive syscalls
        // After all, the use of process_vm_readv is largely as a
        // performance optimization.
        m_canUseProcVmReadSyscall = false;
        assert(m_fd != -1);
        *read = pread64(m_fd, buffer, size, (off64_t)address);
    }

    if (*read == (size_t)-1)
    {
        int readErrno = errno;
        TRACE_VERBOSE("ReadProcessMemory FAILED, addr: %" PRIA PRIx ", size: %zu, ERRNO %d: %s\n", address, size, readErrno, strerror(readErrno));
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
                *name = line + 6;
            }
        }
    }

    free(line);
    fclose(statusFile);
    return true;
}
