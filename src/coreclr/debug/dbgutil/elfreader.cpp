// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <windows.h>
#include <clrdata.h>
#include <cor.h>
#include <cordebug.h>
#define __STDC_FORMAT_MACROS
#include <inttypes.h>
#include "elfreader.h"

#define Elf_Ehdr   ElfW(Ehdr)
#define Elf_Phdr   ElfW(Phdr)
#define Elf_Shdr   ElfW(Shdr)
#define Elf_Nhdr   ElfW(Nhdr)
#define Elf_Dyn    ElfW(Dyn)
#define Elf_Sym    ElfW(Sym)

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

#ifndef HOST_WINDOWS
static const char ElfMagic[] = { 0x7f, 'E', 'L', 'F', '\0' };
#endif

class ElfReaderExport : public ElfReader
{
private:
    ICorDebugDataTarget* m_dataTarget;

public:
    ElfReaderExport(ICorDebugDataTarget* dataTarget) :
        m_dataTarget(dataTarget)
    {
        dataTarget->AddRef();
    }

    virtual ~ElfReaderExport()
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
    ElfReaderExport elfreader(dataTarget);
    if (elfreader.PopulateForSymbolLookup(baseAddress))
    {
        uint64_t symbolOffset;
        if (elfreader.TryLookupSymbol(symbolName, &symbolOffset))
        {
            *symbolAddress = baseAddress + symbolOffset;
            return true;
        }
    }
    *symbolAddress = 0;
    return false;
}

//
// ELF reader constructor/destructor
//

ElfReader::ElfReader() :
    m_gnuHashTableAddr(nullptr),
    m_stringTableAddr(nullptr),
    m_stringTableSize(0),
    m_symbolTableAddr(nullptr),
    m_buckets(nullptr),
    m_chainsAddress(nullptr)
{
    memset(&m_hashTable, 0, sizeof(m_hashTable));
}

ElfReader::~ElfReader()
{
    if (m_buckets != nullptr) {
        free(m_buckets);
    }
}

//
// Initialize the ELF reader from a module the base address. This function
// caches the info neccessary in the ElfReader class look up symbols.
//
bool
ElfReader::PopulateForSymbolLookup(uint64_t baseAddress)
{
    Trace("PopulateForSymbolLookup: base %" PRIA PRIx64 "\n", baseAddress);
    Elf_Dyn* dynamicAddr = nullptr;
    uint64_t loadbias = 0;

    // Enumerate program headers searching for the PT_DYNAMIC header, etc.
    if (!EnumerateProgramHeaders(
        baseAddress,
#ifdef TARGET_ALPINE_LINUX
        // On Alpine, the below dynamic entries for hash, string table, etc. are
        // RVAs instead of absolute address like on all other Linux distros. Get
        // the "loadbias" (basically the base address of the module) and add to
        // these RVAs.
        &loadbias,
#else
        nullptr,
#endif
        &dynamicAddr))
    {
        return false;
    }

    if (dynamicAddr == nullptr) {
        return false;
    }

    // Search for dynamic entries
    for (;;)
    {
        Elf_Dyn dyn;
        if (!ReadMemory(dynamicAddr, &dyn, sizeof(dyn))) {
            Trace("ERROR: ReadMemory(%p, %" PRIx ") dyn FAILED\n", dynamicAddr, sizeof(dyn));
            return false;
        }
        Trace("DSO: dyn %p tag %" PRId " (%" PRIx ") d_ptr %" PRIxA "\n", dynamicAddr, dyn.d_tag, dyn.d_tag, dyn.d_un.d_ptr);
        if (dyn.d_tag == DT_NULL) {
            break;
        }
        else if (dyn.d_tag == DT_GNU_HASH) {
            m_gnuHashTableAddr = (void*)(dyn.d_un.d_ptr + loadbias);
        }
        else if (dyn.d_tag == DT_STRTAB) {
            m_stringTableAddr = (void*)(dyn.d_un.d_ptr + loadbias);
        }
        else if (dyn.d_tag == DT_STRSZ) {
            m_stringTableSize = (int)dyn.d_un.d_ptr;
        }
        else if (dyn.d_tag == DT_SYMTAB) {
            m_symbolTableAddr = (void*)(dyn.d_un.d_ptr + loadbias);
        }
        dynamicAddr++;
    }

    if (m_gnuHashTableAddr == nullptr || m_stringTableAddr == nullptr || m_symbolTableAddr == nullptr) {
        Trace("ERROR: hash, string or symbol table address not found\n");
        return false;
    }

    // Initialize the hash table
    if (!InitializeGnuHashTable()) {
        return false;
    }

    return true;
}

//
// Symbol table support
//

bool
ElfReader::TryLookupSymbol(std::string symbolName, uint64_t* symbolOffset)
{
    std::vector<int32_t> symbolIndexes;
    if (GetPossibleSymbolIndex(symbolName, symbolIndexes)) {
        Elf_Sym symbol;
        for (int32_t possibleLocation : symbolIndexes)
        {
            if (GetSymbol(possibleLocation, &symbol))
            {
                std::string possibleName;
                if (GetStringAtIndex(symbol.st_name, possibleName))
                {
                    if (symbolName.compare(possibleName) == 0)
                    {
                        *symbolOffset = symbol.st_value;
                        Trace("TryLookupSymbol found '%s' at offset %" PRIxA "\n", symbolName.c_str(), *symbolOffset);
                        return true;
                    }
                }
            }
        }
    }
    Trace("TryLookupSymbol '%s' not found\n", symbolName.c_str());
    *symbolOffset = 0;
    return false;
}

bool
ElfReader::GetSymbol(int32_t index, Elf_Sym* symbol)
{
    int symSize = sizeof(Elf_Sym);
    if (!ReadMemory((char*)m_symbolTableAddr + (index * symSize), symbol, symSize)) {
        return false;
    }
    return true;
}

//
// Hash (GNU) hash table support
//

bool
ElfReader::InitializeGnuHashTable()
{
    if (!ReadMemory(m_gnuHashTableAddr, &m_hashTable, sizeof(m_hashTable))) {
        Trace("ERROR: InitializeGnuHashTable hashtable ReadMemory(%p) FAILED\n", m_gnuHashTableAddr);
        return false;
    }
    if (m_hashTable.BucketCount <= 0 || m_hashTable.SymbolOffset == 0) {
        Trace("ERROR: InitializeGnuHashTable invalid BucketCount or SymbolOffset\n");
        return false;
    }
    m_buckets = (int32_t*)malloc(m_hashTable.BucketCount * sizeof(int32_t));
    if (m_buckets == nullptr) {
        return false;
    }
    void* bucketsAddress = (char*)m_gnuHashTableAddr + sizeof(GnuHashTable) + (m_hashTable.BloomSize * sizeof(size_t));
    if (!ReadMemory(bucketsAddress, m_buckets, m_hashTable.BucketCount * sizeof(int32_t))) {
        Trace("ERROR: InitializeGnuHashTable buckets ReadMemory(%p) FAILED\n", bucketsAddress);
        return false;
    }
    m_chainsAddress = (char*)bucketsAddress + (m_hashTable.BucketCount * sizeof(int32_t));
    return true;
}

bool
ElfReader::GetPossibleSymbolIndex(const std::string& symbolName, std::vector<int32_t>& symbolIndexes)
{
    uint32_t hash = Hash(symbolName);
    int i = m_buckets[hash % m_hashTable.BucketCount] - m_hashTable.SymbolOffset;
    Trace("GetPossibleSymbolIndex hash %08x index: %d BucketCount %d SymbolOffset %08x\n", hash, i, m_hashTable.BucketCount, m_hashTable.SymbolOffset);
    for (;; i++)
    {
        int32_t chainVal;
        if (!GetChain(i, &chainVal)) {
            Trace("ERROR: GetPossibleSymbolIndex GetChain FAILED\n");
            return false;
        }
        if ((chainVal & 0xfffffffe) == (hash & 0xfffffffe))
        {
            symbolIndexes.push_back(i + m_hashTable.SymbolOffset);
        }
        if ((chainVal & 0x1) == 0x1)
        {
            break;
        }
    }
    return true;
}

uint32_t
ElfReader::Hash(const std::string& symbolName)
{
    uint32_t h = 5381;
    for (unsigned int i = 0; i < symbolName.length(); i++)
    {
        h = (h << 5) + h + symbolName[i];
    }
    return h;
}

bool
ElfReader::GetChain(int index, int32_t* chain)
{
    return ReadMemory((char*)m_chainsAddress + (index * sizeof(int32_t)), chain, sizeof(int32_t));
}

//
// String table support
//

bool
ElfReader::GetStringAtIndex(int index, std::string& result)
{
    while(true)
    {
        if (index > m_stringTableSize) {
            Trace("ERROR: GetStringAtIndex index %d > string table size\n", index);
            return false;
        }
        char ch;
        void* address = (char*)m_stringTableAddr + index;
        if (!ReadMemory(address, &ch, sizeof(ch))) {
            Trace("ERROR: GetStringAtIndex ReadMemory(%p) FAILED\n", address);
            return false;
        }
        if (ch == '\0') {
            break;
        }
        result.append(1, ch);
        index++;
    }
    return true;
}

#ifdef HOST_UNIX

//
// Enumerate all the ELF info starting from the root program header. This
// function doesn't cache any state in the ElfReader class.
//
bool
ElfReader::EnumerateElfInfo(Elf_Phdr* phdrAddr, int phnum)
{
    Trace("EnumerateElfInfo: phdr %p phnum %d\n", phdrAddr, phnum);

    if (phdrAddr == nullptr || phnum <= 0) {
        return false;
    }
    uint64_t baseAddress = (uint64_t)phdrAddr - sizeof(Elf_Ehdr);
    Elf_Dyn* dynamicAddr = nullptr;

    // Enumerate program headers searching for the PT_DYNAMIC header, etc.
    if (!EnumerateProgramHeaders(phdrAddr, phnum, baseAddress, nullptr, &dynamicAddr)) {
        return false;
    }
    return EnumerateLinkMapEntries(dynamicAddr);
}

//
// Enumerate through the dynamic debug link map entries
//
bool
ElfReader::EnumerateLinkMapEntries(Elf_Dyn* dynamicAddr)
{
    if (dynamicAddr == nullptr) {
        return false;
    }

    // Search dynamic entries for DT_DEBUG (r_debug entry)
    struct r_debug* rdebugAddr = nullptr;
    for (;;)
    {
        Elf_Dyn dyn;
        if (!ReadMemory(dynamicAddr, &dyn, sizeof(dyn))) {
            Trace("ERROR: ReadMemory(%p, %" PRIx ") dyn FAILED\n", dynamicAddr, sizeof(dyn));
            return false;
        }
        Trace("DSO: dyn %p tag %" PRId " (%" PRIx ") d_ptr %" PRIxA "\n", dynamicAddr, dyn.d_tag, dyn.d_tag, dyn.d_un.d_ptr);
        if (dyn.d_tag == DT_NULL) {
            break;
        }
        else if (dyn.d_tag == DT_DEBUG) {
            rdebugAddr = reinterpret_cast<struct r_debug*>(dyn.d_un.d_ptr);
        }
        dynamicAddr++;
    }

    Trace("DSO: rdebugAddr %p\n", rdebugAddr);
    if (rdebugAddr == nullptr) {
        return false;
    }

    struct r_debug debugEntry;
    if (!ReadMemory(rdebugAddr, &debugEntry, sizeof(debugEntry))) {
        Trace("ERROR: ReadMemory(%p, %" PRIx ") r_debug FAILED\n", rdebugAddr, sizeof(debugEntry));
        return false;
    }

    // Add the DSO link_map entries
    for (struct link_map* linkMapAddr = debugEntry.r_map; linkMapAddr != nullptr;)
    {
        struct link_map map;
        if (!ReadMemory(linkMapAddr, &map, sizeof(map))) {
            Trace("ERROR: ReadMemory(%p, %" PRIx ") link_map FAILED\n", linkMapAddr, sizeof(map));
            return false;
        }
        // Read the module's name and make sure the memory is added to the core dump
        std::string moduleName;
        if (map.l_name != 0)
        {
            for (int i = 0; i < PATH_MAX; i++)
            {
                char ch;
                if (!ReadMemory(map.l_name + i, &ch, sizeof(ch))) {
                    Trace("DSO: ReadMemory link_map name %p + %d FAILED\n", map.l_name, i);
                    break;
                }
                if (ch == '\0') {
                    break;
                }
                moduleName.append(1, ch);
            }
        }
        Trace("\nDSO: link_map entry %p l_ld %p l_addr (Ehdr) %p l_name %p %s\n", linkMapAddr, map.l_ld, map.l_addr, map.l_name, moduleName.c_str());

        // Call the derived class for each module
        VisitModule(map.l_addr, moduleName);

        linkMapAddr = map.l_next;
    }

    return true;
}

#endif // HOST_UNIX

bool
ElfReader::EnumerateProgramHeaders(uint64_t baseAddress, uint64_t* ploadbias, Elf_Dyn** pdynamicAddr)
{
    Elf_Ehdr ehdr;
    if (!ReadMemory((void*)baseAddress, &ehdr, sizeof(ehdr))) {
        Trace("ERROR: EnumerateProgramHeaders ReadMemory(%p, %" PRIx ") ehdr FAILED\n", (void*)baseAddress, sizeof(ehdr));
        return false;
    }
    if (memcmp(ehdr.e_ident, ElfMagic, strlen(ElfMagic)) != 0) {
        Trace("ERROR: EnumerateProgramHeaders Invalid elf header signature\n");
        return false;
    }
    _ASSERTE(ehdr.e_phentsize == sizeof(Elf_Phdr));
#ifdef TARGET_64BIT
    _ASSERTE(ehdr.e_ident[EI_CLASS] == ELFCLASS64);
#else
    _ASSERTE(ehdr.e_ident[EI_CLASS] == ELFCLASS32);
#endif
    _ASSERTE(ehdr.e_ident[EI_DATA] == ELFDATA2LSB);
    int phnum = ehdr.e_phnum;
#ifdef PN_XNUM
     _ASSERTE(phnum != PN_XNUM);
#endif
    if (ehdr.e_phoff == 0 || phnum <= 0) {
        return false;
    }
    Trace("ELF: type %d mach 0x%x ver %d flags 0x%x phnum %d phoff %" PRIxA " phentsize 0x%02x shnum %d shoff %" PRIxA " shentsize 0x%02x shstrndx %d\n",
        ehdr.e_type, ehdr.e_machine, ehdr.e_version, ehdr.e_flags, phnum, ehdr.e_phoff, ehdr.e_phentsize, ehdr.e_shnum, ehdr.e_shoff, ehdr.e_shentsize, ehdr.e_shstrndx);

    Elf_Phdr* phdrAddr = reinterpret_cast<Elf_Phdr*>(baseAddress + ehdr.e_phoff);
    return EnumerateProgramHeaders(phdrAddr, phnum, baseAddress, ploadbias, pdynamicAddr);
}

//
// Enumerate and find the dynamic program header entry
//
bool
ElfReader::EnumerateProgramHeaders(Elf_Phdr* phdrAddr, int phnum, uint64_t baseAddress, uint64_t* ploadbias, Elf_Dyn** pdynamicAddr)
{
    uint64_t loadbias = baseAddress;

    // Calculate the load bias from the PT_LOAD program headers
    for (int i = 0; i < phnum; i++)
    {
        Elf_Phdr ph;
        if (!ReadMemory(phdrAddr + i, &ph, sizeof(ph))) {
            Trace("ERROR: ReadMemory(%p, %" PRIx ") phdr FAILED\n", phdrAddr + i, sizeof(ph));
            return false;
        }
        if (ph.p_type == PT_LOAD && ph.p_offset == 0) {
            loadbias -= ph.p_vaddr;
            Trace("PHDR: loadbias %" PRIA PRIx64 "\n", loadbias);
            break;
        }
    }

    if (ploadbias != nullptr) {
        *ploadbias = loadbias;
    }

    // Enumerate all the program headers
    for (int i = 0; i < phnum; i++)
    {
        Elf_Phdr ph;
        if (!ReadMemory(phdrAddr + i, &ph, sizeof(ph))) {
            Trace("ERROR: ReadMemory(%p, %" PRIx ") phdr FAILED\n", phdrAddr + i, sizeof(ph));
            return false;
        }
        Trace("PHDR: %p type %d (%x) vaddr %" PRIxA " memsz %" PRIxA " paddr %" PRIxA " filesz %" PRIxA " offset %" PRIxA " align %" PRIxA "\n",
            phdrAddr + i, ph.p_type, ph.p_type, ph.p_vaddr, ph.p_memsz, ph.p_paddr, ph.p_filesz, ph.p_offset, ph.p_align);

        switch (ph.p_type)
        {
        case PT_DYNAMIC:
            if (pdynamicAddr != nullptr) {
                *pdynamicAddr = reinterpret_cast<Elf_Dyn*>(loadbias + ph.p_vaddr);
            }
            break;
        }

        // Give any derived classes a chance at the program header
        VisitProgramHeader(loadbias, baseAddress, &ph);
    }

    return true;
}

#ifdef HOST_WINDOWS

/* ELF 32bit header */
Elf32_Ehdr::Elf32_Ehdr()
{
    e_ident[EI_MAG0] = ElfMagic[0];
    e_ident[EI_MAG1] = ElfMagic[1];
    e_ident[EI_MAG2] = ElfMagic[2];
    e_ident[EI_MAG3] = ElfMagic[3];
    e_ident[EI_CLASS] = ELFCLASS32;
    e_ident[EI_DATA] = ELFDATA2LSB;
    e_ident[EI_VERSION] = EV_CURRENT;
    e_ident[EI_OSABI] = ELFOSABI_NONE;
    e_ident[EI_ABIVERSION] = 0;
    for (int i = EI_PAD; i < EI_NIDENT; ++i) {
        e_ident[i] = 0;
    }
    e_type = ET_REL;
#if defined(TARGET_X86)
    e_machine = EM_386;
#elif defined(TARGET_ARM)
    e_machine = EM_ARM;
#endif
    e_flags = 0;
    e_version = 1;
    e_entry = 0;
    e_phoff = 0;
    e_ehsize = sizeof(Elf32_Ehdr);
    e_phentsize = 0;
    e_phnum = 0;
}

/* ELF 64bit header */
Elf64_Ehdr::Elf64_Ehdr()
{
    e_ident[EI_MAG0] = ElfMagic[0];
    e_ident[EI_MAG1] = ElfMagic[1];
    e_ident[EI_MAG2] = ElfMagic[2];
    e_ident[EI_MAG3] = ElfMagic[3];
    e_ident[EI_CLASS] = ELFCLASS64;
    e_ident[EI_DATA] = ELFDATA2LSB;
    e_ident[EI_VERSION] = EV_CURRENT;
    e_ident[EI_OSABI] = ELFOSABI_NONE;
    e_ident[EI_ABIVERSION] = 0;
    for (int i = EI_PAD; i < EI_NIDENT; ++i) {
        e_ident[i] = 0;
    }
    e_type = ET_REL;
#if defined(TARGET_AMD64)
    e_machine = EM_X86_64;
#elif defined(TARGET_ARM64)
    e_machine = EM_AARCH64;
#elif defined(TARGET_LOONGARCH64)
    e_machine = EM_LOONGARCH;
#endif
    e_flags = 0;
    e_version = 1;
    e_entry = 0;
    e_phoff = 0;
    e_ehsize = sizeof(Elf64_Ehdr);
    e_phentsize = 0;
    e_phnum = 0;
}

#endif // HOST_WINDOWS
