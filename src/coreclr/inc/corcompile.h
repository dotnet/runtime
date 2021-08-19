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
#include <fixuppointer.h>

typedef DPTR(struct CORCOMPILE_CODE_MANAGER_ENTRY)
    PTR_CORCOMPILE_CODE_MANAGER_ENTRY;
typedef DPTR(struct CORCOMPILE_EE_INFO_TABLE)
    PTR_CORCOMPILE_EE_INFO_TABLE;
typedef DPTR(struct CORCOMPILE_HEADER)
    PTR_CORCOMPILE_HEADER;
typedef DPTR(struct CORCOMPILE_COLD_METHOD_ENTRY)
    PTR_CORCOMPILE_COLD_METHOD_ENTRY;
typedef DPTR(struct CORCOMPILE_EXCEPTION_LOOKUP_TABLE)
    PTR_CORCOMPILE_EXCEPTION_LOOKUP_TABLE;
typedef DPTR(struct CORCOMPILE_EXCEPTION_LOOKUP_TABLE_ENTRY)
   PTR_CORCOMPILE_EXCEPTION_LOOKUP_TABLE_ENTRY;
typedef DPTR(struct CORCOMPILE_EXCEPTION_CLAUSE)
   PTR_CORCOMPILE_EXCEPTION_CLAUSE;
typedef DPTR(struct CORCOMPILE_VIRTUAL_IMPORT_THUNK)
    PTR_CORCOMPILE_VIRTUAL_IMPORT_THUNK;
typedef DPTR(struct CORCOMPILE_EXTERNAL_METHOD_THUNK)
    PTR_CORCOMPILE_EXTERNAL_METHOD_THUNK;
typedef DPTR(struct CORCOMPILE_EXTERNAL_METHOD_DATA_ENTRY)
    PTR_CORCOMPILE_EXTERNAL_METHOD_DATA_ENTRY;
typedef DPTR(struct CORCOMPILE_VIRTUAL_SECTION_INFO)
    PTR_CORCOMPILE_VIRTUAL_SECTION_INFO;
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

// The stride is choosen as maximum value that still gives good page locality of RUNTIME_FUNCTION table touches (only one page of
// RUNTIME_FUNCTION table is going to be touched during most IP2MD lookups).
//
// Smaller stride values also improve speed of IP2MD lookups, but this improvement is not significant (5% when going
// from 8192 to 1024), so the working set / page locality was used as the metric to choose the optimum value.
//
#define RUNTIME_FUNCTION_LOOKUP_STRIDE  8192


typedef DPTR(struct CORCOMPILE_METHOD_PROFILE_LIST)
    PTR_CORCOMPILE_METHOD_PROFILE_LIST;
typedef DPTR(struct CORCOMPILE_RUNTIME_DLL_INFO)
    PTR_CORCOMPILE_RUNTIME_DLL_INFO;
typedef DPTR(struct CORCOMPILE_VERSION_INFO)  PTR_CORCOMPILE_VERSION_INFO;
typedef DPTR(struct COR_ILMETHOD) PTR_COR_ILMETHOD;

// This can be used to specify a dll that should be used as the compiler during ngen.
// If this is not specified, the default compiler dll will be used.
// If this is specified, it needs to be specified for all the assemblies that are ngenned.
#define NGEN_COMPILER_OVERRIDE_KEY W("NGen_JitName")

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

enum CorCompileBuild
{
    CORCOMPILE_BUILD_CHECKED,
    CORCOMPILE_BUILD_FREE
};

enum CorCompileCodegen
{
    CORCOMPILE_CODEGEN_DEBUGGING            = 0x0001,   // suports debugging (unoptimized code with symbol info)

    CORCOMPILE_CODEGEN_PROFILING            = 0x0004,   // supports profiling
    CORCOMPILE_CODEGEN_PROF_INSTRUMENTING   = 0x0008,   // code is instrumented to collect profile count info

};


// Used for INativeImageInstallInfo::GetConfigMask()
// A bind will ask for the particular bits it needs set; if all bits are set, it is a match.  Additional
// bits are ignored.

enum CorCompileConfigFlags
{
    CORCOMPILE_CONFIG_DEBUG_NONE         = 0x01, // Assembly has Optimized code
    CORCOMPILE_CONFIG_DEBUG              = 0x02, // Assembly has non-Optimized debuggable code
    CORCOMPILE_CONFIG_DEBUG_DEFAULT      = 0x08, // Additional flag set if this particular setting is the
                                                 // one indicated by the assembly debug custom attribute.

    CORCOMPILE_CONFIG_PROFILING_NONE            = 0x100, // Assembly code has profiling hooks
    CORCOMPILE_CONFIG_PROFILING                 = 0x200, // Assembly code has profiling hooks

    CORCOMPILE_CONFIG_INSTRUMENTATION_NONE      = 0x1000, // Assembly code has no instrumentation
    CORCOMPILE_CONFIG_INSTRUMENTATION           = 0x2000, // Assembly code has basic block instrumentation
};

// Values for Flags field of CORCOMPILE_HEADER.
enum CorCompileHeaderFlags
{
    CORCOMPILE_HEADER_HAS_SECURITY_DIRECTORY    = 0x00000001,   // Original image had a security directory
                                                                // Note it is useless to cache the actual directory contents
                                                                // since it must be verified as part of the original image
    CORCOMPILE_HEADER_IS_IBC_OPTIMIZED          = 0x00000002,

    CORCOMPILE_HEADER_IS_READY_TO_RUN           = 0x00000004,
};

//
// !!! INCREMENT THE MAJOR VERSION ANY TIME THERE IS CHANGE IN CORCOMPILE_HEADER STRUCTURE !!!
//
#define CORCOMPILE_SIGNATURE     0x0045474E     // 'NGEN'
#define CORCOMPILE_MAJOR_VERSION 0x0001
#define CORCOMPILE_MINOR_VERSION 0x0000

// This structure is pointed to by the code:IMAGE_COR20_HEADER (see file:corcompile.h#ManagedHeader)
// See the file:../../doc/BookOfTheRuntime/NGEN/NGENDesign.doc for more
struct CORCOMPILE_HEADER
{
    // For backward compatibility reasons, VersionInfo field must be at offset 40, ManifestMetaData
    // must be at 88, PEKind must be at 112/116 bytes, Machine must be at 120/124 bytes, and
    // size of CORCOMPILE_HEADER must be 164/168 bytes.  Be careful when you modify this struct.
    // See code:PEDecoder::GetMetaDataHelper.
    DWORD                   Signature;
    USHORT                  MajorVersion;
    USHORT                  MinorVersion;

    IMAGE_DATA_DIRECTORY    HelperTable;    // Table of function pointers to JIT helpers indexed by helper number
    IMAGE_DATA_DIRECTORY    ImportSections; // points to array of code:CORCOMPILE_IMPORT_SECTION
    IMAGE_DATA_DIRECTORY    Dummy0;
    IMAGE_DATA_DIRECTORY    StubsData;      // contains the value to register with the stub manager for the delegate stubs & AMD64 tail call stubs
    IMAGE_DATA_DIRECTORY    VersionInfo;    // points to a code:CORCOMPILE_VERSION_INFO
    IMAGE_DATA_DIRECTORY    Dependencies;   // points to an array of code:CORCOMPILE_DEPENDENCY
    IMAGE_DATA_DIRECTORY    DebugMap;       // points to an array of code:CORCOMPILE_DEBUG_RID_ENTRY hashed by method RID
    IMAGE_DATA_DIRECTORY    ModuleImage;    // points to the freeze dried  Module structure
    IMAGE_DATA_DIRECTORY    CodeManagerTable;  // points to a code:CORCOMPILE_CODE_MANAGER_ENTRY
    IMAGE_DATA_DIRECTORY    ProfileDataList;// points to the list of code:CORCOMPILE_METHOD_PROFILE_LIST
    IMAGE_DATA_DIRECTORY    ManifestMetaData; // points to the native manifest metadata
    IMAGE_DATA_DIRECTORY    VirtualSectionsTable;// List of CORCOMPILE_VIRTUAL_SECTION_INFO. Contains a list of Section
                                                // ranges for debugging purposes. There is one entry in this table per
                                                // ZapVirtualSection in the NGEN image.  This data is used to fire ETW
                                                // events that describe the various VirtualSection in the NGEN image. These
                                                // events are used for diagnostics and performance purposes. Some of the
                                                // questions these events help answer are like : how effective is IBC
                                                // training data. They can also be used to have better nidump support for
                                                // decoding virtual section information ( start - end ranges for each
                                                // virtual section )

    TADDR                   ImageBase;      // Actual image base address (ASLR fakes the image base in PE header while applying relocations in kernel)
    DWORD                   Flags;          // Flags, see CorCompileHeaderFlags above

    DWORD                   PEKind;         // CorPEKind of the original IL image

    ULONG                   COR20Flags;     // Cached value of code:IMAGE_COR20_HEADER.Flags from original IL image
    WORD                    Machine;        // Cached value of _IMAGE_FILE_HEADER.Machine from original IL image
    WORD                    Characteristics;// Cached value of _IMAGE_FILE_HEADER.Characteristics from original IL image

    IMAGE_DATA_DIRECTORY    EEInfoTable;    // points to a code:CORCOMPILE_EE_INFO_TABLE

    // For backward compatibility (see above)
    IMAGE_DATA_DIRECTORY    Dummy1;
    IMAGE_DATA_DIRECTORY    Dummy2;
    IMAGE_DATA_DIRECTORY    Dummy3;
    IMAGE_DATA_DIRECTORY    Dummy4;
};

// CORCOMPILE_VIRTUAL_SECTION_INFO describes virtual section ranges. This data is used by nidump
// and to fire ETW that are used for diagnostics and performance purposes. Some of the questions
// these events help answer are like : how effective is IBC training data.
struct CORCOMPILE_VIRTUAL_SECTION_INFO
{
    ULONG   VirtualAddress;
    ULONG   Size;
    DWORD   SectionType;
};

#define CORCOMPILE_SECTION_TYPES()                            \
    CORCOMPILE_SECTION_TYPE(Module)                           \
    CORCOMPILE_SECTION_TYPE(EETable)                          \
    CORCOMPILE_SECTION_TYPE(WriteData)                        \
    CORCOMPILE_SECTION_TYPE(WriteableData)                    \
    CORCOMPILE_SECTION_TYPE(Data)                             \
    CORCOMPILE_SECTION_TYPE(RVAStatics)                       \
    CORCOMPILE_SECTION_TYPE(EEData)                           \
    CORCOMPILE_SECTION_TYPE(DelayLoadInfoTableEager)          \
    CORCOMPILE_SECTION_TYPE(DelayLoadInfoTable)               \
    CORCOMPILE_SECTION_TYPE(EEReadonlyData)                   \
    CORCOMPILE_SECTION_TYPE(ReadonlyData)                     \
    CORCOMPILE_SECTION_TYPE(Class)                            \
    CORCOMPILE_SECTION_TYPE(CrossDomainInfo)                  \
    CORCOMPILE_SECTION_TYPE(MethodDesc)                       \
    CORCOMPILE_SECTION_TYPE(MethodDescWriteable)              \
    CORCOMPILE_SECTION_TYPE(Exception)                        \
    CORCOMPILE_SECTION_TYPE(Instrument)                       \
    CORCOMPILE_SECTION_TYPE(VirtualImportThunk)               \
    CORCOMPILE_SECTION_TYPE(ExternalMethodThunk)              \
    CORCOMPILE_SECTION_TYPE(HelperTable)                      \
    CORCOMPILE_SECTION_TYPE(MethodPrecodeWriteable)           \
    CORCOMPILE_SECTION_TYPE(MethodPrecodeWrite)               \
    CORCOMPILE_SECTION_TYPE(MethodPrecode)                    \
    CORCOMPILE_SECTION_TYPE(Win32Resources)                   \
    CORCOMPILE_SECTION_TYPE(Header)                           \
    CORCOMPILE_SECTION_TYPE(Metadata)                         \
    CORCOMPILE_SECTION_TYPE(DelayLoadInfo)                    \
    CORCOMPILE_SECTION_TYPE(ImportTable)                      \
    CORCOMPILE_SECTION_TYPE(Code)                             \
    CORCOMPILE_SECTION_TYPE(CodeHeader)                       \
    CORCOMPILE_SECTION_TYPE(CodeManager)                      \
    CORCOMPILE_SECTION_TYPE(UnwindData)                       \
    CORCOMPILE_SECTION_TYPE(RuntimeFunction)                  \
    CORCOMPILE_SECTION_TYPE(Stubs)                            \
    CORCOMPILE_SECTION_TYPE(StubDispatchData)                 \
    CORCOMPILE_SECTION_TYPE(ExternalMethodData)               \
    CORCOMPILE_SECTION_TYPE(DelayLoadInfoDelayList)           \
    CORCOMPILE_SECTION_TYPE(ReadonlyShared)                   \
    CORCOMPILE_SECTION_TYPE(Readonly)                         \
    CORCOMPILE_SECTION_TYPE(IL)                               \
    CORCOMPILE_SECTION_TYPE(GCInfo)                           \
    CORCOMPILE_SECTION_TYPE(ILMetadata)                       \
    CORCOMPILE_SECTION_TYPE(Resources)                        \
    CORCOMPILE_SECTION_TYPE(CompressedMaps)                   \
    CORCOMPILE_SECTION_TYPE(Debug)                            \
    CORCOMPILE_SECTION_TYPE(BaseRelocs)                       \

// Hot: Items are frequently accessed ( Indicated by either IBC data, or
//      statically known )

// Warm : Items are less frequently accessed, or frequently accessed
//        but were not touched during IBC profiling.

// Cold : Least frequently accessed /shouldn't not be accessed
//        when running a scenario that was used during IBC
//        training ( training scenario )

// HotColdSorted : Sections marked with this category means they contain both
//                 Hot items and Cold items. The hot items are placed before
//                 the cold items (Sorted)

#define CORCOMPILE_SECTION_RANGE_TYPES()                     \
    CORCOMPILE_SECTION_RANGE_TYPE(Hot, 0x00010000)           \
    CORCOMPILE_SECTION_RANGE_TYPE(Warm, 0x00020000)          \
    CORCOMPILE_SECTION_RANGE_TYPE(Cold, 0x00040000)          \
    CORCOMPILE_SECTION_RANGE_TYPE(HotColdSorted, 0x00080000) \


// IBCUnProfiled: Items in this VirtualSection are statically determined to be cold.
//                 (IBC Profiling wouldn't have helped put these item in a hot section).
//                 Items that currently doesn't have IBC probs, or are always put in a specific section
//                 regardless of IBC data should fall in this category.

// IBCProfiled: IBC profiling placed items in this section, or
//              items are NOT placed into a hot section they didn't have IBC profiling data
//              ( IBC profiling would have helped put these items in a hot section )

#define CORCOMPILE_SECTION_IBCTYPES()                       \
    CORCOMPILE_SECTION_IBCTYPE(IBCUnProfiled, 0x01000000)  \
    CORCOMPILE_SECTION_IBCTYPE(IBCProfiled, 0x02000000)     \


// Support for VirtualSection Metadata/Categories
// Please update the VirtualSetionType ETW map in ClrEtwAll.man if you changed this enum.
// ZapVirtualSectionType is used to describe metadata about VirtualSections.
// The metadata consists of 3 sub-metadata parts.
// ---------------------------------------------------
// 1 byte       1 byte      2 bytes                 --
// <IBCType> <RangeType> <VirtualSectionType>       --
// ---------------------------------------------------
//
//
// VirtualSections are a CLR concept to aggregate data
// items that share common properties together (Hot/Cold/Warm, Writeable/
// Readonly ...etc.). VirtualSections are tagged with some categories when they
// are created (code:NewVirtualSection)
// The VirtualSection categorize are described more in VirtualSectionType enum.
// The categories describe 2 important aspects for each VirtualSection
//
// ***********************************************
// IBCProfiled v.s NonIBCProfiled Categories.
// **********************************************
//
// IBCProfiled: Distinguish between sections that IBC profiling data has been used
//               to decide the layout of the data items in this section.
// NonIBCProfiled: We don't have IBC data for all our datastructures.
//                  The access pattern/frequency for some data structures
//                  are statically determined. Sections that contain these data items
//                  are marked as NonIBCProfiled.
//
//***************************************************
// Access Frequency categories
// **************************************************
// Hot: Data is frequently accessed
// Warm: Less frequently accessed than Hot
// Cold: Should be rarely accessed.
//
// The combination of these 2 sub-categories gives us the following valid categories
// 1-IBCProfiled | Hot: Hot based on IBC profiling data.
// 2-IBCProfiled | Cold: IBC profiling could have helped make this section hot.
// 3-NonIBCProfiled | Hot: Statically determined hot.
// 4-NonIBCProfiled | Warm: Staticaly determined warm.
// 5-NonIBCProfiled | Cold: Statically determined cold.
//
// We should try to place data items into the correct section based on
// the above categorization, this could mean that we might split
// a virtual section into 2 sections if it contains multiple heterogeneous items.

enum ZapVirtualSectionType
{
    // <IBCType>
    IBCTypeReservedFlag = 0xFF000000,
#define CORCOMPILE_SECTION_IBCTYPE(ibcType, flag) ibcType##Section = flag,
    CORCOMPILE_SECTION_IBCTYPES()
#undef CORCOMPILE_SECTION_IBCTYPE

    // <RangeType>
    RangeTypeReservedFlag = 0x00FF0000,
#define CORCOMPILE_SECTION_RANGE_TYPE(rangeType, flag) rangeType##Range = flag,
    CORCOMPILE_SECTION_RANGE_TYPES()
#undef CORCOMPILE_SECTION_RANGE_TYPE

    // <VirtualSectionType>
    VirtualSectionTypeReservedFlag = 0x0000FFFF,
    VirtualSectionTypeStartSection = 0x0, // reserved so the first section start at 0x1
#define CORCOMPILE_SECTION_TYPE(virtualSectionType) virtualSectionType##Section,
    CORCOMPILE_SECTION_TYPES()
#undef CORCOMPILE_SECTION_TYPE

    CORCOMPILE_SECTION_TYPE_COUNT
};

class VirtualSectionData
{

public :
    static UINT8 IBCType(DWORD sectionType) { return (UINT8) ((sectionType & IBCTypeReservedFlag) >> 24); }
    static UINT8 RangeType(DWORD sectionType) { return (UINT8) ((sectionType & RangeTypeReservedFlag) >> 16); }
    static UINT16 VirtualSectionType(DWORD sectionType) { return (UINT16) ((sectionType & VirtualSectionTypeReservedFlag)); }
    static BOOL IsIBCProfiledColdSection(DWORD sectionType)
    {
        return ((sectionType & ColdRange) == ColdRange) && ((sectionType & IBCProfiledSection) == IBCProfiledSection);
    }
};

struct CORCOMPILE_EE_INFO_TABLE
{
    TADDR                      inlinedCallFrameVptr;
    PTR_LONG                   addrOfCaptureThreadGlobal;
    PTR_DWORD                  addrOfJMCFlag;
    SIZE_T                     gsCookie;
    CORINFO_Object **          emptyString;

    DWORD                      threadTlsIndex;

    DWORD                      rvaStaticTlsIndex;
};

/*********************************************************************************/

// This is the offset to the compressed blob of debug information

typedef ULONG CORCOMPILE_DEBUG_ENTRY;

// A single generic method may be get compiled into multiple copies of code for
// different instantiations, and can have multiple entries for the same RID.

struct CORCOMPILE_DEBUG_LABELLED_ENTRY
{
    DWORD                       nativeCodeRVA;   // the ngen code RVA distinguishes this entry from others with the same RID.
    CORCOMPILE_DEBUG_ENTRY      debugInfoOffset; // offset to the debug information for this native code
};

// Debug information is accessed using a table of RVAs indexed by the RID token for
// the method.

typedef CORCOMPILE_DEBUG_ENTRY CORCOMPILE_DEBUG_RID_ENTRY;

// If this bit is not set, the CORCOMPILE_DEBUG_RID_ENTRY RVA points to a compressed
// debug information blob.
// If this bit is set, the RVA points to CORCOMPILE_DEBUG_LABELLED_ENTRY.
// If this bit is set in CORCOMPILE_DEBUG_LABELLED_ENTRY, there is another entry following it.

const CORCOMPILE_DEBUG_RID_ENTRY CORCOMPILE_DEBUG_MULTIPLE_ENTRIES = 0x80000000;

inline bool IsMultipleLabelledEntries(CORCOMPILE_DEBUG_RID_ENTRY rva)
{
    SUPPORTS_DAC;

    return (rva & CORCOMPILE_DEBUG_MULTIPLE_ENTRIES) != 0;
}

inline unsigned GetDebugRidEntryHash(mdToken token)
{
    SUPPORTS_DAC;

    unsigned hashCode = token;

    // mix it
    hashCode -= hashCode >> 17;
    hashCode -= hashCode >> 11;
    hashCode -= hashCode >> 5;

    return hashCode;
}

typedef DPTR(CORCOMPILE_DEBUG_ENTRY)   PTR_CORCOMPILE_DEBUG_ENTRY;
typedef DPTR(struct CORCOMPILE_DEBUG_LABELLED_ENTRY)   PTR_CORCOMPILE_DEBUG_LABELLED_ENTRY;
typedef DPTR(CORCOMPILE_DEBUG_RID_ENTRY)   PTR_CORCOMPILE_DEBUG_RID_ENTRY;

/*********************************************************************************/

struct CORCOMPILE_CODE_MANAGER_ENTRY
{
    IMAGE_DATA_DIRECTORY    HotCode;
    IMAGE_DATA_DIRECTORY    Code;
    IMAGE_DATA_DIRECTORY    ColdCode;

    IMAGE_DATA_DIRECTORY    ROData;

    //Layout is
    //HOT COMMON
    //HOT IBC
    //HOT GENERICS
    //Hot due to procedure splitting
    ULONG HotIBCMethodOffset;
    ULONG HotGenericsMethodOffset;

    //Layout is
    //COLD IBC
    //Cold due to procedure splitting.
    ULONG ColdUntrainedMethodOffset;
};

#if defined(TARGET_X86) || defined(TARGET_AMD64)

#define _PRECODE_EXTERNAL_METHOD_THUNK      0x41
#define _PRECODE_VIRTUAL_IMPORT_THUNK       0x42

    struct  CORCOMPILE_VIRTUAL_IMPORT_THUNK
    {
        BYTE                callJmp[5];     // Call/Jmp Pc-Rel32
        BYTE                precodeType;    // 0x42 _PRECODE_VIRTUAL_IMPORT_THUNK
        WORD                slotNum;
    };

    struct  CORCOMPILE_EXTERNAL_METHOD_THUNK
    {
        BYTE                callJmp[5];     // Call/Jmp Pc-Rel32
        BYTE                precodeType;    // 0x41 _PRECODE_EXTERNAL_METHOD_THUNK
        WORD                padding;
    };

#elif defined(TARGET_ARM)

    struct  CORCOMPILE_VIRTUAL_IMPORT_THUNK
    {
        // Array of words to do the following:
        //
        // mov r12, pc       ; Save the current address relative to which we will get slot ID and address to patch.
        // ldr pc, [pc, #4]  ; Load the target address. Initially it will point to the helper stub that will patch it
        //                   ; to point to the actual target on the first run.
        WORD                m_rgCode[3];

        // WORD to store the slot ID
        WORD                slotNum;

        // The target address - initially, this will point to VirtualMethodFixupStub.
        // Post patchup by the stub, it will point to the actual method body.
        PCODE               m_pTarget;
    };

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
    struct  CORCOMPILE_VIRTUAL_IMPORT_THUNK
    {
        // Array of words to do the following:
        //
        // adr         x12, #0            ; Save the current address relative to which we will get slot ID and address to patch.
        // ldr         x10, [x12, #16]    ; Load the target address.
        // br          x10                ; Jump to the target
        DWORD                m_rgCode[3];

        // WORD to store the slot ID
        WORD                slotNum;

        // The target address - initially, this will point to VirtualMethodFixupStub.
        // Post patchup by the stub, it will point to the actual method body.
        PCODE                m_pTarget;
    };

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

//lower order bit (HAS_EXCEPTION_INFO_MASK) used to determine if the method has any exception handling
#define HAS_EXCEPTION_INFO_MASK 1

struct CORCOMPILE_COLD_METHOD_ENTRY
{
#ifdef FEATURE_EH_FUNCLETS
    DWORD       mainFunctionEntryRVA;
#endif
    // TODO: hotCodeSize should be encoded in GC info
    ULONG       hotCodeSize;
};

// MVID used by the metadata of all ngen images
// {70E9452F-5F0A-4f0e-8E02-203992F4221C}
EXTERN_GUID(NGEN_IMAGE_MVID, 0x70e9452f, 0x5f0a, 0x4f0e, 0x8e, 0x2, 0x20, 0x39, 0x92, 0xf4, 0x22, 0x1c);

typedef GUID CORCOMPILE_NGEN_SIGNATURE;

// To indicate that the dependency is not hardbound
// {DB15CD8C-1378-4963-9DF3-14D97E95D1A1}
EXTERN_GUID(INVALID_NGEN_SIGNATURE, 0xdb15cd8c, 0x1378, 0x4963, 0x9d, 0xf3, 0x14, 0xd9, 0x7e, 0x95, 0xd1, 0xa1);

struct CORCOMPILE_ASSEMBLY_SIGNATURE
{
    // Metadata MVID.
    GUID                    mvid;

    // timestamp and IL image size for the source IL assembly.
    // This is used for mini-dump to find matching metadata.
    DWORD                   timeStamp;
    DWORD                   ilImageSize;
};

typedef enum
{
    CORECLR_INFO,
    CROSSGEN_COMPILER_INFO,
    NUM_RUNTIME_DLLS
} CorCompileRuntimeDlls;

extern LPCWSTR CorCompileGetRuntimeDllName(CorCompileRuntimeDlls id);

struct CORCOMPILE_RUNTIME_DLL_INFO
{
    // This structure can only contain information not updated by authenticode signing. It is required
    // for crossgen to work in buildlab. It particular, it cannot contain PE checksum because of it is
    // update by authenticode signing.
    DWORD                   timeStamp;
    DWORD                   virtualSize;
};



struct CORCOMPILE_VERSION_INFO
{
    // OS
    WORD                    wOSPlatformID;
    WORD                    wOSMajorVersion;

    // For backward compatibility reasons, the following four fields must start at offset 4,
    // be consequtive, and be 2 bytes each.  See code:PEDecoder::GetMetaDataHelper.
    // EE Version
    WORD                    wVersionMajor;
    WORD                    wVersionMinor;
    WORD                    wVersionBuildNumber;
    WORD                    wVersionPrivateBuildNumber;

    // Codegen flags
    WORD                    wCodegenFlags;
    WORD                    wConfigFlags;
    WORD                    wBuild;

    // Processor
    WORD                    wMachine;
    CORINFO_CPU             cpuInfo;

    // Signature of source assembly
    CORCOMPILE_ASSEMBLY_SIGNATURE   sourceAssembly;

    // Signature which identifies this ngen image
    CORCOMPILE_NGEN_SIGNATURE       signature;

    // Timestamp info for runtime dlls
    CORCOMPILE_RUNTIME_DLL_INFO     runtimeDllInfo[NUM_RUNTIME_DLLS];
};




struct CORCOMPILE_DEPENDENCY
{
    // Pre-bind Ref
    mdAssemblyRef                   dwAssemblyRef;

    // Post-bind Def
    mdAssemblyRef                   dwAssemblyDef;
    CORCOMPILE_ASSEMBLY_SIGNATURE   signAssemblyDef;

    CORCOMPILE_NGEN_SIGNATURE       signNativeImage;    // INVALID_NGEN_SIGNATURE if this a soft-bound dependency


};

/*********************************************************************************/
// Flags used to encode HelperTable
#if defined(TARGET_ARM64)
#define HELPER_TABLE_ENTRY_LEN      16
#else
#define HELPER_TABLE_ENTRY_LEN      8
#endif //defined(TARGET_ARM64)

#define HELPER_TABLE_ALIGN          8
#define CORCOMPILE_HELPER_PTR       0x80000000 // The entry is pointer to the helper (jump thunk otherwise)

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

// see code:CorProfileData.GetHotTokens for how we determine what is in hot meta-data.
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


    // see code:MetaData::HotMetaDataHeader for details on reading hot meta-data
    //
    // for detail on where we use the API to store the hot meta data
    //     * code:CMiniMdRW.SaveFullTablesToStream#WritingHotMetaData
    //     * code:CMiniMdRW.SaveHotPoolsToStream
    //     * code:CMiniMdRW.SaveHotPoolToStream#CallToGetHotTokens
    //
    ULONG GetHotTokens(int table, DWORD mask, DWORD hotValue, mdToken *tokenBuffer, ULONG maxCount)
    {
        ULONG count = 0;
        SectionFormat format = (SectionFormat)(FirstTokenFlagSection + table);

        CORBBTPROF_TOKEN_INFO *profilingData = profilingTokenFlagsData[format].data;
        DWORD cProfilingData = profilingTokenFlagsData[format].count;

        if (profilingData != NULL)
        {
            for (DWORD i = 0; i < cProfilingData; i++)
            {
                if ((profilingData[i].flags & mask) == hotValue)
                {
                    if (tokenBuffer != NULL && count < maxCount)
                        tokenBuffer[count] = profilingData[i].token;
                    count++;
                }
            }
        }
        return count;
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
// IL region is used to group frequently used IL method bodies together

enum CorCompileILRegion
{
    CORCOMPILE_ILREGION_INLINEABLE,     // Public inlineable methods
    CORCOMPILE_ILREGION_WARM,           // Other inlineable methods and methods that failed to NGen
    CORCOMPILE_ILREGION_GENERICS,       // Generic methods (may be needed to compile non-NGened instantiations)
    CORCOMPILE_ILREGION_COLD,           // Everything else (should be touched in rare scenarios like reflection or profiling only)
    CORCOMPILE_ILREGION_COUNT,
};

//
// The DataImage provides several "sections", which can be used
// to sort data into different sets for locality control.  The Arrange
// phase is responsible for placing items into sections.
//

#define CORCOMPILE_SECTIONS() \
    CORCOMPILE_SECTION(MODULE) \
    CORCOMPILE_SECTION(WRITE) \
    CORCOMPILE_SECTION(METHOD_PRECODE_WRITE) \
    CORCOMPILE_SECTION(HOT_WRITEABLE) \
    CORCOMPILE_SECTION(WRITEABLE) \
    CORCOMPILE_SECTION(HOT) \
    CORCOMPILE_SECTION(METHOD_PRECODE_HOT) \
    CORCOMPILE_SECTION(RVA_STATICS_HOT) \
    CORCOMPILE_SECTION(RVA_STATICS_COLD) \
    CORCOMPILE_SECTION(WARM) \
    CORCOMPILE_SECTION(READONLY_SHARED_HOT) \
    CORCOMPILE_SECTION(READONLY_HOT) \
    CORCOMPILE_SECTION(READONLY_WARM) \
    CORCOMPILE_SECTION(READONLY_COLD) \
    CORCOMPILE_SECTION(READONLY_VCHUNKS) \
    CORCOMPILE_SECTION(READONLY_DICTIONARY) \
    CORCOMPILE_SECTION(CLASS_COLD) \
    CORCOMPILE_SECTION(CROSS_DOMAIN_INFO) \
    CORCOMPILE_SECTION(METHOD_PRECODE_COLD) \
    CORCOMPILE_SECTION(METHOD_PRECODE_COLD_WRITEABLE) \
    CORCOMPILE_SECTION(METHOD_DESC_COLD) \
    CORCOMPILE_SECTION(METHOD_DESC_COLD_WRITEABLE) \
    CORCOMPILE_SECTION(MODULE_COLD) \
    CORCOMPILE_SECTION(DEBUG_COLD) \
    CORCOMPILE_SECTION(COMPRESSED_MAPS) \

enum CorCompileSection
{
#define CORCOMPILE_SECTION(section) CORCOMPILE_SECTION_##section,
    CORCOMPILE_SECTIONS()
#undef CORCOMPILE_SECTION

    CORCOMPILE_SECTION_COUNT
};

enum VerboseLevel
{
    CORCOMPILE_NO_LOG,
    CORCOMPILE_STATS,
    CORCOMPILE_VERBOSE
};

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

// Stress mode to leave some methods/types uncompiled in the ngen image.
// Those methods will be JIT-compiled at runtime as needed.

extern "C" unsigned __stdcall PartialNGenStressPercentage();

// create a PDB dumping all functions in hAssembly into pdbPath
extern "C" HRESULT __stdcall CreatePdb(CORINFO_ASSEMBLY_HANDLE hAssembly, BSTR pNativeImagePath, BSTR pPdbPath, BOOL pdbLines, BSTR pManagedPdbSearchPath, LPCWSTR pDiasymreaderPath);

extern bool g_fNGenMissingDependenciesOk;
#endif /* COR_COMPILE_H_ */
