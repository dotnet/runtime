// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// File: gdbjit.h
// 

//
// Header file for GDB JIT interface implemenation.
//
//*****************************************************************************


#ifndef __GDBJIT_H__
#define __GDBJIT_H__

#include <stdint.h>
#include "method.hpp"
#include "../inc/llvm/ELF.h"
#include "../inc/llvm/Dwarf.h"

#if defined(_TARGET_X86_) || defined(_TARGET_ARM_)
    typedef Elf32_Ehdr  Elf_Ehdr;
    typedef Elf32_Shdr  Elf_Shdr;
#define ADDRESS_SIZE 4    
#elif defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)
    typedef Elf64_Ehdr  Elf_Ehdr;
    typedef Elf64_Shdr  Elf_Shdr;
#define ADDRESS_SIZE 8    
#else
#error "Target is not supported"
#endif

struct __attribute__((packed)) DwarfCompUnit
{
    uint32_t m_length;
    uint16_t m_version;
    uint32_t m_abbrev_offset;
    uint8_t m_addr_size;
};

struct __attribute__((packed)) DwarfPubHeader
{
    uint32_t m_length;
    uint16_t m_version;
    uint32_t m_debug_info_off;
    uint32_t m_debug_info_len;
};

#define DW_LNS_MAX DW_LNS_set_isa

struct __attribute__((packed)) DwarfLineNumHeader
{
    uint32_t m_length;
    uint16_t m_version;
    uint32_t m_hdr_length;
    uint8_t m_min_instr_len;
    uint8_t m_def_is_stmt;
    int8_t m_line_base;
    uint8_t m_line_range;
    uint8_t m_opcode_base;
    uint8_t m_std_num_arg[DW_LNS_MAX];
};

struct SymbolsInfo
{
    int lineNumber, ilOffset, nativeOffset, fileIndex;
    char fileName[2*MAX_PATH_FNAME];
};


class NotifyGdb
{
public:
    static void MethodCompiled(MethodDesc* MethodDescPtr);
    static void MethodDropped(MethodDesc* MethodDescPtr);
private:
    struct MemBuf
    {
        NewArrayHolder<char> MemPtr;
        unsigned MemSize;
        MemBuf() : MemPtr(0), MemSize(0)
        {}
    };

    static bool BuildELFHeader(MemBuf& buf);
    static bool BuildSectionNameTable(MemBuf& buf);
    static bool BuildSectionTable(MemBuf& buf);
    static bool BuildDebugStrings(MemBuf& buf);
    static bool BuildDebugAbbrev(MemBuf& buf);    
    static bool BuildDebugInfo(MemBuf& buf);
    static bool BuildDebugPub(MemBuf& buf, const char* name, uint32_t size, uint32_t dieOffset);
    static bool BuildLineTable(MemBuf& buf, PCODE startAddr, SymbolsInfo* lines, unsigned nlines);
    static bool BuildFileTable(MemBuf& buf, SymbolsInfo* lines, unsigned nlines);
    static bool BuildLineProg(MemBuf& buf, PCODE startAddr, SymbolsInfo* lines, unsigned nlines);
    static bool FitIntoSpecialOpcode(int8_t line_shift, uint8_t addr_shift);
    static void IssueSetAddress(char*& ptr, PCODE addr);
    static void IssueEndOfSequence(char*& ptr);
    static void IssueSimpleCommand(char*& ptr, uint8_t command);
    static void IssueParamCommand(char*& ptr, uint8_t command, char* param, int param_len);
    static void IssueSpecialCommand(char*& ptr, int8_t line_shift, uint8_t addr_shift);
    static void SplitPathname(const char* path, const char*& pathName, const char*& fileName);
    static int Leb128Encode(uint32_t num, char* buf, int size);
    static int Leb128Encode(int32_t num, char* buf, int size);
};

#endif // #ifndef __GDBJIT_H__
