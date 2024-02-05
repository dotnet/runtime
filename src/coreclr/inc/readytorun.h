// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// readytorun.h
//

//
// Contains definitions for the Ready to Run file format
//

#ifndef __READYTORUN_H__
#define __READYTORUN_H__

#define READYTORUN_SIGNATURE 0x00525452 // 'RTR'

// Keep these in sync with
//  src/coreclr/tools/Common/Internal/Runtime/ModuleHeaders.cs
//  src/coreclr/nativeaot/Runtime/inc/ModuleHeaders.h
#define READYTORUN_MAJOR_VERSION 0x0009
#define READYTORUN_MINOR_VERSION 0x0001

#define MINIMUM_READYTORUN_MAJOR_VERSION 0x009

// R2R Version 2.1 adds the InliningInfo section
// R2R Version 2.2 adds the ProfileDataInfo section
// R2R Version 3.0 changes calling conventions to correctly handle explicit structures to spec.
//     R2R 3.0 is not backward compatible with 2.x.
// R2R Version 6.0 changes managed layout for sequential types with any unmanaged non-blittable fields.
//     R2R 6.0 is not backward compatible with 5.x or earlier.
// R2R Version 8.0 Changes the alignment of the Int128 type
// R2R Version 9.0 adds support for the Vector512 type
// R2R Version 9.1 adds new helpers to allocate objects on frozen segments

struct READYTORUN_CORE_HEADER
{
    DWORD                   Flags;          // READYTORUN_FLAG_XXX

    DWORD                   NumberOfSections;

    // Array of sections follows. The array entries are sorted by Type
    // READYTORUN_SECTION   Sections[];
};

struct READYTORUN_HEADER
{
    DWORD                   Signature;      // READYTORUN_SIGNATURE
    USHORT                  MajorVersion;   // READYTORUN_VERSION_XXX
    USHORT                  MinorVersion;

    READYTORUN_CORE_HEADER  CoreHeader;
};

struct READYTORUN_COMPONENT_ASSEMBLIES_ENTRY
{
    IMAGE_DATA_DIRECTORY CorHeader;
    IMAGE_DATA_DIRECTORY ReadyToRunCoreHeader;
};

enum ReadyToRunFlag
{
    READYTORUN_FLAG_PLATFORM_NEUTRAL_SOURCE     = 0x00000001,   // Set if the original IL assembly was platform-neutral
    READYTORUN_FLAG_SKIP_TYPE_VALIDATION        = 0x00000002,   // Set of methods with native code was determined using profile data
    READYTORUN_FLAG_PARTIAL                     = 0x00000004,
    READYTORUN_FLAG_NONSHARED_PINVOKE_STUBS     = 0x00000008,   // PInvoke stubs compiled into image are non-shareable (no secret parameter)
    READYTORUN_FLAG_EMBEDDED_MSIL               = 0x00000010,   // MSIL is embedded in the composite R2R executable
    READYTORUN_FLAG_COMPONENT                   = 0x00000020,   // This is the header describing a component assembly of composite R2R
    READYTORUN_FLAG_MULTIMODULE_VERSION_BUBBLE  = 0x00000040,   // This R2R module has multiple modules within its version bubble (For versions before version 6.2, all modules are assumed to possibly have this characteristic)
    READYTORUN_FLAG_UNRELATED_R2R_CODE          = 0x00000080,   // This R2R module has code in it that would not be naturally encoded into this module
};

enum class ReadyToRunSectionType : uint32_t
{
    CompilerIdentifier          = 100,
    ImportSections              = 101,
    RuntimeFunctions            = 102,
    MethodDefEntryPoints        = 103,
    ExceptionInfo               = 104,
    DebugInfo                   = 105,
    DelayLoadMethodCallThunks   = 106,
    // 107 used by an older format of AvailableTypes
    AvailableTypes              = 108,
    InstanceMethodEntryPoints   = 109,
    InliningInfo                = 110, // Added in V2.1, deprecated in 4.1
    ProfileDataInfo             = 111, // Added in V2.2
    ManifestMetadata            = 112, // Added in V2.3
    AttributePresence           = 113, // Added in V3.1
    InliningInfo2               = 114, // Added in V4.1
    ComponentAssemblies         = 115, // Added in V4.1
    OwnerCompositeExecutable    = 116, // Added in V4.1
    PgoInstrumentationData      = 117, // Added in V5.2
    ManifestAssemblyMvids       = 118, // Added in V5.3
    CrossModuleInlineInfo       = 119, // Added in V6.2
    HotColdMap                  = 120, // Added in V8.0
    MethodIsGenericMap          = 121, // Added in V9.0
    EnclosingTypeMap            = 122, // Added in V9.0
    TypeGenericInfoMap          = 123, // Added in V9.0

    // If you add a new section consider whether it is a breaking or non-breaking change.
    // Usually it is non-breaking, but if it is preferable to have older runtimes fail
    // to load the image vs. ignoring the new section it could be marked breaking.
    // Increment the READYTORUN_MINOR_VERSION (non-breaking) or READYTORUN_MAJOR_VERSION
    // (breaking) as appropriate.
};

struct READYTORUN_SECTION
{
    ReadyToRunSectionType   Type;           // READYTORUN_SECTION_XXX
    IMAGE_DATA_DIRECTORY    Section;
};

enum class ReadyToRunImportSectionType : uint8_t
{
    Unknown      = 0,
    StubDispatch = 2,
    StringHandle = 3,
    ILBodyFixups = 7,
};

enum class ReadyToRunImportSectionFlags : uint16_t
{
    None     = 0x0000,
    Eager    = 0x0001, // Section at module load time.
    PCode    = 0x0004, // Section contains pointers to code
};

// All values in this enum should within a nibble (4 bits).
enum class ReadyToRunTypeGenericInfo : uint8_t
{
    GenericCountMask = 0x3,
    HasConstraints = 0x4,
    HasVariance = 0x8,
};

// All values in this enum should fit within 2 bits.
enum class ReadyToRunGenericInfoGenericCount : uint32_t
{
    Zero = 0,
    One = 1,
    Two = 2,
    MoreThanTwo = 3
};

enum class ReadyToRunEnclosingTypeMap
{
    MaxTypeCount = 0xFFFE
};

//
// READYTORUN_IMPORT_SECTION describes image range with references to code or runtime data structures
//
// There is number of different types of these ranges: eagerly initialized at image load vs. lazily initialized at method entry
// vs. lazily initialized on first use; handles vs. code pointers, etc.
//
struct READYTORUN_IMPORT_SECTION
{
    IMAGE_DATA_DIRECTORY         Section;            // Section containing values to be fixed up
    ReadyToRunImportSectionFlags Flags;              // One or more of ReadyToRunImportSectionFlags
    ReadyToRunImportSectionType  Type;               // One of ReadyToRunImportSectionType
    BYTE                         EntrySize;
    DWORD                        Signatures;         // RVA of optional signature descriptors
    DWORD                        AuxiliaryData;      // RVA of optional auxiliary data (typically GC info)
};

//
// Constants for method and field encoding
//

enum ReadyToRunMethodSigFlags
{
    READYTORUN_METHOD_SIG_UnboxingStub          = 0x01,
    READYTORUN_METHOD_SIG_InstantiatingStub     = 0x02,
    READYTORUN_METHOD_SIG_MethodInstantiation   = 0x04,
    READYTORUN_METHOD_SIG_SlotInsteadOfToken    = 0x08,
    READYTORUN_METHOD_SIG_MemberRefToken        = 0x10,
    READYTORUN_METHOD_SIG_Constrained           = 0x20,
    READYTORUN_METHOD_SIG_OwnerType             = 0x40,
    READYTORUN_METHOD_SIG_UpdateContext         = 0x80,
};

enum ReadyToRunFieldSigFlags
{
    READYTORUN_FIELD_SIG_IndexInsteadOfToken    = 0x08,
    READYTORUN_FIELD_SIG_MemberRefToken         = 0x10,
    READYTORUN_FIELD_SIG_OwnerType              = 0x40,
};

enum ReadyToRunTypeLayoutFlags
{
    READYTORUN_LAYOUT_HFA                       = 0x01,
    READYTORUN_LAYOUT_Alignment                 = 0x02,
    READYTORUN_LAYOUT_Alignment_Native          = 0x04,
    READYTORUN_LAYOUT_GCLayout                  = 0x08,
    READYTORUN_LAYOUT_GCLayout_Empty            = 0x10,
};

enum ReadyToRunVirtualFunctionOverrideFlags
{
    READYTORUN_VIRTUAL_OVERRIDE_None = 0x00,
    READYTORUN_VIRTUAL_OVERRIDE_VirtualFunctionOverridden = 0x01,
};

enum class ReadyToRunCrossModuleInlineFlags : uint32_t
{
    CrossModuleInlinee           = 0x1,
    HasCrossModuleInliners       = 0x2,
    CrossModuleInlinerIndexShift = 2,

    InlinerRidHasModule          = 0x1,
    InlinerRidShift              = 1,
};

//
// Constants for fixup signature encoding
//

enum ReadyToRunFixupKind
{
    READYTORUN_FIXUP_ThisObjDictionaryLookup    = 0x07,
    READYTORUN_FIXUP_TypeDictionaryLookup       = 0x08,
    READYTORUN_FIXUP_MethodDictionaryLookup     = 0x09,

    READYTORUN_FIXUP_TypeHandle                 = 0x10,
    READYTORUN_FIXUP_MethodHandle               = 0x11,
    READYTORUN_FIXUP_FieldHandle                = 0x12,

    READYTORUN_FIXUP_MethodEntry                = 0x13, /* For calling a method entry point */
    READYTORUN_FIXUP_MethodEntry_DefToken       = 0x14, /* Smaller version of MethodEntry - method is def token */
    READYTORUN_FIXUP_MethodEntry_RefToken       = 0x15, /* Smaller version of MethodEntry - method is ref token */

    READYTORUN_FIXUP_VirtualEntry               = 0x16, /* For invoking a virtual method */
    READYTORUN_FIXUP_VirtualEntry_DefToken      = 0x17, /* Smaller version of VirtualEntry - method is def token */
    READYTORUN_FIXUP_VirtualEntry_RefToken      = 0x18, /* Smaller version of VirtualEntry - method is ref token */
    READYTORUN_FIXUP_VirtualEntry_Slot          = 0x19, /* Smaller version of VirtualEntry - type & slot */

    READYTORUN_FIXUP_Helper                     = 0x1A, /* Helper */
    READYTORUN_FIXUP_StringHandle               = 0x1B, /* String handle */

    READYTORUN_FIXUP_NewObject                  = 0x1C, /* Dynamically created new helper */
    READYTORUN_FIXUP_NewArray                   = 0x1D,

    READYTORUN_FIXUP_IsInstanceOf               = 0x1E, /* Dynamically created casting helper */
    READYTORUN_FIXUP_ChkCast                    = 0x1F,

    READYTORUN_FIXUP_FieldAddress               = 0x20, /* For accessing a cross-module static fields */
    READYTORUN_FIXUP_CctorTrigger               = 0x21, /* Static constructor trigger */

    READYTORUN_FIXUP_StaticBaseNonGC            = 0x22, /* Dynamically created static base helpers */
    READYTORUN_FIXUP_StaticBaseGC               = 0x23,
    READYTORUN_FIXUP_ThreadStaticBaseNonGC      = 0x24,
    READYTORUN_FIXUP_ThreadStaticBaseGC         = 0x25,

    READYTORUN_FIXUP_FieldBaseOffset            = 0x26, /* Field base offset */
    READYTORUN_FIXUP_FieldOffset                = 0x27, /* Field offset */

    READYTORUN_FIXUP_TypeDictionary             = 0x28,
    READYTORUN_FIXUP_MethodDictionary           = 0x29,

    READYTORUN_FIXUP_Check_TypeLayout           = 0x2A, /* size, alignment, HFA, reference map */
    READYTORUN_FIXUP_Check_FieldOffset          = 0x2B,

    READYTORUN_FIXUP_DelegateCtor               = 0x2C, /* optimized delegate ctor */
    READYTORUN_FIXUP_DeclaringTypeHandle        = 0x2D,

    READYTORUN_FIXUP_IndirectPInvokeTarget      = 0x2E, /* Target (indirect) of an inlined pinvoke */
    READYTORUN_FIXUP_PInvokeTarget              = 0x2F, /* Target of an inlined pinvoke */

    READYTORUN_FIXUP_Check_InstructionSetSupport= 0x30, /* Define the set of instruction sets that must be supported/unsupported to use the fixup */

    READYTORUN_FIXUP_Verify_FieldOffset         = 0x31, /* Generate a runtime check to ensure that the field offset matches between compile and runtime. Unlike Check_FieldOffset, this will generate a runtime failure instead of silently dropping the method */
    READYTORUN_FIXUP_Verify_TypeLayout          = 0x32, /* Generate a runtime check to ensure that the type layout (size, alignment, HFA, reference map) matches between compile and runtime. Unlike Check_TypeLayout, this will generate a runtime failure instead of silently dropping the method */

    READYTORUN_FIXUP_Check_VirtualFunctionOverride = 0x33, /* Generate a runtime check to ensure that virtual function resolution has equivalent behavior at runtime as at compile time. If not equivalent, code will not be used */
    READYTORUN_FIXUP_Verify_VirtualFunctionOverride = 0x34, /* Generate a runtime check to ensure that virtual function resolution has equivalent behavior at runtime as at compile time. If not equivalent, generate runtime failure. */

    READYTORUN_FIXUP_Check_IL_Body              = 0x35, /* Check to see if an IL method is defined the same at runtime as at compile time. A failed match will cause code not to be used. */
    READYTORUN_FIXUP_Verify_IL_Body             = 0x36, /* Verify an IL body is defined the same at compile time and runtime. A failed match will cause a hard runtime failure. */
};

//
// Intrinsics and helpers
//

enum ReadyToRunHelper
{
    READYTORUN_HELPER_Invalid                   = 0x00,

    // Not a real helper - handle to current module passed to delay load helpers.
    READYTORUN_HELPER_Module                    = 0x01,
    READYTORUN_HELPER_GSCookie                  = 0x02,
    READYTORUN_HELPER_IndirectTrapThreads       = 0x03,

    //
    // Delay load helpers
    //

    // All delay load helpers use custom calling convention:
    // - scratch register - address of indirection cell. 0 = address is inferred from callsite.
    // - stack - section index, module handle
    READYTORUN_HELPER_DelayLoad_MethodCall      = 0x08,

    READYTORUN_HELPER_DelayLoad_Helper          = 0x10,
    READYTORUN_HELPER_DelayLoad_Helper_Obj      = 0x11,
    READYTORUN_HELPER_DelayLoad_Helper_ObjObj   = 0x12,

    // JIT helpers

    // Exception handling helpers
    READYTORUN_HELPER_Throw                     = 0x20,
    READYTORUN_HELPER_Rethrow                   = 0x21,
    READYTORUN_HELPER_Overflow                  = 0x22,
    READYTORUN_HELPER_RngChkFail                = 0x23,
    READYTORUN_HELPER_FailFast                  = 0x24,
    READYTORUN_HELPER_ThrowNullRef              = 0x25,
    READYTORUN_HELPER_ThrowDivZero              = 0x26,

    // Write barriers
    READYTORUN_HELPER_WriteBarrier              = 0x30,
    READYTORUN_HELPER_CheckedWriteBarrier       = 0x31,
    READYTORUN_HELPER_ByRefWriteBarrier         = 0x32,

    // Array helpers
    READYTORUN_HELPER_Stelem_Ref                = 0x38,
    READYTORUN_HELPER_Ldelema_Ref               = 0x39,

    READYTORUN_HELPER_MemSet                    = 0x40,
    READYTORUN_HELPER_MemCpy                    = 0x41,

    // PInvoke helpers
    READYTORUN_HELPER_PInvokeBegin              = 0x42,
    READYTORUN_HELPER_PInvokeEnd                = 0x43,
    READYTORUN_HELPER_GCPoll                    = 0x44,
    READYTORUN_HELPER_ReversePInvokeEnter       = 0x45,
    READYTORUN_HELPER_ReversePInvokeExit        = 0x46,

    // Get string handle lazily
    READYTORUN_HELPER_GetString                 = 0x50,

    // Used by /Tuning for Profile optimizations
    READYTORUN_HELPER_LogMethodEnter            = 0x51,

    // Reflection helpers
    READYTORUN_HELPER_GetRuntimeTypeHandle      = 0x54,
    READYTORUN_HELPER_GetRuntimeMethodHandle    = 0x55,
    READYTORUN_HELPER_GetRuntimeFieldHandle     = 0x56,

    READYTORUN_HELPER_Box                       = 0x58,
    READYTORUN_HELPER_Box_Nullable              = 0x59,
    READYTORUN_HELPER_Unbox                     = 0x5A,
    READYTORUN_HELPER_Unbox_Nullable            = 0x5B,
    READYTORUN_HELPER_NewMultiDimArr            = 0x5C,

    // Helpers used with generic handle lookup cases
    READYTORUN_HELPER_NewObject                 = 0x60,
    READYTORUN_HELPER_NewArray                  = 0x61,
    READYTORUN_HELPER_CheckCastAny              = 0x62,
    READYTORUN_HELPER_CheckInstanceAny          = 0x63,
    READYTORUN_HELPER_GenericGcStaticBase       = 0x64,
    READYTORUN_HELPER_GenericNonGcStaticBase    = 0x65,
    READYTORUN_HELPER_GenericGcTlsBase          = 0x66,
    READYTORUN_HELPER_GenericNonGcTlsBase       = 0x67,
    READYTORUN_HELPER_VirtualFuncPtr            = 0x68,
    READYTORUN_HELPER_IsInstanceOfException     = 0x69,
    READYTORUN_HELPER_NewMaybeFrozenArray       = 0x6A,
    READYTORUN_HELPER_NewMaybeFrozenObject      = 0x6B,

    // Long mul/div/shift ops
    READYTORUN_HELPER_LMul                      = 0xC0,
    READYTORUN_HELPER_LMulOfv                   = 0xC1,
    READYTORUN_HELPER_ULMulOvf                  = 0xC2,
    READYTORUN_HELPER_LDiv                      = 0xC3,
    READYTORUN_HELPER_LMod                      = 0xC4,
    READYTORUN_HELPER_ULDiv                     = 0xC5,
    READYTORUN_HELPER_ULMod                     = 0xC6,
    READYTORUN_HELPER_LLsh                      = 0xC7,
    READYTORUN_HELPER_LRsh                      = 0xC8,
    READYTORUN_HELPER_LRsz                      = 0xC9,
    READYTORUN_HELPER_Lng2Dbl                   = 0xCA,
    READYTORUN_HELPER_ULng2Dbl                  = 0xCB,

    // 32-bit division helpers
    READYTORUN_HELPER_Div                       = 0xCC,
    READYTORUN_HELPER_Mod                       = 0xCD,
    READYTORUN_HELPER_UDiv                      = 0xCE,
    READYTORUN_HELPER_UMod                      = 0xCF,

    // Floating point conversions
    READYTORUN_HELPER_Dbl2Int                   = 0xD0,
    READYTORUN_HELPER_Dbl2IntOvf                = 0xD1,
    READYTORUN_HELPER_Dbl2Lng                   = 0xD2,
    READYTORUN_HELPER_Dbl2LngOvf                = 0xD3,
    READYTORUN_HELPER_Dbl2UInt                  = 0xD4,
    READYTORUN_HELPER_Dbl2UIntOvf               = 0xD5,
    READYTORUN_HELPER_Dbl2ULng                  = 0xD6,
    READYTORUN_HELPER_Dbl2ULngOvf               = 0xD7,

    // Floating point ops
    READYTORUN_HELPER_DblRem                    = 0xE0,
    READYTORUN_HELPER_FltRem                    = 0xE1,

#ifdef FEATURE_EH_FUNCLETS
    // Personality routines
    READYTORUN_HELPER_PersonalityRoutine        = 0xF0,
    READYTORUN_HELPER_PersonalityRoutineFilterFunclet = 0xF1,
#endif

    // Synchronized methods
    READYTORUN_HELPER_MonitorEnter              = 0xF8,
    READYTORUN_HELPER_MonitorExit               = 0xF9,

    //
    // Deprecated/legacy
    //

    // JIT32 x86-specific write barriers
    READYTORUN_HELPER_WriteBarrier_EAX          = 0x100,
    READYTORUN_HELPER_WriteBarrier_EBX          = 0x101,
    READYTORUN_HELPER_WriteBarrier_ECX          = 0x102,
    READYTORUN_HELPER_WriteBarrier_ESI          = 0x103,
    READYTORUN_HELPER_WriteBarrier_EDI          = 0x104,
    READYTORUN_HELPER_WriteBarrier_EBP          = 0x105,
    READYTORUN_HELPER_CheckedWriteBarrier_EAX   = 0x106,
    READYTORUN_HELPER_CheckedWriteBarrier_EBX   = 0x107,
    READYTORUN_HELPER_CheckedWriteBarrier_ECX   = 0x108,
    READYTORUN_HELPER_CheckedWriteBarrier_ESI   = 0x109,
    READYTORUN_HELPER_CheckedWriteBarrier_EDI   = 0x10A,
    READYTORUN_HELPER_CheckedWriteBarrier_EBP   = 0x10B,

    // JIT32 x86-specific exception handling
    READYTORUN_HELPER_EndCatch                  = 0x110,

    // Stack probing helper
    READYTORUN_HELPER_StackProbe                = 0x111,

    READYTORUN_HELPER_GetCurrentManagedThreadId = 0x112,

    // Array helpers for use with native ints
    READYTORUN_HELPER_Stelem_Ref_I                = 0x113,
    READYTORUN_HELPER_Ldelema_Ref_I               = 0x114,
};

#include "readytoruninstructionset.h"

//
// Exception info
//

struct READYTORUN_EXCEPTION_LOOKUP_TABLE_ENTRY
{
    DWORD MethodStart;
    DWORD ExceptionInfo;
};

struct READYTORUN_EXCEPTION_CLAUSE
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

enum ReadyToRunRuntimeConstants : DWORD
{
    READYTORUN_PInvokeTransitionFrameSizeInPointerUnits = 11,
#ifdef TARGET_X86
    READYTORUN_ReversePInvokeTransitionFrameSizeInPointerUnits = 5,
#else
    READYTORUN_ReversePInvokeTransitionFrameSizeInPointerUnits = 2,
#endif
};

enum ReadyToRunHFAElemType : DWORD
{
    READYTORUN_HFA_ELEMTYPE_None = 0,
    READYTORUN_HFA_ELEMTYPE_Float32 = 1,
    READYTORUN_HFA_ELEMTYPE_Float64 = 2,
    READYTORUN_HFA_ELEMTYPE_Vector64 = 3,
    READYTORUN_HFA_ELEMTYPE_Vector128 = 4,
};

#endif // __READYTORUN_H__
