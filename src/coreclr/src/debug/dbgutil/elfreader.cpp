// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "elfreader.h"

class ElfReaderTarget : public ElfReader
{
private:
    ICorDebugDataTarget* m_dataTarget;

public:
    ElfReaderTarget(ICorDebugDataTarget* dataTarget, uint64_t baseAddress) :
        ElfReader(baseAddress),
        m_dataTarget(dataTarget)
    {
        dataTarget->AddRef();
    }

    virtual ~ElfReaderTarget()
    {
        m_dataTarget->Release();
    }

private:
    virtual bool ReadMemory(void* address, void* buffer, size_t size)
    {
        uint32_t read = 0;
        return SUCCEEDED(m_dataTarget->ReadVirtual(reinterpret_cast<CLRDATA_ADDRESS>(address), reinterpret_cast<PBYTE>(buffer), size, &read));
    }
};

//
// Main entry point to get an export symbol
//
bool
TryGetSymbol(ICorDebugDataTarget* dataTarget, uint64_t baseAddress, const char* symbolName, uint64_t* symbolAddress)
{
    ElfReader* reader = new ElfReaderTarget(dataTarget, baseAddress);
    if (reader->PopulateELFInfo())
    {
        return reader->TryLookupSymbol(symbolName, symbolAddress);
    }
    *symbolAddress = 0;
    return false;
}

//
// ELF reader constructor/destructor
//

ElfReader::ElfReader(uint64_t baseAddress) :
    m_baseAddress(baseAddress)
{
}

ElfReader::~ElfReader()
{
    if (m_buckets != nullptr) {
        delete m_buckets;
    }
}

static const char ElfMagic[] = { 0x7f, 'E', 'L', 'F', '\0' };

//
// Initialize the ELF reader
//
bool
ElfReader::PopulateELFInfo()
{
    Elf_Ehdr ehdr;
    if (!ReadMemory((void*)m_baseAddress, &ehdr, sizeof(ehdr))) {
        TRACE("ERROR: ReadMemory(%p, %" PRIx ") ehdr FAILED\n", (void*)m_baseAddress, sizeof(ehdr));
        return false;
    }
    if (memcmp(ehdr.e_ident, ElfMagic, strlen(ElfMagic)) != 0) {
        TRACE("ERROR: invalid elf header signature\n");
        return false;
    }
    Elf_Phdr* phdrAddr = reinterpret_cast<Elf_Phdr*>(m_baseAddress + ehdr.e_phoff);
    int phnum = ehdr.e_phnum;

    if (phnum <= 0 || phdrAddr == nullptr) {
        return false;
    }
    Elf_Dyn* dynamicAddr = nullptr;

    TRACE("PopulateELFInfo: base %" PRIA PRIx64 " phdr %p phnum %d\n", m_baseAddress, phdrAddr, phnum);

    // Enumerate program headers searching for the PT_DYNAMIC header, etc.
    if (!EnumerateProgramHeaders(phdrAddr, phnum, &dynamicAddr)) {
        return false;
    }

    if (dynamicAddr == nullptr) {
        return false;
    }

    // Search for dynamic entries
    for (;;) {
        Elf_Dyn dyn;
        if (!ReadMemory(dynamicAddr, &dyn, sizeof(dyn))) {
            TRACE("ERROR: ReadMemory(%p, %" PRIx ") dyn FAILED\n", dynamicAddr, sizeof(dyn));
            return false;
        }
        TRACE("DSO: dyn %p tag %" PRId " (%" PRIx ") d_ptr %" PRIxA "\n", dynamicAddr, dyn.d_tag, dyn.d_tag, dyn.d_un.d_ptr);
        if (dyn.d_tag == DT_NULL) {
            break;
        }
        else if (dyn.d_tag == DT_DEBUG) {
            m_rdebugAddr = (void*)dyn.d_un.d_ptr;
        }
        else if (dyn.d_tag == DT_GNU_HASH) {
            m_gnuHashTableAddr = (void*)dyn.d_un.d_ptr;
        }
        else if (dyn.d_tag == DT_STRTAB) {
            m_stringTableAddr = (void*)dyn.d_un.d_ptr;
        }
        else if (dyn.d_tag == DT_STRSZ) {
            m_stringTableSize = (int)dyn.d_un.d_ptr;
        }
        else if (dyn.d_tag == DT_SYMTAB) {
            m_symbolTableAddr = (void*)dyn.d_un.d_ptr;
        }
        dynamicAddr++;
    }

    if (m_gnuHashTableAddr == nullptr || m_stringTableAddr == nullptr || m_symbolTableAddr == nullptr) {
        TRACE("ERROR: hash, string or symbol table address not found\n");
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
ElfReader::TryLookupSymbol(std::string symbolName, uint64_t* symbolAddress)
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
                        *symbolAddress = m_baseAddress + symbol.st_value;
                        TRACE("TryLookupSymbol found '%s' at %" PRIxA "\n", symbolName.c_str(), *symbolAddress);
                        return true;
                    }
                }
            }
        }
    }
    TRACE("TryLookupSymbol '%s' not found\n", symbolName.c_str());
    *symbolAddress = 0;
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
        TRACE("ERROR: InitializeGnuHashTable hashtable ReadMemory(%p) FAILED\n", m_gnuHashTableAddr);
        return false;
    }
    m_buckets = new (std::nothrow) int32_t[m_hashTable.BucketCount];
    if (m_buckets == nullptr) {
        return false;
    }
    void* bucketsAddress = (char*)m_gnuHashTableAddr + sizeof(GnuHashTable) + (m_hashTable.BloomSize * sizeof(size_t));
    if (!ReadMemory(bucketsAddress, m_buckets, m_hashTable.BucketCount * sizeof(int32_t))) {
        TRACE("ERROR: InitializeGnuHashTable buckets ReadMemory(%p) FAILED\n", bucketsAddress);
        return false;
    }
    m_chainsAddress = (char*)bucketsAddress + (m_hashTable.BucketCount * sizeof(int32_t));
    return true;
}

bool
ElfReader::GetPossibleSymbolIndex(const std::string& symbolName, std::vector<int32_t>& symbolIndexes)
{
    uint hash = Hash(symbolName);
    int i = m_buckets[hash % m_hashTable.BucketCount] - m_hashTable.SymbolOffset;
    TRACE("GetPossibleSymbolIndex hash %08x index: %d BucketCount %d SymbolOffset %08x\n", hash, i, m_hashTable.BucketCount, m_hashTable.SymbolOffset);
    for (;; i++)
    {
        int32_t chainVal;
        if (!GetChain(i, &chainVal)) {
            TRACE("ERROR: GetPossibleSymbolIndex GetChain FAILED\n");
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

uint
ElfReader::Hash(const std::string& symbolName)
{
    uint h = 5381;
    for (int i = 0; i < symbolName.length(); i++)
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
            TRACE("ERROR: GetStringAtIndex index %d > string table size\n", index);
            return false;
        }
        char ch;
        void* address = (char*)m_stringTableAddr + index;
        if (!ReadMemory(address, &ch, sizeof(ch))) {
            TRACE("ERROR: GetStringAtIndex ReadMemory(%p) FAILED\n", address);
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

//
// Enumerate and find the dynamic program header entry
//
bool
ElfReader::EnumerateProgramHeaders(Elf_Phdr* phdrAddr, int phnum, Elf_Dyn** pdynamicAddr)
{
    uint64_t loadbias = m_baseAddress;

    for (int i = 0; i < phnum; i++)
    {
        Elf_Phdr ph;
        if (!ReadMemory(phdrAddr + i, &ph, sizeof(ph))) {
            TRACE("ERROR: ReadMemory(%p, %" PRIx ") phdr FAILED\n", phdrAddr + i, sizeof(ph));
            return false;
        }
        if (ph.p_type == PT_LOAD && ph.p_offset == 0) {
            loadbias -= ph.p_vaddr;
            TRACE("PHDR: loadbias %" PRIA PRIx64 "\n", loadbias);
            break;
        }
    }

    for (int i = 0; i < phnum; i++)
    {
        Elf_Phdr ph;
        if (!ReadMemory(phdrAddr + i, &ph, sizeof(ph))) {
            TRACE("ERROR: ReadMemory(%p, %" PRIx ") phdr FAILED\n", phdrAddr + i, sizeof(ph));
            return false;
        }
        TRACE("PHDR: %p type %d (%x) vaddr %" PRIxA " memsz %" PRIxA " paddr %" PRIxA " filesz %" PRIxA " offset %" PRIxA " align %" PRIxA "\n",
            phdrAddr + i, ph.p_type, ph.p_type, ph.p_vaddr, ph.p_memsz, ph.p_paddr, ph.p_filesz, ph.p_offset, ph.p_align);

        switch (ph.p_type)
        {
        case PT_DYNAMIC:
            if (pdynamicAddr != nullptr) {
                *pdynamicAddr = reinterpret_cast<Elf_Dyn*>(loadbias + ph.p_vaddr);
            }
            break;

        case PT_NOTE:
        case PT_GNU_EH_FRAME:
            if (ph.p_vaddr != 0 && ph.p_memsz != 0) {
                // loadbias + ph.p_vaddr, ph.p_memsz
            }
            break;

        case PT_LOAD:
            // start: loadbias + ph.p_vaddr;
            // end:   loadbias + ph.p_vaddr + ph.p_memsz;
            break;
        }
    }

    return true;
}
