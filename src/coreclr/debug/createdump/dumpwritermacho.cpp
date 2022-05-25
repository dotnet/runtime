// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"
#include "specialthreadinfo.h"

//
// Write the core dump file
//
bool
DumpWriter::WriteDump()
{
    BuildSegmentLoadCommands();

    BuildThreadLoadCommands();

    uint64_t fileOffset = 0;
    if (!WriteHeader(&fileOffset)) {
        return false;
    }

    TRACE("Writing %zd thread commands to core file\n", m_threadLoadCommands.size());

    // Write thread commands
    for (const ThreadCommand& thread : m_threadLoadCommands)
    {
        if (!WriteData(&thread, thread.command.cmdsize)) {
            return false;
        }
    }

    TRACE("Writing %zd segment commands to core file\n", m_segmentLoadCommands.size());

    // Align first segment's file offset to the next page (0x1000) byte boundary
    uint64_t alignment = 0;
    if (fileOffset & (PAGE_SIZE - 1))
    {
        alignment = fileOffset;
        fileOffset = (fileOffset + (PAGE_SIZE - 1)) & PAGE_MASK;
        alignment = fileOffset - alignment;
    }

    // Write segment commands
    for (segment_command_64& segment : m_segmentLoadCommands)
    {
        segment.fileoff = fileOffset;
        fileOffset += segment.vmsize;
        assert(segment.vmsize == segment.filesize);

        if (!WriteData(&segment, segment.cmdsize)) {
            return false;
        }
    }

    // Write any segment alignment required to the core file
    if (alignment > 0)
    {
        if (alignment > sizeof(m_tempBuffer)) {
            printf_error("Segment alignment %llu > sizeof(m_tempBuffer)\n", alignment);
            return false;
        }
        memset(m_tempBuffer, 0, alignment);
        if (!WriteData(m_tempBuffer, alignment)) {
            return false;
        }
    }

    // Write memory regions
    return WriteSegments();
}

vm_prot_t
ConvertFlags(uint32_t flags)
{
    vm_prot_t prot = 0;
    if (flags & PF_R) {
        prot |= VM_PROT_READ;
    }
    if (flags & PF_W) {
        prot |= VM_PROT_WRITE;
    }
    if (flags & PF_X) {
        prot |= VM_PROT_EXECUTE;
    }
    return prot;
}

void
DumpWriter::BuildSegmentLoadCommands()
{
    for (const MemoryRegion& memoryRegion : m_crashInfo.MemoryRegions())
    {
        if (memoryRegion.IsBackedByMemory())
        {
            uint64_t size = memoryRegion.Size();
            uint32_t prot = ConvertFlags(memoryRegion.Permissions());

            segment_command_64 segment = {
                LC_SEGMENT_64,                  // uint32_t cmd;
                sizeof(segment_command_64),     // uint32_t cmdsize;
                {0},                            // char segname[16];
                memoryRegion.StartAddress(),    // uint64_t vmaddr;   
                size,                           // uint64_t vmsize;
                0,                              // uint64_t fileoff;
                size,                           // uint64_t filesize;
                prot,                           // uint32_t maxprot;
                prot,                           // uint32_t initprot;
                0,                              // uint32_t nsects;
                0                               // uint32_t flags;
            };
            m_segmentLoadCommands.push_back(segment);
        }
    }

    // Add special memory region containing the process and thread info
    uint64_t size = sizeof(SpecialThreadInfoHeader) + (m_crashInfo.Threads().size() * sizeof(SpecialThreadInfoEntry));
    segment_command_64 segment = {
        LC_SEGMENT_64,                  // uint32_t cmd;
        sizeof(segment_command_64),     // uint32_t cmdsize;
        {0},                            // char segname[16];
        SpecialThreadInfoAddress,       // uint64_t vmaddr;   
        size,                           // uint64_t vmsize;
        0,                              // uint64_t fileoff;
        size,                           // uint64_t filesize;
        VM_PROT_READ,                   // uint32_t maxprot;
        VM_PROT_READ,                   // uint32_t initprot;
        0,                              // uint32_t nsects;
        0                               // uint32_t flags;
    };
    m_segmentLoadCommands.push_back(segment);
}

void
DumpWriter::BuildThreadLoadCommands()
{
    for (const ThreadInfo* thread : m_crashInfo.Threads())
    {
        ThreadCommand threadCommand = {
            { LC_THREAD, sizeof(ThreadCommand) },
        };
#if defined(__x86_64__)
        threadCommand.gpflavor = x86_THREAD_STATE64;
        threadCommand.gpcount = x86_THREAD_STATE64_COUNT;
        threadCommand.fpflavor = x86_FLOAT_STATE64;
        threadCommand.fpcount = x86_FLOAT_STATE64_COUNT;
        assert(x86_THREAD_STATE64_COUNT == sizeof(x86_thread_state64_t) / sizeof(uint32_t));
        assert(x86_FLOAT_STATE64_COUNT == sizeof(x86_float_state64_t) / sizeof(uint32_t));
        memcpy(&threadCommand.gpregisters, thread->GPRegisters(), sizeof(x86_thread_state64_t));
        memcpy(&threadCommand.fpregisters, thread->FPRegisters(), sizeof(x86_float_state64_t));
#elif defined(__aarch64__)
        threadCommand.gpflavor = ARM_THREAD_STATE64;
        threadCommand.gpcount = ARM_THREAD_STATE64_COUNT;
        threadCommand.fpflavor = ARM_NEON_STATE64;
        threadCommand.fpcount = ARM_NEON_STATE64_COUNT;
        assert(ARM_THREAD_STATE64_COUNT == sizeof(arm_thread_state64_t) / sizeof(uint32_t));
        assert(ARM_NEON_STATE64_COUNT == sizeof(arm_neon_state64_t) / sizeof(uint32_t));
        memcpy(&threadCommand.gpregisters, thread->GPRegisters(), sizeof(arm_thread_state64_t));
        memcpy(&threadCommand.fpregisters, thread->FPRegisters(), sizeof(arm_neon_state64_t));
#endif
        m_threadLoadCommands.push_back(threadCommand);
    }
}

bool
DumpWriter::WriteHeader(uint64_t* pFileOffset)
{
    mach_header_64 header;
    memset(&header, 0, sizeof(mach_header_64));

    header.magic = MH_MAGIC_64;
#if defined(__x86_64__)
    header.cputype = CPU_TYPE_X86_64;
    header.cpusubtype = CPU_SUBTYPE_I386_ALL | CPU_SUBTYPE_LITTLE_ENDIAN;
#elif defined(__aarch64__)
    header.cputype = CPU_TYPE_ARM64;
    header.cpusubtype = CPU_SUBTYPE_ARM64_ALL | CPU_SUBTYPE_LITTLE_ENDIAN;
#else
#error Unexpected architecture
#endif
    header.filetype = MH_CORE;

    for (const ThreadCommand& thread : m_threadLoadCommands)
    {
        header.ncmds++;
        header.sizeofcmds += thread.command.cmdsize;
    }

    for (const segment_command_64& segment : m_segmentLoadCommands)
    {
        header.ncmds++;
        header.sizeofcmds += segment.cmdsize;
    }

    *pFileOffset = sizeof(mach_header_64) + header.sizeofcmds;
    
    TRACE("Macho header: magic %08x cputype %08x cpusubtype %08x filetype %08x ncmds %08x sizeofcmds %08x flags %08x reserved %08x\n",
        header.magic,
        header.cputype,
        header.cpusubtype,
        header.filetype,
        header.ncmds,
        header.sizeofcmds,
        header.flags,
        header.reserved);

    // Write header
    return WriteData(&header, sizeof(mach_header_64));
}

bool
DumpWriter::WriteSegments()
{
    TRACE("Writing %" PRIu64 " memory regions to core file\n", m_segmentLoadCommands.size());

    // Read from target process and write memory regions to core
    uint64_t total = 0;
    for (const segment_command_64& segment : m_segmentLoadCommands)
    {
        uint64_t address = segment.vmaddr;
        size_t size = segment.vmsize;
        total += size;

        TRACE("%" PRIA PRIx64 " - %" PRIA PRIx64 " (%06" PRIx64 ") %" PRIA PRIx64 " %c%c%c %02x\n",
            segment.vmaddr,
            segment.vmaddr + segment.vmsize,
            segment.vmsize / PAGE_SIZE,
            segment.fileoff,
            (segment.initprot & VM_PROT_READ) ? 'r' : '-',
            (segment.initprot & VM_PROT_WRITE) ? 'w' : '-',
            (segment.initprot & VM_PROT_EXECUTE) ? 'x' : '-',
            segment.initprot);

        if (address == SpecialThreadInfoAddress)
        {
            // Write the header
            SpecialThreadInfoHeader header = {
                {SPECIAL_THREADINFO_SIGNATURE},
                m_crashInfo.Pid(),
                m_crashInfo.Threads().size()
            };

            if (!WriteData(&header, sizeof(header))) {
                return false;
            }

            // Write the tid and stack pointer for each thread
            for (const ThreadInfo* thread : m_crashInfo.Threads())
            {
                SpecialThreadInfoEntry entry = {
                    thread->Tid(),
                    thread->GetStackPointer()
                };

                if (!WriteData(&entry, sizeof(entry))) {
                    return false;
                }
            }
        }
        else
        {
            while (size > 0)
            {
                size_t bytesToRead = std::min(size, sizeof(m_tempBuffer));
                size_t read = 0;

                if (!m_crashInfo.ReadProcessMemory((void*)address, m_tempBuffer, bytesToRead, &read)) {
                    printf_error("ReadProcessMemory(%" PRIA PRIx64 ", %08zx) FAILED\n", address, bytesToRead);
                    return false;
                }

                // This can happen if the target process dies before createdump is finished
                if (read == 0) {
                    printf_error("ReadProcessMemory(%" PRIA PRIx64 ", %08zx) returned 0 bytes read\n", address, bytesToRead);
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
