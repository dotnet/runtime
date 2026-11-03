// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Write the core dump file:
//   ELF header
//   Single section header (Shdr) for 64 bit program header count
//   Phdr for the PT_NOTE
//   PT_LOAD
//   PT_NOTEs
//      process info (prpsinfo_t)
//      NT_FILE entries
//      threads
//      alignment
//   memory blocks
template <typename TModuleMappings, typename TDumpRegions>
bool DumpWriter::WriteDump(const TModuleMappings& moduleMappings, const TDumpRegions& dumpRegions)
{
    // Write the ELF header
    Ehdr ehdr;
    memset(&ehdr, 0, sizeof(Ehdr));
    ehdr.e_ident[0] = ELFMAG0;
    ehdr.e_ident[1] = ELFMAG1;
    ehdr.e_ident[2] = ELFMAG2;
    ehdr.e_ident[3] = ELFMAG3;
    ehdr.e_ident[EI_CLASS] = ELF_CLASS;
    ehdr.e_ident[EI_DATA] = ELFDATA2LSB;
    ehdr.e_ident[EI_VERSION] = EV_CURRENT;
    ehdr.e_ident[EI_OSABI] = ELFOSABI_LINUX;

    ehdr.e_type = ET_CORE;
    ehdr.e_machine = ELF_ARCH;
    ehdr.e_version = EV_CURRENT;
    ehdr.e_phoff = sizeof(Ehdr);

    ehdr.e_ehsize = sizeof(Ehdr);
    ehdr.e_phentsize = sizeof(Phdr);

    // The ELF header only allows UINT16 for the number of program
    // headers. In a core dump this equates to PT_NODE and PT_LOAD.
    //
    // When more program headers than 65534 the first section entry
    // is used to store the actual program header count.

    // PT_NOTE + number of memory regions
    uint64_t phnum = 1 + dumpRegions.size();

    if (phnum < PH_HDR_CANARY) {
        ehdr.e_phnum = phnum;
    }
    else {
        ehdr.e_phnum = PH_HDR_CANARY;
        ehdr.e_phoff = sizeof(Ehdr) + sizeof(Shdr);
        ehdr.e_shnum = 1;
        ehdr.e_shoff = sizeof(Ehdr);
        ehdr.e_shentsize = sizeof(Shdr);
    }

    if (!WriteData(&ehdr, sizeof(Ehdr))) {
        return false;
    }

    size_t offset = sizeof(Ehdr) + (phnum * sizeof(Phdr));
    size_t filesz = GetProcessInfoSize() + GetAuxvInfoSize() + GetThreadInfoSize() + GetNTFileInfoSize(moduleMappings);

    if (ehdr.e_phnum == PH_HDR_CANARY)
    {
        // Add single section containing the actual count of the program headers to be written.
        Shdr shdr;
        memset(&shdr, 0, sizeof(shdr));
        shdr.sh_info = phnum;
        shdr.sh_size = 1;
        offset += sizeof(Shdr);

        // When section header offset is present but ehdr section num = 0 then it is expected that
        // the sh_size indicates the size of the section array or 1 in our case.
        if (!WriteData(&shdr, sizeof(shdr))) {
            return false;
        }
    }

    // PT_NOTE header
    Phdr phdr;
    memset(&phdr, 0, sizeof(phdr));
    phdr.p_type = PT_NOTE;
    phdr.p_offset = offset;
    phdr.p_filesz = filesz;

    if (!WriteData(&phdr, sizeof(phdr))) {
        return false;
    }

    // PT_NOTE sections must end on 4 byte boundary
    // We output the NT_FILE, AUX and Thread entries
    // AUX is aligned, NT_FILE is aligned and then we
    // check to pad end of the thread list
    phdr.p_type = PT_LOAD;
    phdr.p_align = 4096;

    size_t finalNoteAlignment = phdr.p_align - ((offset + filesz) % phdr.p_align);
    if (finalNoteAlignment == phdr.p_align) {
        finalNoteAlignment = 0;
    }
    offset += finalNoteAlignment;

    TRACE("Writing memory region headers to core file\n");

    // Write memory region note headers
    for (const MemoryRegion& memoryRegion : dumpRegions)
    {
        phdr.p_flags = memoryRegion.Permissions();
        phdr.p_vaddr = memoryRegion.StartAddress();
        phdr.p_memsz = memoryRegion.Size();

        offset += filesz;
        phdr.p_filesz = filesz = memoryRegion.Size();
        phdr.p_offset = offset;

        if (!WriteData(&phdr, sizeof(phdr))) {
            return false;
        }
    }

    // Write process info data to core file
    if (!WriteProcessInfo()) {
        return false;
    }

    // Write auxv data to core file
    if (!WriteAuxv()) {
        return false;
    }

    // Write NT_FILE entries to the core file
    if (!WriteNTFileInfo(moduleMappings)) {
        return false;
    }

    TRACE("Writing %zd thread entries to core file\n", m_processInfo.Threads().size());

    // Write all the thread's state and registers
    for (const ThreadSnapshot& thread : m_processInfo.Threads())
    {
        if (!WriteThread(thread)) {
            return false;
        }
    }

    // Zero out the end of the PT_NOTE section to the boundary
    // and then laydown the memory blocks
    if (finalNoteAlignment > 0) {
        if (finalNoteAlignment > sizeof(m_tempBuffer)) {
            printf_error("Internal error: finalNoteAlignment %zu > sizeof(m_tempBuffer)\n", finalNoteAlignment);
            return false;
        }
        memset(m_tempBuffer, 0, finalNoteAlignment);
        if (!WriteData(m_tempBuffer, finalNoteAlignment)) {
            return false;
        }
    }

    TRACE("Writing %" PRIu64 " memory regions to core file\n", phnum - 1);

    // Read from target process and write memory regions to core
    uint64_t total = 0;
    for (const MemoryRegion& memoryRegion : dumpRegions)
    {
        uint64_t address = memoryRegion.StartAddress();
        size_t size = memoryRegion.Size();
        total += size;

        if (address == SpecialDiagInfoAddress)
        {
            if (!WriteDiagInfo(size)) {
                return false;
            }
        }
        else
        {
            while (size > 0)
            {
                size_t bytesToRead = size < sizeof(m_tempBuffer) ? size : sizeof(m_tempBuffer);
                size_t read = 0;

                if (!m_processInfo.ReadProcessMemory(address, m_tempBuffer, bytesToRead, &read)) {
                    printf_error("Error reading memory at %" PRIA PRIx64 " size %08zx FAILED %s (%d)\n", address, bytesToRead, strerror(g_readProcessMemoryErrno), g_readProcessMemoryErrno);
                    return false;
                }

                // This can happen if the target process dies before createdump is finished
                if (read == 0) {
                    printf_error("Error reading memory at %" PRIA PRIx64 " size %08zx returned 0 bytes read: %s (%d)\n", address, bytesToRead, strerror(g_readProcessMemoryErrno), g_readProcessMemoryErrno);
                    return false;
                }

                if (!WriteData(m_tempBuffer, read)) {
                    return false;
                }

                address += read;
                size -= read;
            }
        }
    }

    printf_status("Written %" PRId64 " bytes (%" PRId64 " pages) to core file\n", total, total / PAGE_SIZE);
    return true;
}

// Calculate the NT_FILE entries total size
template <typename TModuleMappings>
size_t DumpWriter::GetNTFileInfoSize(const TModuleMappings& moduleMappings, size_t* alignmentBytes)
{
    size_t count = moduleMappings.size();
    size_t size = 0;

    // Header, CORE, entry count, page size
    size = sizeof(Nhdr) + 8 + sizeof(count) + sizeof(size);

    // start_address, end_address, offset
    size += count * sizeof(NTFileEntry);

    // \0 terminator for each filename
    size += count;

    // File name storage needed
    for (const ModuleRegion& image : moduleMappings) {
        size += image.FileNameLength();
    }
    // Notes must end on 4 byte alignment
    size_t alignmentBytesNeeded = 4 - (size % 4);
    size += alignmentBytesNeeded;

    if (alignmentBytes != nullptr) {
        *alignmentBytes = alignmentBytesNeeded;
    }
    return size;
}

//  Write NT_FILE entries to the PT_NODE section
//
//  Nhdr (NT_FILE)
//  Total entries
//  Page size
//  [0] start_address end_address offset
//  [1] start_address end_address offset
//  [file name]\0[file name]\0...
template <typename TModuleMappings>
bool DumpWriter::WriteNTFileInfo(const TModuleMappings& moduleMappings)
{
    Nhdr nhdr;
    memset(&nhdr, 0, sizeof(nhdr));

    // CORE + \0 and we align on 4 byte boundary
    // so we can use CORE\0FIL for easier hex debugging
    nhdr.n_namesz = 5;
    nhdr.n_type = NT_FILE;  // "FILE"

    // Size of payload for NT_FILE after CORE tag written
    size_t alignmentBytesNeeded = 0;
    nhdr.n_descsz = GetNTFileInfoSize(moduleMappings, &alignmentBytesNeeded) - sizeof(nhdr) - 8;

    size_t count = moduleMappings.size();
    size_t pageSize = PAGE_SIZE;

    TRACE("Writing %zd NT_FILE entries to core file\n", moduleMappings.size());

    if (!WriteData(&nhdr, sizeof(nhdr)) ||
        !WriteData("CORE\0FIL", 8) ||
        !WriteData(&count, sizeof(count)) ||
        !WriteData(&pageSize, sizeof(pageSize))) {
        return false;
    }

    for (const ModuleRegion& image : moduleMappings)
    {
        struct NTFileEntry entry { (unsigned long)image.StartAddress(), (unsigned long)image.EndAddress(), (unsigned long)(image.Offset() / pageSize) };
        if (!WriteData(&entry, sizeof(entry))) {
            return false;
        }
    }

    for (const ModuleRegion& image : moduleMappings)
    {
        if (!WriteData(image.FileName(), image.FileNameLength()) ||
            !WriteData("\0", 1)) {
            return false;
        }
    }

    // Has to end on a 4 byte boundary.  Debugger, readelf and such
    // will automatically align on next 4 bytes and look for a PT_NOTE
    // header.
    if (alignmentBytesNeeded) {
        if (!WriteData("\0\0\0\0", alignmentBytesNeeded)) {
            return false;
        }
    }

    return true;
}