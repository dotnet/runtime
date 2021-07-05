// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"
#include "specialthreadinfo.h"

// Include the .NET Core version string instead of link because it is "static".
#include "version.c"

static void
WriteSysctl(const char* sysctlname, JsonWriter& writer, const char* valueName)
{
    size_t size = 0;
    if (sysctlbyname(sysctlname, nullptr, &size, NULL, 0) >= 0)
    {
        ArrayHolder<char> buffer = new char[size];
        if (sysctlbyname(sysctlname, buffer, &size, NULL, 0) >= 0)
        {
            writer.WriteValue(valueName, buffer);
        }
        else
        {
            TRACE("sysctlbyname(%s) 1 FAILED %s\n", sysctlname, strerror(errno));
        }
    }
    else
    {
        TRACE("sysctlbyname(%s) 2 FAILED %s\n", sysctlname, strerror(errno));
    }
}

//
// Write the crash report info to the json file
//
void
DumpWriter::WriteCrashReport(JsonWriter& writer)
{
    const char* exceptionType = nullptr;
    writer.OpenSection("payload");
    writer.WriteValue("protocol_version", "0.0.7");

    writer.OpenSection("configuration");
#if defined(__x86_64__)
    writer.WriteValue("architecture", "amd64");
#elif defined(__aarch64__)
    writer.WriteValue("architecture", "arm64");
#endif
    std::string version;
    assert(strncmp(sccsid, "@(#)Version ", 12) == 0);
    version.append(sccsid + 12);    // skip "@(#)Version "
    version.append(" ");            // the analyzer requires a space after the version
    writer.WriteValue("version", version.c_str());
    writer.CloseSection();          // configuration

    writer.OpenArray("threads");
    for (const ThreadInfo* thread : m_crashInfo.Threads())
    {
        writer.OpenArrayEntry();
        bool crashed = false;
        if (thread->ManagedExceptionObject() != 0)
        {
            crashed = true;
            exceptionType = "0x05000000";   // ManagedException
        }
        else
        {
            if (thread->Tid() == m_crashInfo.CrashThread())
            {
                crashed = true;
                switch (m_crashInfo.Signal())
                {
                case SIGILL:
                    exceptionType = "0x50000000";
                    break;

                case SIGFPE:
                    exceptionType = "0x70000000";
                    break;

                case SIGBUS:
                    exceptionType = "0x60000000";
                    break;

                case SIGTRAP:
                    exceptionType = "0x03000000";
                    break;

                case SIGSEGV:
                    exceptionType = "0x20000000";
                    break;

                case SIGTERM:
                    exceptionType = "0x02000000";
                    break;

                case SIGABRT:
                default:
                    exceptionType = "0x30000000";
                    break;
                }
            }
        }
        writer.WriteValueBool("is_managed", thread->IsManaged());
        writer.WriteValueBool("crashed", crashed);
        if (thread->ManagedExceptionObject() != 0)
        {
            writer.WriteValue64("managed_exception_object", thread->ManagedExceptionObject());
        }
        if (!thread->ManagedExceptionType().empty())
        {
            writer.WriteValue("managed_exception_type", thread->ManagedExceptionType().c_str());
        }
        writer.WriteValue64("native_thread_id", thread->Tid());
        writer.OpenSection("ctx");
        writer.WriteValue64("IP", thread->GetInstructionPointer());
        writer.WriteValue64("SP", thread->GetStackPointer());
        writer.WriteValue64("BP", thread->GetFramePointer());
        writer.CloseSection();      // ctx

        writer.OpenArray("unmanaged_frames");
        for (const StackFrame& frame : thread->StackFrames())
        {
            WriteStackFrame(writer, frame);
        }
        writer.CloseArray();        // unmanaged_frames
        writer.CloseArrayEntry();
    }
    writer.CloseArray();            // threads
    writer.CloseSection();          // payload

    writer.OpenSection("parameters");
    if (exceptionType != nullptr)
    {
        writer.WriteValue("ExceptionType", exceptionType);
    }
    WriteSysctl("kern.osproductversion", writer, "OSVersion");
    WriteSysctl("hw.model", writer, "SystemModel");
    writer.WriteValue("SystemManufacturer", "apple");
    writer.CloseSection();          // parameters
}

void
DumpWriter::WriteStackFrame(JsonWriter& writer, const StackFrame& frame)
{ 
    writer.OpenArrayEntry();
    writer.WriteValueBool("is_managed", frame.IsManaged());
    writer.WriteValue64("module_address", frame.ModuleAddress());
    writer.WriteValue64("stack_pointer", frame.StackPointer());
    writer.WriteValue64("native_address", frame.ReturnAddress());
    writer.WriteValue64("native_offset", frame.NativeOffset());
    if (frame.IsManaged())
    {
        writer.WriteValue32("token", frame.Token());
        writer.WriteValue32("il_offset", frame.ILOffset());
    }
    if (frame.ModuleAddress() != 0)
    {
        const ModuleInfo* moduleInfo = m_crashInfo.GetModuleInfoFromBaseAddress(frame.ModuleAddress());
        if (moduleInfo != nullptr)
        {
            std::string moduleName = GetFileName(moduleInfo->ModuleName());
            if (frame.IsManaged())
            {
                writer.WriteValue32("timestamp", moduleInfo->TimeStamp());
                writer.WriteValue32("sizeofimage", moduleInfo->ImageSize());
                writer.WriteValue("filename", moduleName.c_str());
                writer.WriteValue("guid", FormatGuid(moduleInfo->Mvid()).c_str());
            }
            else
            {
                writer.WriteValue("native_module", moduleName.c_str());
            }
        }
    }
    writer.CloseArrayEntry();
}

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
            fprintf(stderr, "Segment alignment %llu > sizeof(m_tempBuffer)\n", alignment);
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
                    fprintf(stderr, "ReadProcessMemory(%" PRIA PRIx64 ", %08zx) FAILED\n", address, bytesToRead);
                    return false;
                }

                // This can happen if the target process dies before createdump is finished
                if (read == 0) {
                    fprintf(stderr, "ReadProcessMemory(%" PRIA PRIx64 ", %08zx) returned 0 bytes read\n", address, bytesToRead);
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

    printf("Written %" PRId64 " bytes (%" PRId64 " pages) to core file\n", total, total / PAGE_SIZE);
    return true;
}
