// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly partial struct CodeVersions_1 : ICodeVersions
{
    private readonly Target _target;


    public CodeVersions_1(Target target)
    {
        _target = target;
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

    NativeCodeVersionHandle ICodeVersions.GetActiveNativeCodeVersion(TargetPointer methodDesc)
    {
        // CodeVersionManager::GetActiveILCodeVersion
        // then ILCodeVersion::GetActiveNativeCodeVersion
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        MethodDescHandle md = rts.GetMethodDescHandle(methodDesc);
        TargetPointer mtAddr = rts.GetMethodTable(md);
        TypeHandle typeHandle = rts.GetTypeHandle(mtAddr);
        TargetPointer module = rts.GetModule(typeHandle);
        uint methodDefToken = rts.GetMethodToken(md);
        ILCodeVersionHandle methodDefActiveVersion = FindActiveILCodeVersion(module, methodDefToken);
        if (!methodDefActiveVersion.IsValid)
        {
            return NativeCodeVersionHandle.Invalid;
        }
        return FindActiveNativeCodeVersion(methodDefActiveVersion, methodDesc);
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
        if (codeVersionHandle.MethodDescAddress != TargetPointer.Null)
        {
            MethodDescHandle md = _target.Contracts.RuntimeTypeSystem.GetMethodDescHandle(codeVersionHandle.MethodDescAddress);
            return _target.Contracts.RuntimeTypeSystem.GetNativeCode(md);
        }
        else if (codeVersionHandle.CodeVersionNodeAddress != TargetPointer.Null)
        {
            Data.NativeCodeVersionNode nativeCodeVersionNode = _target.ProcessedData.GetOrAdd<Data.NativeCodeVersionNode>(codeVersionHandle.CodeVersionNodeAddress);
            return nativeCodeVersionNode.NativeCode;
        }
        else
        {
            throw new ArgumentException("Invalid NativeCodeVersionHandle");
        }
    }

    internal struct ILCodeVersionHandle
    {
        internal readonly TargetPointer Module;
        internal uint MethodDefinition;
        internal readonly TargetPointer ILCodeVersionNode;
        internal readonly uint RejitId;

        internal ILCodeVersionHandle(TargetPointer module, uint methodDef, TargetPointer ilCodeVersionNodeAddress)
        {
            if (module != TargetPointer.Null && ilCodeVersionNodeAddress != TargetPointer.Null)
                throw new ArgumentException("Both MethodDesc and ILCodeVersionNode cannot be non-null");

            if (module != TargetPointer.Null && methodDef == 0)
                throw new ArgumentException("MethodDefinition must be non-zero if Module is non-null");

            Module = module;
            MethodDefinition = methodDef;
            ILCodeVersionNode = ilCodeVersionNodeAddress;
        }
        public static ILCodeVersionHandle Invalid => new ILCodeVersionHandle(TargetPointer.Null, 0, TargetPointer.Null);
        public bool IsValid => Module != TargetPointer.Null || ILCodeVersionNode != TargetPointer.Null;
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
    private static ILCodeVersionHandle ILCodeVersionHandleFromState(Data.ILCodeVersioningState ilState)
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

    private ILCodeVersionHandle FindActiveILCodeVersion(TargetPointer module, uint methodDefinition)
    {
        // CodeVersionManager::GetActiveILCodeVersion
        ModuleHandle moduleHandle = _target.Contracts.Loader.GetModuleHandle(module);
        TargetPointer ilCodeVersionTable = _target.Contracts.Loader.GetLookupTables(moduleHandle).MethodDefToILCodeVersioningState;
        TargetPointer ilVersionStateAddress = _target.Contracts.Loader.GetModuleLookupMapElement(ilCodeVersionTable, methodDefinition, out var _);
        if (ilVersionStateAddress == TargetPointer.Null)
        {
            return new ILCodeVersionHandle(module, methodDefinition, TargetPointer.Null);
        }
        Data.ILCodeVersioningState ilState = _target.ProcessedData.GetOrAdd<Data.ILCodeVersioningState>(ilVersionStateAddress);
        return ILCodeVersionHandleFromState(ilState);
    }

    [Flags]
    internal enum NativeCodeVersionNodeFlags : uint
    {
        IsActiveChild = 1
    };

    private bool IsActiveNativeCodeVersion(NativeCodeVersionHandle nativeCodeVersion)
    {
        // NativeCodeVersion::IsActiveChildVersion
        if (nativeCodeVersion.MethodDescAddress != TargetPointer.Null)
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
        else if (nativeCodeVersion.CodeVersionNodeAddress != TargetPointer.Null)
        {
            // NativeCodeVersionNode::IsActiveChildVersion
            Data.NativeCodeVersionNode codeVersion = _target.ProcessedData.GetOrAdd<Data.NativeCodeVersionNode>(nativeCodeVersion.CodeVersionNodeAddress);
            return ((NativeCodeVersionNodeFlags)codeVersion.Flags).HasFlag(NativeCodeVersionNodeFlags.IsActiveChild);
        }
        else
        {
            throw new ArgumentException("Invalid NativeCodeVersionHandle");
        }
    }

    private NativeCodeVersionHandle FindActiveNativeCodeVersion(ILCodeVersionHandle methodDefActiveVersion, TargetPointer methodDescAddress)
    {
        TargetNUInt? ilVersionId = default;
        if (methodDefActiveVersion.Module != TargetPointer.Null)
        {
            NativeCodeVersionHandle provisionalHandle = new NativeCodeVersionHandle(methodDescAddress: methodDescAddress, codeVersionNodeAddress: TargetPointer.Null);
            if (IsActiveNativeCodeVersion(provisionalHandle))
            {
                return provisionalHandle;
            }
        }
        else
        {
            // Get the explicit IL code version
            Debug.Assert(methodDefActiveVersion.ILCodeVersionNode != TargetPointer.Null);
            Data.ILCodeVersionNode ilCodeVersion = _target.ProcessedData.GetOrAdd<Data.ILCodeVersionNode>(methodDefActiveVersion.ILCodeVersionNode);
            ilVersionId = ilCodeVersion.VersionId;
        }

        // Iterate through versioning state nodes and return the active one, matching any IL code version
        Contracts.IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        MethodDescHandle md = rts.GetMethodDescHandle(methodDescAddress);
        return FindFirstCodeVersion(rts, md, (codeVersion) =>
        {
            return (!ilVersionId.HasValue || ilVersionId.Value.Value == codeVersion.ILVersionId.Value)
                && ((NativeCodeVersionNodeFlags)codeVersion.Flags).HasFlag(NativeCodeVersionNodeFlags.IsActiveChild);
        });
    }

}
