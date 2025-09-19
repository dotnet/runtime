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
        Tier0 = 0,
        Tier1 = 1,
        Tier1OSR = 2,
        TierOptimized = 3,
        Tier0Instrumented = 4,
        Tier1Instrumented = 5,
    };


    public enum CallCountingStage : byte
    {
        StubIsNotActive = 0,
        StubMayBeActive = 1,
        PendingCompletion = 2,
        Complete = 3,
        Disabled = 4
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

    private sealed class CodeVersionHashTraits : ITraits<NativeCodeVersion, CallCountingInfo>
    {
        private readonly Target _target;
        public CodeVersionHashTraits(Target target)
        {
            _target = target;
        }
        public NativeCodeVersion GetKey(CallCountingInfo entry)
        {
            return _target.ProcessedData.GetOrAdd<NativeCodeVersion>(entry.CodeVersion);
        }
        public bool Equals(NativeCodeVersion left, NativeCodeVersion right) => left.StorageKind == right.StorageKind && left.MethodDescOrNode == right.MethodDescOrNode;
        public uint Hash(NativeCodeVersion key)
        {
            switch (key.StorageKind)
            {
                case 1:
                    return (uint)key.MethodDescOrNode;
                case 2:
                    NativeCodeVersionNode node = _target.ProcessedData.GetOrAdd<NativeCodeVersionNode>(key.MethodDescOrNode);
                    return (uint)node.MethodDesc + node.NativeId;
                default:
                    throw new NotSupportedException();
            }
        }
        public bool IsNull(CallCountingInfo entry) => entry.Address == TargetPointer.Null;
        public CallCountingInfo Null() => new CallCountingInfo(TargetPointer.Null);
        public bool IsDeleted(CallCountingInfo entry) => false;
    }

    private sealed class CallCountingTable : IData<CallCountingTable>
    {
        static CallCountingTable IData<CallCountingTable>.Create(Target target, TargetPointer address)
            => new CallCountingTable(target, address);

        public CallCountingTable(Target target, TargetPointer address)
        {
            ISHash sHashContract = target.Contracts.SHash;
            Target.TypeInfo type = target.GetTypeInfo(DataType.CallCountingInfo);
            HashTable = sHashContract.CreateSHash(target, address, type, new CodeVersionHashTraits(target));
        }
        public ISHash<NativeCodeVersion, CallCountingInfo> HashTable { get; init; }
    }
    private bool IsCallCountingEnabled(MethodDescHandle mdh)
    {
        // get loader allocator
        Contracts.IRuntimeTypeSystem rtsContract = _target.Contracts.RuntimeTypeSystem;
        Contracts.ILoader loaderContract = _target.Contracts.Loader;
        TargetPointer mt = rtsContract.GetMethodTable(mdh);
        TargetPointer modulePtr = rtsContract.GetLoaderModule(rtsContract.GetTypeHandle(mt));
        Contracts.ModuleHandle moduleHandle = loaderContract.GetModuleHandleFromModulePtr(modulePtr);
        TargetPointer loaderAllocator = loaderContract.GetLoaderAllocator(moduleHandle);
        Data.LoaderAllocator loaderAllocatorData = _target.ProcessedData.GetOrAdd<LoaderAllocator>(loaderAllocator);

        // get call counting manager and hash
        TargetPointer callCountingMgr = loaderAllocatorData.CallCountingManager!.Value;
        TargetPointer callCountingHash = _target.ProcessedData.GetOrAdd<CallCountingManager>(callCountingMgr).CallCountingHash;
        CallCountingTable callCountingTable = _target.ProcessedData.GetOrAdd<CallCountingTable>(callCountingHash);

        ISHash shashContract = _target.Contracts.SHash;
        CallCountingInfo entry = shashContract.LookupSHash(callCountingTable.HashTable, new NativeCodeVersion(2, mdh.Address));
        return entry.Address != TargetPointer.Null && entry.Stage != (byte)CallCountingStage.Disabled;
    }
    private NativeOptimizationTier GetInitialOptimizationTier(bool isReadyToRun, MethodDescHandle mdh)
    {
        if (_target.ReadGlobal<byte>(Constants.Globals.FeatureTieredCompilation) == 0
                || !IsCallCountingEnabled(mdh))
            return NativeOptimizationTier.TierOptimized;
        Data.EEConfig eeConfig = _target.ProcessedData.GetOrAdd<Data.EEConfig>(_target.ReadGlobalPointer(Constants.Globals.EEConfig));
        if (eeConfig.TieredPGO!.Value)
        {
            if (eeConfig.TieredPGO_InstrumentOnlyHotCode!.Value || isReadyToRun)
                return NativeOptimizationTier.Tier0;
            else
                return NativeOptimizationTier.Tier0Instrumented;
        }
        else
            return NativeOptimizationTier.Tier0;
    }

    private static OptimizationTierEnum GetOptimizationTier(NativeOptimizationTier nativeOptimizationTier)
    {
        return nativeOptimizationTier switch
        {
            NativeOptimizationTier.Tier0 => OptimizationTierEnum.QuickJitted,
            NativeOptimizationTier.Tier1 => OptimizationTierEnum.OptimizedTier1,
            NativeOptimizationTier.Tier1OSR => OptimizationTierEnum.OptimizedTier1OSR,
            NativeOptimizationTier.TierOptimized => OptimizationTierEnum.Optimized,
            NativeOptimizationTier.Tier0Instrumented => OptimizationTierEnum.QuickJittedInstrumented,
            NativeOptimizationTier.Tier1Instrumented => OptimizationTierEnum.OptimizedTier1Instrumented,
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
                    optTier = GetInitialOptimizationTier(isReadyToRun, mdh);
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
