// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Minimal ELF core dump writer for NativeAOT.
// Produces a standard ELF core dump that tools like gdb and lldb can read.
//
// ELF core layout:
//   ELF Header
//   Program Headers: [PT_NOTE] [PT_LOAD x N]
//   Note Section: NT_PRPSINFO, NT_AUXV, NT_FILE, per-thread (NT_PRSTATUS, optional NT_FPREGSET/NT_SIGINFO)
//   Memory Region Contents (system-page aligned)
//
// Region filtering (heap mode): reconstructable file-backed content is
// excluded because debuggers can reload it through the NT_FILE note.

#include "elf_dump_writer.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <errno.h>
#include <unistd.h>
#include <fcntl.h>
#include <sys/stat.h>
#include <sys/sysmacros.h>
#include <elf.h>
#include <sys/procfs.h>

// Verify that our GP register type matches the size expected by elf_prstatus.
// A mismatch would cause a buffer overflow in the memcpy into pr_reg.
_Static_assert(sizeof(gp_regs_t) == sizeof(((struct elf_prstatus*)0)->pr_reg),
               "GP register struct must match elf_prstatus pr_reg");

// The elf64 header should be 4-byte aligned.
_Static_assert(sizeof(Elf64_Nhdr) % 4 == 0, "ELF note header must be 4-byte aligned");

#define ALIGN_UP(val, align) (((val) + (align) - 1) & ~((align) - 1))

static bool IsFileMappingReconstructable(const MemRegion* region)
{
    if (region->fileName == NULL || region->fileName[0] != '/' || region->inode == 0)
    {
        return false;
    }

    int fd = open(region->fileName, O_RDONLY);
    if (fd == -1)
    {
        return false;
    }

    struct stat fileStatus;
    bool result =
        fstat(fd, &fileStatus) == 0 &&
        S_ISREG(fileStatus.st_mode) &&
        (uint64_t)major(fileStatus.st_dev) == region->deviceMajor &&
        (uint64_t)minor(fileStatus.st_dev) == region->deviceMinor &&
        (uint64_t)fileStatus.st_ino == region->inode;
    close(fd);
    return result;
}

static bool ShouldIncludeInNtFile(const MemRegion* region)
{
    return region->fileName != NULL && region->fileName[0] != '[';
}

// Determines how many bytes of a memory region to include in the dump.
//
// Full mode (DbgMiniDumpType=4): includes all readable memory.
//
// Heap mode (default and DbgMiniDumpType=2): includes anonymous memory,
// private writable mappings, non-reconstructable files, and the first page
// of private read-only mappings. Reconstructable shared mappings are omitted.
static uint64_t GetDumpSize(
    const MemRegion* region,
    const ProcessInfo* info,
    bool fullDump)
{
    if ((region->permissions & PF_R) == 0)
    {
        return 0;
    }

    uint64_t regionSize = region->endAddress - region->startAddress;

    // In full mode, include all readable memory.
    if (fullDump)
    {
        return regionSize;
    }

    // Include anonymous regions and special mappings.
    if (!ShouldIncludeInNtFile(region))
    {
        return regionSize;
    }

    // A debugger can omit file-backed memory only when the original file is
    // still available and is the same inode that the process mapped.
    if (!IsFileMappingReconstructable(region))
    {
        return regionSize;
    }

    bool isPrivate = (region->permissions & MR_PRIVATE) != 0;
    bool isMainExecutable =
        info->exePath[0] != '\0' &&
        strcmp(region->fileName, info->exePath) == 0;

    // Private writable mappings may contain copy-on-write changes that are
    // not present in the backing file. Shared writable mappings are reflected
    // in the file's page cache and can be reconstructed.
    if ((region->permissions & PF_W) != 0)
    {
        return isPrivate ? regionSize : 0;
    }

    // Keep the first page of each private mapping. This preserves ELF headers
    // and the beginning of RELRO mappings containing loader-patched data such
    // as DT_DEBUG without embedding NativeAOT executable code and rodata.
    if (isPrivate || isMainExecutable || region->offset == 0)
    {
        return regionSize < info->pageSize ? regionSize : info->pageSize;
    }

    // Other reconstructable file content is loaded through NT_FILE.
    return 0;
}

// Note name is "CORE\0" padded to 8 bytes (5 bytes + 3 padding)
#define CORE_NOTE_NAME "CORE"
#define CORE_NOTE_NAME_SIZE 5

// Elf64_Nhdr stores name and descriptor sizes in 32-bit Elf64_Word fields.
#define ELF64_NOTE_HEADER_FIELD_MAX UINT32_MAX

// Elf64_Phdr stores segment sizes, including p_filesz, in 64-bit Elf64_Xword fields.
#define ELF64_PROGRAM_HEADER_SIZE_FIELD_MAX UINT64_MAX

_Static_assert(SIZE_MAX == ELF64_PROGRAM_HEADER_SIZE_FIELD_MAX,
               "size_t must represent ELF64 program header sizes");

// Calculate the size of a note including header, name, and data
static size_t NoteSize(size_t nameSize, size_t dataSize)
{
    return sizeof(Elf64_Nhdr) + ALIGN_UP(nameSize, 4) + ALIGN_UP(dataSize, 4);
}

static bool TryAddSize(size_t* total, size_t value, size_t limit)
{
    if (*total > limit || value > limit - *total)
    {
        return false;
    }

    *total += value;
    return true;
}

static bool TryAddNoteSize(size_t* total, size_t dataSize)
{
    if (dataSize > ELF64_NOTE_HEADER_FIELD_MAX)
    {
        return false;
    }

    return TryAddSize(total, NoteSize(CORE_NOTE_NAME_SIZE, dataSize), ELF64_PROGRAM_HEADER_SIZE_FIELD_MAX);
}

// Count memory regions that have file names (for NT_FILE note)
static size_t CountFileRegions(const ProcessInfo* info)
{
    size_t count = 0;
    for (size_t i = 0; i < info->regions.count; i++)
    {
        if (ShouldIncludeInNtFile(&info->regions.items[i]))
        {
            count++;
        }
    }
    return count;
}

static bool CalculateAuxvDataSize(const ProcessInfo* info, size_t* dataSize)
{
    if (info->auxv.count > ELF64_NOTE_HEADER_FIELD_MAX / sizeof(Elf64_auxv_t))
    {
        return false;
    }

    *dataSize = info->auxv.count * sizeof(Elf64_auxv_t);
    return true;
}

// Calculate the size of the NT_FILE note data
static bool CalculateNtFileDataSize(const ProcessInfo* info, size_t* dataSize)
{
    // Header: count (8 bytes) + page_size (8 bytes)
    size_t size = 2 * sizeof(uint64_t);

    // Per-file entry: start (8) + end (8) + offset (8)
    size_t fileCount = CountFileRegions(info);
    const size_t fileEntrySize = 3 * sizeof(uint64_t);
    if (fileCount > ELF64_NOTE_HEADER_FIELD_MAX / fileEntrySize ||
        !TryAddSize(&size, fileCount * fileEntrySize, ELF64_NOTE_HEADER_FIELD_MAX))
    {
        return false;
    }

    // File name strings (null-terminated)
    for (size_t i = 0; i < info->regions.count; i++)
    {
        const MemRegion* region = &info->regions.items[i];
        if (ShouldIncludeInNtFile(region))
        {
            size_t nameLength = strlen(region->fileName);
            if (!TryAddSize(&size, nameLength + 1, ELF64_NOTE_HEADER_FIELD_MAX))
            {
                return false;
            }
        }
    }

    *dataSize = size;
    return true;
}

// Calculate the total size of all notes
static bool CalculateNotesSize(const ProcessInfo* info, size_t* notesSize)
{
    size_t total = 0;

    // NT_PRPSINFO
    if (!TryAddNoteSize(&total, sizeof(struct elf_prpsinfo)))
        return false;

    // NT_AUXV
    size_t auxvDataSize;
    if (!CalculateAuxvDataSize(info, &auxvDataSize) || !TryAddNoteSize(&total, auxvDataSize))
        return false;

    // NT_FILE
    size_t ntFileDataSize;
    if (!CalculateNtFileDataSize(info, &ntFileDataSize) || !TryAddNoteSize(&total, ntFileDataSize))
        return false;

    // Per-thread notes
    for (size_t i = 0; i < info->threads.count; i++)
    {
        const ThreadData* thread = &info->threads.items[i];
        // Inconsistent FP register size
        if (thread->fpRegsSize != 0 && thread->fpRegsSize != sizeof(thread->fpRegs))
            return false;

        // NT_PRSTATUS
        if (!TryAddNoteSize(&total, sizeof(struct elf_prstatus)))
            return false;

        // NT_FPREGSET, when available
        if (thread->fpRegsSize != 0 && !TryAddNoteSize(&total, thread->fpRegsSize))
            return false;

        // NT_SIGINFO for crash thread
        if (thread->isCrashThread && info->crashSignal != 0)
        {
            if (!TryAddNoteSize(&total, sizeof(siginfo_t)))
                return false;
        }
    }

    *notesSize = total;
    return true;
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
    if (nameSize > ELF64_NOTE_HEADER_FIELD_MAX || dataSize > ELF64_NOTE_HEADER_FIELD_MAX)
        return false;

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
    prpsinfo.pr_pgrp = info->pgrp;
    strncpy(prpsinfo.pr_fname, info->name, sizeof(prpsinfo.pr_fname) - 1);
    prpsinfo.pr_sname = 'R';

    return WriteNote(fp, NT_PRPSINFO, CORE_NOTE_NAME, CORE_NOTE_NAME_SIZE,
                     &prpsinfo, sizeof(prpsinfo));
}

static bool WriteNtAuxv(FILE* fp, const ProcessInfo* info)
{
    size_t dataSize;
    if (!CalculateAuxvDataSize(info, &dataSize))
        return false;

    return WriteNote(fp, NT_AUXV, CORE_NOTE_NAME, CORE_NOTE_NAME_SIZE,
                     info->auxv.items, dataSize);
}

static bool WriteNtFile(FILE* fp, const ProcessInfo* info)
{
    size_t dataSize;
    if (!CalculateNtFileDataSize(info, &dataSize))
    {
        return false;
    }

    uint8_t* data = (uint8_t*)malloc(dataSize);
    if (data == NULL)
    {
        return false;
    }

    uint8_t* ptr = data;

    // Header: count and page size
    uint64_t fileCount = CountFileRegions(info);
    uint64_t pageSize = info->pageSize;
    memcpy(ptr, &fileCount, sizeof(uint64_t)); ptr += sizeof(uint64_t);
    memcpy(ptr, &pageSize, sizeof(uint64_t)); ptr += sizeof(uint64_t);

    // Per-file entries: start, end, offset (in pages)
    for (size_t i = 0; i < info->regions.count; i++)
    {
        const MemRegion* region = &info->regions.items[i];
        if (!ShouldIncludeInNtFile(region))
        {
            continue;
        }
        uint64_t start = region->startAddress;
        uint64_t end = region->endAddress;
        uint64_t offset = region->offset / info->pageSize;
        memcpy(ptr, &start, sizeof(uint64_t)); ptr += sizeof(uint64_t);
        memcpy(ptr, &end, sizeof(uint64_t)); ptr += sizeof(uint64_t);
        memcpy(ptr, &offset, sizeof(uint64_t)); ptr += sizeof(uint64_t);
    }

    // File name strings
    for (size_t i = 0; i < info->regions.count; i++)
    {
        const MemRegion* region = &info->regions.items[i];
        if (!ShouldIncludeInNtFile(region))
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
        if (thread->fpRegsSize != 0 && thread->fpRegsSize != sizeof(thread->fpRegs))
        {
            return false;
        }

        // NT_PRSTATUS
        struct elf_prstatus prstatus;
        memset(&prstatus, 0, sizeof(prstatus));
        prstatus.pr_pid = thread->tid;
        prstatus.pr_ppid = info->ppid;
        prstatus.pr_pgrp = info->pgrp;
        prstatus.pr_fpvalid = thread->fpRegsSize != 0;

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
        if (thread->fpRegsSize != 0 &&
            !WriteNote(fp, NT_FPREGSET, CORE_NOTE_NAME, CORE_NOTE_NAME_SIZE,
                       &thread->fpRegs, thread->fpRegsSize))
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

static bool WriteMemoryRegions(
    FILE* fp,
    const ProcessInfo* info,
    const uint64_t* dumpSizes)
{
    uint8_t* buffer = malloc((size_t)info->pageSize);
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

        uint64_t dumpSize = dumpSizes[i];
        if (dumpSize == 0)
        {
            continue;
        }

        uint64_t address = region->startAddress;
        uint64_t remaining = dumpSize;

        while (remaining > 0)
        {
            size_t toRead = remaining > info->pageSize ? (size_t)info->pageSize : (size_t)remaining;
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

bool WriteElfCoreDump(const char* dumpPath, ProcessInfo* info, bool fullDump)
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
        remove(dumpPath);
        return false;
    }

    bool result = false;
    uint64_t* dumpSizes = NULL;

    // Count included regions for PT_LOAD headers
    size_t loadCount = 0;
    if (info->regions.count > SIZE_MAX / sizeof(uint64_t))
    {
        fprintf(stderr, "[createdump] Too many memory regions\n");
        goto cleanup;
    }
    dumpSizes = calloc(info->regions.count, sizeof(uint64_t));
    if (dumpSizes == NULL && info->regions.count != 0)
    {
        goto cleanup;
    }
    for (size_t i = 0; i < info->regions.count; i++)
    {
        dumpSizes[i] = GetDumpSize(&info->regions.items[i], info, fullDump);
        if (dumpSizes[i] > 0)
        {
            loadCount++;
        }
    }

    // Total program headers: 1 (PT_NOTE) + N (PT_LOAD)
    size_t phdrCount = 1 + loadCount;
    if (phdrCount >= PN_XNUM)
    {
        // In this case, we would need to change the ELF header to use extended numbering.
        // This is extremely unlikely (>65533 memory regions), so fail rather than
        // produce an invalid core dump.
        fprintf(stderr, "[createdump] Too many program headers (%zu), cannot write core dump\n", phdrCount);
        goto cleanup;
    }

    // Calculate sizes
    size_t notesSize;
    if (!CalculateNotesSize(info, &notesSize))
    {
        fprintf(stderr, "[createdump] Note data is too large\n");
        goto cleanup;
    }
    size_t phdrOffset = sizeof(Elf64_Ehdr);
    if (phdrCount > (SIZE_MAX - phdrOffset) / sizeof(Elf64_Phdr))
    {
        fprintf(stderr, "[createdump] Program header table is too large\n");
        goto cleanup;
    }
    size_t notesOffset = phdrOffset + phdrCount * sizeof(Elf64_Phdr);
    if (notesSize > SIZE_MAX - notesOffset ||
        notesOffset + notesSize > SIZE_MAX - ((size_t)info->pageSize - 1))
    {
        fprintf(stderr, "[createdump] Note section is too large\n");
        goto cleanup;
    }
    size_t dataOffset = ALIGN_UP(notesOffset + notesSize, (size_t)info->pageSize);
    errno = 0;

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
    ehdr.e_phnum = (Elf64_Half)phdrCount;

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

            uint64_t dumpSize = dumpSizes[i];
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
            loadPhdr.p_align = info->pageSize;

            if (!WriteData(fp, &loadPhdr, sizeof(loadPhdr)))
            {
                goto cleanup;
            }

            if (dumpSize > UINT64_MAX - currentOffset)
            {
                fprintf(stderr, "[createdump] Dump file layout is too large\n");
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

        size_t currentPosition = (size_t)currentPos;
        size_t expectedPosition = notesOffset + notesSize;
        if (currentPosition != expectedPosition)
        {
            fprintf(stderr, "[createdump] Note section ended at %zu, expected %zu\n",
                    currentPosition, expectedPosition);
            goto cleanup;
        }

        size_t padding = dataOffset - currentPosition;
        if (padding > 0 && !WritePadding(fp, padding))
        {
            goto cleanup;
        }
    }

    // Memory region contents
    if (!WriteMemoryRegions(fp, info, dumpSizes))
    {
        goto cleanup;
    }

    result = true;

cleanup:
    {
        int savedErrno = result ? 0 : errno;
        if (fclose(fp) != 0)
        {
            savedErrno = errno;
            result = false;
        }

        if (!result)
        {
            if (savedErrno != 0)
            {
                fprintf(stderr, "[createdump] Failed to write dump file: %s (%d)\n",
                        strerror(savedErrno), savedErrno);
            }
            else
            {
                fprintf(stderr, "[createdump] Failed to write dump file\n");
            }

            if (remove(dumpPath) != 0 && errno != ENOENT)
            {
                fprintf(stderr, "[createdump] Failed to remove incomplete dump file %s: %s (%d)\n",
                        dumpPath, strerror(errno), errno);
            }
        }
    }

    free(dumpSizes);
    return result;
}
