// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Minimal ELF core dump writer for NativeAOT.
// Produces a standard ELF core dump that tools like gdb and lldb can read.
//
// ELF core layout:
//   ELF Header
//   Program Headers: [PT_NOTE] [PT_LOAD x N]
//   Note Section: NT_PRPSINFO, NT_AUXV, NT_FILE, per-thread (NT_PRSTATUS, NT_FPREGSET, NT_SIGINFO)
//   Memory Region Contents (4KB aligned)
//
// Region filtering (heap mode): file-backed read-only regions from shared
// libraries are excluded because debuggers can reconstruct them from the
// original files using the NT_FILE note. This significantly reduces dump
// size (e.g., libicudata.so alone is ~30MB).

#include "elf_dump_writer.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <errno.h>
#include <unistd.h>
#include <fcntl.h>
#include <sys/stat.h>
#include <elf.h>
#include <sys/procfs.h>

// Verify that our GP register type matches the size expected by elf_prstatus.
// A mismatch would cause a buffer overflow in the memcpy into pr_reg.
_Static_assert(sizeof(gp_regs_t) <= sizeof(((struct elf_prstatus*)0)->pr_reg),
               "GP register struct must not be larger than elf_prstatus pr_reg");

#define DUMP_PAGE_SIZE 4096
#define ALIGN_UP(val, align) (((val) + (align) - 1) & ~((align) - 1))

// Determines how many bytes of a memory region to include in the dump.
//
// Full mode (DbgMiniDumpType=4): includes all readable memory.
//
// Heap mode (default, types 0-3): excludes shared library code/rodata since
// debuggers load those from disk via NT_FILE. Includes: anonymous memory
// (heap, stack, GC heaps), writable regions, the main executable's regions
// (including RELRO pages with r_debug), and the first page of each shared
// library (ELF header for identification).
static uint64_t GetDumpSize(const MemRegion* region, const ProcessInfo* info, bool fullDump)
{
    uint64_t regionSize = region->endAddress - region->startAddress;

    if ((region->permissions & PF_R) == 0)
    {
        return 0;
    }

    // In full mode, include all readable memory.
    if (fullDump)
    {
        return regionSize;
    }

    // Always include the full content of writable regions.
    if ((region->permissions & PF_W) != 0)
    {
        return regionSize;
    }

    // Include anonymous read-only regions (no file path) and special mappings.
    if (region->fileName[0] == '\0' ||
        region->fileName[0] == '[')
    {
        return regionSize;
    }

    // Include all regions from the main executable. These contain RELRO pages
    // with .dynamic/.got.plt that the dynamic linker patches at startup then
    // mprotect's read-only. GDB reads r_debug from .dynamic to discover
    // shared libraries. The main exe is typically small (<5MB total).
    if (info->exePath[0] != '\0' && strcmp(region->fileName, info->exePath) == 0)
    {
        return regionSize;
    }

    // Shared library file-backed read-only at file offset 0: include the
    // first page. This contains the ELF header that GDB needs to identify
    // the library and load the remaining content from disk.
    if (region->offset == 0 && regionSize >= DUMP_PAGE_SIZE)
    {
        return DUMP_PAGE_SIZE;
    }

    // Other shared library read-only regions (.text, .rodata): skip.
    // The debugger loads these from the original files via NT_FILE.
    return 0;
}

// Note name is "CORE\0" padded to 8 bytes (5 bytes + 3 padding)
#define CORE_NOTE_NAME "CORE"
#define CORE_NOTE_NAME_SIZE 5
#define CORE_NOTE_NAME_ALIGNED 8

// "LINUX\0" padded to 8 bytes (6 bytes + 2 padding)
#define LINUX_NOTE_NAME "LINUX"
#define LINUX_NOTE_NAME_SIZE 6
#define LINUX_NOTE_NAME_ALIGNED 8

// Calculate the size of a note including header, name, and data
static size_t NoteSize(size_t nameSize, size_t dataSize)
{
    return sizeof(Elf64_Nhdr) + ALIGN_UP(nameSize, 4) + ALIGN_UP(dataSize, 4);
}

// Count memory regions that have file names (for NT_FILE note)
static size_t CountFileRegions(const ProcessInfo* info)
{
    size_t count = 0;
    for (size_t i = 0; i < info->regions.count; i++)
    {
        if (info->regions.items[i].fileName[0] != '\0' &&
            info->regions.items[i].fileName[0] != '[')
        {
            count++;
        }
    }
    return count;
}

// Calculate the size of the NT_FILE note data
static size_t NtFileDataSize(const ProcessInfo* info)
{
    // Header: count (8 bytes) + page_size (8 bytes)
    size_t size = 2 * sizeof(uint64_t);

    // Per-file entry: start (8) + end (8) + offset (8)
    size_t fileCount = CountFileRegions(info);
    size += fileCount * 3 * sizeof(uint64_t);

    // File name strings (null-terminated)
    for (size_t i = 0; i < info->regions.count; i++)
    {
        if (info->regions.items[i].fileName[0] != '\0' &&
            info->regions.items[i].fileName[0] != '[')
        {
            size += strlen(info->regions.items[i].fileName) + 1;
        }
    }

    return size;
}

// Calculate the total size of all notes
static size_t CalculateNotesSize(const ProcessInfo* info)
{
    size_t total = 0;

    // NT_PRPSINFO
    total += NoteSize(CORE_NOTE_NAME_SIZE, sizeof(struct elf_prpsinfo));

    // NT_AUXV
    total += NoteSize(CORE_NOTE_NAME_SIZE, info->auxv.count * sizeof(Elf64_auxv_t));

    // NT_FILE
    total += NoteSize(CORE_NOTE_NAME_SIZE, NtFileDataSize(info));

    // Per-thread notes
    for (size_t i = 0; i < info->threads.count; i++)
    {
        // NT_PRSTATUS
        total += NoteSize(CORE_NOTE_NAME_SIZE, sizeof(struct elf_prstatus));

        // NT_FPREGSET
        total += NoteSize(CORE_NOTE_NAME_SIZE, sizeof(fp_regs_t));

        // NT_SIGINFO for crash thread
        if (info->threads.items[i].isCrashThread && info->crashSignal != 0)
        {
            total += NoteSize(CORE_NOTE_NAME_SIZE, sizeof(siginfo_t));
        }
    }

    return total;
}

// Write helpers
static bool WriteData(FILE* fp, const void* data, size_t size)
{
    return fwrite(data, 1, size, fp) == size;
}

static bool WritePadding(FILE* fp, size_t count)
{
    static const char zeros[16] = {0};
    while (count > 0)
    {
        size_t chunk = count > sizeof(zeros) ? sizeof(zeros) : count;
        if (!WriteData(fp, zeros, chunk))
        {
            return false;
        }
        count -= chunk;
    }
    return true;
}

static bool WriteNote(FILE* fp, uint32_t type, const char* name, size_t nameSize,
                      const void* data, size_t dataSize)
{
    Elf64_Nhdr nhdr;
    nhdr.n_namesz = (Elf64_Word)nameSize;
    nhdr.n_descsz = (Elf64_Word)dataSize;
    nhdr.n_type = type;

    if (!WriteData(fp, &nhdr, sizeof(nhdr)))
        return false;

    if (!WriteData(fp, name, nameSize))
        return false;

    size_t namePadding = ALIGN_UP(nameSize, 4) - nameSize;
    if (namePadding > 0 && !WritePadding(fp, namePadding))
        return false;

    if (!WriteData(fp, data, dataSize))
        return false;

    size_t dataPadding = ALIGN_UP(dataSize, 4) - dataSize;
    if (dataPadding > 0 && !WritePadding(fp, dataPadding))
        return false;

    return true;
}

static bool WriteNtPrpsinfo(FILE* fp, const ProcessInfo* info)
{
    struct elf_prpsinfo prpsinfo;
    memset(&prpsinfo, 0, sizeof(prpsinfo));
    prpsinfo.pr_pid = info->pid;
    prpsinfo.pr_ppid = info->ppid;
    prpsinfo.pr_pgrp = info->tgid;
    strncpy(prpsinfo.pr_fname, info->name, sizeof(prpsinfo.pr_fname) - 1);
    prpsinfo.pr_sname = 'R';

    return WriteNote(fp, NT_PRPSINFO, CORE_NOTE_NAME, CORE_NOTE_NAME_SIZE,
                     &prpsinfo, sizeof(prpsinfo));
}

static bool WriteNtAuxv(FILE* fp, const ProcessInfo* info)
{
    return WriteNote(fp, NT_AUXV, CORE_NOTE_NAME, CORE_NOTE_NAME_SIZE,
                     info->auxv.items, info->auxv.count * sizeof(Elf64_auxv_t));
}

static bool WriteNtFile(FILE* fp, const ProcessInfo* info)
{
    size_t dataSize = NtFileDataSize(info);
    uint8_t* data = (uint8_t*)malloc(dataSize);
    if (data == NULL)
    {
        return false;
    }

    uint8_t* ptr = data;

    // Header: count and page size
    uint64_t fileCount = CountFileRegions(info);
    uint64_t pageSize = DUMP_PAGE_SIZE;
    memcpy(ptr, &fileCount, sizeof(uint64_t)); ptr += sizeof(uint64_t);
    memcpy(ptr, &pageSize, sizeof(uint64_t)); ptr += sizeof(uint64_t);

    // Per-file entries: start, end, offset (in pages)
    for (size_t i = 0; i < info->regions.count; i++)
    {
        const MemRegion* region = &info->regions.items[i];
        if (region->fileName[0] == '\0' || region->fileName[0] == '[')
        {
            continue;
        }
        uint64_t start = region->startAddress;
        uint64_t end = region->endAddress;
        uint64_t offset = region->offset / DUMP_PAGE_SIZE;
        memcpy(ptr, &start, sizeof(uint64_t)); ptr += sizeof(uint64_t);
        memcpy(ptr, &end, sizeof(uint64_t)); ptr += sizeof(uint64_t);
        memcpy(ptr, &offset, sizeof(uint64_t)); ptr += sizeof(uint64_t);
    }

    // File name strings
    for (size_t i = 0; i < info->regions.count; i++)
    {
        const MemRegion* region = &info->regions.items[i];
        if (region->fileName[0] == '\0' || region->fileName[0] == '[')
        {
            continue;
        }
        size_t nameLen = strlen(region->fileName) + 1;
        memcpy(ptr, region->fileName, nameLen);
        ptr += nameLen;
    }

    bool result = WriteNote(fp, NT_FILE, CORE_NOTE_NAME, CORE_NOTE_NAME_SIZE, data, dataSize);
    free(data);
    return result;
}

static bool WriteThreadNotes(FILE* fp, const ProcessInfo* info)
{
    for (size_t i = 0; i < info->threads.count; i++)
    {
        const ThreadData* thread = &info->threads.items[i];

        // NT_PRSTATUS
        struct elf_prstatus prstatus;
        memset(&prstatus, 0, sizeof(prstatus));
        prstatus.pr_pid = thread->tid;
        prstatus.pr_ppid = info->ppid;
        prstatus.pr_pgrp = info->tgid;

        if (thread->isCrashThread)
        {
            prstatus.pr_info.si_signo = info->crashSignal;
            prstatus.pr_info.si_code = info->signalCode;
            prstatus.pr_info.si_errno = info->signalErrno;
            prstatus.pr_cursig = (short)info->crashSignal;
        }

        memcpy(&prstatus.pr_reg, &thread->gpRegs, sizeof(thread->gpRegs));

        if (!WriteNote(fp, NT_PRSTATUS, CORE_NOTE_NAME, CORE_NOTE_NAME_SIZE,
                       &prstatus, sizeof(prstatus)))
        {
            return false;
        }

        // NT_FPREGSET
        if (!WriteNote(fp, NT_FPREGSET, CORE_NOTE_NAME, CORE_NOTE_NAME_SIZE,
                       &thread->fpRegs, sizeof(thread->fpRegs)))
        {
            return false;
        }

        // NT_SIGINFO for crash thread
        if (thread->isCrashThread && info->crashSignal != 0)
        {
            siginfo_t siginfo;
            memset(&siginfo, 0, sizeof(siginfo));
            siginfo.si_signo = info->crashSignal;
            siginfo.si_code = info->signalCode;
            siginfo.si_errno = info->signalErrno;
            siginfo.si_addr = (void*)(uintptr_t)info->signalAddress;

            if (!WriteNote(fp, NT_SIGINFO, CORE_NOTE_NAME, CORE_NOTE_NAME_SIZE,
                           &siginfo, sizeof(siginfo)))
            {
                return false;
            }
        }
    }

    return true;
}

static bool WriteMemoryRegions(FILE* fp, const ProcessInfo* info, bool fullDump, bool diagnostics)
{
    uint8_t* buffer = (uint8_t*)malloc(DUMP_PAGE_SIZE);
    if (buffer == NULL)
    {
        return false;
    }

    // Open /proc/pid/mem once for all reads instead of per-page
    char memPath[64];
    snprintf(memPath, sizeof(memPath), "/proc/%d/mem", info->pid);
    int memFd = open(memPath, O_RDONLY);
    if (memFd == -1)
    {
        fprintf(stderr, "[createdump] Failed to open %s: %s (%d)\n", memPath, strerror(errno), errno);
        free(buffer);
        return false;
    }

    for (size_t i = 0; i < info->regions.count; i++)
    {
        const MemRegion* region = &info->regions.items[i];

        uint64_t dumpSize = GetDumpSize(region, info, fullDump);
        if (dumpSize == 0)
        {
            continue;
        }

        uint64_t address = region->startAddress;
        uint64_t remaining = dumpSize;

        while (remaining > 0)
        {
            size_t toRead = remaining > DUMP_PAGE_SIZE ? DUMP_PAGE_SIZE : (size_t)remaining;
            ssize_t bytesRead = pread(memFd, buffer, toRead, (off_t)address);

            if (bytesRead <= 0)
            {
                // Write zeros for unreadable pages
                memset(buffer, 0, toRead);
                bytesRead = (ssize_t)toRead;
            }

            if (!WriteData(fp, buffer, (size_t)bytesRead))
            {
                close(memFd);
                free(buffer);
                return false;
            }

            address += (uint64_t)bytesRead;
            remaining -= (uint64_t)bytesRead;
        }
    }

    close(memFd);
    free(buffer);
    return true;
}

bool WriteElfCoreDump(const char* dumpPath, ProcessInfo* info, bool fullDump, bool diagnostics)
{
    // Create the dump file with restrictive permissions (0600) since core dumps
    // may contain secrets (heap data, keys, etc.).
    int fd = open(dumpPath, O_WRONLY | O_CREAT | O_TRUNC, S_IRUSR | S_IWUSR);
    if (fd == -1)
    {
        fprintf(stderr, "[createdump] Failed to create dump file %s: %s (%d)\n",
                dumpPath, strerror(errno), errno);
        return false;
    }
    FILE* fp = fdopen(fd, "wb");
    if (fp == NULL)
    {
        fprintf(stderr, "[createdump] Failed to open dump file %s: %s (%d)\n",
                dumpPath, strerror(errno), errno);
        close(fd);
        return false;
    }

    bool result = false;

    // Count included regions for PT_LOAD headers
    size_t loadCount = 0;
    for (size_t i = 0; i < info->regions.count; i++)
    {
        if (GetDumpSize(&info->regions.items[i], info, fullDump) > 0)
        {
            loadCount++;
        }
    }

    // Total program headers: 1 (PT_NOTE) + N (PT_LOAD)
    size_t phdrCount = 1 + loadCount;

    // Calculate sizes
    size_t notesSize = CalculateNotesSize(info);
    size_t phdrOffset = sizeof(Elf64_Ehdr);
    size_t notesOffset = phdrOffset + phdrCount * sizeof(Elf64_Phdr);
    size_t dataOffset = ALIGN_UP(notesOffset + notesSize, DUMP_PAGE_SIZE);

    // ELF Header
    Elf64_Ehdr ehdr;
    memset(&ehdr, 0, sizeof(ehdr));
    ehdr.e_ident[EI_MAG0] = ELFMAG0;
    ehdr.e_ident[EI_MAG1] = ELFMAG1;
    ehdr.e_ident[EI_MAG2] = ELFMAG2;
    ehdr.e_ident[EI_MAG3] = ELFMAG3;
    ehdr.e_ident[EI_CLASS] = ELFCLASS64;
    ehdr.e_ident[EI_DATA] = ELFDATA2LSB;
    ehdr.e_ident[EI_VERSION] = EV_CURRENT;
    ehdr.e_ident[EI_OSABI] = ELFOSABI_LINUX;
    ehdr.e_type = ET_CORE;
#ifdef __x86_64__
    ehdr.e_machine = EM_X86_64;
#elif defined(__aarch64__)
    ehdr.e_machine = EM_AARCH64;
#endif
    ehdr.e_version = EV_CURRENT;
    ehdr.e_phoff = phdrOffset;
    ehdr.e_ehsize = sizeof(Elf64_Ehdr);
    ehdr.e_phentsize = sizeof(Elf64_Phdr);
    if (phdrCount > 0xffff)
    {
        // PN_XNUM requires a section header at index 0 with sh_info = real phnum.
        // This is extremely unlikely (>65534 memory regions), so fail rather than
        // produce an invalid core dump.
        fprintf(stderr, "[createdump] Too many program headers (%zu), cannot write core dump\n", phdrCount);
        goto cleanup;
    }
    ehdr.e_phnum = (Elf64_Half)phdrCount;
    ehdr.e_shentsize = sizeof(Elf64_Shdr);

    if (!WriteData(fp, &ehdr, sizeof(ehdr)))
    {
        goto cleanup;
    }

    // Program Headers
    // PT_NOTE header
    {
        Elf64_Phdr notePhdr;
        memset(&notePhdr, 0, sizeof(notePhdr));
        notePhdr.p_type = PT_NOTE;
        notePhdr.p_offset = notesOffset;
        notePhdr.p_filesz = notesSize;

        if (!WriteData(fp, &notePhdr, sizeof(notePhdr)))
        {
            goto cleanup;
        }
    }

    // PT_LOAD headers
    {
        uint64_t currentOffset = dataOffset;
        for (size_t i = 0; i < info->regions.count; i++)
        {
            const MemRegion* region = &info->regions.items[i];

            uint64_t dumpSize = GetDumpSize(region, info, fullDump);
            if (dumpSize == 0)
            {
                continue;
            }

            uint64_t regionSize = region->endAddress - region->startAddress;

            Elf64_Phdr loadPhdr;
            memset(&loadPhdr, 0, sizeof(loadPhdr));
            loadPhdr.p_type = PT_LOAD;
            loadPhdr.p_offset = currentOffset;
            loadPhdr.p_vaddr = region->startAddress;
            loadPhdr.p_paddr = 0;
            loadPhdr.p_filesz = dumpSize;
            loadPhdr.p_memsz = regionSize;
            loadPhdr.p_flags = region->permissions & (PF_R | PF_W | PF_X);
            loadPhdr.p_align = DUMP_PAGE_SIZE;

            if (!WriteData(fp, &loadPhdr, sizeof(loadPhdr)))
            {
                goto cleanup;
            }

            currentOffset += dumpSize;
        }
    }

    // Note section
    if (!WriteNtPrpsinfo(fp, info))
        goto cleanup;
    if (!WriteNtAuxv(fp, info))
        goto cleanup;
    if (!WriteNtFile(fp, info))
        goto cleanup;
    if (!WriteThreadNotes(fp, info))
        goto cleanup;

    // Pad to page boundary before memory regions
    {
        long currentPos = ftell(fp);
        if (currentPos < 0)
        {
            goto cleanup;
        }
        size_t padding = dataOffset - (size_t)currentPos;
        if (padding > 0 && !WritePadding(fp, padding))
        {
            goto cleanup;
        }
    }

    // Memory region contents
    if (!WriteMemoryRegions(fp, info, fullDump, diagnostics))
    {
        goto cleanup;
    }

    result = true;

cleanup:
    if (!result)
    {
        fprintf(stderr, "[createdump] Failed to write dump file: %s (%d)\n", strerror(errno), errno);
    }
    fclose(fp);
    return result;
}
