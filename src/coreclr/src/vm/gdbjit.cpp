// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// File: gdbjit.cpp
//

//
// NotifyGdb implementation.
//
//*****************************************************************************

#include "common.h"
#include "gdbjit.h"
#include "gdbjithelpers.h"

struct DebuggerILToNativeMap
{
    ULONG ilOffset;
    ULONG nativeStartOffset;
    ULONG nativeEndOffset;
    ICorDebugInfo::SourceTypes source;
};
BYTE* DebugInfoStoreNew(void * pData, size_t cBytes)
{
    return new (nothrow) BYTE[cBytes];
}

/* Get IL to native offsets map */
HRESULT
GetMethodNativeMap(MethodDesc* methodDesc,
                   ULONG32* numMap,
                   DebuggerILToNativeMap** map)
{
    // Use the DebugInfoStore to get IL->Native maps.
    // It doesn't matter whether we're jitted, ngenned etc.

    DebugInfoRequest request;
    TADDR nativeCodeStartAddr = PCODEToPINSTR(methodDesc->GetNativeCode());
    request.InitFromStartingAddr(methodDesc, nativeCodeStartAddr);

    // Bounds info.
    ULONG32 countMapCopy;
    NewHolder<ICorDebugInfo::OffsetMapping> mapCopy(NULL);

    BOOL success = DebugInfoManager::GetBoundariesAndVars(request,
                                                          DebugInfoStoreNew,
                                                          NULL, // allocator
                                                          &countMapCopy,
                                                          &mapCopy,
                                                          NULL,
                                                          NULL);

    if (!success)
    {
        return E_FAIL;
    }

    // Need to convert map formats.
    *numMap = countMapCopy;

    *map = new (nothrow) DebuggerILToNativeMap[countMapCopy];
    if (!*map)
    {
        return E_OUTOFMEMORY;
    }

    ULONG32 i;
    for (i = 0; i < *numMap; i++)
    {
        (*map)[i].ilOffset = mapCopy[i].ilOffset;
        (*map)[i].nativeStartOffset = mapCopy[i].nativeOffset;
        if (i > 0)
        {
            (*map)[i - 1].nativeEndOffset = (*map)[i].nativeStartOffset;
        }
        (*map)[i].source = mapCopy[i].source;
    }
    if (*numMap >= 1)
    {
        (*map)[i - 1].nativeEndOffset = 0;
    }
    return S_OK;
}

/* Get mapping of IL offsets to source line numbers */
HRESULT
GetDebugInfoFromPDB(MethodDesc* MethodDescPtr, SymbolsInfo** symInfo, unsigned int &symInfoLen)
{
    DebuggerILToNativeMap* map = NULL;

    ULONG32 numMap;

    if (!getInfoForMethodDelegate)
        return E_FAIL;
    
    if (GetMethodNativeMap(MethodDescPtr, &numMap, &map) != S_OK)
        return E_FAIL;

    const Module* mod = MethodDescPtr->GetMethodTable()->GetModule();
    SString modName = mod->GetFile()->GetPath();
    if (modName.IsEmpty())
        return E_FAIL;

    StackScratchBuffer scratch;
    const char* szModName = modName.GetUTF8(scratch);

    MethodDebugInfo* methodDebugInfo = new (nothrow) MethodDebugInfo();
    if (methodDebugInfo == nullptr)
        return E_OUTOFMEMORY;

    methodDebugInfo->points = (SequencePointInfo*) CoTaskMemAlloc(sizeof(SequencePointInfo) * numMap);
    if (methodDebugInfo->points == nullptr)
        return E_OUTOFMEMORY;

    methodDebugInfo->size = numMap;

    if (getInfoForMethodDelegate(szModName, MethodDescPtr->GetMemberDef(), *methodDebugInfo) == FALSE)
        return E_FAIL;

    symInfoLen = methodDebugInfo->size;
    *symInfo = new (nothrow) SymbolsInfo[symInfoLen];
    if (*symInfo == nullptr)
        return E_FAIL;

    for (ULONG32 i = 0; i < symInfoLen; i++)
    {
        for (ULONG32 j = 0; j < numMap; j++)
        {
            if (methodDebugInfo->points[i].ilOffset == map[j].ilOffset)
            {
                SymbolsInfo& s = (*symInfo)[i];
                const SequencePointInfo& sp = methodDebugInfo->points[i];

                s.nativeOffset = map[j].nativeStartOffset;
                s.ilOffset = map[j].ilOffset;
                s.fileIndex = 0;
                //wcscpy(s.fileName, sp.fileName);
                int len = WideCharToMultiByte(CP_UTF8, 0, sp.fileName, -1, s.fileName, sizeof(s.fileName), NULL, NULL);
                s.fileName[len] = 0;
                s.lineNumber = sp.lineNumber;
            }
        }
    }

    CoTaskMemFree(methodDebugInfo->points);
    return S_OK;
}

// GDB JIT interface
typedef enum
{
  JIT_NOACTION = 0,
  JIT_REGISTER_FN,
  JIT_UNREGISTER_FN
} jit_actions_t;

struct jit_code_entry
{
  struct jit_code_entry *next_entry;
  struct jit_code_entry *prev_entry;
  const char *symfile_addr;
  UINT64 symfile_size;
};

struct jit_descriptor
{
  UINT32 version;
  /* This type should be jit_actions_t, but we use uint32_t
     to be explicit about the bitwidth.  */
  UINT32 action_flag;
  struct jit_code_entry *relevant_entry;
  struct jit_code_entry *first_entry;
};
// GDB puts a breakpoint in this function.
// To prevent from inlining we add noinline attribute and inline assembler statement.
extern "C"
void __attribute__((noinline)) __jit_debug_register_code() { __asm__(""); };

/* Make sure to specify the version statically, because the
   debugger may check the version before we can set it.  */
struct jit_descriptor __jit_debug_descriptor = { 1, 0, 0, 0 };

// END of GDB JIT interface

/* Predefined section names */
const char* SectionNames[] = {
    "", ".text", ".shstrtab", ".debug_str", ".debug_abbrev", ".debug_info",
    ".debug_pubnames", ".debug_pubtypes", ".debug_line", ""
};

const int SectionNamesCount = sizeof(SectionNames) / sizeof(SectionNames[0]);

/* Static data for section headers */
struct SectionHeader {
    uint32_t m_type;
    uint64_t m_flags;
} Sections[] = {
    {SHT_NULL, 0},
    {SHT_PROGBITS, SHF_ALLOC | SHF_EXECINSTR},
    {SHT_STRTAB, 0},
    {SHT_PROGBITS, SHF_MERGE | SHF_STRINGS },
    {SHT_PROGBITS, 0},
    {SHT_PROGBITS, 0},
    {SHT_PROGBITS, 0},
    {SHT_PROGBITS, 0},
    {SHT_PROGBITS, 0}
};

/* Static data for .debug_str section */
const char* DebugStrings[] = {
    "CoreCLR", "" /* module name */, "" /* module path */, "" /* method name */, "int"
};

const int DebugStringCount = sizeof(DebugStrings) / sizeof(DebugStrings[0]);

/* Static data for .debug_abbrev */
const unsigned char AbbrevTable[] = {
    1, DW_TAG_compile_unit, DW_CHILDREN_yes,
        DW_AT_producer, DW_FORM_strp, DW_AT_language, DW_FORM_data2, DW_AT_name, DW_FORM_strp,
        DW_AT_stmt_list, DW_FORM_sec_offset, 0, 0,
    2, DW_TAG_subprogram, DW_CHILDREN_no,
        DW_AT_name, DW_FORM_strp, DW_AT_decl_file, DW_FORM_data1, DW_AT_decl_line, DW_FORM_data1,
        DW_AT_type, DW_FORM_ref4, DW_AT_external, DW_FORM_flag_present, 0, 0,
    3, DW_TAG_base_type, DW_CHILDREN_no,
        DW_AT_name, DW_FORM_strp, DW_AT_encoding, DW_FORM_data1, DW_AT_byte_size, DW_FORM_data1,0, 0,
    0
};

const int AbbrevTableSize = sizeof(AbbrevTable);

/* Static data for .debug_line, including header */
#define DWARF_LINE_BASE (-5)
#define DWARF_LINE_RANGE 14
#define DWARF_OPCODE_BASE 13

DwarfLineNumHeader LineNumHeader = {
    0, 2, 0, 1, 1, DWARF_LINE_BASE, DWARF_LINE_RANGE, DWARF_OPCODE_BASE, {0, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 1}
};

/* Static data for .debug_info */
struct __attribute__((packed)) DebugInfo
{
    uint8_t m_cu_abbrev;
    uint32_t m_prod_off;
    uint16_t m_lang;
    uint32_t m_cu_name;
    uint32_t m_line_num;
    
    uint8_t m_sub_abbrev;
    uint32_t m_sub_name;
    uint8_t m_file, m_line;
    uint32_t m_sub_type;
    
    uint8_t m_type_abbrev;
    uint32_t m_type_name;
    uint8_t m_encoding;
    uint8_t m_byte_size;
} debugInfo = {
    1, 0, DW_LANG_C89, 0, 0,
    2, 0, 1, 1, 37,
    3, 0, DW_ATE_signed, 4
};

/* Create ELF/DWARF debug info for jitted method */
void NotifyGdb::MethodCompiled(MethodDesc* MethodDescPtr)
{
    PCODE pCode = MethodDescPtr->GetNativeCode();

    if (pCode == NULL)
        return;
    unsigned int symInfoLen = 0;
    NewArrayHolder<SymbolsInfo> symInfo = nullptr;

    /* Get method name & size of jitted code */
    LPCUTF8 methodName = MethodDescPtr->GetName();
    EECodeInfo codeInfo(pCode);
    TADDR codeSize = codeInfo.GetCodeManager()->GetFunctionSize(codeInfo.GetGCInfoToken());
    
#ifdef _TARGET_ARM_    
    pCode &= ~1; // clear thumb flag for debug info
#endif    

    /* Get module name */
    const Module* mod = MethodDescPtr->GetMethodTable()->GetModule();
    SString modName = mod->GetFile()->GetPath();
    StackScratchBuffer scratch;
    const char* szModName = modName.GetUTF8(scratch);
    const char *szModulePath, *szModuleFile;
    
    SplitPathname(szModName, szModulePath, szModuleFile);
    
    /* Get debug info for method from portable PDB */
    HRESULT hr = GetDebugInfoFromPDB(MethodDescPtr, &symInfo, symInfoLen);
    if (FAILED(hr) || symInfoLen == 0)
    {
        return;
    }

    MemBuf elfHeader, sectHeaders, sectStr, dbgInfo, dbgAbbrev, dbgPubname, dbgPubType, dbgLine, dbgStr, elfFile;

    /* Build .debug_abbrev section */
    if (!BuildDebugAbbrev(dbgAbbrev))
    {
        return;
    }
    
    /* Build .debug_line section */
    if (!BuildLineTable(dbgLine, pCode, symInfo, symInfoLen))
    {
        return;
    }
    
    DebugStrings[1] = szModuleFile;
    DebugStrings[3] = methodName;
    
    /* Build .debug_str section */
    if (!BuildDebugStrings(dbgStr))
    {
        return;
    }
    
    /* Build .debug_info section */
    if (!BuildDebugInfo(dbgInfo))
    {
        return;
    }
    
    /* Build .debug_pubname section */
    if (!BuildDebugPub(dbgPubname, methodName, dbgInfo.MemSize, 26))
    {
        return;
    }
    
    /* Build debug_pubtype section */
    if (!BuildDebugPub(dbgPubType, "int", dbgInfo.MemSize, 37))
    {
        return;
    }
    
    /* Build section names section */
    if (!BuildSectionNameTable(sectStr))
    {
        return;
    }

    /* Build section headers table */
    if (!BuildSectionTable(sectHeaders))
    {
        return;
    }

    /* Patch section offsets & sizes */
    long offset = sizeof(Elf_Ehdr);
    Elf_Shdr* pShdr = reinterpret_cast<Elf_Shdr*>(sectHeaders.MemPtr.GetValue());
    ++pShdr; // .text
    pShdr->sh_addr = pCode;
    pShdr->sh_size = codeSize;
    ++pShdr; // .shstrtab
    pShdr->sh_offset = offset;
    pShdr->sh_size = sectStr.MemSize;
    offset += sectStr.MemSize;
    ++pShdr; // .debug_str
    pShdr->sh_offset = offset;
    pShdr->sh_size = dbgStr.MemSize;
    offset += dbgStr.MemSize;
    ++pShdr; // .debug_abbrev
    pShdr->sh_offset = offset;
    pShdr->sh_size = dbgAbbrev.MemSize;
    offset += dbgAbbrev.MemSize;
    ++pShdr; // .debug_info
    pShdr->sh_offset = offset;
    pShdr->sh_size = dbgInfo.MemSize;
    offset += dbgInfo.MemSize;
    ++pShdr; // .debug_pubnames
    pShdr->sh_offset = offset;
    pShdr->sh_size = dbgPubname.MemSize;
    offset += dbgPubname.MemSize;
    ++pShdr; // .debug_pubtypes
    pShdr->sh_offset = offset;
    pShdr->sh_size = dbgPubType.MemSize;
    offset += dbgPubType.MemSize;
    ++pShdr; // .debug_line
    pShdr->sh_offset = offset;
    pShdr->sh_size = dbgLine.MemSize;
    offset += dbgLine.MemSize;
    
    /* Build ELF header */
    if (!BuildELFHeader(elfHeader))
    {
        return;
    }
    Elf_Ehdr* header = reinterpret_cast<Elf_Ehdr*>(elfHeader.MemPtr.GetValue());
#ifdef _TARGET_ARM_
    header->e_flags = EF_ARM_EABI_VER5;
#ifdef ARM_SOFTFP
    header->e_flags |= EF_ARM_SOFT_FLOAT;
#else    
    header->e_flags |= EF_ARM_VFP_FLOAT;
#endif
#endif    
    header->e_shoff = offset;
    header->e_shentsize = sizeof(Elf_Shdr);
    header->e_shnum = SectionNamesCount - 1;
    header->e_shstrndx = 2;

    /* Build ELF image in memory */
    elfFile.MemSize = elfHeader.MemSize + sectStr.MemSize + dbgStr.MemSize + dbgAbbrev.MemSize
                        + dbgInfo.MemSize + dbgPubname.MemSize + dbgPubType.MemSize + dbgLine.MemSize + sectHeaders.MemSize;
    elfFile.MemPtr =  new (nothrow) char[elfFile.MemSize];
    if (elfFile.MemPtr == nullptr)
    {
        return;
    }
    
    /* Copy section data */
    offset = 0;
    memcpy(elfFile.MemPtr, elfHeader.MemPtr, elfHeader.MemSize);
    offset += elfHeader.MemSize;
    memcpy(elfFile.MemPtr + offset, sectStr.MemPtr, sectStr.MemSize);
    offset +=  sectStr.MemSize;
    memcpy(elfFile.MemPtr + offset, dbgStr.MemPtr, dbgStr.MemSize);
    offset +=  dbgStr.MemSize;
    memcpy(elfFile.MemPtr + offset, dbgAbbrev.MemPtr, dbgAbbrev.MemSize);
    offset +=  dbgAbbrev.MemSize;
    memcpy(elfFile.MemPtr + offset, dbgInfo.MemPtr, dbgInfo.MemSize);
    offset +=  dbgInfo.MemSize;
    memcpy(elfFile.MemPtr + offset, dbgPubname.MemPtr, dbgPubname.MemSize);
    offset +=  dbgPubname.MemSize;
    memcpy(elfFile.MemPtr + offset, dbgPubType.MemPtr, dbgPubType.MemSize);
    offset +=  dbgPubType.MemSize;
    memcpy(elfFile.MemPtr + offset, dbgLine.MemPtr, dbgLine.MemSize);
    offset +=  dbgLine.MemSize;
    memcpy(elfFile.MemPtr + offset, sectHeaders.MemPtr, sectHeaders.MemSize);

    /* Create GDB JIT structures */
    jit_code_entry* jit_symbols = new (nothrow) jit_code_entry;
    
    if (jit_symbols == nullptr)
    {
        return;
    }
    
    /* Fill the new entry */
    jit_symbols->next_entry = jit_symbols->prev_entry = 0;
    jit_symbols->symfile_addr = elfFile.MemPtr;
    jit_symbols->symfile_size = elfFile.MemSize;
    
    /* Link into list */
    jit_code_entry *head = __jit_debug_descriptor.first_entry;
    __jit_debug_descriptor.first_entry = jit_symbols;
    if (head != 0)
    {
        jit_symbols->next_entry = head;
        head->prev_entry = jit_symbols;
    }
    
    /* Notify the debugger */
    __jit_debug_descriptor.relevant_entry = jit_symbols;
    __jit_debug_descriptor.action_flag = JIT_REGISTER_FN;
    __jit_debug_register_code();

}

void NotifyGdb::MethodDropped(MethodDesc* MethodDescPtr)
{
    PCODE pCode = MethodDescPtr->GetNativeCode();

    if (pCode == NULL)
        return;
    
    /* Find relevant entry */
    for (jit_code_entry* jit_symbols = __jit_debug_descriptor.first_entry; jit_symbols != 0; jit_symbols = jit_symbols->next_entry)
    {
        const char* ptr = jit_symbols->symfile_addr;
        uint64_t size = jit_symbols->symfile_size;
        
        const Elf_Ehdr* pEhdr = reinterpret_cast<const Elf_Ehdr*>(ptr);
        const Elf_Shdr* pShdr = reinterpret_cast<const Elf_Shdr*>(ptr + pEhdr->e_shoff);
        ++pShdr; // bump to .text section
        if (pShdr->sh_addr == pCode)
        {
            /* Notify the debugger */
            __jit_debug_descriptor.relevant_entry = jit_symbols;
            __jit_debug_descriptor.action_flag = JIT_UNREGISTER_FN;
            __jit_debug_register_code();
            
            /* Free memory */
            delete[] ptr;
            
            /* Unlink from list */
            if (jit_symbols->prev_entry == 0)
                __jit_debug_descriptor.first_entry = jit_symbols->next_entry;
            else
                jit_symbols->prev_entry->next_entry = jit_symbols->next_entry;
            delete jit_symbols;
            break;
        }
    }
}

/* Build the DWARF .debug_line section */
bool NotifyGdb::BuildLineTable(MemBuf& buf, PCODE startAddr, SymbolsInfo* lines, unsigned nlines)
{
    MemBuf fileTable, lineProg;
    
    /* Build file table */
    if (!BuildFileTable(fileTable, lines, nlines))
        return false;
    /* Build line info program */ 
    if (!BuildLineProg(lineProg, startAddr, lines, nlines))
    {
        return false;
    }
    
    buf.MemSize = sizeof(DwarfLineNumHeader) + 1 + fileTable.MemSize + lineProg.MemSize;
    buf.MemPtr = new (nothrow) char[buf.MemSize];
    
    if (buf.MemPtr == nullptr)
    {
        return false;
    }
    
    /* Fill the line info header */
    DwarfLineNumHeader* header = reinterpret_cast<DwarfLineNumHeader*>(buf.MemPtr.GetValue());
    memcpy(buf.MemPtr, &LineNumHeader, sizeof(DwarfLineNumHeader));
    header->m_length = buf.MemSize - sizeof(uint32_t);
    header->m_hdr_length = sizeof(DwarfLineNumHeader) + 1 + fileTable.MemSize - 2 * sizeof(uint32_t) - sizeof(uint16_t);
    buf.MemPtr[sizeof(DwarfLineNumHeader)] = 0; // this is for missing directory table
    /* copy file table */
    memcpy(buf.MemPtr + sizeof(DwarfLineNumHeader) + 1, fileTable.MemPtr, fileTable.MemSize);
    /* copy line program */
    memcpy(buf.MemPtr + sizeof(DwarfLineNumHeader) + 1 + fileTable.MemSize, lineProg.MemPtr, lineProg.MemSize);

    return true;
}

/* Buid the source files table for DWARF source line info */
bool NotifyGdb::BuildFileTable(MemBuf& buf, SymbolsInfo* lines, unsigned nlines)
{
    const char** files = nullptr;
    unsigned nfiles = 0;
    
    /* GetValue file names and replace them with indices in file table */
    files = new (nothrow) const char*[nlines];
    if (files == nullptr)
        return false;
    for (unsigned i = 0; i < nlines; ++i)
    {
        const char *filePath, *fileName;
        SplitPathname(lines[i].fileName, filePath, fileName);

        /* if this isn't first then we already added file, so adjust index */
        lines[i].fileIndex = (nfiles) ? (nfiles - 1) : (nfiles);

        bool found = false;
        for (int j = 0; j < nfiles; ++j)
        {
            if (strcmp(fileName, files[j]) == 0)
            {
                found = true;
                break;
            }
        }
        
        /* add new source file */
        if (!found)
        {
            files[nfiles++] = fileName;
        }
    }
    
    /* build file table */
    unsigned totalSize = 0;
    
    for (unsigned i = 0; i < nfiles; ++i)
    {
        totalSize += strlen(files[i]) + 1 + 3;
    }
    totalSize += 1;
    
    buf.MemSize = totalSize;
    buf.MemPtr = new (nothrow) char[buf.MemSize];
    
    if (buf.MemPtr == nullptr)
    {
        delete[] files;
        return false;
    }
    
    /* copy collected file names */
    char *ptr = buf.MemPtr;
    for (unsigned i = 0; i < nfiles; ++i)
    {
        strcpy(ptr, files[i]);
        ptr += strlen(files[i]) + 1;
        // three LEB128 entries which we don't care
        *ptr++ = 0;
        *ptr++ = 0;
        *ptr++ = 0;
    }
    // final zero byte
    *ptr = 0;

    delete[] files;
    return true;
}

/* Command to set absolute address */
void NotifyGdb::IssueSetAddress(char*& ptr, PCODE addr)
{
    *ptr++ = 0;
    *ptr++ = ADDRESS_SIZE + 1;
    *ptr++ = DW_LNE_set_address;
    *reinterpret_cast<PCODE*>(ptr) = addr;
    ptr += ADDRESS_SIZE;
}

/* End of line program */
void NotifyGdb::IssueEndOfSequence(char*& ptr)
{
    *ptr++ = 0;
    *ptr++ = 1;
    *ptr++ = DW_LNE_end_sequence;
}

/* Command w/o parameters */
void NotifyGdb::IssueSimpleCommand(char*& ptr, uint8_t command)
{
    *ptr++ = command;
}

/* Command with one LEB128 parameter */
void NotifyGdb::IssueParamCommand(char*& ptr, uint8_t command, char* param, int param_size)
{
    *ptr++ = command;
    while (param_size-- > 0)
    {
        *ptr++ = *param++;
    }
}

/* Special command moves address, line number and issue one row to source line matrix */
void NotifyGdb::IssueSpecialCommand(char*& ptr, int8_t line_shift, uint8_t addr_shift)
{
    *ptr++ = (line_shift - DWARF_LINE_BASE) + addr_shift * DWARF_LINE_RANGE + DWARF_OPCODE_BASE;
}

/* Check to see if given shifts are fit into one byte command */
bool NotifyGdb::FitIntoSpecialOpcode(int8_t line_shift, uint8_t addr_shift)
{
    unsigned opcode = (line_shift - DWARF_LINE_BASE) + addr_shift * DWARF_LINE_RANGE + DWARF_OPCODE_BASE;
    
    return opcode < 255;
}

/* Build program for DWARF source line section */
bool NotifyGdb::BuildLineProg(MemBuf& buf, PCODE startAddr, SymbolsInfo* lines, unsigned nlines)
{
    static char cnv_buf[16];
    
    /* reserve memory assuming worst case: one extended and one special command for each line */
    buf.MemSize = nlines * ( 4 + ADDRESS_SIZE) + 4;
    buf.MemPtr = new (nothrow) char[buf.MemSize];
    char* ptr = buf.MemPtr;
  
    if (buf.MemPtr == nullptr)
        return false;
    
    /* set absolute start address */
    IssueSetAddress(ptr, startAddr);
    IssueSimpleCommand(ptr, DW_LNS_set_prologue_end);
    
    int prevLine = 1, prevAddr = 0, prevFile = 0;
    
    for (int i = 0; i < nlines; ++i)
    {
        /* different source file */
        if (lines[i].fileIndex != prevFile)
        {
            int len = Leb128Encode(static_cast<uint32_t>(lines[i].fileIndex+1), cnv_buf, sizeof(cnv_buf));
            IssueParamCommand(ptr, DW_LNS_set_file, cnv_buf, len);
            prevFile = lines[i].fileIndex;
        }
        /* too big line number shift */
        if (lines[i].lineNumber - prevLine > (DWARF_LINE_BASE + DWARF_LINE_RANGE - 1))
        {
            int len = Leb128Encode(static_cast<int32_t>(lines[i].lineNumber - prevLine), cnv_buf, sizeof(cnv_buf));
            IssueParamCommand(ptr, DW_LNS_advance_line, cnv_buf, len);
            prevLine = lines[i].lineNumber;
        }
        /* first try special opcode */
        if (FitIntoSpecialOpcode(lines[i].lineNumber - prevLine, lines[i].nativeOffset - prevAddr))
            IssueSpecialCommand(ptr, lines[i].lineNumber - prevLine, lines[i].nativeOffset - prevAddr);
        else
        {
            IssueSetAddress(ptr, startAddr + lines[i].nativeOffset);
            IssueSpecialCommand(ptr, lines[i].lineNumber - prevLine, 0);
        }
           
        prevLine = lines[i].lineNumber;
        prevAddr = lines[i].nativeOffset;
    }
    
    IssueEndOfSequence(ptr); 
    
    buf.MemSize = ptr - buf.MemPtr;
    return true;
}

/* Build the DWARF .debug_str section */
bool NotifyGdb::BuildDebugStrings(MemBuf& buf)
{
    uint32_t totalLength = 0;
    
    /* calculate total section size */
    for (int i = 0; i < DebugStringCount; ++i)
    {
        totalLength += strlen(DebugStrings[i]) + 1;
    }
    
    buf.MemSize = totalLength;
    buf.MemPtr = new (nothrow) char[totalLength];
    
    if (buf.MemPtr == nullptr)
        return false;

    /* copy strings */
    char* bufPtr = buf.MemPtr;
    for (int i = 0; i < DebugStringCount; ++i)
    {
        strcpy(bufPtr, DebugStrings[i]);
        bufPtr += strlen(DebugStrings[i]) + 1;
    }
    
    return true;
}

/* Build the DWARF .debug_abbrev section */
bool NotifyGdb::BuildDebugAbbrev(MemBuf& buf)
{
    buf.MemPtr = new (nothrow) char[AbbrevTableSize];
    buf.MemSize = AbbrevTableSize;
    
    if (buf.MemPtr == nullptr)
        return false;
    
    memcpy(buf.MemPtr, AbbrevTable, AbbrevTableSize);
    return true;
}

/* Build tge DWARF .debug_info section */
bool NotifyGdb::BuildDebugInfo(MemBuf& buf)
{
    buf.MemSize = sizeof(DwarfCompUnit) + sizeof(DebugInfo) + 1;
    buf.MemPtr = new (nothrow) char[buf.MemSize];

    if (buf.MemPtr == nullptr)
        return false;
    
    /* Compile uint header */
    DwarfCompUnit* cu = reinterpret_cast<DwarfCompUnit*>(buf.MemPtr.GetValue());
    cu->m_length = buf.MemSize - sizeof(uint32_t);
    cu->m_version = 4;
    cu->m_abbrev_offset = 0;
    cu->m_addr_size = ADDRESS_SIZE;
    
    /* copy debug information */
    DebugInfo* di = reinterpret_cast<DebugInfo*>(buf.MemPtr + sizeof(DwarfCompUnit));
    memcpy(buf.MemPtr + sizeof(DwarfCompUnit), &debugInfo, sizeof(DebugInfo));
    di->m_prod_off = 0;
    di->m_cu_name = strlen(DebugStrings[0]) + 1;
    di->m_sub_name = strlen(DebugStrings[0]) + 1 + strlen(DebugStrings[1]) + 1 + strlen(DebugStrings[2]) + 1;
    di->m_type_name = strlen(DebugStrings[0]) + 1 + strlen(DebugStrings[1]) + 1 + strlen(DebugStrings[2]) + 1 + strlen(DebugStrings[3]) + 1;
    
    /* zero end marker */
    buf.MemPtr[buf.MemSize-1] = 0;
    return true;
}

/* Build the DWARF lookup section */
bool NotifyGdb::BuildDebugPub(MemBuf& buf, const char* name, uint32_t size, uint32_t die_offset)
{
    uint32_t length = sizeof(DwarfPubHeader) + sizeof(uint32_t) + strlen(name) + 1 + sizeof(uint32_t);
    
    buf.MemSize = length;
    buf.MemPtr = new (nothrow) char[buf.MemSize];
    
    if (buf.MemPtr == nullptr)
        return false;

    DwarfPubHeader* header = reinterpret_cast<DwarfPubHeader*>(buf.MemPtr.GetValue());
    header->m_length = length - sizeof(uint32_t);
    header->m_version = 2;
    header->m_debug_info_off = 0;
    header->m_debug_info_len = size;
    *reinterpret_cast<uint32_t*>(buf.MemPtr + sizeof(DwarfPubHeader)) = die_offset;
    strcpy(buf.MemPtr + sizeof(DwarfPubHeader) + sizeof(uint32_t), name);
    *reinterpret_cast<uint32_t*>(buf.MemPtr + length - sizeof(uint32_t)) = 0;
    
    return true;
}

/* Build ELF string section */
bool NotifyGdb::BuildSectionNameTable(MemBuf& buf)
{
    uint32_t totalLength = 0;
    
    /* calculate total size */
    for (int i = 0; i < SectionNamesCount; ++i)
    {
        totalLength += strlen(SectionNames[i]) + 1;
    }
    
    buf.MemSize = totalLength;
    buf.MemPtr = new (nothrow) char[totalLength];
    if (buf.MemPtr == nullptr)
        return false;

    /* copy strings */
    char* bufPtr = buf.MemPtr;
    for (int i = 0; i < SectionNamesCount; ++i)
    {
        strcpy(bufPtr, SectionNames[i]);
        bufPtr += strlen(SectionNames[i]) + 1;
    }
    
    return true;
}

/* Build the ELF section headers table */
bool NotifyGdb::BuildSectionTable(MemBuf& buf)
{
    Elf_Shdr* sectionHeaders = new (nothrow) Elf_Shdr[SectionNamesCount - 1];    
    Elf_Shdr* pSh = sectionHeaders;

    if (sectionHeaders == nullptr)
    {
        return false;
    }
    
    /* NULL entry */
    pSh->sh_name = 0;
    pSh->sh_type = SHT_NULL;
    pSh->sh_flags = 0;
    pSh->sh_addr = 0;
    pSh->sh_offset = 0;
    pSh->sh_size = 0;
    pSh->sh_link = SHN_UNDEF;
    pSh->sh_info = 0;
    pSh->sh_addralign = 0;
    pSh->sh_entsize = 0;
    
    ++pSh;
    /* fill section header data */
    uint32_t sectNameOffset = 1;
    for (int i = 1; i < SectionNamesCount - 1; ++i, ++pSh)
    {
        pSh->sh_name = sectNameOffset;
        sectNameOffset += strlen(SectionNames[i]) + 1;
        pSh->sh_type = Sections[i].m_type;
        pSh->sh_flags = Sections[i].m_flags;
        pSh->sh_addr = 0;
        pSh->sh_offset = 0;
        pSh->sh_size = 0;
        pSh->sh_link = SHN_UNDEF;
        pSh->sh_info = 0;
        pSh->sh_addralign = 1;
        pSh->sh_entsize = 0;
    }

    buf.MemPtr = reinterpret_cast<char*>(sectionHeaders);
    buf.MemSize = sizeof(Elf_Shdr) * (SectionNamesCount - 1);
    return true;
}

/* Build the ELF header */
bool NotifyGdb::BuildELFHeader(MemBuf& buf)
{
    Elf_Ehdr* header = new (nothrow) Elf_Ehdr;
    buf.MemPtr = reinterpret_cast<char*>(header);
    buf.MemSize = sizeof(Elf_Ehdr);
    
    if (header == nullptr)
        return false;
    
    return true;
        
}

/* Split full path name into directory & file anmes */
void NotifyGdb::SplitPathname(const char* path, const char*& pathName, const char*& fileName)
{
    char* pSlash = strrchr(path, '/');
    
    if (pSlash != nullptr)
    {
        *pSlash = 0;
        fileName = ++pSlash;
        pathName = path;
    }
    else 
    {
        fileName = path;
        pathName = nullptr;
    }
}

/* LEB128 for 32-bit unsigned integer */
int NotifyGdb::Leb128Encode(uint32_t num, char* buf, int size)
{
    int i = 0;
    
    do
    {
        uint8_t byte = num & 0x7F;
        if (i >= size)
            break;
        num >>= 7;
        if (num != 0)
            byte |= 0x80;
        buf[i++] = byte;
    }
    while (num != 0);
    
    return i;
}

/* LEB128 for 32-bit signed integer */
int NotifyGdb::Leb128Encode(int32_t num, char* buf, int size)
{
    int i = 0;
    bool hasMore = true, isNegative = num < 0;
    
    while (hasMore && i < size)
    {
        uint8_t byte = num & 0x7F;
        num >>= 7;
        
        if ((num == 0 && (byte & 0x40) == 0) || (num  == -1 && (byte & 0x40) == 0x40))
            hasMore = false;
        else
            byte |= 0x80;
        buf[i++] = byte;
    }
    
    return i;
}

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
    for (int i = EI_PAD; i < EI_NIDENT; ++i)
        e_ident[i] = 0;

    e_type = ET_REL;
#if defined(_TARGET_X86_)
    e_machine = EM_386;
#elif defined(_TARGET_ARM_)
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
    for (int i = EI_PAD; i < EI_NIDENT; ++i)
        e_ident[i] = 0;

    e_type = ET_REL;
#if defined(_TARGET_AMD64_)
    e_machine = EM_X86_64;
#elif defined(_TARGET_ARM64_)
    e_machine = EM_AARCH64;
#endif
    e_flags = 0;
    e_version = 1;
    e_entry = 0;
    e_phoff = 0;
    e_ehsize = sizeof(Elf64_Ehdr);
    e_phentsize = 0;
    e_phnum = 0;
}
