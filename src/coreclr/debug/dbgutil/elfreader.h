// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#if defined(HOST_WINDOWS) || defined(HOST_APPLE)
#include <ELF.h>
#else
#include <elf.h>
#endif
#if defined(HOST_UNIX) && !defined(HOST_APPLE)
#include <link.h>
#endif // HOST_UNIX
#include <string>
#include <vector>

struct Elf32Types
{
    using Ehdr = Elf32_Ehdr;
    using Phdr = Elf32_Phdr;
    using Dyn = Elf32_Dyn;
    using Sym = Elf32_Sym;
    using Word = uint32_t;
    static constexpr unsigned char Class = ELFCLASS32;
};

struct Elf64Types
{
    using Ehdr = Elf64_Ehdr;
    using Phdr = Elf64_Phdr;
    using Dyn = Elf64_Dyn;
    using Sym = Elf64_Sym;
    using Word = uint64_t;
    static constexpr unsigned char Class = ELFCLASS64;
};

typedef struct {
    int32_t BucketCount;
    int32_t SymbolOffset;
    int32_t BloomSize;
    int32_t BloomShift;
} GnuHashTable;

template <typename Types>
class ElfReader
{
private:
    using Elf_Ehdr = typename Types::Ehdr;
    using Elf_Phdr = typename Types::Phdr;
    using Elf_Dyn = typename Types::Dyn;
    using Elf_Sym = typename Types::Sym;
    using Elf_Word = typename Types::Word;

    void* m_gnuHashTableAddr;               // DT_GNU_HASH
    void* m_stringTableAddr;                // DT_STRTAB
    int m_stringTableSize;                  // DT_STRSIZ
    void* m_symbolTableAddr;                // DT_SYMTAB

    GnuHashTable m_hashTable;               // gnu hash table info
    int32_t* m_buckets;                     // gnu hash table buckets
    void* m_chainsAddress;

public:
    ElfReader();
    virtual ~ElfReader();
#if defined(HOST_UNIX) && !defined(HOST_APPLE)
    bool EnumerateElfInfo(Elf_Phdr* phdrAddr, int phnum);
#endif
    bool PopulateForSymbolLookup(uint64_t baseAddress);
    bool TryLookupSymbol(std::string symbolName, uint64_t* symbolOffset);
    bool EnumerateProgramHeaders(uint64_t baseAddress, uint64_t* ploadbias = nullptr, Elf_Dyn** pdynamicAddr = nullptr);

private:
    bool GetSymbol(int32_t index, Elf_Sym* symbol);
    bool InitializeGnuHashTable();
    bool GetPossibleSymbolIndex(const std::string& symbolName, std::vector<int32_t>& symbolIndexes);
    uint32_t Hash(const std::string& symbolName);
    bool GetChain(int index, int32_t* chain);
    bool GetStringAtIndex(int index, std::string& result);
    bool NeedsLoadBias(uint64_t baseAddress);
    bool HasAndroidNote(uint64_t loadbias, const Elf_Phdr& header);
    bool HasMuslDependency(uint64_t loadbias, Elf_Dyn* dynamicAddr, Elf_Phdr* phdrAddr, int phnum);
#if defined(HOST_UNIX) && !defined(HOST_APPLE)
    bool EnumerateLinkMapEntries(Elf_Dyn* dynamicAddr);
#endif
    bool EnumerateProgramHeaders(Elf_Phdr* phdrAddr, int phnum, uint64_t baseAddress, uint64_t* ploadbias, Elf_Dyn** pdynamicAddr);
    virtual void VisitModule(uint64_t baseAddress, std::string& moduleName) { };
    virtual void VisitProgramHeader(uint64_t loadbias, uint64_t baseAddress, Elf_Phdr* phdr) { };
    virtual bool ReadMemory(void* address, void* buffer, size_t size) = 0;
    virtual void Trace(const char* format, ...) { };
    virtual void TraceVerbose(const char* format, ...) { };
};

extern template class ElfReader<Elf32Types>;
extern template class ElfReader<Elf64Types>;

#ifdef HOST_64BIT
using HostElfTypes = Elf64Types;
#else
using HostElfTypes = Elf32Types;
#endif
using HostElfReader = ElfReader<HostElfTypes>;

struct ICorDebugDataTarget;
bool TryGetElfSymbol(ICorDebugDataTarget* dataTarget, uint64_t baseAddress, const char* symbolName, uint64_t* symbolAddress);
