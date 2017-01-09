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
#include "dbginterface.h"
#include "../inc/llvm/ELF.h"
#include "../inc/llvm/Dwarf.h"

#if defined(_TARGET_X86_) || defined(_TARGET_ARM_)
    typedef Elf32_Ehdr  Elf_Ehdr;
    typedef Elf32_Shdr  Elf_Shdr;
    typedef Elf32_Sym   Elf_Sym;
    const uint16_t DW_FORM_size = DW_FORM_data4;
#define ADDRESS_SIZE 4    
#elif defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)
    typedef Elf64_Ehdr  Elf_Ehdr;
    typedef Elf64_Shdr  Elf_Shdr;
    typedef Elf64_Sym   Elf_Sym;
    const uint16_t DW_FORM_size = DW_FORM_data8;
#define ADDRESS_SIZE 8
#else
#error "Target is not supported"
#endif


static constexpr const int CorElementTypeToDWEncoding[] = 
{
/* ELEMENT_TYPE_END */          0,
/* ELEMENT_TYPE_VOID */         DW_ATE_address,
/* ELEMENT_TYPE_BOOLEAN */      DW_ATE_boolean,
/* ELEMENT_TYPE_CHAR */         DW_ATE_UTF,
/* ELEMENT_TYPE_I1 */           DW_ATE_signed,
/* ELEMENT_TYPE_U1 */           DW_ATE_unsigned,
/* ELEMENT_TYPE_I2 */           DW_ATE_signed,
/* ELEMENT_TYPE_U2 */           DW_ATE_unsigned,
/* ELEMENT_TYPE_I4 */           DW_ATE_signed,
/* ELEMENT_TYPE_U4 */           DW_ATE_unsigned,
/* ELEMENT_TYPE_I8 */           DW_ATE_signed,
/* ELEMENT_TYPE_U8 */           DW_ATE_unsigned,
/* ELEMENT_TYPE_R4 */           DW_ATE_float,
/* ELEMENT_TYPE_R8 */           DW_ATE_float,
/* ELEMENT_TYPE_STRING */       DW_ATE_address,
/* ELEMENT_TYPE_PTR */          DW_ATE_address,
/* ELEMENT_TYPE_BYREF */        DW_ATE_address,
/* ELEMENT_TYPE_VALUETYPE */    DW_ATE_address,
/* ELEMENT_TYPE_CLASS */        DW_ATE_address,
/* ELEMENT_TYPE_VAR */          DW_ATE_address,
/* ELEMENT_TYPE_ARRAY */        DW_ATE_address,
/* ELEMENT_TYPE_GENERICINST */  DW_ATE_address,
/* ELEMENT_TYPE_TYPEDBYREF */   DW_ATE_address,
/* SKIP 17 */                   DW_ATE_address,
/* ELEMENT_TYPE_I */            DW_ATE_signed,
/* ELEMENT_TYPE_U */            DW_ATE_unsigned,
/* SKIP 1a */                   DW_ATE_address,
/* ELEMENT_TYPE_FNPTR */        DW_ATE_address,
/* ELEMENT_TYPE_OBJECT */       DW_ATE_address,
/* ELEMENT_TYPE_SZARRAY */      DW_ATE_address,
/* ELEMENT_TYPE_MVAR */         DW_ATE_address,
/* ELEMENT_TYPE_CMOD_REQD */    DW_ATE_address,
/* ELEMENT_TYPE_CMOD_OPT */     DW_ATE_address,
/* ELEMENT_TYPE_INTERNAL */     DW_ATE_address,
/* ELEMENT_TYPE_MAX */          DW_ATE_address,
};

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

const ULONG32 HiddenLine = 0x00feefee;

struct SymbolsInfo
{
    int lineNumber, ilOffset, nativeOffset, fileIndex;
    char fileName[2*MAX_PATH_FNAME];
    ICorDebugInfo::SourceTypes source;
};

class DwarfDumpable
{
public:
    // writes all string literals this type needs to ptr
    virtual void DumpStrings(char* ptr, int& offset) = 0;

    virtual void DumpDebugInfo(char* ptr, int& offset) = 0;
};

class LocalsInfo 
{
public:
    int size;
    char** localsName;
    ULONG32 countVars;
    ICorDebugInfo::NativeVarInfo *pVars;
};

class TypeMember;

class TypeInfoBase : public DwarfDumpable
{
public:
    TypeInfoBase(TypeHandle typeHandle) 
        : m_type_name(nullptr),
          m_type_name_offset(0),
          m_type_size(0),
          m_type_offset(0),
          typeHandle(typeHandle),
          typeKey(typeHandle.GetTypeKey())
    {
    }

    virtual ~TypeInfoBase()
    {
        if (m_type_name != nullptr)
        {
            delete[] m_type_name;
        }
    }

    virtual void DumpStrings(char* ptr, int& offset) override;
    void CalculateName();
    void SetTypeHandle(TypeHandle handle);
    TypeHandle GetTypeHandle();
    TypeKey* GetTypeKey();

    char* m_type_name;
    int m_type_name_offset;
    ULONG m_type_size;
    int m_type_offset;
private:
    TypeHandle typeHandle;
    TypeKey typeKey;
};

class PrimitiveTypeInfo: public TypeInfoBase
{
public:
    PrimitiveTypeInfo(TypeHandle typeHandle, int encoding)
        : TypeInfoBase(typeHandle),
          m_type_encoding(encoding)
    {
    }

    void DumpDebugInfo(char* ptr, int& offset) override;

    int m_type_encoding;
};

class RefTypeInfo: public TypeInfoBase
{
public:
    RefTypeInfo(TypeHandle typeHandle, TypeInfoBase *value_type)
        : TypeInfoBase(typeHandle),
          m_value_type(value_type)
    {
    }
    void DumpStrings(char* ptr, int& offset) override;
    void DumpDebugInfo(char* ptr, int& offset) override;
    TypeInfoBase *m_value_type;
};

class ClassTypeInfo: public TypeInfoBase
{
public:
    ClassTypeInfo(TypeHandle typeHandle, int num_members);
    ~ClassTypeInfo();

    void DumpStrings(char* ptr, int& offset) override;
    void DumpDebugInfo(char* ptr, int& offset) override;

    int m_num_members;
    TypeMember* members;
};

class TypeMember: public DwarfDumpable
{
public:
    TypeMember()
        : m_member_name(nullptr),
          m_member_name_offset(0),
          m_member_offset(0),
          m_static_member_address(0),
          m_member_type(nullptr)
    {
    }

    ~TypeMember()
    {
        if (m_member_name != nullptr)
        {
            delete[] m_member_name;
        }
    }

    void DumpStrings(char* ptr, int& offset) override;
    void DumpDebugInfo(char* ptr, int& offset) override;
    void DumpStaticDebugInfo(char* ptr, int& offset);

    char* m_member_name;
    int m_member_name_offset;
    int m_member_offset;
    TADDR m_static_member_address;
    TypeInfoBase *m_member_type;
};

class ArrayTypeInfo: public TypeInfoBase
{
public:
    ArrayTypeInfo(TypeHandle typeHandle, int countOffset, TypeInfoBase* elemType)
        : TypeInfoBase(typeHandle),
          m_count_offset(countOffset),
          m_elem_type(elemType)
    {
    }

    ~ArrayTypeInfo()
    {
        if (m_elem_type != nullptr)
        {
            delete m_elem_type;
        }
    }

    void DumpDebugInfo(char* ptr, int& offset) override;

    int m_count_offset;
    TypeInfoBase *m_elem_type;
};

class VarDebugInfo: public DwarfDumpable
{
public:
    VarDebugInfo(int abbrev)
        : m_var_name(nullptr),
          m_var_abbrev(abbrev),
          m_var_name_offset(0),
          m_il_index(0),
          m_native_offset(0),
          m_var_type(nullptr)
    {
    }

    VarDebugInfo()
        : m_var_name(nullptr),
          m_var_abbrev(6),
          m_var_name_offset(0),
          m_il_index(0),
          m_native_offset(0),
          m_var_type(nullptr)
    {
    }

    virtual ~VarDebugInfo()
    {
        delete[] m_var_name;
    }

    void DumpStrings(char* ptr, int& offset) override;
    void DumpDebugInfo(char* ptr, int& offset) override;

    char* m_var_name;
    int m_var_abbrev;
    int m_var_name_offset;
    int m_il_index;
    int m_native_offset;
    TypeInfoBase *m_var_type;
};

class NotifyGdb
{
public:
    static void MethodCompiled(MethodDesc* MethodDescPtr);
    static void MethodDropped(MethodDesc* MethodDescPtr);
    template <typename PARENT_TRAITS>
    class DeleteValuesOnDestructSHashTraits : public PARENT_TRAITS
    {
    public:
        static inline void OnDestructPerEntryCleanupAction(typename PARENT_TRAITS::element_t e)
        {
            delete e.Value();
        }
        static const bool s_DestructPerEntryCleanupAction = true;
    };

    template <typename VALUE>
    class TypeKeyHashTraits : public DefaultSHashTraits< KeyValuePair<TypeKey*,VALUE> >
    {
    public:
        // explicitly declare local typedefs for these traits types, otherwise 
        // the compiler may get confused
        typedef typename DefaultSHashTraits< KeyValuePair<TypeKey*,VALUE> >::element_t element_t;
        typedef typename DefaultSHashTraits< KeyValuePair<TypeKey*,VALUE> >::count_t count_t;
        typedef TypeKey* key_t;

        static key_t GetKey(element_t e)
        {
            LIMITED_METHOD_CONTRACT;
            return e.Key();
        }
        static BOOL Equals(key_t k1, key_t k2)
        {
            LIMITED_METHOD_CONTRACT;
            return k1->Equals(k2);
        }
        static count_t Hash(key_t k)
        {
            LIMITED_METHOD_CONTRACT;
            return k->ComputeHash();
        }

        static const element_t Null() { LIMITED_METHOD_CONTRACT; return element_t(key_t(),VALUE()); }
        static const element_t Deleted() { LIMITED_METHOD_CONTRACT; return element_t(key_t(-1), VALUE()); }
        static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return e.Key() == key_t(); }
        static bool IsDeleted(const element_t &e) { return e.Key() == key_t(-1); }
    };

    typedef MapSHash<TypeKey*, TypeInfoBase*, DeleteValuesOnDestructSHashTraits<TypeKeyHashTraits<TypeInfoBase*>>> TK_TypeInfoMap;
    typedef TK_TypeInfoMap* PTK_TypeInfoMap;
    typedef SetSHash< TADDR,
                      NoRemoveSHashTraits <
                      NonDacAwareSHashTraits< SetSHashTraits <TADDR> >
                    > > AddrSet;
private:

    struct MemBuf
    {
        NewArrayHolder<char> MemPtr;
        unsigned MemSize;
        MemBuf() : MemPtr(0), MemSize(0)
        {}
        bool Resize(unsigned newSize)
        {
            if (newSize == 0)
            {
                MemPtr = nullptr;
                MemSize = 0;
                return true;
            }
            char *tmp = new (nothrow) char [newSize];
            if (tmp == nullptr)
                return false;
            memmove(tmp, MemPtr.GetValue(), newSize < MemSize ? newSize : MemSize);
            MemPtr = tmp;
            MemSize = newSize;
            return true;
        }
    };

    static int GetSectionIndex(const char *sectName);
    static bool BuildELFHeader(MemBuf& buf);
    static bool BuildSectionTables(MemBuf& sectBuf, MemBuf& strBuf);
    static bool BuildSymbolTableSection(MemBuf& buf, PCODE addr, TADDR codeSize);
    static bool BuildStringTableSection(MemBuf& strTab);
    static bool BuildDebugStrings(MemBuf& buf, PTK_TypeInfoMap pTypeMap);
    static bool BuildDebugAbbrev(MemBuf& buf);
    static bool BuildDebugInfo(MemBuf& buf, PTK_TypeInfoMap pTypeMap, SymbolsInfo* lines, unsigned nlines);
    static bool BuildDebugPub(MemBuf& buf, const char* name, uint32_t size, uint32_t dieOffset);
    static bool BuildLineTable(MemBuf& buf, PCODE startAddr, TADDR codeSize, SymbolsInfo* lines, unsigned nlines);
    static bool BuildFileTable(MemBuf& buf, SymbolsInfo* lines, unsigned nlines);
    static bool BuildLineProg(MemBuf& buf, PCODE startAddr, TADDR codeSize, SymbolsInfo* lines, unsigned nlines);
    static void IssueSetAddress(char*& ptr, PCODE addr);
    static void IssueEndOfSequence(char*& ptr);
    static void IssueSimpleCommand(char*& ptr, uint8_t command);
    static void IssueParamCommand(char*& ptr, uint8_t command, char* param, int param_len);
    static void SplitPathname(const char* path, const char*& pathName, const char*& fileName);
    static bool CollectCalledMethods(CalledMethod* pCM, TADDR nativeCode);
#ifdef _DEBUG
    static void DumpElf(const char* methodName, const MemBuf& buf);
#endif
};

class FunctionMember: public TypeMember
{
public:
    FunctionMember(MethodDesc *md, int num_locals, int num_args)
        : TypeMember(),
          md(md),
          m_file(1),
          m_line(1),
          m_sub_low_pc(0),
          m_sub_high_pc(0),
          m_sub_loc(),
          m_num_args(num_args),
          m_num_locals(num_locals),
          m_num_vars(num_args + num_locals),
          m_entry_offset(0),
          vars(new VarDebugInfo[m_num_vars]),
          lines(NULL),
          nlines(0),
          m_linkage_name_offset(0),
          dumped(false)
    {
        m_sub_loc[0] = 1;
#if defined(_TARGET_AMD64_)
        m_sub_loc[1] = DW_OP_reg6;
#elif defined(_TARGET_X86_)
        m_sub_loc[1] = DW_OP_reg5;
#elif defined(_TARGET_ARM_)
        m_sub_loc[1] = DW_OP_reg11;
#else
#error Unsupported platform!
#endif
    }

    virtual ~FunctionMember()
    {
        delete[] vars;
    }

    void DumpStrings(char* ptr, int& offset) override;
    void DumpDebugInfo(char* ptr, int& offset) override;
    void DumpTryCatchDebugInfo(char* ptr, int& offset);
    HRESULT GetLocalsDebugInfo(NotifyGdb::PTK_TypeInfoMap pTypeMap,
                           LocalsInfo& locals,
                           int startNativeOffset);
    BOOL IsDumped()
    {
        return dumped;
    }

    MethodDesc *md;
    uint8_t m_file, m_line;
    uintptr_t m_sub_low_pc, m_sub_high_pc;
    uint8_t m_sub_loc[2];
    uint8_t m_num_args;
    uint8_t m_num_locals;
    uint16_t m_num_vars;
    int m_entry_offset;
    VarDebugInfo* vars;
    SymbolsInfo* lines;
    unsigned nlines;
    int m_linkage_name_offset;
private:
    int GetArgsAndLocalsLen();
    void MangleName(char *buf, int &buf_offset, const char *name);
    void DumpMangledNamespaceAndMethod(char *buf, int &offset, const char *nspace, const char *mname);
    void DumpLinkageName(char* ptr, int& offset);
    bool GetBlockInNativeCode(int blockILOffset, int blockILLen, TADDR *startOffset, TADDR *endOffset);
    void DumpTryCatchBlock(char* ptr, int& offset, int ilOffset, int ilLen, int abbrev);
    BOOL dumped;
};
#endif // #ifndef __GDBJIT_H__
