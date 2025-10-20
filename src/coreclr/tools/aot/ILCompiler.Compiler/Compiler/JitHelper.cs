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
                    methodDesc = context.GetHelperEntryPoint("ThrowHelpers"u8, "ThrowOverflowException"u8);
                    break;
                case ReadyToRunHelper.RngChkFail:
                    methodDesc = context.GetHelperEntryPoint("ThrowHelpers"u8, "ThrowIndexOutOfRangeException"u8);
                    break;
                case ReadyToRunHelper.FailFast:
                    mangledName = "RhpFallbackFailFast"; // TODO: Report stack buffer overrun
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
                    mangledName = "RhDebugBreak";
                    break;

                case ReadyToRunHelper.WriteBarrier:
                    mangledName = context.Target.Architecture switch
                    {
                        TargetArchitecture.ARM64 => "RhpAssignRefArm64",
                        TargetArchitecture.LoongArch64 => "RhpAssignRefLoongArch64",
                        TargetArchitecture.RiscV64 => "RhpAssignRefRiscV64",
                        _ => "RhpAssignRef"
                    };
                    break;
                case ReadyToRunHelper.CheckedWriteBarrier:
                    mangledName = context.Target.Architecture == TargetArchitecture.ARM64 ? "RhpCheckedAssignRefArm64" : "RhpCheckedAssignRef";
                    break;
                case ReadyToRunHelper.BulkWriteBarrier:
                    methodDesc = context.GetCoreLibEntryPoint("System"u8, "Buffer"u8, "BulkMoveWithWriteBarrier"u8, null);
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
                    methodDesc = context.GetHelperEntryPoint("ArrayHelpers"u8, "NewObjArray"u8);
                    break;
                case ReadyToRunHelper.NewMultiDimArrRare:
                    methodDesc = context.GetHelperEntryPoint("ArrayHelpers"u8, "NewObjArrayRare"u8);
                    break;

                case ReadyToRunHelper.NewArray:
                    mangledName = "RhNewArray";
                    break;
                case ReadyToRunHelper.NewObject:
                    mangledName = "RhNewObject";
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
                    mangledName = "memset";
                    break;

                case ReadyToRunHelper.GetRuntimeTypeHandle:
                    methodDesc = context.GetHelperEntryPoint("LdTokenHelpers"u8, "GetRuntimeTypeHandle"u8);
                    break;
                case ReadyToRunHelper.GetRuntimeType:
                    methodDesc = context.GetHelperEntryPoint("LdTokenHelpers"u8, "GetRuntimeType"u8);
                    break;
                case ReadyToRunHelper.GetRuntimeMethodHandle:
                    methodDesc = context.GetHelperEntryPoint("LdTokenHelpers"u8, "GetRuntimeMethodHandle"u8);
                    break;
                case ReadyToRunHelper.GetRuntimeFieldHandle:
                    methodDesc = context.GetHelperEntryPoint("LdTokenHelpers"u8, "GetRuntimeFieldHandle"u8);
                    break;

                case ReadyToRunHelper.Lng2Dbl:
                    mangledName = "RhpLng2Dbl";
                    break;
                case ReadyToRunHelper.ULng2Dbl:
                    mangledName = "RhpULng2Dbl";
                    break;
                case ReadyToRunHelper.Lng2Flt:
                    mangledName = "RhpLng2Flt";
                    break;
                case ReadyToRunHelper.ULng2Flt:
                    mangledName = "RhpULng2Flt";
                    break;

                case ReadyToRunHelper.Dbl2Lng:
                    mangledName = "RhpDbl2Lng";
                    break;
                case ReadyToRunHelper.Dbl2ULng:
                    mangledName = "RhpDbl2ULng";
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
                    mangledName = "fmod";
                    break;
                case ReadyToRunHelper.FltRem:
                    mangledName = "fmodf";
                    break;

                case ReadyToRunHelper.LMul:
                    mangledName = "RhpLMul";
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
                    methodDesc = context.GetHelperEntryPoint("SynchronizedMethodHelpers"u8, "MonitorEnter"u8);
                    break;
                case ReadyToRunHelper.MonitorExit:
                    methodDesc = context.GetHelperEntryPoint("SynchronizedMethodHelpers"u8, "MonitorExit"u8);
                    break;

                case ReadyToRunHelper.GVMLookupForSlot:
                    methodDesc = context.SystemModule.GetKnownType("System.Runtime"u8, "TypeLoaderExports"u8).GetKnownMethod("GVMLookupForSlot"u8, null);
                    break;

                case ReadyToRunHelper.TypeHandleToRuntimeType:
                    methodDesc = context.GetHelperEntryPoint("TypedReferenceHelpers"u8, "TypeHandleToRuntimeTypeMaybeNull"u8);
                    break;
                case ReadyToRunHelper.GetRefAny:
                    methodDesc = context.GetHelperEntryPoint("TypedReferenceHelpers"u8, "GetRefAny"u8);
                    break;
                case ReadyToRunHelper.TypeHandleToRuntimeTypeHandle:
                    methodDesc = context.GetHelperEntryPoint("TypedReferenceHelpers"u8, "TypeHandleToRuntimeTypeHandleMaybeNull"u8);
                    break;

                case ReadyToRunHelper.GetCurrentManagedThreadId:
                    methodDesc = context.SystemModule.GetKnownType("System"u8, "Environment"u8).GetKnownMethod("get_CurrentManagedThreadId"u8, null);
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
    }
}
