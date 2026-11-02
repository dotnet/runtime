// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#ifdef HOST_APPLE
#include <mach/mach.h>
#include <mach-o/loader.h>
#include <mach-o/nlist.h>
#include <mach-o/dyld_images.h>
#else
// The export reader uses these fixed-width Mach-O records on every host.
typedef uint64_t mach_vm_address_t;
const uint32_t LC_SYMTAB = 2;
const uint32_t LC_DYSYMTAB = 0xb;
const uint32_t LC_SEGMENT_64 = 0x19;
const int VM_PROT_READ = 1;
const int VM_PROT_WRITE = 2;
const int VM_PROT_EXECUTE = 4;
#define SEG_TEXT "__TEXT"

struct mach_header_64
{
    uint32_t magic;
    int32_t cputype, cpusubtype;
    uint32_t filetype, ncmds, sizeofcmds, flags, reserved;
};
struct load_command
{
    uint32_t cmd, cmdsize;
};
struct segment_command_64
{
    uint32_t cmd, cmdsize;
    char segname[16];
    uint64_t vmaddr, vmsize, fileoff, filesize;
    int32_t maxprot, initprot;
    uint32_t nsects, flags;
};
struct section_64
{
    char sectname[16], segname[16];
    uint64_t addr, size;
    uint32_t offset, align, reloff, nreloc, flags, reserved1, reserved2, reserved3;
};
struct symtab_command
{
    uint32_t cmd, cmdsize, symoff, nsyms, stroff, strsize;
};
struct dysymtab_command
{
    uint32_t cmd, cmdsize;
    uint32_t ilocalsym, nlocalsym, iextdefsym, nextdefsym, iundefsym, nundefsym;
    uint32_t tocoff, ntoc, modtaboff, nmodtab, extrefsymoff, nextrefsyms;
    uint32_t indirectsymoff, nindirectsyms, extreloff, nextrel, locreloff, nlocrel;
};
struct nlist_64
{
    union { uint32_t n_strx; } n_un;
    uint8_t n_type, n_sect;
    uint16_t n_desc;
    uint64_t n_value;
};
#endif
#include <string>
#include <vector>

struct ICorDebugDataTarget;
bool TryGetMachOSymbol(ICorDebugDataTarget* dataTarget, uint64_t baseAddress, const char* symbolName, uint64_t* symbolAddress);

class MachOReader;

class MachOModule
{
    friend MachOReader;
private:
    MachOReader& m_reader;
    mach_vm_address_t m_baseAddress;
    mach_vm_address_t m_loadBias;
    mach_header_64 m_header;
    std::string m_name;
    load_command* m_commands;
    std::vector<segment_command_64*> m_segments;
    symtab_command* m_symtabCommand;
    dysymtab_command* m_dysymtabCommand;
    nlist_64* m_nlists;
    uint64_t m_strtabAddress;

public:
    MachOModule(MachOReader& reader, mach_vm_address_t baseAddress, std::string* name = nullptr);
    ~MachOModule();

    inline mach_vm_address_t BaseAddress() const { return m_baseAddress; }
    inline mach_vm_address_t LoadBias() const { return m_loadBias; }
    inline const mach_header_64& Header() const { return m_header; }
    inline const std::string& Name() const { return m_name; }

    bool ReadHeader();
    // Returns the relocated virtual address of the symbol in the target process.
    bool TryLookupSymbol(const char* symbolName, uint64_t* symbolAddress);
    bool TryLookupSymbol(int start, int nsyms, const char* symbolName, uint64_t* symbolAddress);
    bool EnumerateSegments();

private:
    bool ReadLoadCommands();
    bool ReadSymbolTable();
    uint64_t GetAddressFromFileOffset(uint32_t offset);
    std::string GetSymbolName(int index);
};

class MachOReader
{
    friend MachOModule;
public:
    MachOReader();
#ifdef HOST_APPLE
    bool EnumerateModules(mach_vm_address_t dyldInfoAddress);
#endif

private:
#ifdef HOST_APPLE
    bool TryRegisterModule(const struct mach_header* imageAddress, const char* imageFilePathAddress, bool dylinker);
#endif
    bool ReadString(const char* address, std::string& str);
    virtual void VisitModule(MachOModule& module) { };
    virtual void VisitSegment(MachOModule& module, const segment_command_64& segment) { };
    virtual void VisitSection(MachOModule& module, const section_64& section) { };
    virtual bool ReadMemory(void* address, void* buffer, size_t size) = 0;
    virtual void Trace(const char* format, ...) { };
    virtual void TraceVerbose(const char* format, ...) { };
};
