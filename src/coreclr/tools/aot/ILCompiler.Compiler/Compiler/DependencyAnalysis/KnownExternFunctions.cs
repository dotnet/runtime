// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Enumerates the fixed set of extern functions (mostly NativeAOT runtime helpers) that are referenced by name only (i.e.
    /// without a backing <see cref="MethodDesc"/>) from the JIT interface and dependency analysis, e.g.
    /// from <see cref="JitHelper"/> and CorInfoImpl.RyuJit's GetHelperFtnUncached. Each value corresponds
    /// 1:1 with the mangled native symbol name emitted for it (see <see cref="KnownExternFunctions"/>),
    /// and the mapping must produce, byte-for-byte, the strings of the exports that they correspond to
    /// (including their architecture-dependent variants).
    /// </summary>
    public enum KnownExternFunction
    {
        // Exception helpers - Runtime/<arch>/ExceptionHandling.{S,asm}
        ThrowEx,
        Rethrow,
        ThrowExact,
        FallbackFailFast,
        DebugBreak,

        // GC write barriers - Runtime/portable.cpp (RhpAssignRef/RhpCheckedAssignRef) and per-arch asm
        WriteBarrier,
        CheckedWriteBarrier,
        WriteBarrier_EAX,
        WriteBarrier_EBX,
        WriteBarrier_ECX,
        WriteBarrier_EDI,
        WriteBarrier_ESI,
        WriteBarrier_EBP,
        CheckedWriteBarrier_EAX,
        CheckedWriteBarrier_EBX,
        CheckedWriteBarrier_ECX,
        CheckedWriteBarrier_EDI,
        CheckedWriteBarrier_ESI,
        CheckedWriteBarrier_EBP,
        BulkMoveWithWriteBarrier,

        // Allocation helpers - nativeaot/Runtime/portable.cpp, runtime/portable/AllocFast.cpp, and per-arch asm
        NewArray,
        NewObject,
        NewFast,
        NewFinalizable,
        NewFastAlign8,
        NewFinalizableAlign8,
        NewFastMisalign,
        NewPtrArrayFast,
        NewArrayFastAlign8,
        NewArrayFast,

        // libc helpers
        NativeMemSet,
        DblRem,
        FltRem,

        // Runtime/MathHelpers.cpp
        Lng2Dbl,
        ULng2Dbl,
        Lng2Flt,
        ULng2Flt,
        Dbl2Lng,
        Dbl2ULng,
        LMul,
        LRsz,
        LRsh,
        LLsh,

        // Runtime/<arch>/PInvoke.{S,asm}, Runtime/thread.cpp
        PInvokeBegin,
        PInvokeEnd,
        ReversePInvokeEnter,
        ReversePInvokeExit,

        // Interface/generic virtual dispatch stubs - Runtime/EHHelpers.cpp, Runtime/<arch>/DispatchResolve.{S,asm}
        GVMLookupForSlot,
        InterfaceDispatch,
        InterfaceDispatchGuarded,
        ResolveInterfaceMethodFast,
        ResolveInterfaceMethod,

        // Misc
        StackProbe,
        GcPoll,
        NyiLdVirtFtn,
        TlsGetAddr,
    }

    /// <summary>
    /// Names and signatures of the <see cref="KnownExternFunction"/> helpers. The helpers are referenced by name
    /// only, so there is no backing <see cref="MethodDesc"/> to source a signature from; the signature (needed to
    /// import the helper on Wasm) is derived here from each helper's declaration.
    /// </summary>
    internal static class KnownExternFunctions
    {
        public static Utf8String GetName(KnownExternFunction function, TargetDetails target)
        {
            string name = function switch
            {
                KnownExternFunction.ThrowEx => "RhpThrowEx",
                KnownExternFunction.Rethrow => "RhpRethrow",
                KnownExternFunction.ThrowExact => "RhpThrowExact",
                KnownExternFunction.FallbackFailFast => "RhpFallbackFailFast",
                KnownExternFunction.DebugBreak => "RhDebugBreak",

                KnownExternFunction.WriteBarrier => target.Architecture switch
                {
                    TargetArchitecture.ARM64 => "RhpAssignRefArm64",
                    TargetArchitecture.LoongArch64 => "RhpAssignRefLoongArch64",
                    TargetArchitecture.RiscV64 => "RhpAssignRefRiscV64",
                    _ => "RhpAssignRef"
                },
                KnownExternFunction.CheckedWriteBarrier =>
                    target.Architecture == TargetArchitecture.ARM64 ? "RhpCheckedAssignRefArm64" : "RhpCheckedAssignRef",
                KnownExternFunction.WriteBarrier_EAX => "RhpAssignRefEAX",
                KnownExternFunction.WriteBarrier_EBX => "RhpAssignRefEBX",
                KnownExternFunction.WriteBarrier_ECX => "RhpAssignRefECX",
                KnownExternFunction.WriteBarrier_EDI => "RhpAssignRefEDI",
                KnownExternFunction.WriteBarrier_ESI => "RhpAssignRefESI",
                KnownExternFunction.WriteBarrier_EBP => "RhpAssignRefEBP",
                KnownExternFunction.CheckedWriteBarrier_EAX => "RhpCheckedAssignRefEAX",
                KnownExternFunction.CheckedWriteBarrier_EBX => "RhpCheckedAssignRefEBX",
                KnownExternFunction.CheckedWriteBarrier_ECX => "RhpCheckedAssignRefECX",
                KnownExternFunction.CheckedWriteBarrier_EDI => "RhpCheckedAssignRefEDI",
                KnownExternFunction.CheckedWriteBarrier_ESI => "RhpCheckedAssignRefESI",
                KnownExternFunction.CheckedWriteBarrier_EBP => "RhpCheckedAssignRefEBP",
                KnownExternFunction.BulkMoveWithWriteBarrier => "RhBulkMoveWithWriteBarrier",

                KnownExternFunction.NewArray => "RhNewArray",
                KnownExternFunction.NewObject => "RhNewObject",
                KnownExternFunction.NewFast => "RhpNewFast",
                KnownExternFunction.NewFinalizable => "RhpNewFinalizable",
                KnownExternFunction.NewFastAlign8 => "RhpNewFastAlign8",
                KnownExternFunction.NewFinalizableAlign8 => "RhpNewFinalizableAlign8",
                KnownExternFunction.NewFastMisalign => "RhpNewFastMisalign",
                KnownExternFunction.NewPtrArrayFast => "RhpNewPtrArrayFast",
                KnownExternFunction.NewArrayFastAlign8 => "RhpNewArrayFastAlign8",
                KnownExternFunction.NewArrayFast => "RhpNewArrayFast",

                KnownExternFunction.NativeMemSet => "memset",
                KnownExternFunction.DblRem => "fmod",
                KnownExternFunction.FltRem => "fmodf",

                KnownExternFunction.Lng2Dbl => "RhpLng2Dbl",
                KnownExternFunction.ULng2Dbl => "RhpULng2Dbl",
                KnownExternFunction.Lng2Flt => "RhpLng2Flt",
                KnownExternFunction.ULng2Flt => "RhpULng2Flt",
                KnownExternFunction.Dbl2Lng => "RhpDbl2Lng",
                KnownExternFunction.Dbl2ULng => "RhpDbl2ULng",
                KnownExternFunction.LMul => "RhpLMul",
                KnownExternFunction.LRsz => "RhpLRsz",
                KnownExternFunction.LRsh => "RhpLRsh",
                KnownExternFunction.LLsh => "RhpLLsh",

                KnownExternFunction.PInvokeBegin => "RhpPInvoke",
                KnownExternFunction.PInvokeEnd => "RhpPInvokeReturn",
                KnownExternFunction.ReversePInvokeEnter => "RhpReversePInvoke",
                KnownExternFunction.ReversePInvokeExit => "RhpReversePInvokeReturn",

                KnownExternFunction.GVMLookupForSlot => "RhpDispatchResolve",
                KnownExternFunction.InterfaceDispatch => "RhpInterfaceDispatch",
                KnownExternFunction.InterfaceDispatchGuarded => "RhpInterfaceDispatchGuarded",
                KnownExternFunction.ResolveInterfaceMethodFast => "RhpResolveInterfaceMethodFast",
                KnownExternFunction.ResolveInterfaceMethod => "RhpResolveInterfaceMethod",

                KnownExternFunction.StackProbe => "RhpStackProbe",
                KnownExternFunction.GcPoll => "RhpGcPoll",
                KnownExternFunction.NyiLdVirtFtn => "NYI_LDVIRTFTN",
                KnownExternFunction.TlsGetAddr => "__tls_get_addr",

                _ => throw new NotImplementedException(function.ToString())
            };

            return new Utf8String(name);
        }

        /// <summary>
        /// Gets the signature of <paramref name="function"/>, or null if it has no standard-ABI signature.
        /// </summary>
        /// <remarks>
        /// Extern function nodes are shared by name, so when a function is also reachable through a direct
        /// P/Invoke (e.g. CoreLib's memset), its signature must exactly match that P/Invoke's signature.
        /// </remarks>
        public static ExternalTypeSignature? GetTypeSignature(KnownExternFunction function, TypeSystemContext context)
        {
            TypeDesc voidType = context.GetWellKnownType(WellKnownType.Void);
            TypeDesc voidPointerType = voidType.MakePointerType();
            TypeDesc objectType = context.GetWellKnownType(WellKnownType.Object);
            TypeDesc nativeIntType = context.GetWellKnownType(WellKnownType.IntPtr);
            TypeDesc nativeUIntType = context.GetWellKnownType(WellKnownType.UIntPtr);
            TypeDesc int32Type = context.GetWellKnownType(WellKnownType.Int32);
            TypeDesc int64Type = context.GetWellKnownType(WellKnownType.Int64);
            TypeDesc uint64Type = context.GetWellKnownType(WellKnownType.UInt64);
            TypeDesc singleType = context.GetWellKnownType(WellKnownType.Single);
            TypeDesc doubleType = context.GetWellKnownType(WellKnownType.Double);

            // All of these use the unmanaged calling convention, like the native FCIMPLs that implement most of them.
            ExternalTypeSignature UnmanagedSignature(TypeDesc returnType, params TypeDesc[] parameters) =>
                ExternalTypeSignature.Unmanaged(new MethodSignature(MethodSignatureFlags.Static, 0, returnType, parameters));

            return function switch
            {
                KnownExternFunction.ThrowEx => UnmanagedSignature(voidType, objectType),
                KnownExternFunction.ThrowExact => UnmanagedSignature(voidType, objectType),

                KnownExternFunction.Rethrow => UnmanagedSignature(voidType),

                KnownExternFunction.FallbackFailFast => UnmanagedSignature(voidType),

                KnownExternFunction.DebugBreak => UnmanagedSignature(voidType),

                KnownExternFunction.WriteBarrier => UnmanagedSignature(voidType, nativeIntType, objectType),
                KnownExternFunction.CheckedWriteBarrier => UnmanagedSignature(voidType, nativeIntType, objectType),

                KnownExternFunction.WriteBarrier_EAX or
                KnownExternFunction.WriteBarrier_EBX or
                KnownExternFunction.WriteBarrier_ECX or
                KnownExternFunction.WriteBarrier_EDI or
                KnownExternFunction.WriteBarrier_ESI or
                KnownExternFunction.WriteBarrier_EBP or
                KnownExternFunction.CheckedWriteBarrier_EAX or
                KnownExternFunction.CheckedWriteBarrier_EBX or
                KnownExternFunction.CheckedWriteBarrier_ECX or
                KnownExternFunction.CheckedWriteBarrier_EDI or
                KnownExternFunction.CheckedWriteBarrier_ESI or
                KnownExternFunction.CheckedWriteBarrier_EBP => null,

                KnownExternFunction.BulkMoveWithWriteBarrier => UnmanagedSignature(voidType, nativeIntType, nativeIntType, nativeUIntType),

                KnownExternFunction.NewArray => UnmanagedSignature(objectType, nativeIntType, nativeIntType),
                KnownExternFunction.NewObject => UnmanagedSignature(objectType, nativeIntType),

                KnownExternFunction.NewFast => UnmanagedSignature(objectType, nativeIntType),
                KnownExternFunction.NewFinalizable => UnmanagedSignature(objectType, nativeIntType),
                KnownExternFunction.NewFastAlign8 => UnmanagedSignature(objectType, nativeIntType),
                KnownExternFunction.NewFinalizableAlign8 => UnmanagedSignature(objectType, nativeIntType),
                KnownExternFunction.NewFastMisalign => UnmanagedSignature(objectType, nativeIntType),

                KnownExternFunction.NewPtrArrayFast => UnmanagedSignature(objectType, nativeIntType, nativeIntType),
                KnownExternFunction.NewArrayFastAlign8 => UnmanagedSignature(objectType, nativeIntType, nativeIntType),
                KnownExternFunction.NewArrayFast => UnmanagedSignature(objectType, nativeIntType, nativeIntType),

                KnownExternFunction.NativeMemSet => UnmanagedSignature(voidPointerType, voidPointerType, int32Type, nativeUIntType),
                KnownExternFunction.DblRem => UnmanagedSignature(doubleType, doubleType, doubleType),
                KnownExternFunction.FltRem => UnmanagedSignature(singleType, singleType, singleType),

                KnownExternFunction.Lng2Dbl => UnmanagedSignature(doubleType, int64Type),
                KnownExternFunction.ULng2Dbl => UnmanagedSignature(doubleType, uint64Type),
                KnownExternFunction.Lng2Flt => UnmanagedSignature(singleType, int64Type),
                KnownExternFunction.ULng2Flt => UnmanagedSignature(singleType, uint64Type),
                KnownExternFunction.Dbl2Lng => UnmanagedSignature(int64Type, doubleType),
                KnownExternFunction.Dbl2ULng => UnmanagedSignature(uint64Type, doubleType),

                KnownExternFunction.LMul => UnmanagedSignature(int64Type, int64Type, int64Type),
                KnownExternFunction.LRsz => UnmanagedSignature(uint64Type, uint64Type, int32Type),
                KnownExternFunction.LRsh => UnmanagedSignature(int64Type, int64Type, int32Type),
                KnownExternFunction.LLsh => UnmanagedSignature(int64Type, int64Type, int32Type),

                KnownExternFunction.PInvokeBegin => UnmanagedSignature(voidType, nativeIntType),
                KnownExternFunction.PInvokeEnd => UnmanagedSignature(voidType, nativeIntType),
                KnownExternFunction.ReversePInvokeEnter => UnmanagedSignature(voidType, nativeIntType),
                KnownExternFunction.ReversePInvokeExit => UnmanagedSignature(voidType, nativeIntType),

                KnownExternFunction.GcPoll => UnmanagedSignature(voidType),

                KnownExternFunction.GVMLookupForSlot => UnmanagedSignature(nativeIntType, objectType, nativeIntType),
                KnownExternFunction.ResolveInterfaceMethod => UnmanagedSignature(nativeIntType, objectType, nativeIntType),
                KnownExternFunction.ResolveInterfaceMethodFast => throw new NotImplementedException(
                    $"{nameof(KnownExternFunction.ResolveInterfaceMethodFast)} is not implemented by the runtime."),

                KnownExternFunction.InterfaceDispatch or
                KnownExternFunction.InterfaceDispatchGuarded => null,

                KnownExternFunction.StackProbe => null,

                KnownExternFunction.NyiLdVirtFtn => null,

                KnownExternFunction.TlsGetAddr => null,

                _ => throw new NotImplementedException(function.ToString())
            };
        }
    }
}
