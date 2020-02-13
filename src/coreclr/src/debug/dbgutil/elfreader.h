// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <windows.h>
#include <clrdata.h>
#include <cor.h>
#include <cordebug.h>
#include <arrayholder.h>
#include <elf.h>
#include <link.h>
#define __STDC_FORMAT_MACROS
#include <inttypes.h>
#include <string>
#include <vector>

#if defined(__i386) || defined(__ARM_EABI__)
#define PRIx PRIx32
#define PRIu PRIu32
#define PRId PRId32
#define PRIA "08"
#define PRIxA PRIA PRIx
#elif defined(__x86_64__) || defined(__aarch64__)
#define PRIx PRIx64
#define PRIu PRIu64
#define PRId PRId64
#define PRIA "016"
#define PRIxA PRIA PRIx
#endif

#ifndef ElfW
#define __ELF_NATIVE_CLASS __WORDSIZE

/* We use this macro to refer to ELF types independent of the native wordsize.
   `ElfW(TYPE)' is used in place of `Elf32_TYPE' or `Elf64_TYPE'.  */
#define ElfW(type)      _ElfW (Elf, __ELF_NATIVE_CLASS, type)
#define _ElfW(e,w,t)    _ElfW_1 (e, w, _##t)
#define _ElfW_1(e,w,t)  e##w##t
#endif

#define Elf_Ehdr   ElfW(Ehdr)
#define Elf_Phdr   ElfW(Phdr)
#define Elf_Shdr   ElfW(Shdr)
#define Elf_Nhdr   ElfW(Nhdr)
#define Elf_Dyn    ElfW(Dyn)
#define Elf_Sym    ElfW(Sym)

#define TRACE(args...)

typedef struct {
    int32_t BucketCount;
    int32_t SymbolOffset;
    int32_t BloomSize;
    int32_t BloomShift;
} GnuHashTable;

class ElfReader
{
private:
    uint64_t m_baseAddress;

    void* m_rdebugAddr;                     // DT_DEBUG
    void* m_gnuHashTableAddr;               // DT_GNU_HASH
    void* m_stringTableAddr;                // DT_STRTAB
    int m_stringTableSize;                  // DT_STRSIZ
    void* m_symbolTableAddr;                // DT_SYMTAB

    GnuHashTable m_hashTable;               // gnu hash table info
    int32_t* m_buckets;                     // gnu hash table buckets    
    void* m_chainsAddress;

public:
    ElfReader(uint64_t baseAddress);
    virtual ~ElfReader();
    bool PopulateELFInfo();
    bool TryLookupSymbol(std::string symbolName, uint64_t* symbolAddress);

private:
    bool GetSymbol(int32_t index, Elf_Sym* symbol);
    bool InitializeGnuHashTable();
    bool GetPossibleSymbolIndex(const std::string& symbolName, std::vector<int32_t>& symbolIndexes);
    uint Hash(const std::string& symbolName);
    bool GetChain(int index, int32_t* chain);
    bool GetStringAtIndex(int index, std::string& result);
    bool EnumerateProgramHeaders(Elf_Phdr* phdrAddr, int phnum, Elf_Dyn** pdynamicAddr);
    virtual bool ReadMemory(void* address, void* buffer, size_t size) = 0;
};
