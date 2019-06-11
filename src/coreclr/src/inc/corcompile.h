// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

#if !defined(_TARGET_X86_) || defined(FEATURE_PAL)
#ifndef WIN64EXCEPTIONS
#define WIN64EXCEPTIONS
#endif
#endif  // !_TARGET_X86_ || FEATURE_PAL

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

#ifdef _TARGET_X86_

typedef DPTR(RUNTIME_FUNCTION) PTR_RUNTIME_FUNCTION;


// Chained unwind info. Used for cold methods.
#define RUNTIME_FUNCTION_INDIRECT 0x80000000

#endif // _TARGET_X86_

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

#ifdef _TARGET_ARM_
// Tagging of code pointers on ARM uses inverse logic because of the thumb bit.
#define CORCOMPILE_IS_PCODE_TAGGED(token)       ((((SIZE_T)(token)) & 0x00000001) == 0x00000000)
#define CORCOMPILE_TAG_PCODE(token)             ((SIZE_T)(((token)<<1)|0x80000000))
#else
#define CORCOMPILE_IS_PCODE_TAGGED(token)       CORCOMPILE_IS_POINTER_TAGGED(token)
#define CORCOMPILE_TAG_PCODE(token)             CORCOMPILE_TAG_TOKEN(token)
#endif

inline BOOL CORCOMPILE_IS_FIXUP_TAGGED(SIZE_T fixup, PTR_CORCOMPILE_IMPORT_SECTION pSection)
{
#ifdef _TARGET_ARM_
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

#if defined(_TARGET_X86_) || defined(_TARGET_AMD64_)

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

#elif defined(_TARGET_ARM_)

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
	
#elif defined(_TARGET_ARM64_)
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

    ENCODE_INDIRECT_PINVOKE_TARGET,                 /* For calling a pinvoke method ptr  */

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
    ENCODE_METHOD_NATIVE_ENTRY,                     /* NativeCallable method token */
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
#ifdef WIN64EXCEPTIONS
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

// Will always return a valid HMODULE for CLR_INFO, but will return NULL for NGEN_COMPILER_INFO
// if the DLL has not yet been loaded (it does not try to cause a load).
extern HMODULE CorCompileGetRuntimeDll(CorCompileRuntimeDlls id);

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
#if defined(_TARGET_ARM64_)
#define HELPER_TABLE_ENTRY_LEN      16
#else
#define HELPER_TABLE_ENTRY_LEN      8
#endif //defined(_TARGET_ARM64_)

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
        if (this == NULL)
            return NULL;
        return this->profilingTokenFlagsData[section].data;
    }

    DWORD GetTokenFlagsCount(SectionFormat section)
    {
        if (this == NULL)
            return 0;
        return this->profilingTokenFlagsData[section].count;
    }

    CORBBTPROF_BLOB_ENTRY *  GetBlobStream()
    {
        if (this == NULL)
            return NULL;
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

/*********************************************************************************
 * ICorCompilePreloader is used to query preloaded EE data structures
 *********************************************************************************/

class ICorCompilePreloader
{
 public:
    typedef void (__stdcall *CORCOMPILE_CompileStubCallback)(LPVOID pContext, CORINFO_METHOD_HANDLE hStub, CORJIT_FLAGS jitFlags);

    //
    // Map methods are available after Serialize() is called
    // (which will cause it to allocate its data.) Note that returned
    // results are RVAs into the image.
    //
    // If compiling after serializing the preloaded image, these methods can
    // be used to avoid making entries in the various info tables.
    // Else, use ICorCompileInfo::CanEmbedXXX()
    //

    virtual DWORD MapMethodEntryPoint(
            CORINFO_METHOD_HANDLE handle
            ) = 0;

    virtual DWORD MapClassHandle(
            CORINFO_CLASS_HANDLE handle
            ) = 0;

    virtual DWORD MapMethodHandle(
            CORINFO_METHOD_HANDLE handle
            ) = 0;

    virtual DWORD MapFieldHandle(
            CORINFO_FIELD_HANDLE handle
            ) = 0;

    virtual DWORD MapAddressOfPInvokeFixup(
            CORINFO_METHOD_HANDLE handle
            ) = 0;

    virtual DWORD MapGenericHandle(
            CORINFO_GENERIC_HANDLE handle
            ) = 0;

    virtual DWORD MapModuleIDHandle(
            CORINFO_MODULE_HANDLE handle
            )  = 0;

    // Load a method for the specified method def
    // If the class or method is generic, instantiate all parameters with <object>
    virtual CORINFO_METHOD_HANDLE LookupMethodDef(mdMethodDef token) = 0;

    // For the given ftnHnd fill in the methInfo structure and return true if successful.
    virtual bool GetMethodInfo(mdMethodDef token, CORINFO_METHOD_HANDLE ftnHnd, CORINFO_METHOD_INFO * methInfo) = 0;

    // Returns region that the IL should be emitted in
    virtual CorCompileILRegion GetILRegion(mdMethodDef token) = 0;

    // Find the (parameterized) method for the given blob from the profile data
    virtual CORINFO_METHOD_HANDLE FindMethodForProfileEntry(CORBBTPROF_BLOB_PARAM_SIG_ENTRY * profileBlobEntry) = 0;

    virtual void ReportInlining(CORINFO_METHOD_HANDLE inliner, CORINFO_METHOD_HANDLE inlinee) = 0;

    //
    // Call Link when you want all the fixups
    // to be applied.  You may call this e.g. after
    // compiling all the code for the module.
    // Return some stats about the types in the ngen image
    //
    virtual void Link() = 0;

    virtual void FixupRVAs() = 0;

    virtual void SetRVAsForFields(IMetaDataEmit * pEmit) = 0;

    virtual void GetRVAFieldData(mdFieldDef fd, PVOID * ppData, DWORD * pcbSize, DWORD * pcbAlignment) = 0;

    // The preloader also maintains a set of uncompiled generic
    // methods or methods in generic classes. A single method can be
    // registered or all the methods in a class can be registered.
    // The method is added to the set only if it should be compiled
    // into this ngen image
    //
    // The zapper registers methods and classes that are resolved by
    // findClass and findMethod during compilation
    virtual void AddMethodToTransitiveClosureOfInstantiations(CORINFO_METHOD_HANDLE handle) = 0;
    virtual void AddTypeToTransitiveClosureOfInstantiations(CORINFO_CLASS_HANDLE handle) = 0;

    // Report reference to the given method from compiled code
    virtual void MethodReferencedByCompiledCode(CORINFO_METHOD_HANDLE handle) = 0;

    virtual BOOL IsUncompiledMethod(CORINFO_METHOD_HANDLE handle) = 0;

    // Return a method handle that was previously registered and
    // hasn't been compiled already, and remove it from the set
    // of uncompiled methods.
    // Return NULL if the set is empty
    virtual CORINFO_METHOD_HANDLE NextUncompiledMethod() = 0;

    // Prepare a method and its statically determinable call graph if
    // a hint attribute has been applied. This is called to save
    // additional preparation information into the ngen image that
    // wouldn't normally be there (since we can't automatically
    // determine it's needed).
    virtual void PrePrepareMethodIfNecessary(CORINFO_METHOD_HANDLE hMethod) = 0;

    // If a method requires stubs, this will call back passing method
    // handles for those stubs.
    virtual void GenerateMethodStubs(
            CORINFO_METHOD_HANDLE hMethod,
            bool                  fNgenProfileImage,
            CORCOMPILE_CompileStubCallback pfnCallback,
            LPVOID                pCallbackContext) = 0;

    // Determines whether or not a method is a dynamic method.  This is used
    // to prevent operations that may require metadata knowledge at times other
    // than compile time.
    virtual bool IsDynamicMethod(CORINFO_METHOD_HANDLE hMethod) = 0;

    // Set method profiling flags for layout of EE datastructures
    virtual void SetMethodProfilingFlags(CORINFO_METHOD_HANDLE hMethod, DWORD flags) = 0;

    // Returns false if precompiled code must ensure that
    // the EE's DoPrestub function gets run before the
    // code for the method is used, i.e. if it returns false
    // then an indirect call must be made.
    //
    // Returning true does not guaratee that a direct call can be made:
    // there can be other reasons why the entry point cannot be embedded.
    //
    virtual bool CanSkipMethodPreparation (
            CORINFO_METHOD_HANDLE   callerHnd,      /* IN  */
            CORINFO_METHOD_HANDLE   calleeHnd,      /* IN  */
            CorInfoIndirectCallReason *pReason = NULL,
            CORINFO_ACCESS_FLAGS    accessFlags = CORINFO_ACCESS_ANY) = 0;

    virtual BOOL CanEmbedModuleHandle(
            CORINFO_MODULE_HANDLE    moduleHandle) = 0;

    // These check if we can hardbind to a handle.  They guarantee either that
    // the structure referred to by the handle is in a referenced zapped image
    // or will be saved into the module currently being zapped.  That is the
    // corresponding GetLoaderModuleForEmeddableXYZ call will return
    // either the module currently being zapped or a referenced zapped module.
    virtual BOOL CanEmbedClassID(CORINFO_CLASS_HANDLE    typeHandle) = 0;
    virtual BOOL CanEmbedModuleID(CORINFO_MODULE_HANDLE    moduleHandle) = 0;
    virtual BOOL CanEmbedClassHandle(CORINFO_CLASS_HANDLE    typeHandle) = 0;
    virtual BOOL CanEmbedMethodHandle(CORINFO_METHOD_HANDLE    methodHandle, CORINFO_METHOD_HANDLE contextHandle = NULL) = 0;
    virtual BOOL CanEmbedFieldHandle(CORINFO_FIELD_HANDLE    fieldHandle) = 0;

    // Return true if we can both embed a direct hardbind to the handle _and_
    // no "restore" action is needed on the handle.  Equivalent to "CanEmbed + Prerestored".
    //
    // Typically a handle needs runtime restore it has embedded cross-module references
    // or other data that cannot be persisted directly.
    virtual BOOL CanPrerestoreEmbedClassHandle(
            CORINFO_CLASS_HANDLE classHnd) = 0;

    // Return true if a method needs runtime restore
    // This is only the case if it is instantiated and any of its type arguments need restoring.
    virtual BOOL CanPrerestoreEmbedMethodHandle(
            CORINFO_METHOD_HANDLE methodHnd) = 0;

    // Can a method entry point be embedded?
    virtual BOOL CanEmbedFunctionEntryPoint(
            CORINFO_METHOD_HANDLE   methodHandle,
            CORINFO_METHOD_HANDLE   contextHandle = NULL,
            CORINFO_ACCESS_FLAGS    accessFlags = CORINFO_ACCESS_ANY
            ) = 0;

    // Prestub is not able to handle method restore in all cases for generics.
    // If it is the case the method has to be restored explicitly upfront.
    // See the comment inside the implemenation method for more details.
    virtual BOOL DoesMethodNeedRestoringBeforePrestubIsRun(
            CORINFO_METHOD_HANDLE   methodHandle
            ) = 0;

    // Returns true if the given activation fixup is not necessary
    virtual BOOL CanSkipDependencyActivation(
            CORINFO_METHOD_HANDLE   context,
            CORINFO_MODULE_HANDLE   moduleFrom,
            CORINFO_MODULE_HANDLE   moduleTo) = 0;

    virtual CORINFO_MODULE_HANDLE GetPreferredZapModuleForClassHandle(
            CORINFO_CLASS_HANDLE classHnd
            ) = 0;

    virtual void NoteDeduplicatedCode(
            CORINFO_METHOD_HANDLE method, 
            CORINFO_METHOD_HANDLE duplicateMethod) = 0;

#ifdef FEATURE_READYTORUN_COMPILER
    // Returns a compressed encoding of the inline tracking map 
    // for this compilation
    virtual void GetSerializedInlineTrackingMap(
            IN OUT SBuffer    * pSerializedInlineTrackingMap
            ) = 0;
#endif

    //
    // Release frees the preloader
    //

    virtual ULONG Release() = 0;
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

class ZapImage;

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

class ICorCompileDataStore
{
 public:
    // Returns ZapImage
    virtual ZapImage * GetZapImage() = 0;

    // Report an error during preloading:
    // 'token' is the metadata token that triggered the error
    // hr is the HRESULT from the thrown Exception, or S_OK if we don't have an thrown exception
    // resID is the resourceID with additional information from the thrown Exception, or 0
    //
    virtual void Error(mdToken token, HRESULT hr, UINT _resID, LPCWSTR description) = 0;
};


class ICorCompilationDomain
{
 public:

    // Sets the application context for fusion
    // to use when binding, using a shell exe file path
    virtual HRESULT SetContextInfo(
            LPCWSTR                 path,
            BOOL                    isExe
            ) = 0;

    // Retrieves the dependencies of the code which
    // has been compiled
    virtual HRESULT GetDependencies(
            CORCOMPILE_DEPENDENCY   **ppDependencies,
            DWORD                   *cDependencies
            ) = 0;


#ifdef CROSSGEN_COMPILE
    virtual HRESULT SetPlatformWinmdPaths(
            LPCWSTR                 pwzPlatformWinmdPaths
            ) = 0;
#endif
};

/*********************************************************************************
 * ICorCompileInfo is the interface for a compiler
 *********************************************************************************/
// Define function pointer ENCODEMODULE_CALLBACK
typedef DWORD (*ENCODEMODULE_CALLBACK)(LPVOID pModuleContext, CORINFO_MODULE_HANDLE moduleHandle);

// Define function pointer DEFINETOKEN_CALLBACK
typedef void (*DEFINETOKEN_CALLBACK)(LPVOID pModuleContext, CORINFO_MODULE_HANDLE moduleHandle, DWORD index, mdTypeRef* token);

class ICorCompileInfo
{
  public:


    //
    // Currently no other instance of the EE may be running inside
    // a process that is used as an NGEN compilation process.
    //
    // So, the host must call StartupAsCompilationProcess before compiling
    // any code, and Shutdown after finishing.
    //
    // The arguments control which native image of mscorlib to use.
    // This matters for hardbinding.
    //

    virtual HRESULT Startup(
            BOOL fForceDebug,
            BOOL fForceProfiling,
            BOOL fForceInstrument) = 0;

    // Creates a new compilation domain
    // The BOOL arguments control what kind of a native image is
    // to be generated. Other factors affect what kind of a native image
    // will actually be generated. GetAssemblyVersionInfo() ultimately reflects
    // the kind of native image that will be generated
    //
    // pEmitter - sets this as the emitter to use when generating tokens for
    // the dependency list.  If this is NULL, dependencies won't be computed.

    virtual HRESULT CreateDomain(
            ICorCompilationDomain **ppDomain, // [OUT]
            IMetaDataAssemblyEmit   *pEmitter,
            BOOL fForceDebug,
            BOOL fForceProfiling,
            BOOL fForceInstrument
            ) = 0;

    // Destroys a compilation domain
    virtual HRESULT DestroyDomain(
            ICorCompilationDomain *pDomain
            ) = 0;

    // Loads an assembly manifest module into the EE
    // and returns a handle to it.
    virtual HRESULT LoadAssemblyByPath(
            LPCWSTR                  wzPath,
            BOOL                     fExplicitBindToNativeImage,
            CORINFO_ASSEMBLY_HANDLE *pHandle
            ) = 0;


#ifdef FEATURE_COMINTEROP
    // Loads a WinRT typeref into the EE and returns
    // a handle to it.  We have to load all typerefs
    // during dependency computation since assemblyrefs 
    // are meaningless to WinRT.
    virtual HRESULT LoadTypeRefWinRT(
            IMDInternalImport       *pAssemblyImport,
            mdTypeRef               ref,
            CORINFO_ASSEMBLY_HANDLE *pHandle
            ) = 0;
#endif

    virtual BOOL IsInCurrentVersionBubble(CORINFO_MODULE_HANDLE hModule) = 0;

    // Loads a module from an assembly into the EE
    // and returns a handle to it.
    virtual HRESULT LoadAssemblyModule(
            CORINFO_ASSEMBLY_HANDLE assembly,
            mdFile                  file,
            CORINFO_MODULE_HANDLE   *pHandle
            ) = 0;


    // Checks to see if an up to date zap exists for the
    // assembly
    virtual BOOL CheckAssemblyZap(
        CORINFO_ASSEMBLY_HANDLE assembly,
      __out_ecount_opt(*cAssemblyManifestModulePath)
        LPWSTR                  assemblyManifestModulePath,
        LPDWORD                 cAssemblyManifestModulePath
        ) = 0;

    // Sets up the compilation target in the EE
    virtual HRESULT SetCompilationTarget(
            CORINFO_ASSEMBLY_HANDLE     assembly,
            CORINFO_MODULE_HANDLE       module
            ) = 0;


    // Returns the dependency load setting for an assembly ref
    virtual HRESULT GetLoadHint(
            CORINFO_ASSEMBLY_HANDLE hAssembly,
            CORINFO_ASSEMBLY_HANDLE hAssemblyDependency,
            LoadHintEnum *loadHint,
            LoadHintEnum *defaultLoadHint = NULL
            ) = 0;

    // Returns information on how the assembly has been loaded
    virtual HRESULT GetAssemblyVersionInfo(
            CORINFO_ASSEMBLY_HANDLE hAssembly,
            CORCOMPILE_VERSION_INFO *pInfo
            ) = 0;

    // Returns the manifest metadata for an assembly
    // Use the internal IMDInternalImport for performance.
    // Creation of the public IMetaDataImport * triggers
    // conversion to R/W metadata that slows down all subsequent accesses.
    virtual IMDInternalImport * GetAssemblyMetaDataImport(
            CORINFO_ASSEMBLY_HANDLE assembly
            ) = 0;

    // Returns an interface to query the metadata for a loaded module
    // Use the internal IMDInternalImport for performance.
    // Creation of the public IMetaDataAssemblyImport * triggers
    // conversion to R/W metadata that slows down all subsequent accesses.
    virtual IMDInternalImport * GetModuleMetaDataImport(
            CORINFO_MODULE_HANDLE   module
            ) = 0;

    // Returns the module of the assembly which contains the manifest,
    // or NULL if the manifest is standalone.
    virtual CORINFO_MODULE_HANDLE GetAssemblyModule(
            CORINFO_ASSEMBLY_HANDLE assembly
            ) = 0;

    // Returns the assembly of a loaded module
    virtual CORINFO_ASSEMBLY_HANDLE GetModuleAssembly(
            CORINFO_MODULE_HANDLE   module
            ) = 0;

    // Returns the current PEDecoder of a loaded module.
    virtual PEDecoder * GetModuleDecoder(
            CORINFO_MODULE_HANDLE   module
            ) = 0;

    // Gets the full file name, including path, of a loaded module
    virtual void GetModuleFileName(
        CORINFO_MODULE_HANDLE module,
        SString               &result
        ) = 0;

    // Get a class def token
    virtual HRESULT GetTypeDef(
            CORINFO_CLASS_HANDLE    classHandle,
            mdTypeDef              *token
            ) = 0;

    // Get a method def token
    virtual HRESULT GetMethodDef(
            CORINFO_METHOD_HANDLE   methodHandle,
            mdMethodDef            *token
            ) = 0;

    // Get a field def token
    virtual HRESULT GetFieldDef(
            CORINFO_FIELD_HANDLE    fieldHandle,
            mdFieldDef             *token
            ) = 0;

    // Get the loader module for mscorlib
    virtual CORINFO_MODULE_HANDLE GetLoaderModuleForMscorlib() = 0;

    // Get the loader module for a type (where the type is regarded as
    // living for the purposes of loading, unloading, and ngen).
    //
    // classHandle must have passed CanEmbedClassHandle, since the zapper
    // should only care about the module where a type
    // prefers to be saved if it knows that that module is either
    // an zapped module or is the module currently being compiled.
    // See vm\ceeload.h for more information
    virtual CORINFO_MODULE_HANDLE GetLoaderModuleForEmbeddableType(
            CORINFO_CLASS_HANDLE   classHandle
            ) = 0;

    // Get the loader module for a method (where the method is regarded as
    // living for the purposes of loading, unloading, and ngen)
    //
    // methodHandle must have passed CanEmbedMethodHandle, since the zapper
    // should only care about the module where a type
    // prefers to be saved if it knows that that module is either
    // an zapped module or is the module currently being compiled.
    // See vm\ceeload.h for more information
    virtual CORINFO_MODULE_HANDLE GetLoaderModuleForEmbeddableMethod(
            CORINFO_METHOD_HANDLE   methodHandle
            ) = 0;

    // Get the loader module for a method (where the method is regarded as
    // living for the purposes of loading, unloading, and ngen)
    // See vm\ceeload.h for more information
    virtual CORINFO_MODULE_HANDLE GetLoaderModuleForEmbeddableField(
            CORINFO_FIELD_HANDLE   fieldHandle
            ) = 0;

    // Set the list of assemblies we can hard bind to
    virtual void SetAssemblyHardBindList(
      __in_ecount(cHardBindList)
        LPWSTR * pHardBindList,
        DWORD    cHardBindList
        ) = 0;

    // Encode a module for the imports table
    virtual void EncodeModuleAsIndex(
            CORINFO_MODULE_HANDLE fromHandle,
            CORINFO_MODULE_HANDLE handle,
            DWORD *pIndex,
            IMetaDataAssemblyEmit *pAssemblyEmit) = 0;


    // Encode a class into the given SigBuilder.
    virtual void EncodeClass(
            CORINFO_MODULE_HANDLE referencingModule,
            CORINFO_CLASS_HANDLE classHandle,
            SigBuilder * pSigBuilder,
            LPVOID encodeContext,
            ENCODEMODULE_CALLBACK pfnEncodeModule) = 0;

    // Encode a method into the given SigBuilder.
    virtual void EncodeMethod(
            CORINFO_MODULE_HANDLE referencingModule,
            CORINFO_METHOD_HANDLE handle,
            SigBuilder * pSigBuilder,
            LPVOID encodeContext,
            ENCODEMODULE_CALLBACK pfnEncodeModule,
            CORINFO_RESOLVED_TOKEN * pResolvedToken = NULL,
            CORINFO_RESOLVED_TOKEN * pConstrainedResolvedToken = NULL,
            BOOL fEncodeUsingResolvedTokenSpecStreams = FALSE) = 0;

    // Returns non-null methoddef or memberref token if it is sufficient to encode the method (no generic instantiations, etc.)
    virtual mdToken TryEncodeMethodAsToken(
            CORINFO_METHOD_HANDLE handle, 
            CORINFO_RESOLVED_TOKEN * pResolvedToken,
            CORINFO_MODULE_HANDLE * referencingModule) = 0;

    // Returns method slot (for encoding virtual stub dispatch)
    virtual DWORD TryEncodeMethodSlot(
            CORINFO_METHOD_HANDLE handle) = 0;

    // Encode a field into the given SigBuilder.
    virtual void EncodeField(
            CORINFO_MODULE_HANDLE referencingModule,
            CORINFO_FIELD_HANDLE handle,
            SigBuilder * pSigBuilder,
            LPVOID encodeContext,
            ENCODEMODULE_CALLBACK pfnEncodeModule,
            CORINFO_RESOLVED_TOKEN * pResolvedToken = NULL,
            BOOL fEncodeUsingResolvedTokenSpecStreams = FALSE) = 0;


    // Encode generic dictionary signature
    virtual void EncodeGenericSignature(
            LPVOID signature,
            BOOL fMethod,
            SigBuilder * pSigBuilder,
            LPVOID encodeContext,
            ENCODEMODULE_CALLBACK pfnEncodeModule) = 0;


    virtual BOOL IsEmptyString(
            mdString token,
            CORINFO_MODULE_HANDLE module) = 0;


    // Preload a modules' EE data structures
    // directly into an executable image

    virtual ICorCompilePreloader * PreloadModule(
            CORINFO_MODULE_HANDLE   moduleHandle,
            ICorCompileDataStore    *pData,
            CorProfileData          *profileData
            ) = 0;

    // Gets the codebase URL for the assembly
    virtual void GetAssemblyCodeBase(
            CORINFO_ASSEMBLY_HANDLE hAssembly,
            SString                 &result) = 0;

    // Returns the GC-information for a method. This is the simple representation
    // and can be used when a code that can trigger a GC does not have access
    // to the CORINFO_METHOD_HANDLE (which is normally used to access the GC information)
    //
    // Returns S_FALSE if there is no simple representation for the method's GC info
    //
    virtual void GetCallRefMap(
            CORINFO_METHOD_HANDLE hMethod,
            GCRefMapBuilder * pBuilder,
            bool isDispatchCell) = 0;

    // Returns a compressed block of debug information
    //
    // Uncompressed debug maps are passed in.
    // Writes to outgoing SBuffer.
    // Throws on failure.
    virtual void CompressDebugInfo(
            IN ICorDebugInfo::OffsetMapping  * pOffsetMapping,
            IN ULONG            iOffsetMapping,
            IN ICorDebugInfo::NativeVarInfo  * pNativeVarInfo,
            IN ULONG            iNativeVarInfo,
            IN OUT SBuffer    * pDebugInfoBuffer
            ) = 0;



    // Allows to set verbose level for log messages, enabled in retail build too for stats
    virtual HRESULT SetVerboseLevel(
            IN  VerboseLevel            level) = 0;

    // Get the compilation flags that are shared between JIT and NGen
    virtual HRESULT GetBaseJitFlags(
            IN  CORINFO_METHOD_HANDLE   hMethod,
            OUT CORJIT_FLAGS           *pFlags) = 0;

    virtual ICorJitHost* GetJitHost() = 0;

    // needed for stubs to obtain the number of bytes to copy into the native image
    // return the beginning of the stub and the size to copy (in bytes)
    virtual void* GetStubSize(void *pStubAddress, DWORD *pSizeToCopy) = 0;

    // Takes a stub and blits it into the buffer, resetting the reference count
    // to 1 on the clone. The buffer has to be large enough to hold the stub object and the code
    virtual HRESULT GetStubClone(void *pStub, BYTE *pBuffer, DWORD dwBufferSize) = 0;

    // true if the method has [NativeCallableAttribute]
    virtual BOOL IsNativeCallableMethod(CORINFO_METHOD_HANDLE handle) = 0;

    virtual BOOL GetIsGeneratingNgenPDB() = 0;
    virtual void SetIsGeneratingNgenPDB(BOOL fGeneratingNgenPDB) = 0;

#ifdef FEATURE_READYTORUN_COMPILER
    virtual CORCOMPILE_FIXUP_BLOB_KIND GetFieldBaseOffset(
            CORINFO_CLASS_HANDLE classHnd, 
            DWORD * pBaseOffset
            ) = 0;

    virtual BOOL NeedsTypeLayoutCheck(CORINFO_CLASS_HANDLE classHnd) = 0;
    virtual void EncodeTypeLayout(CORINFO_CLASS_HANDLE classHandle, SigBuilder * pSigBuilder) = 0;

    virtual BOOL AreAllClassesFullyLoaded(CORINFO_MODULE_HANDLE moduleHandle) = 0;

    virtual int GetVersionResilientTypeHashCode(CORINFO_MODULE_HANDLE moduleHandle, mdToken token) = 0;

    virtual int GetVersionResilientMethodHashCode(CORINFO_METHOD_HANDLE methodHandle) = 0;

    virtual BOOL EnumMethodsForStub(CORINFO_METHOD_HANDLE hMethod, void** enumerator) = 0;
    virtual BOOL EnumNextMethodForStub(void * enumerator, CORINFO_METHOD_HANDLE *hMethod) = 0;
    virtual void EnumCloseForStubEnumerator(void *enumerator) = 0;

#endif

    virtual BOOL HasCustomAttribute(CORINFO_METHOD_HANDLE method, LPCSTR customAttributeName) = 0;
};

/*****************************************************************************/
// This function determines the compile flags to use for a generic intatiation
// since only the open instantiation can be verified.
// See the comment associated with CORJIT_FLAG_SKIP_VERIFICATION for details.
//
// On return:
// if *raiseVerificationException=TRUE, the caller should raise a VerificationException.
// if *unverifiableGenericCode=TRUE, the method is a generic instantiation with
// unverifiable code

CORJIT_FLAGS GetCompileFlagsIfGenericInstantiation(
        CORINFO_METHOD_HANDLE method,
        CORJIT_FLAGS compileFlags,
        ICorJitInfo * pCorJitInfo,
        BOOL * raiseVerificationException,
        BOOL * unverifiableGenericCode);

// Returns the global instance of JIT->EE interface for NGen

extern "C" ICorDynamicInfo * __stdcall GetZapJitInfo();

// Returns the global instance of Zapper->EE interface

extern "C" ICorCompileInfo * __stdcall GetCompileInfo();

// Stress mode to leave some methods/types uncompiled in the ngen image.
// Those methods will be JIT-compiled at runtime as needed.

extern "C" unsigned __stdcall PartialNGenStressPercentage();

// create a PDB dumping all functions in hAssembly into pdbPath
extern "C" HRESULT __stdcall CreatePdb(CORINFO_ASSEMBLY_HANDLE hAssembly, BSTR pNativeImagePath, BSTR pPdbPath, BOOL pdbLines, BSTR pManagedPdbSearchPath, LPCWSTR pDiasymreaderPath);

extern bool g_fNGenMissingDependenciesOk;

extern bool g_fNGenWinMDResilient;

#ifdef FEATURE_READYTORUN_COMPILER
extern bool g_fReadyToRunCompilation;
extern bool g_fLargeVersionBubble;
#endif

inline bool IsReadyToRunCompilation()
{
#ifdef FEATURE_READYTORUN_COMPILER
    return g_fReadyToRunCompilation;
#else
    return false;
#endif
}

#ifdef FEATURE_READYTORUN_COMPILER
inline bool IsLargeVersionBubbleEnabled()
{
    return g_fLargeVersionBubble;
}
#endif

#endif /* COR_COMPILE_H_ */
