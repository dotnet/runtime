// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <windows.h>
#include <clrdata.h>
#include <cor.h>
#include <cordebug.h>
#define __STDC_FORMAT_MACROS
#include <inttypes.h>
#include <arrayholder.h>
#include "machoreader.h"

#if TARGET_64BIT
#define PRIx PRIx64
#define PRIu PRIu64
#define PRId PRId64
#define PRIA "016"
#define PRIxA PRIA PRIx
#else
#define PRIx PRIx32
#define PRIu PRIu32
#define PRId PRId32
#define PRIA "08"
#define PRIxA PRIA PRIx
#endif

class MachOReaderExport : public MachOReader
{
private:
    ICorDebugDataTarget* m_dataTarget;

public:
    MachOReaderExport(ICorDebugDataTarget* dataTarget) :
        m_dataTarget(dataTarget)
    {
        dataTarget->AddRef();
    }

    virtual ~MachOReaderExport()
    {
        m_dataTarget->Release();
    }

private:
    virtual bool ReadMemory(void* address, void* buffer, size_t size)
    {
        uint32_t read = 0;
        return SUCCEEDED(m_dataTarget->ReadVirtual(reinterpret_cast<CLRDATA_ADDRESS>(address), reinterpret_cast<PBYTE>(buffer), (uint32_t)size, &read));
    }
};

//
// Main entry point to get an export symbol
//
extern "C" bool
TryGetSymbol(ICorDebugDataTarget* dataTarget, uint64_t baseAddress, const char* symbolName, uint64_t* symbolAddress)
{
    MachOReaderExport reader(dataTarget);
    MachOModule module(reader, baseAddress);
    if (!module.ReadHeader())
    {
        return false;
    }
    uint64_t symbolOffset;
    if (module.TryLookupSymbol(symbolName, &symbolOffset))
    {
        *symbolAddress = symbolOffset;
        return true;
    }
    *symbolAddress = 0;
    return false;
}

//--------------------------------------------------------------------
// MachO module 
//--------------------------------------------------------------------

MachOModule::MachOModule(MachOReader& reader, mach_vm_address_t baseAddress, mach_header_64* header, std::string* name) :
    m_reader(reader),
    m_baseAddress(baseAddress),
    m_loadBias(0),
    m_commands(nullptr),
    m_symtabCommand(nullptr),
    m_nlists(nullptr),
    m_strtabAddress(0)
{
    if (header != nullptr) {
        m_header = *header;
    }
    if (name != nullptr) {
        m_name = *name;
    }
}

MachOModule::~MachOModule()
{
    if (m_commands != nullptr) {
        free(m_commands);
        m_commands = nullptr;
    }
    if (m_nlists != nullptr) {
        free(m_nlists);
        m_nlists = nullptr;
    }
}

bool
MachOModule::ReadHeader()
{
    _ASSERTE(sizeof(m_header) == sizeof(mach_header_64));
    if (!m_reader.ReadMemory((void*)m_baseAddress, &m_header, sizeof(mach_header_64)))
    {
        m_reader.Trace("ERROR: failed to read header at %p\n", (void*)m_baseAddress);
        return false;
    }
    return true;
}

bool
MachOModule::TryLookupSymbol(const char* symbolName, uint64_t* symbolValue)
{
    _ASSERTE(symbolValue != nullptr);

    if (ReadSymbolTable())
    {
        _ASSERTE(m_nlists != nullptr);
        _ASSERTE(m_strtabAddress != 0);

        // First, search just the "external" export symbols 
        if (TryLookupSymbol(m_dysymtabCommand->iextdefsym, m_dysymtabCommand->nextdefsym, symbolName, symbolValue))
        {
            m_reader.Trace("SYM: Found '%s' in external symbols\n", symbolName);
            return true;
        }
        m_reader.Trace("SYM: Missed '%s' in external symbols\n", symbolName);

        // If not found in external symbols, search all of them
        if (TryLookupSymbol(0, m_symtabCommand->nsyms, symbolName, symbolValue))
        {
            m_reader.Trace("SYM: Found '%s' in all symbols\n", symbolName);
            return true;
        }
        m_reader.Trace("SYM: Missed '%s' in all symbols\n", symbolName);
    }
    *symbolValue = 0;
    return false;
}

bool
MachOModule::TryLookupSymbol(int start, int nsyms, const char* symbolName, uint64_t* symbolValue)
{
    for (int i = 0; i < nsyms; i++)
    {
        std::string name = GetSymbolName(start + i);

        // Skip the leading underscores to match Linux externs
        const char* currentName = name.length() > 0 && name[0] == '_' ? name.c_str() + 1 : name.c_str();

        // Does this symbol match?
        if (strcmp(currentName, symbolName) == 0)
        {
            *symbolValue = m_loadBias + m_nlists[start + i].n_value;
            return true;
        }
    }
    *symbolValue = 0;
    return false;
}

bool
MachOModule::EnumerateSegments()
{
    if (!ReadLoadCommands())
    {
        return false;
    }
    _ASSERTE(!m_segments.empty());

    for (const segment_command_64* segment : m_segments)
    {
        m_reader.VisitSegment(*this, *segment);

        const section_64* section = (section_64*)((uint64_t)segment + sizeof(segment_command_64));

        for (int s = 0; s < segment->nsects; s++, section++)
        {
            m_reader.VisitSection(*this, *section);
        }
    }
    return true;
}

bool
MachOModule::ReadLoadCommands()
{
    if (m_commands == nullptr)
    {
        // Read load commands
        void* commandsAddress = (void*)(m_baseAddress + sizeof(mach_header_64));
        m_commands = (load_command*)malloc(m_header.sizeofcmds);
        if (m_commands == nullptr)
        {
            m_reader.Trace("ERROR: Failed to allocate %d byte load commands\n", m_header.sizeofcmds);
            return false;
        }
        if (!m_reader.ReadMemory(commandsAddress, m_commands, m_header.sizeofcmds))
        {
            m_reader.Trace("ERROR: Failed to read load commands at %p of %d\n", commandsAddress, m_header.sizeofcmds);
            return false;
        }
        load_command* command = m_commands;

        for (int i = 0; i < m_header.ncmds; i++)
        {
            m_reader.TraceVerbose("CMD: load command cmd %02x (%d) size %d\n", command->cmd, command->cmd, command->cmdsize);

            switch (command->cmd)
            {
            case LC_SYMTAB:
                m_symtabCommand = (symtab_command*)command;
                break;

            case LC_DYSYMTAB:
                m_dysymtabCommand = (dysymtab_command*)command;
                break;

            case LC_SEGMENT_64:
                segment_command_64* segment = (segment_command_64*)command;
                m_segments.push_back(segment);

                // Calculate the load bias for the module. This is the value to add to the vmaddr of a
                // segment to get the actual address.
                if (strcmp(segment->segname, SEG_TEXT) == 0)
                {
                    m_loadBias = m_baseAddress - segment->vmaddr;
                }

                m_reader.TraceVerbose("CMD: vmaddr %016llx vmsize %016llx fileoff %016llx filesize %016llx nsects %d max %c%c%c init %c%c%c %02x %s\n",
                    segment->vmaddr,
                    segment->vmsize,
                    segment->fileoff,
                    segment->filesize,
                    segment->nsects,
                    (segment->maxprot & VM_PROT_READ) ? 'r' : '-',
                    (segment->maxprot & VM_PROT_WRITE) ? 'w' : '-',
                    (segment->maxprot & VM_PROT_EXECUTE) ? 'x' : '-',
                    (segment->initprot & VM_PROT_READ) ? 'r' : '-',
                    (segment->initprot & VM_PROT_WRITE) ? 'w' : '-',
                    (segment->initprot & VM_PROT_EXECUTE) ? 'x' : '-',
                    segment->flags,
                    segment->segname);

                section_64* section = (section_64*)((uint64_t)segment + sizeof(segment_command_64));
                for (int s = 0; s < segment->nsects; s++, section++)
                {
                    m_reader.TraceVerbose("     addr %016llx size %016llx off %08x align %02x flags %02x %s\n",
                        section->addr,
                        section->size,
                        section->offset,
                        section->align,
                        section->flags,
                        section->sectname);
                }
                break;
            }
            // Get next load command
            command = (load_command*)((char*)command + command->cmdsize);
        }
        m_reader.TraceVerbose("CMD: load bias %016llx\n", m_loadBias);
    }

    return true;
}

bool
MachOModule::ReadSymbolTable()
{
    if (m_nlists == nullptr)
    {
        if (!ReadLoadCommands())
        {
            return false;
        }
        _ASSERTE(m_symtabCommand != nullptr);
        _ASSERTE(m_strtabAddress == 0);

        m_reader.TraceVerbose("SYM: symoff %08x nsyms %d stroff %08x strsize %d iext %d next %d iundef %d nundef %d extref %d nextref %d\n",
            m_symtabCommand->symoff,
            m_symtabCommand->nsyms,
            m_symtabCommand->stroff,
            m_symtabCommand->strsize,
            m_dysymtabCommand->iextdefsym,
            m_dysymtabCommand->nextdefsym,
            m_dysymtabCommand->iundefsym,
            m_dysymtabCommand->nundefsym,
            m_dysymtabCommand->extrefsymoff,
            m_dysymtabCommand->nextrefsyms);

        // Read the entire symbol part of symbol table. An array of "nlist" structs.
        void* symbolTableAddress = (void*)GetAddressFromFileOffset(m_symtabCommand->symoff);
        size_t symtabSize = sizeof(nlist_64) * m_symtabCommand->nsyms;
        m_nlists = (nlist_64*)malloc(symtabSize);
        if (m_nlists == nullptr)
        {
            m_reader.Trace("ERROR: Failed to allocate %zu byte symtab\n", symtabSize);
            return false;
        }
        if (!m_reader.ReadMemory(symbolTableAddress, m_nlists, symtabSize))
        {
            m_reader.Trace("ERROR: Failed to read symtab at %p of %zu\n", symbolTableAddress, symtabSize);
            return false;
        }

        // Save the symbol string table address.
        m_strtabAddress = GetAddressFromFileOffset(m_symtabCommand->stroff);
    }
    return true;
}

uint64_t
MachOModule::GetAddressFromFileOffset(uint32_t offset)
{
    _ASSERTE(!m_segments.empty());
    for (const segment_command_64* segment : m_segments)
    {
        if (offset >= segment->fileoff && offset < (segment->fileoff + segment->filesize))
        {
            return m_loadBias + offset + segment->vmaddr - segment->fileoff;
        }
    }
    return m_loadBias + offset;
}

std::string
MachOModule::GetSymbolName(int index)
{
    uint64_t symbolNameAddress = m_strtabAddress + m_nlists[index].n_un.n_strx;
    std::string result;
    while (true)
    {
        char c = 0;
        if (!m_reader.ReadMemory((void*)symbolNameAddress, &c, sizeof(char)))
        {
            m_reader.Trace("ERROR: Failed to read string table at %p\n", (void*)symbolNameAddress);
            break;
        }
        if (c == '\0')
        {
            break;
        }
        result.append(1, c);
        symbolNameAddress++;
    }
    return result;
}

//--------------------------------------------------------------------
// MachO reader
//--------------------------------------------------------------------

MachOReader::MachOReader()
{
}

bool
MachOReader::EnumerateModules(mach_vm_address_t address, mach_header_64* header)
{
    _ASSERTE(header->magic == MH_MAGIC_64);
    _ASSERTE(header->filetype == MH_DYLINKER);

    MachOModule dylinker(*this, address, header);

    // Search for symbol for the dyld image info cache
    uint64_t dyldInfoAddress = 0;
    if (!dylinker.TryLookupSymbol("dyld_all_image_infos", &dyldInfoAddress))
    {
        Trace("ERROR: Can not find the _dyld_all_image_infos symbol\n");
        return false;
    }

    // Read the all image info from the dylinker image
    dyld_all_image_infos dyldInfo;

    if (!ReadMemory((void*)dyldInfoAddress, &dyldInfo, sizeof(dyld_all_image_infos)))
    {
        Trace("ERROR: Failed to read dyld_all_image_infos at %p\n", (void*)dyldInfoAddress);
        return false;
    }
    std::string dylinkerPath;
    if (!ReadString(dyldInfo.dyldPath, dylinkerPath))
    {
        Trace("ERROR: Failed to read name at %p\n", dyldInfo.dyldPath);
        return false;
    }
    dylinker.SetName(dylinkerPath);
    Trace("MOD: %016llx %08x %s\n", dylinker.BaseAddress(), dylinker.Header().flags, dylinker.Name().c_str());
    VisitModule(dylinker);

    void* imageInfosAddress = (void*)dyldInfo.infoArray;
    size_t imageInfosSize = dyldInfo.infoArrayCount * sizeof(dyld_image_info);
    Trace("MOD: infoArray %p infoArrayCount %d\n", dyldInfo.infoArray, dyldInfo.infoArrayCount);

    ArrayHolder<dyld_image_info> imageInfos = new (std::nothrow) dyld_image_info[dyldInfo.infoArrayCount];
    if (imageInfos == nullptr)
    {
        Trace("ERROR: Failed to allocate %zu byte image infos\n", imageInfosSize);
        return false;
    }
    if (!ReadMemory(imageInfosAddress, imageInfos, imageInfosSize))
    {
        Trace("ERROR: Failed to read dyld_all_image_infos at %p\n", imageInfosAddress);
        return false;
    }
    for (int i = 0; i < dyldInfo.infoArrayCount; i++)
    {
        mach_vm_address_t imageAddress = (mach_vm_address_t)imageInfos[i].imageLoadAddress;
        const char* imageFilePathAddress = imageInfos[i].imageFilePath;

        std::string imagePath;
        if (!ReadString(imageFilePathAddress, imagePath))
        {
            Trace("ERROR: Failed to read image name at %p\n", imageFilePathAddress);
            continue;
        }
        MachOModule module(*this, imageAddress, nullptr, &imagePath);
        if (!module.ReadHeader())
        {
            continue;
        }
        Trace("MOD: %016llx %08x %s\n", imageAddress, module.Header().flags, imagePath.c_str());
        VisitModule(module);
    }
    return true;
}

bool
MachOReader::ReadString(const char* address, std::string& str)
{
    for (int i = 0; i < MAX_LONGPATH; i++)
    {
        char c = 0;
        if (!ReadMemory((void*)(address + i), &c, sizeof(char)))
        {
            Trace("ERROR: Failed to read string at %p\n", (void*)(address + i));
            return false;
        }
        if (c == '\0')
        {
            break;
        }
        str.append(1, c);
    }
    return true;
}
