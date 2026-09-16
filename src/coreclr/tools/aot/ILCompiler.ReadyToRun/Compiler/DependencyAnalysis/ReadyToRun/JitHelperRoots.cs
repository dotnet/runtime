// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.ReadyToRunConstants;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    internal static class JitHelperRoots
    {
        public static IEnumerable<ReadyToRunHelper> GetReadyToRunHelpers(TargetDetails target)
        {
            yield return ReadyToRunHelper.Throw;
            yield return ReadyToRunHelper.Rethrow;
            yield return ReadyToRunHelper.Overflow;
            yield return ReadyToRunHelper.RngChkFail;
            yield return ReadyToRunHelper.FailFast;
            yield return ReadyToRunHelper.ThrowNullRef;
            yield return ReadyToRunHelper.ThrowDivZero;
            yield return ReadyToRunHelper.ThrowExact;
            yield return ReadyToRunHelper.ThrowArgument;
            yield return ReadyToRunHelper.ThrowArgumentOutOfRange;
            yield return ReadyToRunHelper.ThrowPlatformNotSupported;
            yield return ReadyToRunHelper.ThrowNotImplemented;
            yield return ReadyToRunHelper.WriteBarrier;
            yield return ReadyToRunHelper.CheckedWriteBarrier;
            yield return ReadyToRunHelper.BulkWriteBarrier;
            yield return ReadyToRunHelper.Stelem_Ref;
            yield return ReadyToRunHelper.Ldelema_Ref;
            yield return ReadyToRunHelper.MemSet;
            yield return ReadyToRunHelper.MemZero;
            yield return ReadyToRunHelper.NativeMemSet;
            yield return ReadyToRunHelper.MemCpy;
            yield return ReadyToRunHelper.GetRuntimeTypeHandle;
            yield return ReadyToRunHelper.GetRuntimeMethodHandle;
            yield return ReadyToRunHelper.GetRuntimeFieldHandle;
            yield return ReadyToRunHelper.Box;
            yield return ReadyToRunHelper.Box_Nullable;
            yield return ReadyToRunHelper.Unbox;
            yield return ReadyToRunHelper.Unbox_Nullable;
            yield return ReadyToRunHelper.NewMultiDimArr;
            yield return ReadyToRunHelper.Unbox_TypeTest;
            yield return ReadyToRunHelper.NewObject;
            yield return ReadyToRunHelper.NewArray;
            yield return ReadyToRunHelper.CheckCastAny;
            yield return ReadyToRunHelper.CheckInstanceAny;
            yield return ReadyToRunHelper.GenericGcStaticBase;
            yield return ReadyToRunHelper.GenericNonGcStaticBase;
            yield return ReadyToRunHelper.GenericGcTlsBase;
            yield return ReadyToRunHelper.GenericNonGcTlsBase;
            yield return ReadyToRunHelper.VirtualFuncPtr;
            yield return ReadyToRunHelper.IsInstanceOfException;
            yield return ReadyToRunHelper.NewMaybeFrozenArray;
            yield return ReadyToRunHelper.NewMaybeFrozenObject;
            yield return ReadyToRunHelper.LMul;
            yield return ReadyToRunHelper.LMulOfv;
            yield return ReadyToRunHelper.ULMulOvf;
            yield return ReadyToRunHelper.LDiv;
            yield return ReadyToRunHelper.LMod;
            yield return ReadyToRunHelper.ULDiv;
            yield return ReadyToRunHelper.ULMod;
            yield return ReadyToRunHelper.LLsh;
            yield return ReadyToRunHelper.LRsh;
            yield return ReadyToRunHelper.LRsz;
            yield return ReadyToRunHelper.Lng2Dbl;
            yield return ReadyToRunHelper.ULng2Dbl;
            yield return ReadyToRunHelper.Div;
            yield return ReadyToRunHelper.Mod;
            yield return ReadyToRunHelper.UDiv;
            yield return ReadyToRunHelper.UMod;
            yield return ReadyToRunHelper.Dbl2IntOvf;
            yield return ReadyToRunHelper.Dbl2Lng;
            yield return ReadyToRunHelper.Dbl2LngOvf;
            yield return ReadyToRunHelper.Dbl2UIntOvf;
            yield return ReadyToRunHelper.Dbl2ULng;
            yield return ReadyToRunHelper.Dbl2ULngOvf;
            yield return ReadyToRunHelper.Lng2Flt;
            yield return ReadyToRunHelper.ULng2Flt;
            yield return ReadyToRunHelper.FltRem;
            yield return ReadyToRunHelper.DblRem;

            if (target.Architecture != TargetArchitecture.X86)
            {
                yield return ReadyToRunHelper.PersonalityRoutine;
                yield return ReadyToRunHelper.PersonalityRoutineFilterFunclet;
            }

            if (target.Architecture == TargetArchitecture.X86)
            {
                yield return ReadyToRunHelper.WriteBarrier_EAX;
                yield return ReadyToRunHelper.WriteBarrier_EBX;
                yield return ReadyToRunHelper.WriteBarrier_ECX;
                yield return ReadyToRunHelper.WriteBarrier_ESI;
                yield return ReadyToRunHelper.WriteBarrier_EDI;
                yield return ReadyToRunHelper.WriteBarrier_EBP;
                yield return ReadyToRunHelper.CheckedWriteBarrier_EAX;
                yield return ReadyToRunHelper.CheckedWriteBarrier_EBX;
                yield return ReadyToRunHelper.CheckedWriteBarrier_ECX;
                yield return ReadyToRunHelper.CheckedWriteBarrier_ESI;
                yield return ReadyToRunHelper.CheckedWriteBarrier_EDI;
                yield return ReadyToRunHelper.CheckedWriteBarrier_EBP;
            }

            yield return ReadyToRunHelper.PInvokeBegin;
            yield return ReadyToRunHelper.PInvokeEnd;
            yield return ReadyToRunHelper.GCPoll;
            yield return ReadyToRunHelper.ReversePInvokeEnter;
            yield return ReadyToRunHelper.ReversePInvokeExit;
            yield return ReadyToRunHelper.MonitorEnter;
            yield return ReadyToRunHelper.MonitorExit;

            if (target.Architecture != TargetArchitecture.ARM64)
            {
                yield return ReadyToRunHelper.StackProbe;
            }

            yield return ReadyToRunHelper.GetCurrentManagedThreadId;
            yield return ReadyToRunHelper.AllocContinuation;
            yield return ReadyToRunHelper.AllocContinuationClass;
            yield return ReadyToRunHelper.AllocContinuationMethod;
            yield return ReadyToRunHelper.InitClass;
            yield return ReadyToRunHelper.InitInstClass;
        }
    }
}
