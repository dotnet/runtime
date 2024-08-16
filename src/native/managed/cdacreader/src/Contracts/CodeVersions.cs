// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal interface ICodeVersions : IContract
{
    static string IContract.Name { get; } = nameof(CodeVersions);
    static IContract IContract.Create(Target target, int version)
    {
        return version switch
        {
            1 => new CodeVersions_1(target),
            _ => default(CodeVersions),
        };
    }

    public virtual NativeCodeVersionHandle GetSpecificNativeCodeVersion(TargetCodePointer ip) => throw new NotImplementedException();
    public virtual NativeCodeVersionHandle GetActiveNativeCodeVersion(TargetPointer methodDesc) => throw new NotImplementedException();

    public virtual bool CodeVersionManagerSupportsMethod(TargetPointer methodDesc) => throw new NotImplementedException();

    public virtual TargetCodePointer GetNativeCode(NativeCodeVersionHandle codeVersionHandle) => throw new NotImplementedException();

}

internal struct NativeCodeVersionHandle
{
    // no public constructors
    internal readonly TargetPointer MethodDescAddress;
    internal readonly TargetPointer CodeVersionNodeAddress;
    internal NativeCodeVersionHandle(TargetPointer methodDescAddress, TargetPointer codeVersionNodeAddress)
    {
        if (methodDescAddress != TargetPointer.Null && codeVersionNodeAddress != TargetPointer.Null)
        {
            throw new ArgumentException("Only one of methodDescAddress and codeVersionNodeAddress can be non-null");
        }
        MethodDescAddress = methodDescAddress;
        CodeVersionNodeAddress = codeVersionNodeAddress;
    }

    internal static NativeCodeVersionHandle Invalid => new(TargetPointer.Null, TargetPointer.Null);
    public bool Valid => MethodDescAddress != TargetPointer.Null || CodeVersionNodeAddress != TargetPointer.Null;

}

internal readonly struct CodeVersions : ICodeVersions
{
    // throws NotImplementedException for all methods
}
