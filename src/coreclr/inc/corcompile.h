// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************\
*                                                                             *
* CorCompile.h -    EE / Compiler interface                                   *
*                                                                             *
*               Version 1.0                                                   *
*******************************************************************************
*                                                                             *
*                                                                     *
*                                                                             *
\*****************************************************************************/

#ifndef _COR_COMPILE_H_
#define _COR_COMPILE_H_

#include <cor.h>
#include <corhdr.h>
#include <corinfo.h>
#include <corjit.h>
#include <sstring.h>
#include <shash.h>
#include <daccess.h>
#include <clrtypes.h>
#include <readytorun.h>

typedef DPTR(struct CORCOMPILE_EXCEPTION_LOOKUP_TABLE)
    PTR_CORCOMPILE_EXCEPTION_LOOKUP_TABLE;
typedef DPTR(struct CORCOMPILE_EXCEPTION_LOOKUP_TABLE_ENTRY)
   PTR_CORCOMPILE_EXCEPTION_LOOKUP_TABLE_ENTRY;
typedef DPTR(struct CORCOMPILE_EXCEPTION_CLAUSE)
   PTR_CORCOMPILE_EXCEPTION_CLAUSE;
typedef DPTR(struct CORCOMPILE_EXTERNAL_METHOD_DATA_ENTRY)
    PTR_CORCOMPILE_EXTERNAL_METHOD_DATA_ENTRY;
typedef DPTR(struct READYTORUN_IMPORT_SECTION)
    PTR_READYTORUN_IMPORT_SECTION;

inline ReadyToRunImportSectionFlags operator |( const ReadyToRunImportSectionFlags left, const ReadyToRunImportSectionFlags right)
{
    return static_cast<ReadyToRunImportSectionFlags>(static_cast<uint16_t>(left) | static_cast<uint16_t>(right));
}

inline ReadyToRunImportSectionFlags operator &( const ReadyToRunImportSectionFlags left, const ReadyToRunImportSectionFlags right)
{
    return static_cast<ReadyToRunImportSectionFlags>(static_cast<uint16_t>(left) & static_cast<uint16_t>(right));
}

inline ReadyToRunCrossModuleInlineFlags operator |( const ReadyToRunCrossModuleInlineFlags left, const ReadyToRunCrossModuleInlineFlags right)
{
    return static_cast<ReadyToRunCrossModuleInlineFlags>(static_cast<uint32_t>(left) | static_cast<uint32_t>(right));
}

inline ReadyToRunCrossModuleInlineFlags operator &( const ReadyToRunCrossModuleInlineFlags left, const ReadyToRunCrossModuleInlineFlags right)
{
    return static_cast<ReadyToRunCrossModuleInlineFlags>(static_cast<uint32_t>(left) & static_cast<uint32_t>(right));
}

#ifdef TARGET_WASM
// why was it defined only for x86 before?
typedef DPTR(RUNTIME_FUNCTION) PTR_RUNTIME_FUNCTION;
#endif

#ifdef TARGET_X86

typedef DPTR(RUNTIME_FUNCTION) PTR_RUNTIME_FUNCTION;


// Chained unwind info. Used for cold methods.
#ifdef HOST_X86
#define RUNTIME_FUNCTION_INDIRECT 0x80000000
#else
// If not hosted on X86, undefine RUNTIME_FUNCTION_INDIRECT as it likely isn't correct
#ifdef RUNTIME_FUNCTION_INDIRECT
#undef RUNTIME_FUNCTION_INDIRECT
#endif // RUNTIME_FUNCTION_INDIRECT
#endif // HOST_X86

#endif // TARGET_X86

typedef DPTR(struct CORCOMPILE_RUNTIME_DLL_INFO)
    PTR_CORCOMPILE_RUNTIME_DLL_INFO;
typedef DPTR(struct COR_ILMETHOD) PTR_COR_ILMETHOD;

//
// GCRefMap blob starts with DWORDs lookup index of relative offsets into the blob. This lookup index is used to limit amount
// of linear scanning required to find entry in the GCRefMap. The size of this lookup index is
// <totalNumberOfEntries in the GCRefMap> / GCREFMAP_LOOKUP_STRIDE.
//
#define GCREFMAP_LOOKUP_STRIDE 1024

enum CORCOMPILE_GCREFMAP_TOKENS
{
    GCREFMAP_SKIP = 0,
    GCREFMAP_REF = 1,
    GCREFMAP_INTERIOR = 2,
    GCREFMAP_METHOD_PARAM = 3,
    GCREFMAP_TYPE_PARAM = 4,
    GCREFMAP_VASIG_COOKIE = 5,
};

enum EncodeMethodSigFlags
{
    ENCODE_METHOD_SIG_UnboxingStub              = 0x01,
    ENCODE_METHOD_SIG_InstantiatingStub         = 0x02,
    ENCODE_METHOD_SIG_MethodInstantiation       = 0x04,
    ENCODE_METHOD_SIG_SlotInsteadOfToken        = 0x08,
    ENCODE_METHOD_SIG_MemberRefToken            = 0x10,
    ENCODE_METHOD_SIG_Constrained               = 0x20,
    ENCODE_METHOD_SIG_OwnerType                 = 0x40,
    ENCODE_METHOD_SIG_UpdateContext             = 0x80,
    ENCODE_METHOD_SIG_AsyncVariant             = 0x100,
};

enum EncodeFieldSigFlags
{
    ENCODE_FIELD_SIG_MemberRefToken             = 0x10,
    ENCODE_FIELD_SIG_OwnerType                  = 0x40,
};

//REVIEW: include for ee exception info
#include "eexcp.h"

struct CORCOMPILE_EXCEPTION_LOOKUP_TABLE_ENTRY
{
    DWORD MethodStartRVA;
    DWORD ExceptionInfoRVA;
};

struct CORCOMPILE_EXCEPTION_LOOKUP_TABLE
{
    // pointer to the first element of m_numLookupEntries elements
    CORCOMPILE_EXCEPTION_LOOKUP_TABLE_ENTRY m_Entries[1];

    CORCOMPILE_EXCEPTION_LOOKUP_TABLE_ENTRY* ExceptionLookupEntry(unsigned i)
    {
        SUPPORTS_DAC_WRAPPER;
        return &(PTR_CORCOMPILE_EXCEPTION_LOOKUP_TABLE_ENTRY(PTR_HOST_MEMBER_TADDR(CORCOMPILE_EXCEPTION_LOOKUP_TABLE,this,m_Entries))[i]);
    }
};

struct CORCOMPILE_EXCEPTION_CLAUSE
{
    CorExceptionFlag    Flags;
    DWORD               TryStartPC;
    DWORD               TryEndPC;
    DWORD               HandlerStartPC;
    DWORD               HandlerEndPC;
    union {
        mdToken         ClassToken;
        DWORD           FilterOffset;
    };
};

#endif /* COR_COMPILE_H_ */
