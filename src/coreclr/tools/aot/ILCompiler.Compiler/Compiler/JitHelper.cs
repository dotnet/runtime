// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.TypeSystem;
using Internal.IL;
using Internal.ReadyToRunConstants;

namespace ILCompiler
{
    internal static class JitHelper
    {
        /// <summary>
        /// Returns the managed JIT helper entrypoint, or null if the helper is implemented by a native extern function.
        /// </summary>
        public static MethodDesc GetEntryPoint(TypeSystemContext context, ReadyToRunHelper id)
        {
            switch (id)
            {
                case ReadyToRunHelper.Throw:
                case ReadyToRunHelper.Rethrow:
                case ReadyToRunHelper.ThrowExact:
                case ReadyToRunHelper.FailFast: // TODO: Report stack buffer overrun
                case ReadyToRunHelper.DebugBreak:
                case ReadyToRunHelper.WriteBarrier:
                case ReadyToRunHelper.CheckedWriteBarrier:
                case ReadyToRunHelper.BulkWriteBarrierSmall:
                case ReadyToRunHelper.WriteBarrier_EAX:
                case ReadyToRunHelper.WriteBarrier_EBX:
                case ReadyToRunHelper.WriteBarrier_ECX:
                case ReadyToRunHelper.WriteBarrier_EDI:
                case ReadyToRunHelper.WriteBarrier_ESI:
                case ReadyToRunHelper.WriteBarrier_EBP:
                case ReadyToRunHelper.CheckedWriteBarrier_EAX:
                case ReadyToRunHelper.CheckedWriteBarrier_EBX:
                case ReadyToRunHelper.CheckedWriteBarrier_ECX:
                case ReadyToRunHelper.CheckedWriteBarrier_EDI:
                case ReadyToRunHelper.CheckedWriteBarrier_ESI:
                case ReadyToRunHelper.CheckedWriteBarrier_EBP:
                case ReadyToRunHelper.NewArray:
                case ReadyToRunHelper.NewObject:
                case ReadyToRunHelper.NativeMemSet:
                case ReadyToRunHelper.Lng2Dbl:
                case ReadyToRunHelper.ULng2Dbl:
                case ReadyToRunHelper.Lng2Flt:
                case ReadyToRunHelper.ULng2Flt:
                case ReadyToRunHelper.Dbl2Lng:
                case ReadyToRunHelper.Dbl2ULng:
                case ReadyToRunHelper.DblRem:
                case ReadyToRunHelper.FltRem:
                case ReadyToRunHelper.LMul:
                case ReadyToRunHelper.LRsz:
                case ReadyToRunHelper.LRsh:
                case ReadyToRunHelper.LLsh:
                case ReadyToRunHelper.PInvokeBegin:
                case ReadyToRunHelper.PInvokeEnd:
                case ReadyToRunHelper.ReversePInvokeEnter:
                case ReadyToRunHelper.ReversePInvokeExit:
                case ReadyToRunHelper.GVMLookupForSlot:
                    return null;

                case ReadyToRunHelper.Overflow:
                    return context.GetHelperEntryPoint("ThrowHelpers"u8, "ThrowOverflowException"u8);
                case ReadyToRunHelper.RngChkFail:
                    return context.GetHelperEntryPoint("ThrowHelpers"u8, "ThrowIndexOutOfRangeException"u8);
                case ReadyToRunHelper.ThrowNullRef:
                    return context.GetHelperEntryPoint("ThrowHelpers"u8, "ThrowNullReferenceException"u8);
                case ReadyToRunHelper.ThrowDivZero:
                    return context.GetHelperEntryPoint("ThrowHelpers"u8, "ThrowDivideByZeroException"u8);
                case ReadyToRunHelper.ThrowArgumentOutOfRange:
                    return context.GetHelperEntryPoint("ThrowHelpers"u8, "ThrowArgumentOutOfRangeException"u8);
                case ReadyToRunHelper.ThrowArgument:
                    return context.GetHelperEntryPoint("ThrowHelpers"u8, "ThrowArgumentException"u8);
                case ReadyToRunHelper.ThrowPlatformNotSupported:
                    return context.GetHelperEntryPoint("ThrowHelpers"u8, "ThrowPlatformNotSupportedException"u8);
                case ReadyToRunHelper.ThrowNotImplemented:
                    return context.GetHelperEntryPoint("ThrowHelpers"u8, "ThrowNotImplementedException"u8);

                case ReadyToRunHelper.BulkWriteBarrier:
                    return context.GetCoreLibEntryPoint("System"u8, "Buffer"u8, "BulkMoveWithWriteBarrier"u8, null);
                case ReadyToRunHelper.Box:
                case ReadyToRunHelper.Box_Nullable:
                    return context.GetCoreLibEntryPoint("System.Runtime"u8, "RuntimeExports"u8, "RhBox"u8, null);
                case ReadyToRunHelper.Unbox:
                    return context.GetCoreLibEntryPoint("System.Runtime"u8, "RuntimeExports"u8, "RhUnbox2"u8, null);
                case ReadyToRunHelper.Unbox_Nullable:
                    return context.GetCoreLibEntryPoint("System.Runtime"u8, "RuntimeExports"u8, "RhUnboxNullable"u8, null);
                case ReadyToRunHelper.Unbox_TypeTest:
                    return context.GetCoreLibEntryPoint("System.Runtime"u8, "RuntimeExports"u8, "RhUnboxTypeTest"u8, null);

                case ReadyToRunHelper.NewMultiDimArr:
                    return context.GetCoreLibEntryPoint("System"u8, "Array"u8, "Ctor"u8, null);
                case ReadyToRunHelper.NewMultiDimArrRare:
                    return context.GetCoreLibEntryPoint("System"u8, "Array"u8, "CtorRare"u8, null);

                case ReadyToRunHelper.Stelem_Ref:
                    return context.GetCoreLibEntryPoint("System.Runtime"u8, "TypeCast"u8, "StelemRef"u8, null);
                case ReadyToRunHelper.Ldelema_Ref:
                    return context.GetCoreLibEntryPoint("System.Runtime"u8, "TypeCast"u8, "LdelemaRef"u8, null);

                case ReadyToRunHelper.MemCpy:
                    return context.GetCoreLibEntryPoint("System"u8, "SpanHelpers"u8, "Memmove"u8, null);
                case ReadyToRunHelper.MemSet:
                    return context.GetCoreLibEntryPoint("System"u8, "SpanHelpers"u8, "Fill"u8, null);
                case ReadyToRunHelper.MemZero:
                    return context.GetCoreLibEntryPoint("System"u8, "SpanHelpers"u8, "ClearWithoutReferences"u8, null);

                case ReadyToRunHelper.GetRuntimeTypeHandle:
                    return context.GetCoreLibEntryPoint("System"u8, "RuntimeTypeHandle"u8, "GetRuntimeTypeHandleFromMethodTable"u8, null);
                case ReadyToRunHelper.GetRuntimeType:
                    return context.GetCoreLibEntryPoint("System"u8, "Type"u8, "GetTypeFromMethodTable"u8, null);
                case ReadyToRunHelper.GetRuntimeMethodHandle:
                    return context.GetHelperEntryPoint("LdTokenHelpers"u8, "GetRuntimeMethodHandle"u8);
                case ReadyToRunHelper.GetRuntimeFieldHandle:
                    return context.GetHelperEntryPoint("LdTokenHelpers"u8, "GetRuntimeFieldHandle"u8);

                case ReadyToRunHelper.Dbl2IntOvf:
                    return context.SystemModule.GetKnownType("System"u8, "Math"u8).GetKnownMethod("ConvertToInt32Checked"u8, null);
                case ReadyToRunHelper.Dbl2UIntOvf:
                    return context.SystemModule.GetKnownType("System"u8, "Math"u8).GetKnownMethod("ConvertToUInt32Checked"u8, null);
                case ReadyToRunHelper.Dbl2LngOvf:
                    return context.SystemModule.GetKnownType("System"u8, "Math"u8).GetKnownMethod("ConvertToInt64Checked"u8, null);
                case ReadyToRunHelper.Dbl2ULngOvf:
                    return context.SystemModule.GetKnownType("System"u8, "Math"u8).GetKnownMethod("ConvertToUInt64Checked"u8, null);

                case ReadyToRunHelper.LMulOfv:
                    {
                        TypeDesc t = context.GetWellKnownType(WellKnownType.Int64);
                        return context.SystemModule.GetKnownType("System"u8, "Math"u8).GetKnownMethod("MultiplyChecked"u8,
                            new MethodSignature(MethodSignatureFlags.Static, 0, t, [t, t]));
                    }
                case ReadyToRunHelper.ULMulOvf:
                    {
                        TypeDesc t = context.GetWellKnownType(WellKnownType.UInt64);
                        return context.SystemModule.GetKnownType("System"u8, "Math"u8).GetKnownMethod("MultiplyChecked"u8,
                            new MethodSignature(MethodSignatureFlags.Static, 0, t, [t, t]));
                    }

                case ReadyToRunHelper.Div:
                    return context.SystemModule.GetKnownType("System"u8, "Math"u8).GetKnownMethod("DivInt32"u8, null);
                case ReadyToRunHelper.UDiv:
                    return context.SystemModule.GetKnownType("System"u8, "Math"u8).GetKnownMethod("DivUInt32"u8, null);
                case ReadyToRunHelper.LDiv:
                    return context.SystemModule.GetKnownType("System"u8, "Math"u8).GetKnownMethod("DivInt64"u8, null);
                case ReadyToRunHelper.ULDiv:
                    return context.SystemModule.GetKnownType("System"u8, "Math"u8).GetKnownMethod("DivUInt64"u8, null);

                case ReadyToRunHelper.Mod:
                    return context.SystemModule.GetKnownType("System"u8, "Math"u8).GetKnownMethod("ModInt32"u8, null);
                case ReadyToRunHelper.UMod:
                    return context.SystemModule.GetKnownType("System"u8, "Math"u8).GetKnownMethod("ModUInt32"u8, null);
                case ReadyToRunHelper.LMod:
                    return context.SystemModule.GetKnownType("System"u8, "Math"u8).GetKnownMethod("ModInt64"u8, null);
                case ReadyToRunHelper.ULMod:
                    return context.SystemModule.GetKnownType("System"u8, "Math"u8).GetKnownMethod("ModUInt64"u8, null);

                case ReadyToRunHelper.CheckCastAny:
                    return context.GetCoreLibEntryPoint("System.Runtime"u8, "TypeCast"u8, "CheckCastAny"u8, null);
                case ReadyToRunHelper.CheckCastInterface:
                    return context.GetCoreLibEntryPoint("System.Runtime"u8, "TypeCast"u8, "CheckCastInterface"u8, null);
                case ReadyToRunHelper.CheckCastClass:
                    return context.GetCoreLibEntryPoint("System.Runtime"u8, "TypeCast"u8, "CheckCastClass"u8, null);
                case ReadyToRunHelper.CheckCastClassSpecial:
                    return context.GetCoreLibEntryPoint("System.Runtime"u8, "TypeCast"u8, "CheckCastClassSpecial"u8, null);

                case ReadyToRunHelper.CheckInstanceAny:
                    return context.GetCoreLibEntryPoint("System.Runtime"u8, "TypeCast"u8, "IsInstanceOfAny"u8, null);
                case ReadyToRunHelper.CheckInstanceInterface:
                    return context.GetCoreLibEntryPoint("System.Runtime"u8, "TypeCast"u8, "IsInstanceOfInterface"u8, null);
                case ReadyToRunHelper.CheckInstanceClass:
                    return context.GetCoreLibEntryPoint("System.Runtime"u8, "TypeCast"u8, "IsInstanceOfClass"u8, null);
                case ReadyToRunHelper.IsInstanceOfException:
                    return context.GetCoreLibEntryPoint("System.Runtime"u8, "TypeCast"u8, "IsInstanceOfException"u8, null);

                case ReadyToRunHelper.MonitorEnter:
                    return context.GetCoreLibEntryPoint("System.Threading"u8, "Monitor"u8, "SynchronizedMethodEnter"u8, null);
                case ReadyToRunHelper.MonitorExit:
                    return context.GetCoreLibEntryPoint("System.Threading"u8, "Monitor"u8, "SynchronizedMethodExit"u8, null);

                case ReadyToRunHelper.TypeHandleToRuntimeType:
                    return context.GetCoreLibEntryPoint("System"u8, "Type"u8, "GetTypeFromMethodTableMaybeNull"u8, null);
                case ReadyToRunHelper.GetRefAny:
                    return context.GetCoreLibEntryPoint("System"u8, "TypedReference"u8, "GetRefAny"u8, null);
                case ReadyToRunHelper.TypeHandleToRuntimeTypeHandle:
                    return context.GetCoreLibEntryPoint("System"u8, "RuntimeTypeHandle"u8, "GetRuntimeTypeHandleFromMethodTable"u8, null);

                case ReadyToRunHelper.GetCurrentManagedThreadId:
                    return context.SystemModule.GetKnownType("System"u8, "Environment"u8).GetKnownMethod("get_CurrentManagedThreadId"u8, null);

                case ReadyToRunHelper.AllocContinuation:
                    return context.GetCoreLibEntryPoint("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8, "AllocContinuation"u8, null);

                default:
                    throw new NotImplementedException(id.ToString());
            }
        }

        //
        // These methods are static compiler equivalent of RhGetRuntimeHelperForType
        //
        public static ReadyToRunHelper GetNewObjectHelperForType(TypeDesc type)
        {
            if (type.RequiresAlign8())
            {
                if (type.HasFinalizer)
                    return ReadyToRunHelper.NewFinalizableAlign8;

                if (type.IsValueType)
                    return ReadyToRunHelper.NewFastMisalign;

                return ReadyToRunHelper.NewFastAlign8;
            }

            if (type.HasFinalizer)
                return ReadyToRunHelper.NewFinalizable;

            return ReadyToRunHelper.NewFast;
        }
    }
}
