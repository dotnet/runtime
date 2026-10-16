// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdumpcore.h"

int g_readProcessMemoryErrno = 0;

//
// Suspends all the threads and creating a list of them. Should be the before gathering any info about the process.
//
bool ProcessInfo::EnumerateAndSuspendThreads()
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
            // Reference: http://stackoverflow.com/questions/18577956/how-to-use-ptrace-to-get-a-consistent-view-of-multiple-threads
            if (ptrace(PTRACE_ATTACH, tid, nullptr, nullptr) != -1)
            {
                int waitStatus;
                waitpid(tid, &waitStatus, __WALL);
            }
            else
            {
                printf_error("Problem suspending thread: ptrace(ATTACH, %d) FAILED %s (%d)\n", tid, strerror(errno), errno);
                // If the ptrace on a thread that has already terminated, skip/ignore
                if (errno == ESRCH && tid != CrashThread())
                {
                    continue;
                }
                closedir(taskDir);
                return false;
            }
            // Add to the list of threads
            ThreadSnapshot thread(tid);
            if (!m_threads.Add(thread))
            {
                ptrace(PTRACE_DETACH, tid, nullptr, nullptr);
                closedir(taskDir);
                return false;
            }
        }
    }

    closedir(taskDir);
    return true;
}

//
// Get the auxv entries to use and add to the core dump
//
bool ProcessInfo::GetAuxvEntries()
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
        if (!m_auxvEntries.Add(auxvEntry))
        {
            close(fd);
            return false;
        }
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

void ProcessInfo::CalculateRuntimeBaseAddress()
{
    uint64_t programHeadersStartAddress = m_auxvValues[AT_PHDR];
    uint64_t programHeaderCount = m_auxvValues[AT_PHNUM];
    uint64_t programHeaderSize = m_auxvValues[AT_PHENT];
    if (programHeadersStartAddress == 0 || programHeaderCount == 0 || programHeaderSize != sizeof(elf_program_header) ||
        programHeaderCount > (UINT64_MAX - programHeadersStartAddress) / programHeaderSize)
    {
        return;
    }

    for (uint64_t index = 0; index < programHeaderCount; index++)
    {
        elf_program_header programHeader;
        size_t read = 0;
        if (!ReadProcessMemory(programHeadersStartAddress + (index * programHeaderSize), &programHeader, sizeof(programHeader), &read) ||
            read != sizeof(programHeader))
        {
            return;
        }

        if (programHeader.p_type == PT_PHDR && programHeadersStartAddress >= programHeader.p_offset)
        {
            // AT_PHDR points to PT_PHDR.p_offset in the mapped ELF image.
            m_runtimeBaseAddress = programHeadersStartAddress - programHeader.p_offset;
            TRACE("Runtime base address: %" PRIA PRIx64 "\n", m_runtimeBaseAddress);
            return;
        }
    }
}

bool HasDeletedSuffix(const char* fileName)
{
    if (fileName == nullptr)
        return false;
    size_t len = strlen(fileName);
    if (len < 10) // " (deleted)" is 10 characters
        return false;
    return strcmp(fileName + len - 10, " (deleted)") == 0;
}

static void TraceModuleRegion(
    const ModuleRegion* mapping,
    const char* prefix = "",
    const char* suffix = "")
{
    mapping->MemoryRegion::Trace(prefix, suffix);
}

bool ProcessInfo::EnumerateMemoryRegions(DumpRegionStore& regionStore)
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
    ssize_t read;
    uint64_t cbModuleMappings = 0;

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
            bool includeInNtFile = (moduleName != nullptr && *moduleName == '/') && !HasDeletedSuffix(moduleName);
            if (includeInNtFile)
            {
                ModuleRegion moduleRegion(regionFlags, start, end, offset);
                moduleRegion.TakeFileNameOwnership(moduleName);
                moduleName = nullptr;

                if (!m_moduleMappings.Add(Move(moduleRegion)))
                {
                    free(permissions);
                    free(line);
                    fclose(mapsFile);
                    return false;
                }
                cbModuleMappings += moduleRegion.Size();
            }
            else if (!m_otherMappings.Add(MemoryRegion(regionFlags, start, end, offset)))
            {
                free(moduleName);
                free(permissions);
                free(line);
                fclose(mapsFile);
                return false;
            }

            if (linuxGateAddress != nullptr && reinterpret_cast<void*>(start) == linuxGateAddress)
            {
                MemoryRegion memoryRegion(regionFlags, start, end);
                if (InsertMemoryRegion(regionStore, memoryRegion) < 0)
                {
                    free(permissions);
                    free(line);
                    fclose(mapsFile);
                    return false;
                }
            }
        }
        free(moduleName);
        free(permissions);
    }

    if (g_diagnostics)
    {
        TRACE("Module mappings (%06" PRIx64 "):\n", cbModuleMappings / PAGE_SIZE);
        for (const ModuleRegion& mapping : m_moduleMappings)
        {
            mapping.Trace();
        }
        TRACE("Other mappings:\n");
        for (const MemoryRegion& mapping : m_otherMappings)
        {
            mapping.Trace();
        }
    }

    free(line); // We didn't allocate line, but as per contract of getline we should free it
    fclose(mapsFile);

    return true;
}

//
// Read raw memory
//
bool ProcessInfo::ReadProcessMemory(uint64_t address, void* buffer, size_t size, size_t* read)
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
#ifdef TARGET_ARM64
        // Android's heap allocator (scudo) uses ARM64 Top-Byte Ignore (TBI) for memory tagging.
        // pread on /proc/<pid>/mem treats the offset as a file position, not a virtual address,
        // so the kernel does not apply TBI — tagged pointers cause EINVAL.
        // See https://www.kernel.org/doc/html/latest/arch/arm64/tagged-address-abi.html
        //
        // Currently only Android allocators set a non-zero top byte, so on other ARM64 Linux
        // configurations this is a no-op. However, any future use of TBI tagging (e.g., ARM MTE)
        // on other Linux distros would hit the same issue.
        address &= 0x00FFFFFFFFFFFFFFULL;
#endif
        *read = pread(m_fdMem, buffer, size, (off_t)address);
    }

    if (*read == (size_t)-1)
    {
        // Preserve errno for the ELF dump writer call
        g_readProcessMemoryErrno = errno;
        TRACE_VERBOSE("ReadProcessMemory FAILED addr: %" PRIA PRIx64 " size: %zu error: %s (%d)\n", address, size, strerror(g_readProcessMemoryErrno), g_readProcessMemoryErrno);
        return false;
    }
    return true;
}