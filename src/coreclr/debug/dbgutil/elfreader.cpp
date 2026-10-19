// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !defined(HOST_WASM) && (defined(HOST_WINDOWS) || defined(HOST_OSX) || defined(__linux__))

#include <windows.h>
#include <clrdata.h>
#include <cor.h>
#include <cordebug.h>
#include <inttypes.h>
#include "elfreader.h"

#if !defined(HOST_WINDOWS) && !defined(HOST_APPLE)
static const char ElfMagic[] = { 0x7f, 'E', 'L', 'F', '\0' };
#endif

template <typename Types>
class ElfReaderExport : public ElfReader<Types>
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
    bool ReadMemory(void* address, void* buffer, size_t size) override
    {
        uint32_t read = 0;
        return SUCCEEDED(m_dataTarget->ReadVirtual(reinterpret_cast<CLRDATA_ADDRESS>(address), reinterpret_cast<PBYTE>(buffer), (uint32_t)size, &read));
    }
};

template <typename Types>
bool TryGetElfSymbol(ICorDebugDataTarget* dataTarget, uint64_t baseAddress, const char* symbolName, uint64_t* symbolAddress)
{
    ElfReaderExport<Types> reader(dataTarget);
    uint64_t symbolOffset = 0;
    if (!reader.PopulateForSymbolLookup(baseAddress) || !reader.TryLookupSymbol(symbolName, &symbolOffset))
    {
        return false;
    }
    *symbolAddress = baseAddress + symbolOffset;
    return true;
}

bool
TryGetElfSymbol(ICorDebugDataTarget* dataTarget, uint64_t baseAddress, const char* symbolName, uint64_t* symbolAddress)
{
    BYTE elfClass = 0;
    ULONG32 read = 0;
    if (FAILED(dataTarget->ReadVirtual(baseAddress + EI_CLASS, &elfClass, sizeof(elfClass), &read)) ||
        read != sizeof(elfClass))
    {
        return false;
    }
    switch (elfClass)
    {
        case ELFCLASS32:
            return TryGetElfSymbol<Elf32Types>(dataTarget, baseAddress, symbolName, symbolAddress);
        case ELFCLASS64:
            return TryGetElfSymbol<Elf64Types>(dataTarget, baseAddress, symbolName, symbolAddress);
        default:
            return false;
    }
}

//
// ELF reader constructor/destructor
//

template <typename Types>
ElfReader<Types>::ElfReader() :
    m_gnuHashTableAddr(nullptr),
    m_stringTableAddr(nullptr),
    m_stringTableSize(0),
    m_symbolTableAddr(nullptr),
    m_buckets(nullptr),
    m_chainsAddress(nullptr)
{
    memset(&m_hashTable, 0, sizeof(m_hashTable));
}

template <typename Types>
ElfReader<Types>::~ElfReader()
{
    if (m_buckets != nullptr) {
        free(m_buckets);
    }
}

//
// Initialize the ELF reader from a module the base address. This function
// caches the info necessary in the ElfReader class look up symbols.
//
template <typename Types>
bool
ElfReader<Types>::PopulateForSymbolLookup(uint64_t baseAddress)
{
    Trace("PopulateForSymbolLookup: base %016" PRIx64 "\n", baseAddress);
    Elf_Dyn* dynamicAddr = nullptr;
    uint64_t loadbias = 0;

    // Enumerate program headers searching for the PT_DYNAMIC header, etc.
    if (!EnumerateProgramHeaders(
        baseAddress,
        NeedsLoadBias(baseAddress) ? &loadbias : nullptr,
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
            Trace("ERROR: ReadMemory(%p, %zx) dyn FAILED\n", dynamicAddr, sizeof(dyn));
            return false;
        }
        Trace("DSO: dyn %p tag %" PRId64 " (%" PRIx64 ") d_ptr %016" PRIx64 "\n",
            dynamicAddr, (int64_t)dyn.d_tag, (uint64_t)dyn.d_tag, (uint64_t)dyn.d_un.d_ptr);
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

template <typename Types>
bool
ElfReader<Types>::NeedsLoadBias(uint64_t baseAddress)
{
    Elf_Ehdr header;
    if (!ReadMemory((void*)baseAddress, &header, sizeof(header))) {
        return false;
    }
    if (header.e_machine == EM_RISCV) {
        return true;
    }

    uint64_t loadbias = 0;
    Elf_Dyn* dynamicAddr = nullptr;
    if (!EnumerateProgramHeaders(baseAddress, &loadbias, &dynamicAddr) || loadbias == 0 || dynamicAddr == nullptr) {
        return false;
    }

    Elf_Phdr* phdrAddr = (Elf_Phdr*)(baseAddress + header.e_phoff);
    for (int i = 0; i < header.e_phnum; i++)
    {
        Elf_Phdr ph;
        if (!ReadMemory(phdrAddr + i, &ph, sizeof(ph))) {
            return false;
        }
        if (ph.p_type == PT_NOTE && HasAndroidNote(loadbias, ph)) {
            return true;
        }
    }
    return HasMuslDependency(loadbias, dynamicAddr, phdrAddr, header.e_phnum);
}

template <typename Types>
bool
ElfReader<Types>::HasAndroidNote(uint64_t loadbias, const Elf_Phdr& header)
{
    struct NoteHeader
    {
        uint32_t nameSize;
        uint32_t descriptorSize;
        uint32_t type;
    };
    constexpr uint32_t AndroidIdentNoteType = 1;
    const char AndroidName[] = "Android";

    if (header.p_vaddr > UINT64_MAX - loadbias || header.p_filesz > UINT64_MAX - (loadbias + header.p_vaddr)) {
        return false;
    }
    uint64_t address = loadbias + header.p_vaddr;
    uint64_t remaining = header.p_filesz;
    while (remaining >= sizeof(NoteHeader))
    {
        NoteHeader note;
        if (!ReadMemory((void*)address, &note, sizeof(note))) {
            return false;
        }
        uint64_t nameSize = (static_cast<uint64_t>(note.nameSize) + 3) & ~uint64_t(3);
        uint64_t descriptorSize = (static_cast<uint64_t>(note.descriptorSize) + 3) & ~uint64_t(3);
        uint64_t noteSize = sizeof(note) + nameSize + descriptorSize;
        if (noteSize > remaining) {
            return false;
        }
        if (note.type == AndroidIdentNoteType && note.nameSize == sizeof(AndroidName) &&
            note.descriptorSize >= sizeof(uint32_t))
        {
            char name[sizeof(AndroidName)];
            if (!ReadMemory((void*)(address + sizeof(note)), name, sizeof(name))) {
                return false;
            }
            if (memcmp(name, AndroidName, sizeof(name)) == 0) {
                return true;
            }
        }
        address += noteSize;
        remaining -= noteSize;
    }
    return false;
}

template <typename Types>
bool
ElfReader<Types>::HasMuslDependency(uint64_t loadbias, Elf_Dyn* dynamicAddr, Elf_Phdr* phdrAddr, int phnum)
{
    uint64_t stringAddress = 0;
    uint64_t stringSize = 0;
    std::vector<uint64_t> neededOffsets;
    for (Elf_Dyn* address = dynamicAddr;; address++)
    {
        Elf_Dyn dyn;
        if (!ReadMemory(address, &dyn, sizeof(dyn))) {
            return false;
        }
        if (dyn.d_tag == DT_NULL) {
            break;
        }
        else if (dyn.d_tag == DT_STRTAB) {
            stringAddress = dyn.d_un.d_ptr;
        }
        else if (dyn.d_tag == DT_STRSZ) {
            stringSize = dyn.d_un.d_val;
        }
        else if (dyn.d_tag == DT_NEEDED) {
            neededOffsets.push_back(dyn.d_un.d_val);
        }
    }
    if (neededOffsets.empty() || stringSize == 0 || stringAddress > UINT64_MAX - loadbias) {
        return false;
    }
    stringAddress += loadbias;

    // Only probe the biased string table if it lies within the loaded image.
    bool inLoadSegment = false;
    for (int i = 0; i < phnum; i++)
    {
        Elf_Phdr ph;
        if (!ReadMemory(phdrAddr + i, &ph, sizeof(ph))) {
            return false;
        }
        if (ph.p_type != PT_LOAD || ph.p_vaddr > UINT64_MAX - loadbias) {
            continue;
        }
        uint64_t segmentAddress = loadbias + ph.p_vaddr;
        if (stringAddress >= segmentAddress && stringAddress - segmentAddress <= ph.p_memsz &&
            stringSize <= ph.p_memsz - (stringAddress - segmentAddress) &&
            stringSize <= UINT64_MAX - stringAddress) {
            inLoadSegment = true;
            break;
        }
    }
    if (!inLoadSegment) {
        return false;
    }

    const char MuslPrefix[] = "libc.musl-";
    const char MuslSuffix[] = ".so.1";
    for (uint64_t offset : neededOffsets)
    {
        std::string name;
        for (; offset < stringSize; offset++)
        {
            char ch;
            if (!ReadMemory((void*)(stringAddress + offset), &ch, sizeof(ch))) {
                break;
            }
            if (ch == '\0')
            {
                if (name.size() > sizeof(MuslPrefix) + sizeof(MuslSuffix) - 2 &&
                    name.compare(0, sizeof(MuslPrefix) - 1, MuslPrefix) == 0 &&
                    name.compare(name.size() - (sizeof(MuslSuffix) - 1), sizeof(MuslSuffix) - 1, MuslSuffix) == 0) {
                    return true;
                }
                break;
            }
            name.append(1, ch);
        }
    }
    return false;
}

//
// Symbol table support
//

template <typename Types>
bool
ElfReader<Types>::TryLookupSymbol(std::string symbolName, uint64_t* symbolOffset)
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
                        Trace("TryLookupSymbol found '%s' at offset %016" PRIx64 "\n", symbolName.c_str(), *symbolOffset);
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

template <typename Types>
bool
ElfReader<Types>::GetSymbol(int32_t index, Elf_Sym* symbol)
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

template <typename Types>
bool
ElfReader<Types>::InitializeGnuHashTable()
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
    void* bucketsAddress = (char*)m_gnuHashTableAddr + sizeof(GnuHashTable) + (m_hashTable.BloomSize * sizeof(Elf_Word));
    if (!ReadMemory(bucketsAddress, m_buckets, m_hashTable.BucketCount * sizeof(int32_t))) {
        Trace("ERROR: InitializeGnuHashTable buckets ReadMemory(%p) FAILED\n", bucketsAddress);
        return false;
    }
    m_chainsAddress = (char*)bucketsAddress + (m_hashTable.BucketCount * sizeof(int32_t));
    return true;
}

template <typename Types>
bool
ElfReader<Types>::GetPossibleSymbolIndex(const std::string& symbolName, std::vector<int32_t>& symbolIndexes)
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

template <typename Types>
uint32_t
ElfReader<Types>::Hash(const std::string& symbolName)
{
    uint32_t h = 5381;
    for (unsigned int i = 0; i < symbolName.length(); i++)
    {
        h = (h << 5) + h + symbolName[i];
    }
    return h;
}

template <typename Types>
bool
ElfReader<Types>::GetChain(int index, int32_t* chain)
{
    return ReadMemory((char*)m_chainsAddress + (index * sizeof(int32_t)), chain, sizeof(int32_t));
}

//
// String table support
//

template <typename Types>
bool
ElfReader<Types>::GetStringAtIndex(int index, std::string& result)
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

#if defined(HOST_UNIX) && !defined(HOST_APPLE) && !defined(TARGET_HAIKU)

//
// Enumerate all the ELF info starting from the root program header. This
// function doesn't cache any state in the ElfReader class.
//
template <typename Types>
bool
ElfReader<Types>::EnumerateElfInfo(Elf_Phdr* phdrAddr, int phnum)
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
template <typename Types>
bool
ElfReader<Types>::EnumerateLinkMapEntries(Elf_Dyn* dynamicAddr)
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
            Trace("ERROR: ReadMemory(%p, %zx) dyn FAILED\n", dynamicAddr, sizeof(dyn));
            return false;
        }
        Trace("DSO: dyn %p tag %" PRId64 " (%" PRIx64 ") d_ptr %016" PRIx64 "\n",
            dynamicAddr, (int64_t)dyn.d_tag, (uint64_t)dyn.d_tag, (uint64_t)dyn.d_un.d_ptr);
        if (dyn.d_tag == DT_NULL) {
            break;
        }
        else if (dyn.d_tag == DT_DEBUG) {
            rdebugAddr = reinterpret_cast<struct r_debug*>((uintptr_t)dyn.d_un.d_ptr);
        }
        dynamicAddr++;
    }

    Trace("DSO: rdebugAddr %p\n", rdebugAddr);
    if (rdebugAddr == nullptr) {
        return false;
    }

    struct r_debug debugEntry;
    if (!ReadMemory(rdebugAddr, &debugEntry, sizeof(debugEntry))) {
        Trace("ERROR: ReadMemory(%p, %zx) r_debug FAILED\n", rdebugAddr, sizeof(debugEntry));
        return false;
    }

    // Add the DSO link_map entries.
    //
    // Detect cycles using Brent's algorithm (variant of Floyd's cycle-finding algorithm)
    struct link_map* walker = debugEntry.r_map;
    struct link_map* checkpoint = nullptr;
    int power = 1;
    int lam = 0;
    while (walker != nullptr)
    {
        if (checkpoint != nullptr && walker == checkpoint)
        {
            Trace("ERROR: EnumerateLinkMapEntries detected cycle in link_map chain; aborting\n");
            return false;
        }
        struct link_map map;
        if (!ReadMemory(walker, &map, sizeof(map))) {
            Trace("ERROR: ReadMemory(%p, %zx) link_map FAILED\n", walker, sizeof(map));
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
        Trace("\nDSO: link_map entry %p l_ld %p l_addr (Ehdr) %p l_name %p %s\n", walker, map.l_ld, map.l_addr, map.l_name, moduleName.c_str());

        // Call the derived class for each module
        VisitModule(map.l_addr, moduleName);

        if (lam == power)
        {
            checkpoint = walker;
            power *= 2;
            lam = 0;
        }
        walker = map.l_next;
        lam++;
    }

    return true;
}

#endif // defined(HOST_UNIX) && !defined(TARGET_HAIKU)

template <typename Types>
bool
ElfReader<Types>::EnumerateProgramHeaders(uint64_t baseAddress, uint64_t* ploadbias, Elf_Dyn** pdynamicAddr)
{
    Elf_Ehdr ehdr;
    if (!ReadMemory((void*)baseAddress, &ehdr, sizeof(ehdr))) {
        Trace("ERROR: EnumerateProgramHeaders ReadMemory(%p, %zx) ehdr FAILED\n", (void*)baseAddress, sizeof(ehdr));
        return false;
    }
    if (memcmp(ehdr.e_ident, ElfMagic, strlen(ElfMagic)) != 0) {
        Trace("ERROR: EnumerateProgramHeaders Invalid elf header signature\n");
        return false;
    }
    _ASSERTE(ehdr.e_phentsize == sizeof(Elf_Phdr));
    _ASSERTE(ehdr.e_ident[EI_CLASS] == Types::Class);
    _ASSERTE(ehdr.e_ident[EI_DATA] == ELFDATA2LSB);
    int phnum = ehdr.e_phnum;
#ifdef PN_XNUM
     _ASSERTE(phnum != PN_XNUM);
#endif
    if (ehdr.e_phoff == 0 || phnum <= 0) {
        return false;
    }
    Trace("ELF: type %d mach 0x%x ver %d flags 0x%x phnum %d phoff %016" PRIx64 " phentsize 0x%02x shnum %d shoff %016" PRIx64 " shentsize 0x%02x shstrndx %d\n",
        ehdr.e_type, ehdr.e_machine, ehdr.e_version, ehdr.e_flags, phnum, (uint64_t)ehdr.e_phoff, ehdr.e_phentsize, ehdr.e_shnum, (uint64_t)ehdr.e_shoff, ehdr.e_shentsize, ehdr.e_shstrndx);

    Elf_Phdr* phdrAddr = reinterpret_cast<Elf_Phdr*>(baseAddress + ehdr.e_phoff);
    return EnumerateProgramHeaders(phdrAddr, phnum, baseAddress, ploadbias, pdynamicAddr);
}

//
// Enumerate and find the dynamic program header entry
//
template <typename Types>
bool
ElfReader<Types>::EnumerateProgramHeaders(Elf_Phdr* phdrAddr, int phnum, uint64_t baseAddress, uint64_t* ploadbias, Elf_Dyn** pdynamicAddr)
{
    uint64_t loadbias = baseAddress;

    // Calculate the load bias from the PT_LOAD program headers
    for (int i = 0; i < phnum; i++)
    {
        Elf_Phdr ph;
        if (!ReadMemory(phdrAddr + i, &ph, sizeof(ph))) {
            Trace("ERROR: ReadMemory(%p, %zx) phdr FAILED\n", phdrAddr + i, sizeof(ph));
            return false;
        }
        if (ph.p_type == PT_LOAD && ph.p_offset == 0) {
            loadbias -= ph.p_vaddr;
            Trace("PHDR: loadbias %016" PRIx64 "\n", loadbias);
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
            Trace("ERROR: ReadMemory(%p, %zx) phdr FAILED\n", phdrAddr + i, sizeof(ph));
            return false;
        }
        Trace("PHDR: %p type %d (%x) vaddr %016" PRIx64 " memsz %016" PRIx64 " paddr %016" PRIx64 " filesz %016" PRIx64 " offset %016" PRIx64 " align %016" PRIx64 "\n",
            phdrAddr + i, ph.p_type, ph.p_type, (uint64_t)ph.p_vaddr, (uint64_t)ph.p_memsz, (uint64_t)ph.p_paddr, (uint64_t)ph.p_filesz, (uint64_t)ph.p_offset, (uint64_t)ph.p_align);

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

template class ElfReader<Elf32Types>;
template class ElfReader<Elf64Types>;

#if defined(HOST_WINDOWS) || defined(HOST_APPLE)

/* ELF 32bit header */
Elf32_Ehdr::Elf32_Ehdr() {}

/* ELF 64bit header */
Elf64_Ehdr::Elf64_Ehdr() {}

#endif // HOST_WINDOWS

#endif
