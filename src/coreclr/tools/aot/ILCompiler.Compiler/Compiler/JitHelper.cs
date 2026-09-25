// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.TypeSystem;
using Internal.IL;
using Internal.ReadyToRunConstants;

using ILCompiler.DependencyAnalysis;

namespace ILCompiler
{
    internal static class JitHelper
    {
        /// <summary>
        /// Returns JIT helper entrypoint. JIT helpers can be either implemented by entrypoint with given mangled name or
        /// by a method in class library.
        /// </summary>
        public static void GetEntryPoint(TypeSystemContext context, ReadyToRunHelper id, out KnownExternFunction? knownFunction, out MethodDesc methodDesc)
        {
            knownFunction = null;
            methodDesc = null;

            switch (id)
            {
                case ReadyToRunHelper.Throw:
                    knownFunction = KnownExternFunction.ThrowEx;
                    break;
                case ReadyToRunHelper.Rethrow:
                    knownFunction = KnownExternFunction.Rethrow;
                    break;
                case ReadyToRunHelper.ThrowExact:
                    knownFunction = KnownExternFunction.ThrowExact;
                    break;

                case ReadyToRunHelper.Overflow:
                    methodDesc = context.GetHelperEntryPoint("ThrowHelpers"u8, "ThrowOverflowException"u8);
                    break;
                case ReadyToRunHelper.RngChkFail:
                    methodDesc = context.GetHelperEntryPoint("ThrowHelpers"u8, "ThrowIndexOutOfRangeException"u8);
                    break;
                case ReadyToRunHelper.FailFast:
                    knownFunction = KnownExternFunction.FallbackFailFast; // TODO: Report stack buffer overrun
                    break;
                case ReadyToRunHelper.ThrowNullRef:
                    methodDesc = context.GetHelperEntryPoint("ThrowHelpers"u8, "ThrowNullReferenceException"u8);
                    break;
                case ReadyToRunHelper.ThrowDivZero:
                    methodDesc = context.GetHelperEntryPoint("ThrowHelpers"u8, "ThrowDivideByZeroException"u8);
                    break;
                case ReadyToRunHelper.ThrowArgumentOutOfRange:
                    methodDesc = context.GetHelperEntryPoint("ThrowHelpers"u8, "ThrowArgumentOutOfRangeException"u8);
                    break;
                case ReadyToRunHelper.ThrowArgument:
                    methodDesc = context.GetHelperEntryPoint("ThrowHelpers"u8, "ThrowArgumentException"u8);
                    break;
                case ReadyToRunHelper.ThrowPlatformNotSupported:
                    methodDesc = context.GetHelperEntryPoint("ThrowHelpers"u8, "ThrowPlatformNotSupportedException"u8);
                    break;
                case ReadyToRunHelper.ThrowNotImplemented:
                    methodDesc = context.GetHelperEntryPoint("ThrowHelpers"u8, "ThrowNotImplementedException"u8);
                    break;

                case ReadyToRunHelper.DebugBreak:
                    knownFunction = KnownExternFunction.DebugBreak;
                    break;

                case ReadyToRunHelper.WriteBarrier:
                    knownFunction = KnownExternFunction.WriteBarrier;
                    break;
                case ReadyToRunHelper.CheckedWriteBarrier:
                    knownFunction = KnownExternFunction.CheckedWriteBarrier;
                    break;
                case ReadyToRunHelper.BulkWriteBarrier:
                    methodDesc = context.GetCoreLibEntryPoint("System"u8, "Buffer"u8, "BulkMoveWithWriteBarrier"u8, null);
                    break;
                case ReadyToRunHelper.BulkWriteBarrierSmall:
                    knownFunction = KnownExternFunction.BulkMoveWithWriteBarrier;
                    break;
                case ReadyToRunHelper.WriteBarrier_EAX:
                    knownFunction = KnownExternFunction.WriteBarrier_EAX;
                    break;
                case ReadyToRunHelper.WriteBarrier_EBX:
                    knownFunction = KnownExternFunction.WriteBarrier_EBX;
                    break;
                case ReadyToRunHelper.WriteBarrier_ECX:
                    knownFunction = KnownExternFunction.WriteBarrier_ECX;
                    break;
                case ReadyToRunHelper.WriteBarrier_EDI:
                    knownFunction = KnownExternFunction.WriteBarrier_EDI;
                    break;
                case ReadyToRunHelper.WriteBarrier_ESI:
                    knownFunction = KnownExternFunction.WriteBarrier_ESI;
                    break;
                case ReadyToRunHelper.WriteBarrier_EBP:
                    knownFunction = KnownExternFunction.WriteBarrier_EBP;
                    break;
                case ReadyToRunHelper.CheckedWriteBarrier_EAX:
                    knownFunction = KnownExternFunction.CheckedWriteBarrier_EAX;
                    break;
                case ReadyToRunHelper.CheckedWriteBarrier_EBX:
                    knownFunction = KnownExternFunction.CheckedWriteBarrier_EBX;
                    break;
                case ReadyToRunHelper.CheckedWriteBarrier_ECX:
                    knownFunction = KnownExternFunction.CheckedWriteBarrier_ECX;
                    break;
                case ReadyToRunHelper.CheckedWriteBarrier_EDI:
                    knownFunction = KnownExternFunction.CheckedWriteBarrier_EDI;
                    break;
                case ReadyToRunHelper.CheckedWriteBarrier_ESI:
                    knownFunction = KnownExternFunction.CheckedWriteBarrier_ESI;
                    break;
                case ReadyToRunHelper.CheckedWriteBarrier_EBP:
                    knownFunction = KnownExternFunction.CheckedWriteBarrier_EBP;
                    break;
                case ReadyToRunHelper.Box:
                case ReadyToRunHelper.Box_Nullable:
                    methodDesc = context.GetCoreLibEntryPoint("System.Runtime"u8, "RuntimeExports"u8, "RhBox"u8, null);
                    break;
                case ReadyToRunHelper.Unbox:
                    methodDesc = context.GetCoreLibEntryPoint("System.Runtime"u8, "RuntimeExports"u8, "RhUnbox2"u8, null);
                    break;
                case ReadyToRunHelper.Unbox_Nullable:
                    methodDesc = context.GetCoreLibEntryPoint("System.Runtime"u8, "RuntimeExports"u8, "RhUnboxNullable"u8, null);
                    break;
                case ReadyToRunHelper.Unbox_TypeTest:
                    methodDesc = context.GetCoreLibEntryPoint("System.Runtime"u8, "RuntimeExports"u8, "RhUnboxTypeTest"u8, null);
                    break;

                case ReadyToRunHelper.NewMultiDimArr:
                    methodDesc = context.GetCoreLibEntryPoint("System"u8, "Array"u8, "Ctor"u8, null);
                    break;
                case ReadyToRunHelper.NewMultiDimArrRare:
                    methodDesc = context.GetCoreLibEntryPoint("System"u8, "Array"u8, "CtorRare"u8, null);
                    break;

                case ReadyToRunHelper.NewArray:
                    knownFunction = KnownExternFunction.NewArray;
                    break;
                case ReadyToRunHelper.NewObject:
                    knownFunction = KnownExternFunction.NewObject;
                    break;

                case ReadyToRunHelper.Stelem_Ref:
                    methodDesc = context.GetCoreLibEntryPoint("System.Runtime"u8, "TypeCast"u8, "StelemRef"u8, null);
                    break;
                case ReadyToRunHelper.Ldelema_Ref:
                    methodDesc = context.GetCoreLibEntryPoint("System.Runtime"u8, "TypeCast"u8, "LdelemaRef"u8, null);
                    break;

                case ReadyToRunHelper.MemCpy:
                    methodDesc = context.GetCoreLibEntryPoint("System"u8, "SpanHelpers"u8, "Memmove"u8, null);
                    break;
                case ReadyToRunHelper.MemSet:
                    methodDesc = context.GetCoreLibEntryPoint("System"u8, "SpanHelpers"u8, "Fill"u8, null);
                    break;
                case ReadyToRunHelper.MemZero:
                    methodDesc = context.GetCoreLibEntryPoint("System"u8, "SpanHelpers"u8, "ClearWithoutReferences"u8, null);
                    break;
                case ReadyToRunHelper.NativeMemSet:
                    knownFunction = KnownExternFunction.NativeMemSet;
                    break;

                case ReadyToRunHelper.GetRuntimeTypeHandle:
                    methodDesc = context.GetCoreLibEntryPoint("System"u8, "RuntimeTypeHandle"u8, "GetRuntimeTypeHandleFromMethodTable"u8, null);
                    break;
                case ReadyToRunHelper.GetRuntimeType:
                    methodDesc = context.GetCoreLibEntryPoint("System"u8, "Type"u8, "GetTypeFromMethodTable"u8, null);
                    break;
                case ReadyToRunHelper.GetRuntimeMethodHandle:
                    methodDesc = context.GetHelperEntryPoint("LdTokenHelpers"u8, "GetRuntimeMethodHandle"u8);
                    break;
                case ReadyToRunHelper.GetRuntimeFieldHandle:
                    methodDesc = context.GetHelperEntryPoint("LdTokenHelpers"u8, "GetRuntimeFieldHandle"u8);
                    break;

                case ReadyToRunHelper.Lng2Dbl:
                    knownFunction = KnownExternFunction.Lng2Dbl;
                    break;
                case ReadyToRunHelper.ULng2Dbl:
                    knownFunction = KnownExternFunction.ULng2Dbl;
                    break;
                case ReadyToRunHelper.Lng2Flt:
                    knownFunction = KnownExternFunction.Lng2Flt;
                    break;
                case ReadyToRunHelper.ULng2Flt:
                    knownFunction = KnownExternFunction.ULng2Flt;
                    break;

                case ReadyToRunHelper.Dbl2Lng:
                    knownFunction = KnownExternFunction.Dbl2Lng;
                    break;
                case ReadyToRunHelper.Dbl2ULng:
                    knownFunction = KnownExternFunction.Dbl2ULng;
                    break;

                case ReadyToRunHelper.Dbl2IntOvf:
                    methodDesc = context.SystemModule.GetKnownType("System"u8, "Math"u8).GetKnownMethod("ConvertToInt32Checked"u8, null);
                    break;
                case ReadyToRunHelper.Dbl2UIntOvf:
                    methodDesc = context.SystemModule.GetKnownType("System"u8, "Math"u8).GetKnownMethod("ConvertToUInt32Checked"u8, null);
                    break;
                case ReadyToRunHelper.Dbl2LngOvf:
                    methodDesc = context.SystemModule.GetKnownType("System"u8, "Math"u8).GetKnownMethod("ConvertToInt64Checked"u8, null);
                    break;
                case ReadyToRunHelper.Dbl2ULngOvf:
                    methodDesc = context.SystemModule.GetKnownType("System"u8, "Math"u8).GetKnownMethod("ConvertToUInt64Checked"u8, null);
                    break;

                case ReadyToRunHelper.DblRem:
                    knownFunction = KnownExternFunction.DblRem;
                    break;
                case ReadyToRunHelper.FltRem:
                    knownFunction = KnownExternFunction.FltRem;
                    break;

                case ReadyToRunHelper.LMul:
                    knownFunction = KnownExternFunction.LMul;
                    break;
                case ReadyToRunHelper.LMulOfv:
                    {
                        TypeDesc t = context.GetWellKnownType(WellKnownType.Int64);
                        methodDesc = context.SystemModule.GetKnownType("System"u8, "Math"u8).GetKnownMethod("MultiplyChecked"u8,
                            new MethodSignature(MethodSignatureFlags.Static, 0, t, [t, t]));
                    }
                    break;
                case ReadyToRunHelper.ULMulOvf:
                    {
                        TypeDesc t = context.GetWellKnownType(WellKnownType.UInt64);
                        methodDesc = context.SystemModule.GetKnownType("System"u8, "Math"u8).GetKnownMethod("MultiplyChecked"u8,
                            new MethodSignature(MethodSignatureFlags.Static, 0, t, [t, t]));
                    }
                    break;

                case ReadyToRunHelper.Div:
                    methodDesc = context.SystemModule.GetKnownType("System"u8, "Math"u8).GetKnownMethod("DivInt32"u8, null);
                    break;
                case ReadyToRunHelper.UDiv:
                    methodDesc = context.SystemModule.GetKnownType("System"u8, "Math"u8).GetKnownMethod("DivUInt32"u8, null);
                    break;
                case ReadyToRunHelper.LDiv:
                    methodDesc = context.SystemModule.GetKnownType("System"u8, "Math"u8).GetKnownMethod("DivInt64"u8, null);
                    break;
                case ReadyToRunHelper.ULDiv:
                    methodDesc = context.SystemModule.GetKnownType("System"u8, "Math"u8).GetKnownMethod("DivUInt64"u8, null);
                    break;

                case ReadyToRunHelper.Mod:
                    methodDesc = context.SystemModule.GetKnownType("System"u8, "Math"u8).GetKnownMethod("ModInt32"u8, null);
                    break;
                case ReadyToRunHelper.UMod:
                    methodDesc = context.SystemModule.GetKnownType("System"u8, "Math"u8).GetKnownMethod("ModUInt32"u8, null);
                    break;
                case ReadyToRunHelper.LMod:
                    methodDesc = context.SystemModule.GetKnownType("System"u8, "Math"u8).GetKnownMethod("ModInt64"u8, null);
                    break;
                case ReadyToRunHelper.ULMod:
                    methodDesc = context.SystemModule.GetKnownType("System"u8, "Math"u8).GetKnownMethod("ModUInt64"u8, null);
                    break;

                case ReadyToRunHelper.LRsz:
                    knownFunction = KnownExternFunction.LRsz;
                    break;
                case ReadyToRunHelper.LRsh:
                    knownFunction = KnownExternFunction.LRsh;
                    break;
                case ReadyToRunHelper.LLsh:
                    knownFunction = KnownExternFunction.LLsh;
                    break;

                case ReadyToRunHelper.PInvokeBegin:
                    knownFunction = KnownExternFunction.PInvokeBegin;
                    break;
                case ReadyToRunHelper.PInvokeEnd:
                    knownFunction = KnownExternFunction.PInvokeEnd;
                    break;

                case ReadyToRunHelper.ReversePInvokeEnter:
                    knownFunction = KnownExternFunction.ReversePInvokeEnter;
                    break;
                case ReadyToRunHelper.ReversePInvokeExit:
                    knownFunction = KnownExternFunction.ReversePInvokeExit;
                    break;

                case ReadyToRunHelper.CheckCastAny:
                    methodDesc = context.GetCoreLibEntryPoint("System.Runtime"u8, "TypeCast"u8, "CheckCastAny"u8, null);
                    break;
                case ReadyToRunHelper.CheckCastInterface:
                    methodDesc = context.GetCoreLibEntryPoint("System.Runtime"u8, "TypeCast"u8, "CheckCastInterface"u8, null);
                    break;
                case ReadyToRunHelper.CheckCastClass:
                    methodDesc = context.GetCoreLibEntryPoint("System.Runtime"u8, "TypeCast"u8, "CheckCastClass"u8, null);
                    break;
                case ReadyToRunHelper.CheckCastClassSpecial:
                    methodDesc = context.GetCoreLibEntryPoint("System.Runtime"u8, "TypeCast"u8, "CheckCastClassSpecial"u8, null);
                    break;

                case ReadyToRunHelper.CheckInstanceAny:
                    methodDesc = context.GetCoreLibEntryPoint("System.Runtime"u8, "TypeCast"u8, "IsInstanceOfAny"u8, null);
                    break;
                case ReadyToRunHelper.CheckInstanceInterface:
                    methodDesc = context.GetCoreLibEntryPoint("System.Runtime"u8, "TypeCast"u8, "IsInstanceOfInterface"u8, null);
                    break;
                case ReadyToRunHelper.CheckInstanceClass:
                    methodDesc = context.GetCoreLibEntryPoint("System.Runtime"u8, "TypeCast"u8, "IsInstanceOfClass"u8, null);
                    break;
                case ReadyToRunHelper.IsInstanceOfException:
                    methodDesc = context.GetCoreLibEntryPoint("System.Runtime"u8, "TypeCast"u8, "IsInstanceOfException"u8, null);
                    break;

                case ReadyToRunHelper.MonitorEnter:
                    methodDesc = context.GetCoreLibEntryPoint("System.Threading"u8, "Monitor"u8, "SynchronizedMethodEnter"u8, null);
                    break;
                case ReadyToRunHelper.MonitorExit:
                    methodDesc = context.GetCoreLibEntryPoint("System.Threading"u8, "Monitor"u8, "SynchronizedMethodExit"u8, null);
                    break;

                case ReadyToRunHelper.GVMLookupForSlot:
                    knownFunction = KnownExternFunction.GVMLookupForSlot;
                    break;

                case ReadyToRunHelper.TypeHandleToRuntimeType:
                    methodDesc = context.GetCoreLibEntryPoint("System"u8, "Type"u8, "GetTypeFromMethodTableMaybeNull"u8, null);
                    break;
                case ReadyToRunHelper.GetRefAny:
                    methodDesc = context.GetCoreLibEntryPoint("System"u8, "TypedReference"u8, "GetRefAny"u8, null);
                    break;
                case ReadyToRunHelper.TypeHandleToRuntimeTypeHandle:
                    methodDesc = context.GetCoreLibEntryPoint("System"u8, "RuntimeTypeHandle"u8, "GetRuntimeTypeHandleFromMethodTable"u8, null);
                    break;

                case ReadyToRunHelper.GetCurrentManagedThreadId:
                    methodDesc = context.SystemModule.GetKnownType("System"u8, "Environment"u8).GetKnownMethod("get_CurrentManagedThreadId"u8, null);
                    break;

                case ReadyToRunHelper.AllocContinuation:
                    methodDesc = context.GetCoreLibEntryPoint("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8, "AllocContinuation"u8, null);
                    break;

                default:
                    throw new NotImplementedException(id.ToString());
            }
        }

        //
        // These methods are static compiler equivalent of RhGetRuntimeHelperForType
        //
        public static KnownExternFunction GetNewObjectHelperForType(TypeDesc type)
        {
            if (type.RequiresAlign8())
            {
                if (type.HasFinalizer)
                    return KnownExternFunction.NewFinalizableAlign8;

                if (type.IsValueType)
                    return KnownExternFunction.NewFastMisalign;

                return KnownExternFunction.NewFastAlign8;
            }

            if (type.HasFinalizer)
                return KnownExternFunction.NewFinalizable;

            return KnownExternFunction.NewFast;
        }
    }
}
