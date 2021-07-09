// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <mach/mach.h>
#include <mach-o/loader.h>
#include <mach-o/nlist.h>
#include <mach-o/dyld_images.h>
#include <string>
#include <vector>

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
    MachOModule(MachOReader& reader, mach_vm_address_t baseAddress, mach_header_64* header = nullptr, std::string* name = nullptr);
    ~MachOModule();

    inline mach_vm_address_t BaseAddress() const { return m_baseAddress; }
    inline mach_vm_address_t LoadBias() const { return m_loadBias; }
    inline const mach_header_64& Header() const { return m_header; }
    inline const std::string& Name() const { return m_name; }

    bool ReadHeader();
    bool TryLookupSymbol(const char* symbolName, uint64_t* symbolValue);
    bool EnumerateSegments();

private:
    inline void SetName(std::string& name) { m_name = name; }

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
    bool EnumerateModules(mach_vm_address_t address, mach_header_64* header);

private:
    bool ReadString(const char* address, std::string& str);
    virtual void VisitModule(MachOModule& module) { };
    virtual void VisitSegment(MachOModule& module, const segment_command_64& segment) { };
    virtual void VisitSection(MachOModule& module, const section_64& section) { };
    virtual bool ReadMemory(void* address, void* buffer, size_t size) = 0;
    virtual void Trace(const char* format, ...) { };
    virtual void TraceVerbose(const char* format, ...) { };
};
