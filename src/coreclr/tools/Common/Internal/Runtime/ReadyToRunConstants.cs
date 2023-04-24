// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

// If any of these constants change, update src/coreclr/inc/readytorun.h and
// src/coreclr/tools/Common/Internal/Runtime/ModuleHeaders.cs with the new R2R minor version

namespace Internal.ReadyToRunConstants
{
    [Flags]
    public enum ReadyToRunFlags
    {
        READYTORUN_FLAG_PlatformNeutralSource = 0x00000001,     // Set if the original IL assembly was platform-neutral
        READYTORUN_FLAG_SkipTypeValidation = 0x00000002,        // Set of methods with native code was determined using profile data
        READYTORUN_FLAG_Partial = 0x00000004,
        READYTORUN_FLAG_NonSharedPInvokeStubs = 0x00000008,     // PInvoke stubs compiled into image are non-shareable (no secret parameter)
        READYTORUN_FLAG_EmbeddedMSIL = 0x00000010,              // MSIL is embedded in the composite R2R executable
        READYTORUN_FLAG_Component = 0x00000020,                 // This is the header describing a component assembly of composite R2R
        READYTORUN_FLAG_MultiModuleVersionBubble = 0x00000040,   // This R2R module has multiple modules within its version bubble
        READYTORUN_FLAG_UnrelatedR2RCode = 0x00000080,          // This R2R module has generic code in it that would not be naturally encoded into this module
    }

    public enum ReadyToRunImportSectionType : byte
    {
        Unknown      = 0,
        StubDispatch = 2,
        StringHandle = 3,
        ILBodyFixups = 7,
    }

    [Flags]
    public enum ReadyToRunImportSectionFlags : ushort
    {
        None     = 0x0000,
        Eager    = 0x0001, // Section at module load time.
        PCode    = 0x0004, // Section contains pointers to code
    }

    /// <summary>
    /// Constants for method and field encoding
    /// </summary>
    [Flags]
    public enum ReadyToRunMethodSigFlags : byte
    {
        READYTORUN_METHOD_SIG_None = 0x00,
        READYTORUN_METHOD_SIG_UnboxingStub = 0x01,
        READYTORUN_METHOD_SIG_InstantiatingStub = 0x02,
        READYTORUN_METHOD_SIG_MethodInstantiation = 0x04,
        READYTORUN_METHOD_SIG_SlotInsteadOfToken = 0x08,
        READYTORUN_METHOD_SIG_MemberRefToken = 0x10,
        READYTORUN_METHOD_SIG_Constrained = 0x20,
        READYTORUN_METHOD_SIG_OwnerType = 0x40,
        READYTORUN_METHOD_SIG_UpdateContext = 0x80,
    }

    [Flags]
    public enum ReadyToRunFieldSigFlags : byte
    {
        READYTORUN_FIELD_SIG_IndexInsteadOfToken = 0x08,
        READYTORUN_FIELD_SIG_MemberRefToken = 0x10,
        READYTORUN_FIELD_SIG_OwnerType = 0x40,
    }

    [Flags]
    public enum ReadyToRunTypeLayoutFlags : byte
    {
        READYTORUN_LAYOUT_HFA = 0x01,
        READYTORUN_LAYOUT_Alignment = 0x02,
        READYTORUN_LAYOUT_Alignment_Native = 0x04,
        READYTORUN_LAYOUT_GCLayout = 0x08,
        READYTORUN_LAYOUT_GCLayout_Empty = 0x10,
    }

    [Flags]
    public enum ReadyToRunVirtualFunctionOverrideFlags : uint
    {
        None = 0x00,
        VirtualFunctionOverridden = 0x01,
    }

    [Flags]
    internal enum ReadyToRunCrossModuleInlineFlags : uint
    {
        CrossModuleInlinee  = 0x1,
        HasCrossModuleInliners = 0x2,
        CrossModuleInlinerIndexShift = 2,
        InlinerRidHasModule = 0x1,
        InlinerRidShift = 1,
    };

    public enum DictionaryEntryKind
    {
        EmptySlot = 0,
        TypeHandleSlot = 1,
        MethodDescSlot = 2,
        MethodEntrySlot = 3,
        ConstrainedMethodEntrySlot = 4,
        DispatchStubAddrSlot = 5,
        FieldDescSlot = 6,
        DeclaringTypeHandleSlot = 7,
    }

    public enum ReadyToRunFixupKind
    {
        Invalid = 0x00,

        ThisObjDictionaryLookup = 0x07,
        TypeDictionaryLookup = 0x08,
        MethodDictionaryLookup = 0x09,

        TypeHandle = 0x10,
        MethodHandle = 0x11,
        FieldHandle = 0x12,

        MethodEntry = 0x13,                 // For calling a method entry point
        MethodEntry_DefToken = 0x14,        // Smaller version of MethodEntry - method is def token
        MethodEntry_RefToken = 0x15,        // Smaller version of MethodEntry - method is ref token

        VirtualEntry = 0x16,                // For invoking a virtual method
        VirtualEntry_DefToken = 0x17,       // Smaller version of VirtualEntry - method is def token
        VirtualEntry_RefToken = 0x18,       // Smaller version of VirtualEntry - method is ref token
        VirtualEntry_Slot = 0x19,           // Smaller version of VirtualEntry - type & slot

        Helper = 0x1A,                      // Helper
        StringHandle = 0x1B,                // String handle

        NewObject = 0x1C,                   // Dynamically created new helper
        NewArray = 0x1D,

        IsInstanceOf = 0x1E,                // Dynamically created casting helper
        ChkCast = 0x1F,

        FieldAddress = 0x20,                // For accessing a cross-module static fields
        CctorTrigger = 0x21,                // Static constructor trigger

        StaticBaseNonGC = 0x22,             // Dynamically created static base helpers
        StaticBaseGC = 0x23,
        ThreadStaticBaseNonGC = 0x24,
        ThreadStaticBaseGC = 0x25,

        FieldBaseOffset = 0x26,             // Field base offset
        FieldOffset = 0x27,                 // Field offset

        TypeDictionary = 0x28,
        MethodDictionary = 0x29,

        Check_TypeLayout = 0x2A,            // size, alignment, HFA, reference map
        Check_FieldOffset = 0x2B,

        DelegateCtor = 0x2C,                // optimized delegate ctor
        DeclaringTypeHandle = 0x2D,

        IndirectPInvokeTarget = 0x2E,       // Target (indirect) of an inlined pinvoke
        PInvokeTarget = 0x2F,               // Target of an inlined pinvoke

        Check_InstructionSetSupport = 0x30, // Define the set of instruction sets that must be supported/unsupported to use the fixup

        Verify_FieldOffset = 0x31,  // Generate a runtime check to ensure that the field offset matches between compile and runtime. Unlike CheckFieldOffset, this will generate a runtime exception on failure instead of silently dropping the method
        Verify_TypeLayout = 0x32,  // Generate a runtime check to ensure that the type layout (size, alignment, HFA, reference map) matches between compile and runtime. Unlike Check_TypeLayout, this will generate a runtime failure instead of silently dropping the method

        Check_VirtualFunctionOverride = 0x33, // Generate a runtime check to ensure that virtual function resolution has equivalent behavior at runtime as at compile time. If not equivalent, code will not be used
        Verify_VirtualFunctionOverride = 0x34, // Generate a runtime check to ensure that virtual function resolution has equivalent behavior at runtime as at compile time. If not equivalent, generate runtime failure.

        Check_IL_Body              = 0x35, /* Check to see if an IL method is defined the same at runtime as at compile time. A failed match will cause code not to be used. */
        Verify_IL_Body             = 0x36, /* Verify an IL body is defined the same at compile time and runtime. A failed match will cause a hard runtime failure. */

        ModuleOverride = 0x80,
        // followed by sig-encoded UInt with assemblyref index into either the assemblyref
        // table of the MSIL metadata of the master context module for the signature or
        // into the extra assemblyref table in the manifest metadata R2R header table
        // (used in cases inlining brings in references to assemblies not seen in the MSIL).
    }

    //
    // Intrinsics and helpers
    // Keep in sync with https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/readytorun.h
    //

    [Flags]
    public enum ReadyToRunHelper
    {
        Invalid                     = 0x00,

        // Not a real helper - handle to current module passed to delay load helpers.
        Module                      = 0x01,
        GSCookie                    = 0x02,
        IndirectTrapThreads         = 0x03,

        //
        // Delay load helpers
        //

        // All delay load helpers use custom calling convention:
        // - scratch register - address of indirection cell. 0 = address is inferred from callsite.
        // - stack - section index, module handle
        DelayLoad_MethodCall        = 0x08,

        DelayLoad_Helper            = 0x10,
        DelayLoad_Helper_Obj        = 0x11,
        DelayLoad_Helper_ObjObj     = 0x12,

        // Exception handling helpers
        Throw                       = 0x20,
        Rethrow                     = 0x21,
        Overflow                    = 0x22,
        RngChkFail                  = 0x23,
        FailFast                    = 0x24,
        ThrowNullRef                = 0x25,
        ThrowDivZero                = 0x26,

        // Write barriers
        WriteBarrier                = 0x30,
        CheckedWriteBarrier         = 0x31,
        ByRefWriteBarrier           = 0x32,

        // Array helpers
        Stelem_Ref                  = 0x38,
        Ldelema_Ref                 = 0x39,

        MemSet                      = 0x40,
        MemCpy                      = 0x41,

        // P/Invoke support
        PInvokeBegin                = 0x42,
        PInvokeEnd                  = 0x43,
        GCPoll                      = 0x44,
        ReversePInvokeEnter         = 0x45,
        ReversePInvokeExit          = 0x46,

        // Get string handle lazily
        GetString = 0x50,

        // Used by /Tuning for Profile optimizations
        LogMethodEnter = 0x51,

        // Reflection helpers
        GetRuntimeTypeHandle        = 0x54,
        GetRuntimeMethodHandle      = 0x55,
        GetRuntimeFieldHandle       = 0x56,

        Box                         = 0x58,
        Box_Nullable                = 0x59,
        Unbox                       = 0x5A,
        Unbox_Nullable              = 0x5B,
        NewMultiDimArr              = 0x5C,

        // Helpers used with generic handle lookup cases
        NewObject                   = 0x60,
        NewArray                    = 0x61,
        CheckCastAny                = 0x62,
        CheckInstanceAny            = 0x63,
        GenericGcStaticBase         = 0x64,
        GenericNonGcStaticBase      = 0x65,
        GenericGcTlsBase            = 0x66,
        GenericNonGcTlsBase         = 0x67,
        VirtualFuncPtr              = 0x68,
        IsInstanceOfException       = 0x69,

        // Long mul/div/shift ops
        LMul                        = 0xC0,
        LMulOfv                     = 0xC1,
        ULMulOvf                    = 0xC2,
        LDiv                        = 0xC3,
        LMod                        = 0xC4,
        ULDiv                       = 0xC5,
        ULMod                       = 0xC6,
        LLsh                        = 0xC7,
        LRsh                        = 0xC8,
        LRsz                        = 0xC9,
        Lng2Dbl                     = 0xCA,
        ULng2Dbl                    = 0xCB,

        // 32-bit division helpers
        Div                         = 0xCC,
        Mod                         = 0xCD,
        UDiv                        = 0xCE,
        UMod                        = 0xCF,

        // Floating point conversions
        Dbl2Int                     = 0xD0,
        Dbl2IntOvf                  = 0xD1,
        Dbl2Lng                     = 0xD2,
        Dbl2LngOvf                  = 0xD3,
        Dbl2UInt                    = 0xD4,
        Dbl2UIntOvf                 = 0xD5,
        Dbl2ULng                    = 0xD6,
        Dbl2ULngOvf                 = 0xD7,

        // Floating point ops
        DblRem                      = 0xE0,
        FltRem                      = 0xE1,
        DblRound                    = 0xE2,
        FltRound                    = 0xE3,

        // Personality routines
        PersonalityRoutine          = 0xF0,
        PersonalityRoutineFilterFunclet = 0xF1,

        // Synchronized methods
        MonitorEnter                = 0xF8,
        MonitorExit                 = 0xF9,

        // JIT32 x86-specific write barriers
        WriteBarrier_EAX            = 0x100,
        WriteBarrier_EBX            = 0x101,
        WriteBarrier_ECX            = 0x102,
        WriteBarrier_ESI            = 0x103,
        WriteBarrier_EDI            = 0x104,
        WriteBarrier_EBP            = 0x105,
        CheckedWriteBarrier_EAX     = 0x106,
        CheckedWriteBarrier_EBX     = 0x107,
        CheckedWriteBarrier_ECX     = 0x108,
        CheckedWriteBarrier_ESI     = 0x109,
        CheckedWriteBarrier_EDI     = 0x10A,
        CheckedWriteBarrier_EBP     = 0x10B,

        // JIT32 x86-specific exception handling
        EndCatch                    = 0x110,

        StackProbe                  = 0x111,

        GetCurrentManagedThreadId   = 0x112,

        // **********************************************************************************************
        //
        // These are not actually part of the R2R file format. We have them here because it's convenient.
        //
        // **********************************************************************************************

        // Marker to be used in asserts.
        FirstFakeHelper,

        ThrowArgumentOutOfRange,
        ThrowArgument,
        ThrowPlatformNotSupported,
        ThrowNotImplemented,

        DebugBreak,

        GetRuntimeType,

        AreTypesEquivalent,

        CheckCastClass,
        CheckInstanceClass,
        CheckCastArray,
        CheckInstanceArray,
        CheckCastInterface,
        CheckInstanceInterface,

        MonitorEnterStatic,
        MonitorExitStatic,

        NewMultiDimArrRare,

        // GVM lookup helper
        GVMLookupForSlot,

        // TypedReference
        TypeHandleToRuntimeType,
        GetRefAny,
        TypeHandleToRuntimeTypeHandle,
    }

    // Enum used for HFA type recognition.
    // Supported across architectures, so that it can be used in altjits and cross-compilation.
    public enum ReadyToRunHFAElemType
    {
        None = 0,
        Float32 = 1,
        Float64 = 2,
        Vector64 = 3,
        Vector128 = 4,
    }

    public static class ReadyToRunRuntimeConstants
    {
        public const int READYTORUN_PInvokeTransitionFrameSizeInPointerUnits = 11;
        public const int READYTORUN_ReversePInvokeTransitionFrameSizeInPointerUnits_X86 = 5;
        public const int READYTORUN_ReversePInvokeTransitionFrameSizeInPointerUnits_Universal = 2;
    }
}
