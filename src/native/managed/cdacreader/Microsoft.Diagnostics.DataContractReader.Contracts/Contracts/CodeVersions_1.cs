// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly partial struct CodeVersions_1 : ICodeVersions
{
    private readonly Target _target;


    public CodeVersions_1(Target target)
    {
        _target = target;
    }

    ILCodeVersionHandle ICodeVersions.GetActiveILCodeVersion(TargetPointer methodDesc)
    {
        // CodeVersionManager::GetActiveILCodeVersion
        GetModuleAndMethodDesc(methodDesc, out TargetPointer module, out uint methodDefToken);

        ModuleHandle moduleHandle = _target.Contracts.Loader.GetModuleHandle(module);
        TargetPointer ilCodeVersionTable = _target.Contracts.Loader.GetLookupTables(moduleHandle).MethodDefToILCodeVersioningState;
        TargetPointer ilVersionStateAddress = _target.Contracts.Loader.GetModuleLookupMapElement(ilCodeVersionTable, methodDefToken, out var _);
        if (ilVersionStateAddress == TargetPointer.Null)
        {
            return new ILCodeVersionHandle(module, methodDefToken, TargetPointer.Null);
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

        if (!IsExplicit(nativeCodeVersionHandle))
        {
            // There is only a single synthetic NativeCodeVersion per
            // method and it must be on the synthetic ILCodeVersion
            GetModuleAndMethodDesc(
                nativeCodeVersionHandle.MethodDescAddress,
                out TargetPointer module,
                out uint methodDefToken);
            return new ILCodeVersionHandle(module, methodDefToken, TargetPointer.Null);
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

        ModuleHandle moduleHandle = _target.Contracts.Loader.GetModuleHandle(module);
        TargetPointer ilCodeVersionTable = _target.Contracts.Loader.GetLookupTables(moduleHandle).MethodDefToILCodeVersioningState;
        TargetPointer ilVersionStateAddress = _target.Contracts.Loader.GetModuleLookupMapElement(ilCodeVersionTable, methodDefToken, out var _);

        // always add the synthetic version
        yield return new ILCodeVersionHandle(module, methodDefToken, TargetPointer.Null);

        // if explicit versions exist, iterate linked list and return them
        if (ilVersionStateAddress != TargetPointer.Null)
        {
            Data.ILCodeVersioningState ilState = _target.ProcessedData.GetOrAdd<Data.ILCodeVersioningState>(ilVersionStateAddress);
            TargetPointer nodePointer = ilState.FirstVersionNode;
            while (nodePointer != TargetPointer.Null)
            {
                Data.ILCodeVersionNode current = _target.ProcessedData.GetOrAdd<Data.ILCodeVersionNode>(nodePointer);
                yield return new ILCodeVersionHandle(TargetPointer.Null, 0, nodePointer);
                nodePointer = current.Next;
            }
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
            return new NativeCodeVersionHandle(methodDescAddress, codeVersionNodeAddress: TargetPointer.Null);
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
        ModuleHandle mod = loader.GetModuleHandle(modAddr);
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

        if (!IsExplicit(codeVersionHandle))
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

        if (!IsExplicit(ilCodeVersionHandle))
        {
            // if the ILCodeVersion is synthetic, then check if the active NativeCodeVersion is the synthetic one
            NativeCodeVersionHandle provisionalHandle = new(methodDescAddress: methodDesc, codeVersionNodeAddress: TargetPointer.Null);
            if (IsActiveNativeCodeVersion(provisionalHandle))
            {
                return provisionalHandle;
            }
        }

        // Iterate through versioning state nodes and return the active one, matching any IL code version
        Contracts.IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        MethodDescHandle md = rts.GetMethodDescHandle(methodDesc);
        TargetNUInt ilVersionId = GetId(ilCodeVersionHandle);
        return FindFirstCodeVersion(rts, md, (codeVersion) =>
        {
            return (ilVersionId == codeVersion.ILVersionId)
                && ((NativeCodeVersionNodeFlags)codeVersion.Flags).HasFlag(NativeCodeVersionNodeFlags.IsActiveChild);
        });

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
            NativeCodeVersionHandle first = new NativeCodeVersionHandle(md.Address, TargetPointer.Null);
            return first;
        }

        // CodeVersionManager::GetNativeCodeVersion(PTR_MethodDesc, PCODE startAddress)
        return FindFirstCodeVersion(rts, md, (codeVersion) =>
        {
            return codeVersion.MethodDesc == md.Address && codeVersion.NativeCode == startAddress;
        });
    }

    private NativeCodeVersionHandle FindFirstCodeVersion(IRuntimeTypeSystem rts, MethodDescHandle md, Func<Data.NativeCodeVersionNode, bool> predicate)
    {
        // ImplicitCodeVersion stage of NativeCodeVersionIterator::Next()
        TargetPointer versioningStateAddr = rts.GetMethodDescVersioningState(md);
        if (versioningStateAddr == TargetPointer.Null)
            return NativeCodeVersionHandle.Invalid;

        Data.MethodDescVersioningState versioningState = _target.ProcessedData.GetOrAdd<Data.MethodDescVersioningState>(versioningStateAddr);

        // LinkedList stage of NativeCodeVersion::Next, heavily inlined
        TargetPointer currentAddress = versioningState.NativeCodeVersionNode;
        while (currentAddress != TargetPointer.Null)
        {
            Data.NativeCodeVersionNode current = _target.ProcessedData.GetOrAdd<Data.NativeCodeVersionNode>(currentAddress);
            if (predicate(current))
            {
                return new NativeCodeVersionHandle(methodDescAddress: TargetPointer.Null, currentAddress);
            }
            currentAddress = current.Next;
        }
        return NativeCodeVersionHandle.Invalid;
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
                return new ILCodeVersionHandle(module: TargetPointer.Null, methodDef: 0, ilState.ActiveVersionNode);
            case ILCodeVersionKind.Synthetic:
            case ILCodeVersionKind.Unknown:
                return new ILCodeVersionHandle(ilState.ActiveVersionModule, ilState.ActiveVersionMethodDef, TargetPointer.Null);
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

        if (!IsExplicit(nativeCodeVersion))
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

    private static bool IsExplicit(ILCodeVersionHandle handle)
    {
        return handle.ILCodeVersionNode != TargetPointer.Null;
    }

    private static bool IsExplicit(NativeCodeVersionHandle handle)
    {
        return handle.CodeVersionNodeAddress != TargetPointer.Null;
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
        if (!IsExplicit(ilCodeVersionHandle))
        {
            // for non explicit ILCodeVersions, id is always 0
            return new TargetNUInt(0);
        }
        ILCodeVersionNode ilCodeVersionNode = AsNode(ilCodeVersionHandle);
        return ilCodeVersionNode.VersionId;
    }
}
