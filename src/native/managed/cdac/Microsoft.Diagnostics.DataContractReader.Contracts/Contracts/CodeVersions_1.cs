// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly partial struct CodeVersions_1 : ICodeVersions
{
    private readonly Target _target;

    public CodeVersions_1(Target target)
    {
        _target = target;
    }

    private enum NativeOptimizationTier : uint
    {
        Tier0 = 0,
        Tier1 = 1,
        Tier1OSR = 2,
        TierOptimized = 3,
        Tier0Instrumented = 4,
        Tier1Instrumented = 5,
    };

    private enum Stage : byte
    {
        StubIsNotActive = 0,
        StubMayBeActive = 1,
        PendingCompletion = 2,
        Complete = 3,
        Disabled = 4
    };

    private enum StorageKind
    {
        Unknown = 0,
        Explicit = 1,
        Synthetic = 2
    };


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
            switch ((StorageKind)key.StorageKind)
            {
                case StorageKind.Synthetic:
                    return (uint)key.MethodDescOrNode;
                case StorageKind.Explicit:
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
        TargetPointer callCountingMgr = loaderAllocatorData.CallCountingManager;
        TargetPointer callCountingHash = _target.ProcessedData.GetOrAdd<CallCountingManager>(callCountingMgr).CallCountingHash;
        CallCountingTable callCountingTable = _target.ProcessedData.GetOrAdd<CallCountingTable>(callCountingHash);

        ISHash shashContract = _target.Contracts.SHash;
        CallCountingInfo entry = shashContract.LookupSHash(callCountingTable.HashTable, new NativeCodeVersion((uint)StorageKind.Synthetic, mdh.Address));
        return entry.Address != TargetPointer.Null && entry.Stage != (byte)Stage.Disabled;
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

    IEnumerable<(TargetPointer, TargetPointer, OptimizationTierEnum)> ICodeVersions.GetTieredVersions(TargetPointer methodDesc, int rejitId, int cNativeCodeAddrs)
    {
        Contracts.ICodeVersions codeVersionsContract = this;
        Contracts.IReJIT rejitContract = _target.Contracts.ReJIT;

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

        bool isReadyToRun = loader.GetReadyToRunInfo(moduleHandle, out TargetPointer r2rImageBase, out TargetPointer r2rImageEnd);
        bool isEligibleForTieredCompilation = rts.IsEligibleForTieredCompilation(mdh);
        int count = 0;
        foreach (NativeCodeVersionHandle nativeCodeVersionHandle in ((ICodeVersions)this).GetNativeCodeVersions(methodDesc, ilCodeVersion))
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
                    optTier = (NativeOptimizationTier)nativeCodeVersionNode.OptimizationTier;
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

            if (count >= cNativeCodeAddrs)
                yield break;
        }
    }
    ILCodeVersionHandle ICodeVersions.GetActiveILCodeVersion(TargetPointer methodDesc)
    {
        // CodeVersionManager::GetActiveILCodeVersion
        GetModuleAndMethodDesc(methodDesc, out TargetPointer module, out uint methodDefToken);

        TargetPointer ilVersionStateAddress = GetILVersionStateAddress(module, methodDefToken);
        if (ilVersionStateAddress == TargetPointer.Null)
        {
            return ILCodeVersionHandle.CreateSynthetic(module, methodDefToken);
        }
        Data.ILCodeVersioningState ilState = _target.ProcessedData.GetOrAdd<Data.ILCodeVersioningState>(ilVersionStateAddress);
        return ActiveILCodeVersionHandleFromState(ilState);
    }

    ILCodeVersionHandle ICodeVersions.GetILCodeVersion(NativeCodeVersionHandle nativeCodeVersionHandle)
    {
        // NativeCodeVersion::GetILCodeVersion
        if (!nativeCodeVersionHandle.Valid)
        {
            return ILCodeVersionHandle.Invalid;
        }

        if (!nativeCodeVersionHandle.IsExplicit)
        {
            // There is only a single synthetic NativeCodeVersion per
            // method and it must be on the synthetic ILCodeVersion
            GetModuleAndMethodDesc(
                nativeCodeVersionHandle.MethodDescAddress,
                out TargetPointer module,
                out uint methodDefToken);
            return ILCodeVersionHandle.CreateSynthetic(module, methodDefToken);
        }
        else
        {
            // Otherwise filter all the ILCodeVersions for the one that matches the version id
            NativeCodeVersionNode nativeCodeVersionNode = AsNode(nativeCodeVersionHandle);
            foreach (ILCodeVersionHandle ilCodeVersionHandle in ((ICodeVersions)this).GetILCodeVersions(nativeCodeVersionNode.MethodDesc))
            {
                if (GetId(ilCodeVersionHandle) == nativeCodeVersionNode.ILVersionId)
                {
                    return ilCodeVersionHandle;
                }
            }
        }

        return ILCodeVersionHandle.Invalid;
    }

    IEnumerable<ILCodeVersionHandle> ICodeVersions.GetILCodeVersions(TargetPointer methodDesc)
    {
        // CodeVersionManager::GetILCodeVersions
        GetModuleAndMethodDesc(methodDesc, out TargetPointer module, out uint methodDefToken);

        // always add the synthetic version
        yield return ILCodeVersionHandle.CreateSynthetic(module, methodDefToken);

        // if explicit versions exist, iterate linked list and return them
        TargetPointer ilVersionStateAddress = GetILVersionStateAddress(module, methodDefToken);
        if (ilVersionStateAddress != TargetPointer.Null)
        {
            Data.ILCodeVersioningState ilState = _target.ProcessedData.GetOrAdd<Data.ILCodeVersioningState>(ilVersionStateAddress);
            TargetPointer nodePointer = ilState.FirstVersionNode;
            while (nodePointer != TargetPointer.Null)
            {
                Data.ILCodeVersionNode current = _target.ProcessedData.GetOrAdd<Data.ILCodeVersionNode>(nodePointer);
                yield return ILCodeVersionHandle.CreateExplicit(nodePointer);
                nodePointer = current.Next;
            }
        }
    }

    IEnumerable<NativeCodeVersionHandle> ICodeVersions.GetNativeCodeVersions(TargetPointer methodDesc, ILCodeVersionHandle ilCodeVersionHandle)
    {
        if (!ilCodeVersionHandle.IsValid)
            yield break;

        if (!ilCodeVersionHandle.IsExplicit)
        {
            // if the ILCodeVersion is synthetic, then yield the synthetic NativeCodeVersion
            NativeCodeVersionHandle provisionalHandle = NativeCodeVersionHandle.CreateSynthetic(methodDesc);
            yield return provisionalHandle;
        }

        // Iterate through versioning state nodes and return the active one, matching any IL code version
        Contracts.IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        MethodDescHandle md = rts.GetMethodDescHandle(methodDesc);
        TargetNUInt ilVersionId = GetId(ilCodeVersionHandle);
        IEnumerable<NativeCodeVersionHandle> nativeCodeVersions = FindNativeCodeVersionNodes(
            rts,
            md,
            (codeVersion) => ilVersionId == codeVersion.ILVersionId);
        foreach (NativeCodeVersionHandle nativeCodeVersion in nativeCodeVersions)
        {
            yield return nativeCodeVersion;
        }
    }

    NativeCodeVersionHandle ICodeVersions.GetNativeCodeVersionForIP(TargetCodePointer ip)
    {
        // ExecutionManager::GetNativeCodeVersion(PCODE ip))
        // and EECodeInfo::GetNativeCodeVersion
        Contracts.IExecutionManager executionManager = _target.Contracts.ExecutionManager;
        CodeBlockHandle? info = executionManager.GetCodeBlockHandle(ip);
        if (!info.HasValue)
        {
            return NativeCodeVersionHandle.Invalid;
        }
        TargetPointer methodDescAddress = executionManager.GetMethodDesc(info.Value);
        if (methodDescAddress == TargetPointer.Null)
        {
            return NativeCodeVersionHandle.Invalid;
        }
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        MethodDescHandle md = rts.GetMethodDescHandle(methodDescAddress);
        if (!rts.IsVersionable(md))
        {
            return NativeCodeVersionHandle.CreateSynthetic(methodDescAddress);
        }
        else
        {
            TargetCodePointer startAddress = executionManager.GetStartAddress(info.Value);
            return GetSpecificNativeCodeVersion(rts, md, startAddress);
        }
    }

    bool ICodeVersions.CodeVersionManagerSupportsMethod(TargetPointer methodDescAddress)
    {
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        MethodDescHandle md = rts.GetMethodDescHandle(methodDescAddress);
        if (rts.IsDynamicMethod(md))
            return false;
        if (rts.IsCollectibleMethod(md))
            return false;
        TargetPointer mtAddr = rts.GetMethodTable(md);
        TypeHandle mt = rts.GetTypeHandle(mtAddr);
        TargetPointer modAddr = rts.GetModule(mt);
        ILoader loader = _target.Contracts.Loader;
        ModuleHandle mod = loader.GetModuleHandleFromModulePtr(modAddr);
        ModuleFlags modFlags = loader.GetFlags(mod);
        if (modFlags.HasFlag(ModuleFlags.EditAndContinue))
            return false;
        return true;
    }

    TargetCodePointer ICodeVersions.GetNativeCode(NativeCodeVersionHandle codeVersionHandle)
    {
        if (!codeVersionHandle.Valid)
        {
            throw new ArgumentException("Invalid NativeCodeVersionHandle");
        }

        if (!codeVersionHandle.IsExplicit)
        {
            MethodDescHandle md = _target.Contracts.RuntimeTypeSystem.GetMethodDescHandle(codeVersionHandle.MethodDescAddress);
            return _target.Contracts.RuntimeTypeSystem.GetNativeCode(md);
        }
        else
        {
            Data.NativeCodeVersionNode nativeCodeVersionNode = _target.ProcessedData.GetOrAdd<Data.NativeCodeVersionNode>(codeVersionHandle.CodeVersionNodeAddress);
            return nativeCodeVersionNode.NativeCode;
        }
    }

    NativeCodeVersionHandle ICodeVersions.GetActiveNativeCodeVersionForILCodeVersion(TargetPointer methodDesc, ILCodeVersionHandle ilCodeVersionHandle)
    {
        // ILCodeVersion::GetActiveNativeCodeVersion
        if (!ilCodeVersionHandle.IsValid)
        {
            return NativeCodeVersionHandle.Invalid;
        }

        if (!ilCodeVersionHandle.IsExplicit)
        {
            // if the ILCodeVersion is synthetic, then check if the active NativeCodeVersion is the synthetic one
            NativeCodeVersionHandle provisionalHandle = NativeCodeVersionHandle.CreateSynthetic(methodDescAddress: methodDesc);
            if (IsActiveNativeCodeVersion(provisionalHandle))
            {
                return provisionalHandle;
            }
        }

        // Iterate through versioning state nodes and return the active one, matching any IL code version
        Contracts.IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        MethodDescHandle md = rts.GetMethodDescHandle(methodDesc);
        TargetNUInt ilVersionId = GetId(ilCodeVersionHandle);
        return FindNativeCodeVersionNodes(rts, md, (codeVersion) =>
        {
            return (ilVersionId == codeVersion.ILVersionId)
                && ((NativeCodeVersionNodeFlags)codeVersion.Flags).HasFlag(NativeCodeVersionNodeFlags.IsActiveChild);
        }).FirstOrDefault(NativeCodeVersionHandle.Invalid);
    }

    TargetPointer ICodeVersions.GetGCStressCodeCopy(NativeCodeVersionHandle codeVersionHandle)
    {
        Debug.Assert(codeVersionHandle.Valid);

        if (!codeVersionHandle.IsExplicit)
        {
            // NativeCodeVersion::GetGCCoverageInfo
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            MethodDescHandle md = rts.GetMethodDescHandle(codeVersionHandle.MethodDescAddress);
            return rts.GetGCStressCodeCopy(md);
        }
        else
        {
            // NativeCodeVersionNode::GetGCCoverageInfo
            NativeCodeVersionNode codeVersionNode = AsNode(codeVersionHandle);
            if (codeVersionNode.GCCoverageInfo is TargetPointer gcCoverageInfoAddr && gcCoverageInfoAddr != TargetPointer.Null)
            {
                Target.TypeInfo gcCoverageInfoType = _target.GetTypeInfo(DataType.GCCoverageInfo);
                return gcCoverageInfoAddr + (ulong)gcCoverageInfoType.Fields["SavedCode"].Offset;
            }
            return TargetPointer.Null;
        }
    }

    [Flags]
    internal enum MethodDescVersioningStateFlags : byte
    {
        IsDefaultVersionActiveChildFlag = 0x4
    };

    private NativeCodeVersionHandle GetSpecificNativeCodeVersion(IRuntimeTypeSystem rts, MethodDescHandle md, TargetCodePointer startAddress)
    {
        // initial stage of NativeCodeVersionIterator::Next() with a null m_ilCodeFilter
        TargetCodePointer firstNativeCode = rts.GetNativeCode(md);
        if (firstNativeCode == startAddress)
        {
            NativeCodeVersionHandle first = NativeCodeVersionHandle.CreateSynthetic(md.Address);
            return first;
        }

        // CodeVersionManager::GetNativeCodeVersion(PTR_MethodDesc, PCODE startAddress)
        return FindNativeCodeVersionNodes(rts, md, (codeVersion) =>
        {
            return codeVersion.MethodDesc == md.Address && codeVersion.NativeCode == startAddress;
        }).FirstOrDefault(NativeCodeVersionHandle.Invalid);
    }

    private IEnumerable<NativeCodeVersionHandle> FindNativeCodeVersionNodes(IRuntimeTypeSystem rts, MethodDescHandle md, Func<Data.NativeCodeVersionNode, bool> predicate)
    {
        // ImplicitCodeVersion stage of NativeCodeVersionIterator::Next()
        TargetPointer versioningStateAddr = rts.GetMethodDescVersioningState(md);
        if (versioningStateAddr == TargetPointer.Null)
            yield break;

        Data.MethodDescVersioningState versioningState = _target.ProcessedData.GetOrAdd<Data.MethodDescVersioningState>(versioningStateAddr);

        // LinkedList stage of NativeCodeVersion::Next, heavily inlined
        TargetPointer currentAddress = versioningState.NativeCodeVersionNode;
        while (currentAddress != TargetPointer.Null)
        {
            Data.NativeCodeVersionNode current = _target.ProcessedData.GetOrAdd<Data.NativeCodeVersionNode>(currentAddress);
            if (predicate(current))
            {
                yield return NativeCodeVersionHandle.CreateExplicit(currentAddress);
            }
            currentAddress = current.Next;
        }
        yield break;
    }

    private enum ILCodeVersionKind
    {
        Unknown = 0,
        Explicit = 1, // means Node is set
        Synthetic = 2, // means Module and Token are set
    }
    private static ILCodeVersionHandle ActiveILCodeVersionHandleFromState(Data.ILCodeVersioningState ilState)
    {
        switch ((ILCodeVersionKind)ilState.ActiveVersionKind)
        {
            case ILCodeVersionKind.Explicit:
                return ILCodeVersionHandle.CreateExplicit(ilState.ActiveVersionNode);
            case ILCodeVersionKind.Synthetic:
            case ILCodeVersionKind.Unknown:
                return ILCodeVersionHandle.CreateSynthetic(ilState.ActiveVersionModule, ilState.ActiveVersionMethodDef);
            default:
                throw new InvalidOperationException($"Unknown ILCodeVersionKind {ilState.ActiveVersionKind}");
        }
    }

    [Flags]
    internal enum NativeCodeVersionNodeFlags : uint
    {
        IsActiveChild = 1
    };

    private bool IsActiveNativeCodeVersion(NativeCodeVersionHandle nativeCodeVersion)
    {
        // NativeCodeVersion::IsActiveChildVersion
        if (!nativeCodeVersion.Valid)
        {
            throw new ArgumentException("Invalid NativeCodeVersionHandle");
        }

        if (!nativeCodeVersion.IsExplicit)
        {
            MethodDescHandle md = _target.Contracts.RuntimeTypeSystem.GetMethodDescHandle(nativeCodeVersion.MethodDescAddress);
            TargetPointer versioningStateAddress = _target.Contracts.RuntimeTypeSystem.GetMethodDescVersioningState(md);
            if (versioningStateAddress == TargetPointer.Null)
            {
                return true;
            }

            Data.MethodDescVersioningState versioningState = _target.ProcessedData.GetOrAdd<Data.MethodDescVersioningState>(versioningStateAddress);
            MethodDescVersioningStateFlags flags = (MethodDescVersioningStateFlags)versioningState.Flags;
            return flags.HasFlag(MethodDescVersioningStateFlags.IsDefaultVersionActiveChildFlag);
        }
        else
        {
            // NativeCodeVersionNode::IsActiveChildVersion
            Data.NativeCodeVersionNode codeVersion = _target.ProcessedData.GetOrAdd<Data.NativeCodeVersionNode>(nativeCodeVersion.CodeVersionNodeAddress);
            return ((NativeCodeVersionNodeFlags)codeVersion.Flags).HasFlag(NativeCodeVersionNodeFlags.IsActiveChild);
        }
    }

    private void GetModuleAndMethodDesc(TargetPointer methodDesc, out TargetPointer module, out uint methodDefToken)
    {
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        MethodDescHandle md = rts.GetMethodDescHandle(methodDesc);
        TargetPointer mtAddr = rts.GetMethodTable(md);
        TypeHandle typeHandle = rts.GetTypeHandle(mtAddr);
        module = rts.GetModule(typeHandle);
        methodDefToken = rts.GetMethodToken(md);
    }

    private TargetPointer GetILVersionStateAddress(TargetPointer module, uint methodDefToken)
    {
        // No token - for example, special runtime methods like array methods
        if (methodDefToken == (uint)EcmaMetadataUtils.TokenType.mdtMethodDef)
            return TargetPointer.Null;

        ModuleHandle moduleHandle = _target.Contracts.Loader.GetModuleHandleFromModulePtr(module);
        TargetPointer ilCodeVersionTable = _target.Contracts.Loader.GetLookupTables(moduleHandle).MethodDefToILCodeVersioningState;
        TargetPointer ilVersionStateAddress = _target.Contracts.Loader.GetModuleLookupMapElement(ilCodeVersionTable, methodDefToken, out var _);
        return ilVersionStateAddress;
    }

    private ILCodeVersionNode AsNode(ILCodeVersionHandle handle)
    {
        if (handle.ILCodeVersionNode == TargetPointer.Null)
        {
            throw new InvalidOperationException("Synthetic ILCodeVersion does not have a backing node.");
        }

        return _target.ProcessedData.GetOrAdd<ILCodeVersionNode>(handle.ILCodeVersionNode);
    }

    private NativeCodeVersionNode AsNode(NativeCodeVersionHandle handle)
    {
        if (handle.CodeVersionNodeAddress == TargetPointer.Null)
        {
            throw new InvalidOperationException("Synthetic NativeCodeVersion does not have a backing node.");
        }

        return _target.ProcessedData.GetOrAdd<NativeCodeVersionNode>(handle.CodeVersionNodeAddress);
    }

    private TargetNUInt GetId(ILCodeVersionHandle ilCodeVersionHandle)
    {
        if (!ilCodeVersionHandle.IsExplicit)
        {
            // for non explicit ILCodeVersions, id is always 0
            return new TargetNUInt(0);
        }
        ILCodeVersionNode ilCodeVersionNode = AsNode(ilCodeVersionHandle);
        return ilCodeVersionNode.VersionId;
    }

    TargetPointer ICodeVersions.GetIL(ILCodeVersionHandle ilCodeVersionHandle)
    {

        TargetPointer ilAddress = default;
        if (ilCodeVersionHandle.IsExplicit)
        {
            ILCodeVersionNode ilCodeVersionNode = AsNode(ilCodeVersionHandle);
            ilAddress = ilCodeVersionNode.ILAddress;
        }

        if (ilAddress == TargetPointer.Null)
        {
            // Synthetic ILCodeVersion, get the IL from the module and method def
            ILoader loader = _target.Contracts.Loader;
            ModuleHandle moduleHandle = loader.GetModuleHandleFromModulePtr(ilCodeVersionHandle.Module);
            ilAddress = loader.GetILHeader(moduleHandle, ilCodeVersionHandle.MethodDefinition);
        }

        return ilAddress;
    }
}
