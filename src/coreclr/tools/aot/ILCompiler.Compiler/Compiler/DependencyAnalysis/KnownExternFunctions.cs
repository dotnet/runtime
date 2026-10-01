// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.ReadyToRunConstants;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// NativeAOT extern names and signatures of the <see cref="ReadyToRunHelper"/> helpers. The helpers are referenced by name
    /// only, so there is no backing <see cref="MethodDesc"/> to source a signature from; the signature (needed to
    /// import the helper on Wasm) is derived here from each helper's declaration.
    /// </summary>
    internal static class KnownExternFunctions
    {
        public static Utf8String GetName(ReadyToRunHelper function, TargetDetails target)
        {
            string name = function switch
            {
                ReadyToRunHelper.Throw => "RhpThrowEx",
                ReadyToRunHelper.Rethrow => "RhpRethrow",
                ReadyToRunHelper.ThrowExact => "RhpThrowExact",
                ReadyToRunHelper.FailFast => "RhpFallbackFailFast",
                ReadyToRunHelper.DebugBreak => "RhDebugBreak",

                ReadyToRunHelper.WriteBarrier => target.Architecture switch
                {
                    TargetArchitecture.ARM64 => "RhpAssignRefArm64",
                    TargetArchitecture.LoongArch64 => "RhpAssignRefLoongArch64",
                    TargetArchitecture.RiscV64 => "RhpAssignRefRiscV64",
                    _ => "RhpAssignRef"
                },
                ReadyToRunHelper.CheckedWriteBarrier =>
                    target.Architecture == TargetArchitecture.ARM64 ? "RhpCheckedAssignRefArm64" : "RhpCheckedAssignRef",
                ReadyToRunHelper.WriteBarrier_EAX => "RhpAssignRefEAX",
                ReadyToRunHelper.WriteBarrier_EBX => "RhpAssignRefEBX",
                ReadyToRunHelper.WriteBarrier_ECX => "RhpAssignRefECX",
                ReadyToRunHelper.WriteBarrier_EDI => "RhpAssignRefEDI",
                ReadyToRunHelper.WriteBarrier_ESI => "RhpAssignRefESI",
                ReadyToRunHelper.WriteBarrier_EBP => "RhpAssignRefEBP",
                ReadyToRunHelper.CheckedWriteBarrier_EAX => "RhpCheckedAssignRefEAX",
                ReadyToRunHelper.CheckedWriteBarrier_EBX => "RhpCheckedAssignRefEBX",
                ReadyToRunHelper.CheckedWriteBarrier_ECX => "RhpCheckedAssignRefECX",
                ReadyToRunHelper.CheckedWriteBarrier_EDI => "RhpCheckedAssignRefEDI",
                ReadyToRunHelper.CheckedWriteBarrier_ESI => "RhpCheckedAssignRefESI",
                ReadyToRunHelper.CheckedWriteBarrier_EBP => "RhpCheckedAssignRefEBP",
                ReadyToRunHelper.BulkWriteBarrierSmall => "RhBulkMoveWithWriteBarrier",

                ReadyToRunHelper.NewArray => "RhNewArray",
                ReadyToRunHelper.NewObject => "RhNewObject",
                ReadyToRunHelper.NewFast => "RhpNewFast",
                ReadyToRunHelper.NewFinalizable => "RhpNewFinalizable",
                ReadyToRunHelper.NewFastAlign8 => "RhpNewFastAlign8",
                ReadyToRunHelper.NewFinalizableAlign8 => "RhpNewFinalizableAlign8",
                ReadyToRunHelper.NewFastMisalign => "RhpNewFastMisalign",
                ReadyToRunHelper.NewPtrArrayFast => "RhpNewPtrArrayFast",
                ReadyToRunHelper.NewArrayFastAlign8 => "RhpNewArrayFastAlign8",
                ReadyToRunHelper.NewArrayFast => "RhpNewArrayFast",

                ReadyToRunHelper.NativeMemSet => "memset",
                ReadyToRunHelper.DblRem => "fmod",
                ReadyToRunHelper.FltRem => "fmodf",

                ReadyToRunHelper.Lng2Dbl => "RhpLng2Dbl",
                ReadyToRunHelper.ULng2Dbl => "RhpULng2Dbl",
                ReadyToRunHelper.Lng2Flt => "RhpLng2Flt",
                ReadyToRunHelper.ULng2Flt => "RhpULng2Flt",
                ReadyToRunHelper.Dbl2Lng => "RhpDbl2Lng",
                ReadyToRunHelper.Dbl2ULng => "RhpDbl2ULng",
                ReadyToRunHelper.LMul => "RhpLMul",
                ReadyToRunHelper.LRsz => "RhpLRsz",
                ReadyToRunHelper.LRsh => "RhpLRsh",
                ReadyToRunHelper.LLsh => "RhpLLsh",

                ReadyToRunHelper.PInvokeBegin => "RhpPInvoke",
                ReadyToRunHelper.PInvokeEnd => "RhpPInvokeReturn",
                ReadyToRunHelper.ReversePInvokeEnter => "RhpReversePInvoke",
                ReadyToRunHelper.ReversePInvokeExit => "RhpReversePInvokeReturn",

                ReadyToRunHelper.GVMLookupForSlot => "RhpDispatchResolve",
                ReadyToRunHelper.InterfaceDispatch => "RhpInterfaceDispatch",
                ReadyToRunHelper.InterfaceDispatchGuarded => "RhpInterfaceDispatchGuarded",
                ReadyToRunHelper.ResolveInterfaceMethodFast => "RhpResolveInterfaceMethodFast",
                ReadyToRunHelper.ResolveInterfaceMethod => "RhpResolveInterfaceMethod",

                ReadyToRunHelper.StackProbe => "RhpStackProbe",
                ReadyToRunHelper.GCPoll => "RhpGcPoll",
                ReadyToRunHelper.NyiLdVirtFtn => "NYI_LDVIRTFTN",
                ReadyToRunHelper.TlsGetAddr => "__tls_get_addr",

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
        public static ExternalTypeSignature? GetTypeSignature(ReadyToRunHelper function, TypeSystemContext context)
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
                ReadyToRunHelper.Throw => UnmanagedSignature(voidType, objectType),
                ReadyToRunHelper.ThrowExact => UnmanagedSignature(voidType, objectType),

                ReadyToRunHelper.Rethrow => UnmanagedSignature(voidType),

                ReadyToRunHelper.FailFast => UnmanagedSignature(voidType),

                ReadyToRunHelper.DebugBreak => UnmanagedSignature(voidType),

                ReadyToRunHelper.WriteBarrier => UnmanagedSignature(voidType, nativeIntType, objectType),
                ReadyToRunHelper.CheckedWriteBarrier => UnmanagedSignature(voidType, nativeIntType, objectType),

                ReadyToRunHelper.WriteBarrier_EAX or
                ReadyToRunHelper.WriteBarrier_EBX or
                ReadyToRunHelper.WriteBarrier_ECX or
                ReadyToRunHelper.WriteBarrier_EDI or
                ReadyToRunHelper.WriteBarrier_ESI or
                ReadyToRunHelper.WriteBarrier_EBP or
                ReadyToRunHelper.CheckedWriteBarrier_EAX or
                ReadyToRunHelper.CheckedWriteBarrier_EBX or
                ReadyToRunHelper.CheckedWriteBarrier_ECX or
                ReadyToRunHelper.CheckedWriteBarrier_EDI or
                ReadyToRunHelper.CheckedWriteBarrier_ESI or
                ReadyToRunHelper.CheckedWriteBarrier_EBP => null,

                ReadyToRunHelper.BulkWriteBarrierSmall => UnmanagedSignature(voidType, nativeIntType, nativeIntType, nativeUIntType),

                ReadyToRunHelper.NewArray => UnmanagedSignature(objectType, nativeIntType, nativeIntType),
                ReadyToRunHelper.NewObject => UnmanagedSignature(objectType, nativeIntType),

                ReadyToRunHelper.NewFast => UnmanagedSignature(objectType, nativeIntType),
                ReadyToRunHelper.NewFinalizable => UnmanagedSignature(objectType, nativeIntType),
                ReadyToRunHelper.NewFastAlign8 => UnmanagedSignature(objectType, nativeIntType),
                ReadyToRunHelper.NewFinalizableAlign8 => UnmanagedSignature(objectType, nativeIntType),
                ReadyToRunHelper.NewFastMisalign => UnmanagedSignature(objectType, nativeIntType),

                ReadyToRunHelper.NewPtrArrayFast => UnmanagedSignature(objectType, nativeIntType, nativeIntType),
                ReadyToRunHelper.NewArrayFastAlign8 => UnmanagedSignature(objectType, nativeIntType, nativeIntType),
                ReadyToRunHelper.NewArrayFast => UnmanagedSignature(objectType, nativeIntType, nativeIntType),

                ReadyToRunHelper.NativeMemSet => UnmanagedSignature(voidPointerType, voidPointerType, int32Type, nativeUIntType),
                ReadyToRunHelper.DblRem => UnmanagedSignature(doubleType, doubleType, doubleType),
                ReadyToRunHelper.FltRem => UnmanagedSignature(singleType, singleType, singleType),

                ReadyToRunHelper.Lng2Dbl => UnmanagedSignature(doubleType, int64Type),
                ReadyToRunHelper.ULng2Dbl => UnmanagedSignature(doubleType, uint64Type),
                ReadyToRunHelper.Lng2Flt => UnmanagedSignature(singleType, int64Type),
                ReadyToRunHelper.ULng2Flt => UnmanagedSignature(singleType, uint64Type),
                ReadyToRunHelper.Dbl2Lng => UnmanagedSignature(int64Type, doubleType),
                ReadyToRunHelper.Dbl2ULng => UnmanagedSignature(uint64Type, doubleType),

                ReadyToRunHelper.LMul => UnmanagedSignature(int64Type, int64Type, int64Type),
                ReadyToRunHelper.LRsz => UnmanagedSignature(uint64Type, uint64Type, int32Type),
                ReadyToRunHelper.LRsh => UnmanagedSignature(int64Type, int64Type, int32Type),
                ReadyToRunHelper.LLsh => UnmanagedSignature(int64Type, int64Type, int32Type),

                ReadyToRunHelper.PInvokeBegin => UnmanagedSignature(voidType, nativeIntType),
                ReadyToRunHelper.PInvokeEnd => UnmanagedSignature(voidType, nativeIntType),
                ReadyToRunHelper.ReversePInvokeEnter => UnmanagedSignature(voidType, nativeIntType),
                ReadyToRunHelper.ReversePInvokeExit => UnmanagedSignature(voidType, nativeIntType),

                ReadyToRunHelper.GCPoll => UnmanagedSignature(voidType),

                ReadyToRunHelper.GVMLookupForSlot => UnmanagedSignature(nativeIntType, objectType, nativeIntType),
                ReadyToRunHelper.ResolveInterfaceMethod => UnmanagedSignature(nativeIntType, objectType, nativeIntType),
                ReadyToRunHelper.ResolveInterfaceMethodFast => throw new NotImplementedException(
                    $"{nameof(ReadyToRunHelper.ResolveInterfaceMethodFast)} is not implemented by the runtime."),

                ReadyToRunHelper.InterfaceDispatch or
                ReadyToRunHelper.InterfaceDispatchGuarded => null,

                ReadyToRunHelper.StackProbe => null,

                ReadyToRunHelper.NyiLdVirtFtn => null,

                ReadyToRunHelper.TlsGetAddr => null,

                _ => throw new NotImplementedException(function.ToString())
            };
        }
    }
}
