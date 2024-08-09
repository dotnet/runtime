// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal interface INativeCodePointers : IContract
{
    static string IContract.Name { get; } = nameof(NativeCodePointers);
    static IContract IContract.Create(Target target, int version)
    {
        TargetPointer precodeMachineDescriptorAddress = target.ReadGlobalPointer(Constants.Globals.PrecodeMachineDescriptor);
        Data.PrecodeMachineDescriptor precodeMachineDescriptor = target.ProcessedData.GetOrAdd<Data.PrecodeMachineDescriptor>(precodeMachineDescriptorAddress);
        TargetPointer executionManagerCodeRangeMapAddress = target.ReadGlobalPointer(Constants.Globals.ExecutionManagerCodeRangeMapAddress);
        Data.RangeSectionMap rangeSectionMap = target.ProcessedData.GetOrAdd<Data.RangeSectionMap>(executionManagerCodeRangeMapAddress);
        TargetPointer profControlBlockAddress = target.ReadGlobalPointer(Constants.Globals.ProfilerControlBlock);
        Data.ProfControlBlock profControlBlock = target.ProcessedData.GetOrAdd<Data.ProfControlBlock>(profControlBlockAddress);
        return version switch
        {
            1 => new NativeCodePointers_1(target, precodeMachineDescriptor, rangeSectionMap, profControlBlock),
            _ => default(NativeCodePointers),
        };
    }

    public virtual TargetPointer MethodDescFromStubAddress(TargetCodePointer codeAddress) => throw new NotImplementedException();
    public virtual TargetPointer ExecutionManagerGetCodeMethodDesc(TargetCodePointer jittedCodeAddress) => throw new NotImplementedException();

    public virtual NativeCodeVersionHandle GetSpecificNativeCodeVersion(TargetCodePointer ip) => throw new NotImplementedException();
    public virtual NativeCodeVersionHandle GetActiveNativeCodeVersion(TargetPointer methodDesc) => throw new NotImplementedException();

    public virtual bool IsReJITEnabled() => throw new NotImplementedException();
    public virtual bool CodeVersionManagerSupportsMethod(TargetPointer methodDesc) => throw new NotImplementedException();
}

internal struct NativeCodeVersionHandle
{
    // no public constructors
    internal readonly TargetPointer _methodDescAddress;
    internal readonly TargetPointer _codeVersionNodeAddress;
    internal NativeCodeVersionHandle(TargetPointer methodDescAddress, TargetPointer codeVersionNodeAddress)
    {
        if (methodDescAddress != TargetPointer.Null && codeVersionNodeAddress != TargetPointer.Null)
        {
            throw new ArgumentException("Only one of methodDescAddress and codeVersionNodeAddress can be non-null");
        }
        _methodDescAddress = methodDescAddress;
        _codeVersionNodeAddress = codeVersionNodeAddress;
    }

    internal static NativeCodeVersionHandle Invalid => new(TargetPointer.Null, TargetPointer.Null);
    public bool Valid => _methodDescAddress != TargetPointer.Null || _codeVersionNodeAddress != TargetPointer.Null;
}

internal readonly struct NativeCodePointers : INativeCodePointers
{
    // throws NotImplementedException for all methods
}
