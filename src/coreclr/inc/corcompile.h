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

// Tags for fixup blobs
enum CORCOMPILE_FIXUP_BLOB_KIND
{
    ENCODE_NONE                         = 0,

    ENCODE_MODULE_OVERRIDE              = 0x80,     /* When the high bit is set, override of the module immediately follows */

    ENCODE_DICTIONARY_LOOKUP_THISOBJ    = 0x07,
    ENCODE_DICTIONARY_LOOKUP_TYPE       = 0x08,
    ENCODE_DICTIONARY_LOOKUP_METHOD     = 0x09,

    ENCODE_TYPE_HANDLE                  = 0x10,     /* Type handle */
    ENCODE_METHOD_HANDLE,                           /* Method handle */
    ENCODE_FIELD_HANDLE,                            /* Field handle */

    ENCODE_METHOD_ENTRY,                            /* For calling a method entry point */
    ENCODE_METHOD_ENTRY_DEF_TOKEN,                  /* Smaller version of ENCODE_METHOD_ENTRY - method is def token */
    ENCODE_METHOD_ENTRY_REF_TOKEN,                  /* Smaller version of ENCODE_METHOD_ENTRY - method is ref token */

    ENCODE_VIRTUAL_ENTRY,                           /* For invoking a virtual method */
    ENCODE_VIRTUAL_ENTRY_DEF_TOKEN,                 /* Smaller version of ENCODE_VIRTUAL_ENTRY - method is def token */
    ENCODE_VIRTUAL_ENTRY_REF_TOKEN,                 /* Smaller version of ENCODE_VIRTUAL_ENTRY - method is ref token */
    ENCODE_VIRTUAL_ENTRY_SLOT,                      /* Smaller version of ENCODE_VIRTUAL_ENTRY - type & slot */

    ENCODE_READYTORUN_HELPER,                       /* ReadyToRun helper */
    ENCODE_STRING_HANDLE,                           /* String token */

    ENCODE_NEW_HELPER,                              /* Dynamically created new helpers */
    ENCODE_NEW_ARRAY_HELPER,

    ENCODE_ISINSTANCEOF_HELPER,                     /* Dynamically created casting helper */
    ENCODE_CHKCAST_HELPER,

    ENCODE_FIELD_ADDRESS,                           /* For accessing a cross-module static fields */
    ENCODE_CCTOR_TRIGGER,                           /* Static constructor trigger */

    ENCODE_STATIC_BASE_NONGC_HELPER,                /* Dynamically created static base helpers */
    ENCODE_STATIC_BASE_GC_HELPER,
    ENCODE_THREAD_STATIC_BASE_NONGC_HELPER,
    ENCODE_THREAD_STATIC_BASE_GC_HELPER,

    ENCODE_FIELD_BASE_OFFSET,                       /* Field base */
    ENCODE_FIELD_OFFSET,

    ENCODE_TYPE_DICTIONARY,
    ENCODE_METHOD_DICTIONARY,

    ENCODE_CHECK_TYPE_LAYOUT,
    ENCODE_CHECK_FIELD_OFFSET,

    ENCODE_DELEGATE_CTOR,

    ENCODE_DECLARINGTYPE_HANDLE,

    ENCODE_INDIRECT_PINVOKE_TARGET,                 /* For calling a pinvoke method ptr indirectly */
    ENCODE_PINVOKE_TARGET,                          /* For calling a pinvoke method ptr */

    ENCODE_CHECK_INSTRUCTION_SET_SUPPORT,           /* Define the set of instruction sets that must be supported/unsupported to use the fixup */

    ENCODE_VERIFY_FIELD_OFFSET,                     /* Used for the R2R compiler can generate a check against the real field offset used at runtime */
    ENCODE_VERIFY_TYPE_LAYOUT,                      /* Used for the R2R compiler can generate a check against the real type layout used at runtime */

    ENCODE_CHECK_VIRTUAL_FUNCTION_OVERRIDE,         /* Generate a runtime check to ensure that virtual function resolution has equivalent behavior at runtime as at compile time. If not equivalent, code will not be used */
    ENCODE_VERIFY_VIRTUAL_FUNCTION_OVERRIDE,        /* Generate a runtime check to ensure that virtual function resolution has equivalent behavior at runtime as at compile time. If not equivalent, generate runtime failure. */

    ENCODE_MODULE_HANDLE                = 0x50,     /* Module token */
    ENCODE_STATIC_FIELD_ADDRESS,                    /* For accessing a static field */
    ENCODE_MODULE_ID_FOR_STATICS,                   /* For accessing static fields */
    ENCODE_MODULE_ID_FOR_GENERIC_STATICS,           /* For accessing static fields */
    ENCODE_CLASS_ID_FOR_STATICS,                    /* For accessing static fields */
    ENCODE_SYNC_LOCK,                               /* For synchronizing access to a type */
    ENCODE_PROFILING_HANDLE,                        /* For the method's profiling counter */
    ENCODE_VARARGS_METHODDEF,                       /* For calling a varargs method */
    ENCODE_VARARGS_METHODREF,
    ENCODE_VARARGS_SIG,
    ENCODE_ACTIVE_DEPENDENCY,                       /* Conditional active dependency */
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
};

enum EncodeFieldSigFlags
{
    ENCODE_FIELD_SIG_IndexInsteadOfToken        = 0x08,
    ENCODE_FIELD_SIG_MemberRefToken             = 0x10,
    ENCODE_FIELD_SIG_OwnerType                  = 0x40,
};

class SBuffer;
class SigBuilder;
class PEDecoder;
class GCRefMapBuilder;

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

/*********************************************************************************/
// When NGEN install /Profile is run, the ZapProfilingHandleImport fixup table contains
// these 5 values per MethodDesc
enum
{
    kZapProfilingHandleImportValueIndexFixup        = 0,
    kZapProfilingHandleImportValueIndexEnterAddr    = 1,
    kZapProfilingHandleImportValueIndexLeaveAddr    = 2,
    kZapProfilingHandleImportValueIndexTailcallAddr = 3,
    kZapProfilingHandleImportValueIndexClientData   = 4,

    kZapProfilingHandleImportValueIndexCount
};
#endif /* COR_COMPILE_H_ */
