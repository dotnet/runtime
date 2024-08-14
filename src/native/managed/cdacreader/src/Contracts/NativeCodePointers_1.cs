// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly partial struct NativeCodePointers_1 : INativeCodePointers
{
    private readonly Target _target;
    private readonly PrecodeContract _precodeContract;
    private readonly ExecutionManagerContract _executionManagerContract;
    private readonly NativeCodeVersionContract _nativeCodeVersionContract;


    public NativeCodePointers_1(Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor, Data.RangeSectionMap topRangeSectionMap, Data.ProfControlBlock profControlBlock)
    {
        _target = target;
        _precodeContract = new PrecodeContract(target, precodeMachineDescriptor);
        _executionManagerContract = new ExecutionManagerContract(target, topRangeSectionMap, profControlBlock);
        _nativeCodeVersionContract = new NativeCodeVersionContract(target);
    }

    TargetPointer INativeCodePointers.MethodDescFromStubAddress(TargetCodePointer entryPoint)
    {
        ValidPrecode precode = _precodeContract.GetPrecodeFromEntryPoint(entryPoint);

        return precode.GetMethodDesc(_target, _precodeContract.MachineDescriptor);
    }


    TargetPointer INativeCodePointers.ExecutionManagerGetCodeMethodDesc(TargetCodePointer jittedCodeAddress)
    {
        EECodeInfo? info = _executionManagerContract.GetEECodeInfo(jittedCodeAddress);
        if (info == null || !info.Valid)
        {
            throw new InvalidOperationException($"Failed to get EECodeInfo for {jittedCodeAddress}");
        }
        return info.MethodDescAddress;
    }

    NativeCodeVersionHandle INativeCodePointers.GetSpecificNativeCodeVersion(TargetCodePointer ip)
    {
        // ExecutionManager::GetNativeCodeVersion(PCODE ip))
        // and EECodeInfo::GetNativeCodeVersion
        EECodeInfo? info = _executionManagerContract.GetEECodeInfo(ip);
        if (info == null || !info.Valid)
        {
            return NativeCodeVersionHandle.Invalid;
        }
        TargetPointer methodDescAddress = info.MethodDescAddress;
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
            return _nativeCodeVersionContract.GetSpecificNativeCodeVersion(rts, md, info.StartAddress);
        }
    }

    NativeCodeVersionHandle INativeCodePointers.GetActiveNativeCodeVersion(TargetPointer methodDesc)
    {
        // CodeVersionManager::GetActiveILCodeVersion
        // then ILCodeVersion::GetActiveNativeCodeVersion
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        MethodDescHandle md = rts.GetMethodDescHandle(methodDesc);
        TargetPointer mtAddr = rts.GetMethodTable(md);
        TypeHandle typeHandle = rts.GetTypeHandle(mtAddr);
        TargetPointer module = rts.GetModule(typeHandle);
        uint methodDefToken = rts.GetMethodToken(md);
        ILCodeVersionHandle methodDefActiveVersion = _nativeCodeVersionContract.FindActiveILCodeVersion(module, methodDefToken);
        if (!methodDefActiveVersion.IsValid)
        {
            return NativeCodeVersionHandle.Invalid;
        }
        return _nativeCodeVersionContract.FindActiveNativeCodeVersion(methodDefActiveVersion, methodDesc);
    }

    bool INativeCodePointers.IsReJITEnabled()
    {
        return _executionManagerContract.IsReJITEnabled();
    }

    bool INativeCodePointers.CodeVersionManagerSupportsMethod(TargetPointer methodDescAddress)
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

    TargetCodePointer INativeCodePointers.GetNativeCode(NativeCodeVersionHandle codeVersionHandle)
    {
        if (codeVersionHandle.MethodDescAddress != TargetPointer.Null)
        {
            MethodDescHandle md = _target.Contracts.RuntimeTypeSystem.GetMethodDescHandle(codeVersionHandle.MethodDescAddress);
            return _target.Contracts.RuntimeTypeSystem.GetNativeCode(md);
        }
        else if (codeVersionHandle.CodeVersionNodeAddress != TargetPointer.Null)
        {
            throw new NotImplementedException(); // TODO[cdac]: get native code from NativeCodeVersionNode
        }
        else
        {
            throw new ArgumentException("Invalid NativeCodeVersionHandle");
        }
    }

}
