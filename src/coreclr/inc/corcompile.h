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
// See code:CorProfileData for information on Hot Cold splitting using profile data.


#ifndef _COR_COMPILE_H_
#define _COR_COMPILE_H_

#include <cor.h>
#include <corhdr.h>
#include <corinfo.h>
#include <corjit.h>
#include <sstring.h>
#include <shash.h>
#include <daccess.h>
#include <corbbtprof.h>
#include <clrtypes.h>

typedef DPTR(struct CORCOMPILE_EXCEPTION_LOOKUP_TABLE)
    PTR_CORCOMPILE_EXCEPTION_LOOKUP_TABLE;
typedef DPTR(struct CORCOMPILE_EXCEPTION_LOOKUP_TABLE_ENTRY)
   PTR_CORCOMPILE_EXCEPTION_LOOKUP_TABLE_ENTRY;
typedef DPTR(struct CORCOMPILE_EXCEPTION_CLAUSE)
   PTR_CORCOMPILE_EXCEPTION_CLAUSE;
typedef DPTR(struct CORCOMPILE_EXTERNAL_METHOD_THUNK)
    PTR_CORCOMPILE_EXTERNAL_METHOD_THUNK;
typedef DPTR(struct CORCOMPILE_EXTERNAL_METHOD_DATA_ENTRY)
    PTR_CORCOMPILE_EXTERNAL_METHOD_DATA_ENTRY;
typedef DPTR(struct CORCOMPILE_IMPORT_SECTION)
    PTR_CORCOMPILE_IMPORT_SECTION;

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


typedef DPTR(struct CORCOMPILE_METHOD_PROFILE_LIST)
    PTR_CORCOMPILE_METHOD_PROFILE_LIST;
typedef DPTR(struct CORCOMPILE_RUNTIME_DLL_INFO)
    PTR_CORCOMPILE_RUNTIME_DLL_INFO;
typedef DPTR(struct COR_ILMETHOD) PTR_COR_ILMETHOD;

//
// CORCOMPILE_IMPORT_SECTION describes image range with references to other assemblies or runtime data structures
//
// There is number of different types of these ranges: eagerly initialized at image load vs. lazily initialized at method entry
// vs. lazily initialized on first use; hot vs. cold, handles vs. code pointers, etc.
//
struct CORCOMPILE_IMPORT_SECTION
{
    IMAGE_DATA_DIRECTORY    Section;            // Section containing values to be fixed up
    USHORT                  Flags;              // One or more of CorCompileImportFlags
    BYTE                    Type;               // One of CorCompileImportType
    BYTE                    EntrySize;
    DWORD                   Signatures;         // RVA of optional signature descriptors
    DWORD                   AuxiliaryData;      // RVA of optional auxiliary data (typically GC info)
};

enum CorCompileImportType
{
    CORCOMPILE_IMPORT_TYPE_UNKNOWN          = 0,
    CORCOMPILE_IMPORT_TYPE_EXTERNAL_METHOD  = 1,
    CORCOMPILE_IMPORT_TYPE_STUB_DISPATCH    = 2,
    CORCOMPILE_IMPORT_TYPE_STRING_HANDLE    = 3,
    CORCOMPILE_IMPORT_TYPE_TYPE_HANDLE      = 4,
    CORCOMPILE_IMPORT_TYPE_METHOD_HANDLE    = 5,
    CORCOMPILE_IMPORT_TYPE_VIRTUAL_METHOD   = 6,
};

enum CorCompileImportFlags
{
    CORCOMPILE_IMPORT_FLAGS_EAGER           = 0x0001,   // Section at module load time.
    CORCOMPILE_IMPORT_FLAGS_CODE            = 0x0002,   // Section contains code.
    CORCOMPILE_IMPORT_FLAGS_PCODE           = 0x0004,   // Section contains pointers to code.
};

// ================================================================================
// Portable tagged union of a pointer field with a 30 bit scalar value
// ================================================================================

// The lowest bit of the tag will be set for tagged pointers. We also set the highest bit for convenience.
// It makes dereferences of tagged pointers to crash under normal circumstances.
// The highest bit of the tag will be set for tagged indexes (e.g. classid).

#define CORCOMPILE_TOKEN_TAG 0x80000001

// These two macros are mostly used just for debug-only checks to ensure that we have either tagged pointer (lowest bit is set)
// or tagged index (highest bit is set).
#define CORCOMPILE_IS_POINTER_TAGGED(token)     ((((SIZE_T)(token)) & 0x00000001) != 0)
#define CORCOMPILE_IS_INDEX_TAGGED(token)       ((((SIZE_T)(token)) & 0x80000000) != 0)

// The token (RVA of the fixup in most cases) is stored in the mid 30 bits of DWORD
#define CORCOMPILE_TAG_TOKEN(token)             ((SIZE_T)(((token)<<1)|CORCOMPILE_TOKEN_TAG))
#define CORCOMPILE_UNTAG_TOKEN(token)           ((((SIZE_T)(token))&~CORCOMPILE_TOKEN_TAG)>>1)

#ifdef TARGET_ARM
// Tagging of code pointers on ARM uses inverse logic because of the thumb bit.
#define CORCOMPILE_IS_PCODE_TAGGED(token)       ((((SIZE_T)(token)) & 0x00000001) == 0x00000000)
#define CORCOMPILE_TAG_PCODE(token)             ((SIZE_T)(((token)<<1)|0x80000000))
#else
#define CORCOMPILE_IS_PCODE_TAGGED(token)       CORCOMPILE_IS_POINTER_TAGGED(token)
#define CORCOMPILE_TAG_PCODE(token)             CORCOMPILE_TAG_TOKEN(token)
#endif

inline BOOL CORCOMPILE_IS_FIXUP_TAGGED(SIZE_T fixup, PTR_CORCOMPILE_IMPORT_SECTION pSection)
{
#ifdef TARGET_ARM
    // Tagging of code pointers on ARM has to use inverse logic because of the thumb bit
    if (pSection->Flags & CORCOMPILE_IMPORT_FLAGS_PCODE)
    {
        return CORCOMPILE_IS_PCODE_TAGGED(fixup);
    }
#endif

    return ((((SIZE_T)(fixup)) & CORCOMPILE_TOKEN_TAG) == CORCOMPILE_TOKEN_TAG);
}

/*********************************************************************************/

#if defined(TARGET_X86) || defined(TARGET_AMD64)

#define _PRECODE_EXTERNAL_METHOD_THUNK      0x41
    struct  CORCOMPILE_EXTERNAL_METHOD_THUNK
    {
        BYTE                callJmp[5];     // Call/Jmp Pc-Rel32
        BYTE                precodeType;    // 0x41 _PRECODE_EXTERNAL_METHOD_THUNK
        WORD                padding;
    };

#elif defined(TARGET_ARM)

    struct  CORCOMPILE_EXTERNAL_METHOD_THUNK
    {
        // Array of words to do the following:
        //
        // mov r12, pc       ; Save the current address relative to which we will get GCRef bitmap and address to patch.
        // ldr pc, [pc, #4]  ; Load the target address. Initially it will point to the helper stub that will patch it
        //                   ; to point to the actual target on the first run.
        WORD                m_rgCode[3];

        WORD                m_padding;

        // The target address - initially, this will point to ExternalMethodFixupStub.
        // Post patchup by the stub, it will point to the actual method body.
        PCODE               m_pTarget;
    };

#elif defined(TARGET_ARM64)
    struct  CORCOMPILE_EXTERNAL_METHOD_THUNK
    {
        // Array of words to do the following:
        // adr         x12, #0            ; Save the current address relative to which we will get slot ID and address to patch.
        // ldr         x10, [x12, #16]    ; Load the target address.
        // br          x10                ; Jump to the target
        DWORD                m_rgCode[3];

        DWORD                m_padding; //aligning stack to 16 bytes

        // The target address - initially, this will point to ExternalMethodFixupStub.
        // Post patchup by the stub, it will point to the actual method body.
        PCODE                m_pTarget;
    };

#endif

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
// The layout of this struct is required to be
// a 'next' pointer followed by a CORBBTPROF_METHOD_HEADER
//
struct CORCOMPILE_METHOD_PROFILE_LIST
{
    CORCOMPILE_METHOD_PROFILE_LIST *       next;
//  CORBBTPROF_METHOD_HEADER               info;

    CORBBTPROF_METHOD_HEADER * GetInfo()
    { return (CORBBTPROF_METHOD_HEADER *) (this+1); }
};

class CorProfileData
{
public:
    CorProfileData(void *  rawProfileData);  // really of type ZapImage::ProfileDataSection*

    struct CORBBTPROF_TOKEN_INFO *  GetTokenFlagsData(SectionFormat section)
    {
        return this->profilingTokenFlagsData[section].data;
    }

    DWORD GetTokenFlagsCount(SectionFormat section)
    {
        return this->profilingTokenFlagsData[section].count;
    }

    CORBBTPROF_BLOB_ENTRY *  GetBlobStream()
    {
        return this->blobStream;
    }

    //
    //  Token lookup methods
    //
    ULONG GetTypeProfilingFlagsOfToken(mdToken token)
    {
        _ASSERTE(TypeFromToken(token) == mdtTypeDef);
        return  GetProfilingFlagsOfToken(token);
    }

    CORBBTPROF_BLOB_PARAM_SIG_ENTRY *GetBlobSigEntry(mdToken token)
    {
        _ASSERTE((TypeFromToken(token) == ibcTypeSpec) || (TypeFromToken(token) == ibcMethodSpec));

        CORBBTPROF_BLOB_ENTRY *  pBlobEntry = GetBlobEntry(token);
        if (pBlobEntry == NULL)
            return NULL;

        _ASSERTE(pBlobEntry->token == token);
        _ASSERTE((pBlobEntry->type == ParamTypeSpec) || (pBlobEntry->type == ParamMethodSpec));

        return (CORBBTPROF_BLOB_PARAM_SIG_ENTRY *) pBlobEntry;
    }

    CORBBTPROF_BLOB_NAMESPACE_DEF_ENTRY *GetBlobExternalNamespaceDef(mdToken token)
    {
        _ASSERTE(TypeFromToken(token) == ibcExternalNamespace);

        CORBBTPROF_BLOB_ENTRY *  pBlobEntry = GetBlobEntry(token);
        if (pBlobEntry == NULL)
            return NULL;

        _ASSERTE(pBlobEntry->token == token);
        _ASSERTE(pBlobEntry->type == ExternalNamespaceDef);

        return (CORBBTPROF_BLOB_NAMESPACE_DEF_ENTRY *) pBlobEntry;
    }

    CORBBTPROF_BLOB_TYPE_DEF_ENTRY *GetBlobExternalTypeDef(mdToken token)
    {
        _ASSERTE(TypeFromToken(token) == ibcExternalType);

        CORBBTPROF_BLOB_ENTRY *  pBlobEntry = GetBlobEntry(token);
        if (pBlobEntry == NULL)
            return NULL;

        _ASSERTE(pBlobEntry->token == token);
        _ASSERTE(pBlobEntry->type == ExternalTypeDef);

        return (CORBBTPROF_BLOB_TYPE_DEF_ENTRY *) pBlobEntry;
    }

    CORBBTPROF_BLOB_SIGNATURE_DEF_ENTRY *GetBlobExternalSignatureDef(mdToken token)
    {
        _ASSERTE(TypeFromToken(token) == ibcExternalSignature);

        CORBBTPROF_BLOB_ENTRY *  pBlobEntry = GetBlobEntry(token);
        if (pBlobEntry == NULL)
            return NULL;

        _ASSERTE(pBlobEntry->token == token);
        _ASSERTE(pBlobEntry->type == ExternalSignatureDef);

        return (CORBBTPROF_BLOB_SIGNATURE_DEF_ENTRY *) pBlobEntry;
    }

    CORBBTPROF_BLOB_METHOD_DEF_ENTRY *GetBlobExternalMethodDef(mdToken token)
    {
        _ASSERTE(TypeFromToken(token) == ibcExternalMethod);

        CORBBTPROF_BLOB_ENTRY *  pBlobEntry = GetBlobEntry(token);
        if (pBlobEntry == NULL)
            return NULL;

        _ASSERTE(pBlobEntry->token == token);
        _ASSERTE(pBlobEntry->type == ExternalMethodDef);

        return (CORBBTPROF_BLOB_METHOD_DEF_ENTRY *) pBlobEntry;
    }

private:
    ULONG GetProfilingFlagsOfToken(mdToken token)
    {
        SectionFormat section = (SectionFormat)((TypeFromToken(token) >> 24) + FirstTokenFlagSection);

        CORBBTPROF_TOKEN_INFO *profilingData = this->profilingTokenFlagsData[section].data;
        DWORD cProfilingData = this->profilingTokenFlagsData[section].count;

        if (profilingData != NULL)
        {
            for (DWORD i = 0; i < cProfilingData; i++)
            {
                if (profilingData[i].token == token)
                    return profilingData[i].flags;
            }
        }
        return 0;
    }

    CORBBTPROF_BLOB_ENTRY *GetBlobEntry(idTypeSpec token)
    {
        CORBBTPROF_BLOB_ENTRY *  pBlobEntry = this->GetBlobStream();
        if (pBlobEntry == NULL)
            return NULL;

        while (pBlobEntry->TypeIsValid())
        {
            if (pBlobEntry->token == token)
            {
                return pBlobEntry;
            }
            pBlobEntry = pBlobEntry->GetNextEntry();
        }

        return NULL;
    }

private:
    struct
    {
        struct CORBBTPROF_TOKEN_INFO *data;
        DWORD   count;
    }
    profilingTokenFlagsData[SectionFormatCount];

    CORBBTPROF_BLOB_ENTRY* blobStream;
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
