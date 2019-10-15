// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Keep in sync with https://github.com/dotnet/coreclr/blob/master/src/inc/readytorun.h
//

namespace ILCompiler
{
    //
    // Intrinsics and helpers
    //

    public enum ReadyToRunHelper
    {
        Invalid                     = 0x00,

        // Not a real helper - handle to current module passed to delay load helpers.
        Module                      = 0x01,
        GSCookie                    = 0x02,

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

        // Get string handle lazily
        GetString                   = 0x50,

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
        NewMultiDimArr_NonVarArg    = 0x5D,

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

        // Personality rountines
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

        // P/Invoke support
        ReversePInvokeEnter,
        ReversePInvokeExit,

        MonitorEnterStatic,
        MonitorExitStatic,

        // GVM lookup helper
        GVMLookupForSlot,

        // TypedReference
        TypeHandleToRuntimeType,
        GetRefAny,
        TypeHandleToRuntimeTypeHandle,
    }
}
