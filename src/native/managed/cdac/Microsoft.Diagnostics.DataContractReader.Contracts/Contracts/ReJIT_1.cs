// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly partial struct ReJIT_1 : IReJIT
{
    internal readonly Target _target;
    private readonly Data.ProfControlBlock _profControlBlock;

    // see src/coreclr/inc/corprof.idl
    [Flags]
    private enum COR_PRF_MONITOR
    {
        COR_PRF_ENABLE_REJIT = 0x00040000,
    }

    // see src/coreclr/vm/codeversion.h
    [Flags]
    public enum RejitFlags : uint
    {
        kStateRequested = 0x00000000,

        kStateGettingReJITParameters = 0x00000001,

        kStateActive = 0x00000002,

        kStateMask = 0x0000000F,

        kSuppressParams = 0x80000000
    }

    [Flags]
    private enum NativeOptimizationTier : uint
    {
        OptimizationTier0 = 0,
        OptimizationTier1 = 1,
        OptimizationTier1OSR = 2,
        OptimizationTierOptimized = 3,
        OptimizationTier0Instrumented = 4,
        OptimizationTier1Instrumented = 5,
        OptimizationTierUnknown = 0xffffffff
    };

    public ReJIT_1(Target target, Data.ProfControlBlock profControlBlock)
    {
        _target = target;
        _profControlBlock = profControlBlock;
    }

    bool IReJIT.IsEnabled()
    {
        bool profEnabledReJIT = (_profControlBlock.GlobalEventMask & (ulong)COR_PRF_MONITOR.COR_PRF_ENABLE_REJIT) != 0;
        // FIXME: it is very likely this is always true in the DAC
        // Most people don't set DOTNET_ProfAPI_RejitOnAttach = 0
        // See https://github.com/dotnet/runtime/issues/106148
        bool clrConfigEnabledReJIT = true;
        return profEnabledReJIT || clrConfigEnabledReJIT;
    }

    RejitState IReJIT.GetRejitState(ILCodeVersionHandle ilCodeVersionHandle)
    {
        if (!ilCodeVersionHandle.IsExplicit)
        {
            // for non explicit ILCodeVersions, ReJITState is always kStateActive
            return RejitState.Active;
        }
        ILCodeVersionNode ilCodeVersionNode = AsNode(ilCodeVersionHandle);
        return ((RejitFlags)ilCodeVersionNode.RejitState & RejitFlags.kStateMask) switch
        {
            RejitFlags.kStateRequested => RejitState.Requested,
            RejitFlags.kStateActive => RejitState.Active,
            _ => throw new InvalidOperationException($"Unknown ReJIT state: {ilCodeVersionNode.RejitState}"),
        };
    }

    TargetNUInt IReJIT.GetRejitId(ILCodeVersionHandle ilCodeVersionHandle)
    {
        if (ilCodeVersionHandle.ILCodeVersionNode == TargetPointer.Null)
        {
            // for non explicit ILCodeVersions, ReJITId is always 0
            return new TargetNUInt(0);
        }
        ILCodeVersionNode ilCodeVersionNode = AsNode(ilCodeVersionHandle);
        return ilCodeVersionNode.VersionId;
    }

    private NativeOptimizationTier GetInitialOptimizationTier(TargetPointer mdPointer)
    {
        // validation of the method desc
        MethodDescHandle _ = _target.Contracts.RuntimeTypeSystem.GetMethodDescHandle(mdPointer);
        Data.MethodDesc md = _target.ProcessedData.GetOrAdd<Data.MethodDesc>(mdPointer);
        Data.MethodDescCodeData codeData = _target.ProcessedData.GetOrAdd<Data.MethodDescCodeData>(md.CodeData);
        return (NativeOptimizationTier)codeData.OptimizationTier;
    }

    private static OptimizationTierEnum GetOptimizationTier(NativeOptimizationTier nativeOptimizationTier)
    {
        return nativeOptimizationTier switch
        {
            NativeOptimizationTier.OptimizationTier0 => OptimizationTierEnum.QuickJitted,
            NativeOptimizationTier.OptimizationTier1 => OptimizationTierEnum.OptimizedTier1,
            NativeOptimizationTier.OptimizationTier1OSR => OptimizationTierEnum.OptimizedTier1OSR,
            NativeOptimizationTier.OptimizationTierOptimized => OptimizationTierEnum.Optimized,
            NativeOptimizationTier.OptimizationTier0Instrumented => OptimizationTierEnum.QuickJittedInstrumented,
            NativeOptimizationTier.OptimizationTier1Instrumented => OptimizationTierEnum.OptimizedTier1Instrumented,
            _ => OptimizationTierEnum.Unknown,
        };
    }

    IEnumerable<(TargetPointer, TargetPointer, OptimizationTierEnum)> IReJIT.GetTieredVersions(TargetPointer methodDesc, int rejitId)
    {
        Contracts.ICodeVersions codeVersionsContract = _target.Contracts.CodeVersions;
        Contracts.IReJIT rejitContract = this;

        ILCodeVersionHandle ilCodeVersion = codeVersionsContract.GetILCodeVersions(methodDesc)
            .FirstOrDefault(ilcode => rejitContract.GetRejitId(ilcode).Value == (ulong)rejitId,
                ILCodeVersionHandle.Invalid);

        if (!ilCodeVersion.IsValid)
            throw new ArgumentException();
        // Iterate through versioning state nodes and return the active one, matching any IL code version
        Contracts.IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        Contracts.ILoader loader = _target.Contracts.Loader;
        MethodDescHandle mdh = rts.GetMethodDescHandle(methodDesc);
        TargetPointer methodTable = rts.GetMethodTable(mdh);
        TypeHandle mtTypeHandle = rts.GetTypeHandle(methodTable);
        TargetPointer modulePtr = rts.GetModule(mtTypeHandle);
        ModuleHandle moduleHandle = loader.GetModuleHandleFromModulePtr(modulePtr);

        bool isReadyToRun = loader.GetReadyToRunImageInfo(moduleHandle, out TargetPointer r2rImageBase, out TargetPointer r2rImageEnd);
        bool isEligibleForTieredCompilation = rts.IsEligibleForTieredCompilation(mdh);
        int count = 0;
        foreach (NativeCodeVersionHandle nativeCodeVersionHandle in codeVersionsContract.GetNativeCodeVersions(methodDesc, ilCodeVersion))
        {
            TargetPointer nativeCode = codeVersionsContract.GetNativeCode(nativeCodeVersionHandle).AsTargetPointer;
            TargetPointer nativeCodeAddr = nativeCode;
            TargetPointer nativeCodeVersionNodePtr = nativeCodeVersionHandle.IsExplicit ? AsNode(nativeCodeVersionHandle).Address : TargetPointer.Null;
            OptimizationTierEnum optimizationTier;
            if (r2rImageBase <= nativeCode && nativeCode < r2rImageEnd)
            {
                optimizationTier = OptimizationTierEnum.ReadyToRun;
            }

            else if (isEligibleForTieredCompilation)
            {
                NativeOptimizationTier optTier;
                if (!nativeCodeVersionHandle.IsExplicit)
                    optTier = GetInitialOptimizationTier(nativeCodeVersionHandle.MethodDescAddress);
                else
                {
                    NativeCodeVersionNode nativeCodeVersionNode = AsNode(nativeCodeVersionHandle);
                    optTier = (NativeOptimizationTier)nativeCodeVersionNode.OptimizationTier!.Value;
                }
                optimizationTier = GetOptimizationTier(optTier);
            }
            else if (rts.IsJitOptimizationDisabled(mdh))
            {
                optimizationTier = OptimizationTierEnum.MinOptJitted;
            }
            else
            {
                optimizationTier = OptimizationTierEnum.Optimized;
            }
            count++;
            yield return (nativeCodeAddr, nativeCodeVersionNodePtr, optimizationTier);
        }
    }

    private ILCodeVersionNode AsNode(ILCodeVersionHandle ilCodeVersionHandle)
    {
        if (ilCodeVersionHandle.ILCodeVersionNode == TargetPointer.Null)
        {
            throw new InvalidOperationException("Synthetic ILCodeVersion does not have a backing node.");
        }

        return _target.ProcessedData.GetOrAdd<ILCodeVersionNode>(ilCodeVersionHandle.ILCodeVersionNode);
    }

    private NativeCodeVersionNode AsNode(NativeCodeVersionHandle nativeCodeVersionHandle)
    {
        if (!nativeCodeVersionHandle.IsExplicit)
        {
            throw new InvalidOperationException("Synthetic NativeCodeVersion does not have a backing node.");
        }

        return _target.ProcessedData.GetOrAdd<NativeCodeVersionNode>(nativeCodeVersionHandle.CodeVersionNodeAddress);
    }
}
