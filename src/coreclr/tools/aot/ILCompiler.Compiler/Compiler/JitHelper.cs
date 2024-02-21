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
        /// Returns JIT helper entrypoint. JIT helpers can be either implemented by entrypoint with given mangled name or
        /// by a method in class library.
        /// </summary>
        public static void GetEntryPoint(TypeSystemContext context, ReadyToRunHelper id, out string mangledName, out MethodDesc methodDesc)
        {
            mangledName = null;
            methodDesc = null;

            switch (id)
            {
                case ReadyToRunHelper.Throw:
                    mangledName = "RhpThrowEx";
                    break;
                case ReadyToRunHelper.Rethrow:
                    mangledName = "RhpRethrow";
                    break;

                case ReadyToRunHelper.Overflow:
                    methodDesc = context.GetHelperEntryPoint("ThrowHelpers", "ThrowOverflowException");
                    break;
                case ReadyToRunHelper.RngChkFail:
                    methodDesc = context.GetHelperEntryPoint("ThrowHelpers", "ThrowIndexOutOfRangeException");
                    break;
                case ReadyToRunHelper.FailFast:
                    mangledName = "RhpFallbackFailFast"; // TODO: Report stack buffer overrun
                    break;
                case ReadyToRunHelper.ThrowNullRef:
                    methodDesc = context.GetHelperEntryPoint("ThrowHelpers", "ThrowNullReferenceException");
                    break;
                case ReadyToRunHelper.ThrowDivZero:
                    methodDesc = context.GetHelperEntryPoint("ThrowHelpers", "ThrowDivideByZeroException");
                    break;
                case ReadyToRunHelper.ThrowArgumentOutOfRange:
                    methodDesc = context.GetHelperEntryPoint("ThrowHelpers", "ThrowArgumentOutOfRangeException");
                    break;
                case ReadyToRunHelper.ThrowArgument:
                    methodDesc = context.GetHelperEntryPoint("ThrowHelpers", "ThrowArgumentException");
                    break;
                case ReadyToRunHelper.ThrowPlatformNotSupported:
                    methodDesc = context.GetHelperEntryPoint("ThrowHelpers", "ThrowPlatformNotSupportedException");
                    break;
                case ReadyToRunHelper.ThrowNotImplemented:
                    methodDesc = context.GetHelperEntryPoint("ThrowHelpers", "ThrowNotImplementedException");
                    break;

                case ReadyToRunHelper.DebugBreak:
                    mangledName = "RhDebugBreak";
                    break;

                case ReadyToRunHelper.WriteBarrier:
                    mangledName = context.Target.Architecture == TargetArchitecture.ARM64 ? "RhpAssignRefArm64" : "RhpAssignRef";
                    break;
                case ReadyToRunHelper.CheckedWriteBarrier:
                    mangledName = context.Target.Architecture == TargetArchitecture.ARM64 ? "RhpCheckedAssignRefArm64" : "RhpCheckedAssignRef";
                    break;
                case ReadyToRunHelper.ByRefWriteBarrier:
                    mangledName = context.Target.Architecture == TargetArchitecture.ARM64 ? "RhpByRefAssignRefArm64" : "RhpByRefAssignRef";
                    break;
                case ReadyToRunHelper.WriteBarrier_EAX:
                    mangledName = "RhpAssignRefEAX";
                    break;
                case ReadyToRunHelper.WriteBarrier_EBX:
                    mangledName = "RhpAssignRefEBX";
                    break;
                case ReadyToRunHelper.WriteBarrier_ECX:
                    mangledName = "RhpAssignRefECX";
                    break;
                case ReadyToRunHelper.WriteBarrier_EDI:
                    mangledName = "RhpAssignRefEDI";
                    break;
                case ReadyToRunHelper.WriteBarrier_ESI:
                    mangledName = "RhpAssignRefESI";
                    break;
                case ReadyToRunHelper.WriteBarrier_EBP:
                    mangledName = "RhpAssignRefEBP";
                    break;
                case ReadyToRunHelper.CheckedWriteBarrier_EAX:
                    mangledName = "RhpCheckedAssignRefEAX";
                    break;
                case ReadyToRunHelper.CheckedWriteBarrier_EBX:
                    mangledName = "RhpCheckedAssignRefEBX";
                    break;
                case ReadyToRunHelper.CheckedWriteBarrier_ECX:
                    mangledName = "RhpCheckedAssignRefECX";
                    break;
                case ReadyToRunHelper.CheckedWriteBarrier_EDI:
                    mangledName = "RhpCheckedAssignRefEDI";
                    break;
                case ReadyToRunHelper.CheckedWriteBarrier_ESI:
                    mangledName = "RhpCheckedAssignRefESI";
                    break;
                case ReadyToRunHelper.CheckedWriteBarrier_EBP:
                    mangledName = "RhpCheckedAssignRefEBP";
                    break;
                case ReadyToRunHelper.Box:
                case ReadyToRunHelper.Box_Nullable:
                    mangledName = "RhBox";
                    break;
                case ReadyToRunHelper.Unbox:
                    mangledName = "RhUnbox2";
                    break;
                case ReadyToRunHelper.Unbox_Nullable:
                    mangledName = "RhUnboxNullable";
                    break;

                case ReadyToRunHelper.NewMultiDimArr:
                    methodDesc = context.GetHelperEntryPoint("ArrayHelpers", "NewObjArray");
                    break;
                case ReadyToRunHelper.NewMultiDimArrRare:
                    methodDesc = context.GetHelperEntryPoint("ArrayHelpers", "NewObjArrayRare");
                    break;

                case ReadyToRunHelper.NewArray:
                    mangledName = "RhNewArray";
                    break;
                case ReadyToRunHelper.NewObject:
                    mangledName = "RhNewObject";
                    break;

                case ReadyToRunHelper.Stelem_Ref:
                    mangledName = "RhpStelemRef";
                    break;
                case ReadyToRunHelper.Ldelema_Ref:
                    mangledName = "RhpLdelemaRef";
                    break;

                case ReadyToRunHelper.MemCpy:
                    mangledName = "RhRuntimeHelpers_MemCopy";
                    break;
                case ReadyToRunHelper.MemSet:
                    mangledName = "RhRuntimeHelpers_MemSet";
                    break;

                case ReadyToRunHelper.GetRuntimeTypeHandle:
                    methodDesc = context.GetHelperEntryPoint("LdTokenHelpers", "GetRuntimeTypeHandle");
                    break;
                case ReadyToRunHelper.GetRuntimeType:
                    methodDesc = context.GetHelperEntryPoint("LdTokenHelpers", "GetRuntimeType");
                    break;
                case ReadyToRunHelper.GetRuntimeMethodHandle:
                    methodDesc = context.GetHelperEntryPoint("LdTokenHelpers", "GetRuntimeMethodHandle");
                    break;
                case ReadyToRunHelper.GetRuntimeFieldHandle:
                    methodDesc = context.GetHelperEntryPoint("LdTokenHelpers", "GetRuntimeFieldHandle");
                    break;

                case ReadyToRunHelper.Lng2Dbl:
                    mangledName = "RhpLng2Dbl";
                    break;
                case ReadyToRunHelper.ULng2Dbl:
                    mangledName = "RhpULng2Dbl";
                    break;

                case ReadyToRunHelper.Dbl2Lng:
                    mangledName = "RhpDbl2Lng";
                    break;
                case ReadyToRunHelper.Dbl2ULng:
                    mangledName = "RhpDbl2ULng";
                    break;
                case ReadyToRunHelper.Dbl2Int:
                    mangledName = "RhpDbl2Int";
                    break;
                case ReadyToRunHelper.Dbl2UInt:
                    mangledName = "RhpDbl2UInt";
                    break;

                case ReadyToRunHelper.Dbl2IntOvf:
                    methodDesc = context.GetHelperEntryPoint("MathHelpers", "Dbl2IntOvf");
                    break;
                case ReadyToRunHelper.Dbl2UIntOvf:
                    methodDesc = context.GetHelperEntryPoint("MathHelpers", "Dbl2UIntOvf");
                    break;
                case ReadyToRunHelper.Dbl2LngOvf:
                    methodDesc = context.GetHelperEntryPoint("MathHelpers", "Dbl2LngOvf");
                    break;
                case ReadyToRunHelper.Dbl2ULngOvf:
                    methodDesc = context.GetHelperEntryPoint("MathHelpers", "Dbl2ULngOvf");
                    break;

                case ReadyToRunHelper.DblRem:
                    mangledName = "RhpDblRem";
                    break;
                case ReadyToRunHelper.FltRem:
                    mangledName = "RhpFltRem";
                    break;

                case ReadyToRunHelper.LMul:
                    mangledName = "RhpLMul";
                    break;
                case ReadyToRunHelper.LMulOfv:
                    methodDesc = context.GetHelperEntryPoint("MathHelpers", "LMulOvf");
                    break;
                case ReadyToRunHelper.ULMulOvf:
                    methodDesc = context.GetHelperEntryPoint("MathHelpers", "ULMulOvf");
                    break;

                case ReadyToRunHelper.Mod:
                    methodDesc = context.GetHelperEntryPoint("MathHelpers", "IMod");
                    break;
                case ReadyToRunHelper.UMod:
                    methodDesc = context.GetHelperEntryPoint("MathHelpers", "UMod");
                    break;
                case ReadyToRunHelper.ULMod:
                    methodDesc = context.GetHelperEntryPoint("MathHelpers", "ULMod");
                    break;
                case ReadyToRunHelper.LMod:
                    methodDesc = context.GetHelperEntryPoint("MathHelpers", "LMod");
                    break;

                case ReadyToRunHelper.Div:
                    methodDesc = context.GetHelperEntryPoint("MathHelpers", "IDiv");
                    break;
                case ReadyToRunHelper.UDiv:
                    methodDesc = context.GetHelperEntryPoint("MathHelpers", "UDiv");
                    break;
                case ReadyToRunHelper.ULDiv:
                    methodDesc = context.GetHelperEntryPoint("MathHelpers", "ULDiv");
                    break;
                case ReadyToRunHelper.LDiv:
                    methodDesc = context.GetHelperEntryPoint("MathHelpers", "LDiv");
                    break;

                case ReadyToRunHelper.LRsz:
                    mangledName = "RhpLRsz";
                    break;
                case ReadyToRunHelper.LRsh:
                    mangledName = "RhpLRsh";
                    break;
                case ReadyToRunHelper.LLsh:
                    mangledName = "RhpLLsh";
                    break;

                case ReadyToRunHelper.PInvokeBegin:
                    mangledName = "RhpPInvoke";
                    break;
                case ReadyToRunHelper.PInvokeEnd:
                    mangledName = "RhpPInvokeReturn";
                    break;

                case ReadyToRunHelper.ReversePInvokeEnter:
                    mangledName = "RhpReversePInvoke";
                    break;
                case ReadyToRunHelper.ReversePInvokeExit:
                    mangledName = "RhpReversePInvokeReturn";
                    break;

                case ReadyToRunHelper.CheckCastAny:
                    mangledName = "RhTypeCast_CheckCastAny";
                    break;
                case ReadyToRunHelper.CheckCastInterface:
                    mangledName = "RhTypeCast_CheckCastInterface";
                    break;
                case ReadyToRunHelper.CheckCastClass:
                    mangledName = "RhTypeCast_CheckCastClass";
                    break;
                case ReadyToRunHelper.CheckCastClassSpecial:
                    mangledName = "RhTypeCast_CheckCastClassSpecial";
                    break;

                case ReadyToRunHelper.CheckInstanceAny:
                    mangledName = "RhTypeCast_IsInstanceOfAny";
                    break;
                case ReadyToRunHelper.CheckInstanceInterface:
                    mangledName = "RhTypeCast_IsInstanceOfInterface";
                    break;
                case ReadyToRunHelper.CheckInstanceClass:
                    mangledName = "RhTypeCast_IsInstanceOfClass";
                    break;
                case ReadyToRunHelper.IsInstanceOfException:
                    mangledName = "RhTypeCast_IsInstanceOfException";
                    break;

                case ReadyToRunHelper.MonitorEnter:
                    methodDesc = context.GetHelperEntryPoint("SynchronizedMethodHelpers", "MonitorEnter");
                    break;
                case ReadyToRunHelper.MonitorExit:
                    methodDesc = context.GetHelperEntryPoint("SynchronizedMethodHelpers", "MonitorExit");
                    break;
                case ReadyToRunHelper.MonitorEnterStatic:
                    methodDesc = context.GetHelperEntryPoint("SynchronizedMethodHelpers", "MonitorEnterStatic");
                    break;
                case ReadyToRunHelper.MonitorExitStatic:
                    methodDesc = context.GetHelperEntryPoint("SynchronizedMethodHelpers", "MonitorExitStatic");
                    break;

                case ReadyToRunHelper.GVMLookupForSlot:
                    methodDesc = context.SystemModule.GetKnownType("System.Runtime", "TypeLoaderExports").GetKnownMethod("GVMLookupForSlot", null);
                    break;

                case ReadyToRunHelper.TypeHandleToRuntimeType:
                    methodDesc = context.GetHelperEntryPoint("TypedReferenceHelpers", "TypeHandleToRuntimeTypeMaybeNull");
                    break;
                case ReadyToRunHelper.GetRefAny:
                    methodDesc = context.GetHelperEntryPoint("TypedReferenceHelpers", "GetRefAny");
                    break;
                case ReadyToRunHelper.TypeHandleToRuntimeTypeHandle:
                    methodDesc = context.GetHelperEntryPoint("TypedReferenceHelpers", "TypeHandleToRuntimeTypeHandleMaybeNull");
                    break;

                case ReadyToRunHelper.GetCurrentManagedThreadId:
                    methodDesc = context.SystemModule.GetKnownType("System", "Environment").GetKnownMethod("get_CurrentManagedThreadId", null);
                    break;

                default:
                    throw new NotImplementedException(id.ToString());
            }
        }

        //
        // These methods are static compiler equivalent of RhGetRuntimeHelperForType
        //
        public static string GetNewObjectHelperForType(TypeDesc type)
        {
            if (type.RequiresAlign8())
            {
                if (type.HasFinalizer)
                    return "RhpNewFinalizableAlign8";

                if (type.IsValueType)
                    return "RhpNewFastMisalign";

                return "RhpNewFastAlign8";
            }

            if (type.HasFinalizer)
                return "RhpNewFinalizable";

            return "RhpNewFast";
        }

        public static string GetNewArrayHelperForType(TypeDesc type)
        {
            if (type.RequiresAlign8())
                return "RhpNewArrayAlign8";

            return "RhpNewArray";
        }
    }
}
