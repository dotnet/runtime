// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <elf.h>
#ifdef HOST_UNIX
#include <link.h>
#endif // HOST_UNIX
#include <string>
#include <vector>

#if TARGET_64BIT
#define TARGET_WORDSIZE 64
#else
#define TARGET_WORDSIZE 32
#endif

#ifndef ElfW
/* We use this macro to refer to ELF types independent of the native wordsize.
   `ElfW(TYPE)' is used in place of `Elf32_TYPE' or `Elf64_TYPE'.  */
#define ElfW(type)      _ElfW (Elf, TARGET_WORDSIZE, type)
#define _ElfW(e,w,t)    _ElfW_1 (e, w, _##t)
#define _ElfW_1(e,w,t)  e##w##t
#endif

typedef struct {
    int32_t BucketCount;
    int32_t SymbolOffset;
    int32_t BloomSize;
    int32_t BloomShift;
} GnuHashTable;

class ElfReader
{
private:
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
#ifdef HOST_UNIX
    bool EnumerateElfInfo(ElfW(Phdr)* phdrAddr, int phnum);
#endif
    bool PopulateForSymbolLookup(uint64_t baseAddress);
    bool TryLookupSymbol(std::string symbolName, uint64_t* symbolOffset);
    bool EnumerateProgramHeaders(uint64_t baseAddress, uint64_t* ploadbias = nullptr, ElfW(Dyn)** pdynamicAddr = nullptr);

private:
    bool GetSymbol(int32_t index, ElfW(Sym)* symbol);
    bool InitializeGnuHashTable();
    bool GetPossibleSymbolIndex(const std::string& symbolName, std::vector<int32_t>& symbolIndexes);
    uint32_t Hash(const std::string& symbolName);
    bool GetChain(int index, int32_t* chain);
    bool GetStringAtIndex(int index, std::string& result);
#ifdef HOST_UNIX
    bool EnumerateLinkMapEntries(ElfW(Dyn)* dynamicAddr);
#endif
    bool EnumerateProgramHeaders(ElfW(Phdr)* phdrAddr, int phnum, uint64_t baseAddress, uint64_t* ploadbias, ElfW(Dyn)** pdynamicAddr);
    virtual void VisitModule(uint64_t baseAddress, std::string& moduleName) { };
    virtual void VisitProgramHeader(uint64_t loadbias, uint64_t baseAddress, ElfW(Phdr)* phdr) { };
    virtual bool ReadMemory(void* address, void* buffer, size_t size) = 0;
    virtual void Trace(const char* format, ...) { };
    virtual void TraceVerbose(const char* format, ...) { };
};
