// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal interface ICodeVersions : IContract
{
    static string IContract.Name { get; } = nameof(CodeVersions);

    public virtual ILCodeVersionHandle GetActiveILCodeVersion(TargetPointer methodDesc) => throw new NotImplementedException();

    public virtual ILCodeVersionHandle GetILCodeVersion(NativeCodeVersionHandle codeVersionHandle) => throw new NotImplementedException();

    public virtual IEnumerable<ILCodeVersionHandle> GetILCodeVersions(TargetPointer methodDesc) => throw new NotImplementedException();

    public virtual NativeCodeVersionHandle GetNativeCodeVersionForIP(TargetCodePointer ip) => throw new NotImplementedException();

    public virtual NativeCodeVersionHandle GetActiveNativeCodeVersionForILCodeVersion(TargetPointer methodDesc, ILCodeVersionHandle ilCodeVersionHandle) => throw new NotImplementedException();

    public virtual TargetCodePointer GetNativeCode(NativeCodeVersionHandle codeVersionHandle) => throw new NotImplementedException();

    public virtual TargetPointer GetGCStressCodeCopy(NativeCodeVersionHandle codeVersionHandle) => throw new NotImplementedException();

    public virtual bool CodeVersionManagerSupportsMethod(TargetPointer methodDesc) => throw new NotImplementedException();
}

internal readonly struct ILCodeVersionHandle
{
    internal readonly TargetPointer Module;
    internal readonly uint MethodDefinition;
    internal readonly TargetPointer ILCodeVersionNode;
    private ILCodeVersionHandle(TargetPointer module, uint methodDef, TargetPointer ilCodeVersionNodeAddress)
    {
        if (module != TargetPointer.Null && ilCodeVersionNodeAddress != TargetPointer.Null)
            throw new ArgumentException("Both MethodDesc and ILCodeVersionNode cannot be non-null");

        if (module != TargetPointer.Null && methodDef == 0)
            throw new ArgumentException("MethodDefinition must be non-zero if Module is non-null");

        if (module == TargetPointer.Null && methodDef != 0)
            throw new ArgumentException("MethodDefinition must be zero if Module is null");

        Module = module;
        MethodDefinition = methodDef;
        ILCodeVersionNode = ilCodeVersionNodeAddress;
    }

    // for more information on Explicit/Synthetic code versions see docs/design/features/code-versioning.md
    internal static ILCodeVersionHandle CreateExplicit(TargetPointer ilCodeVersionNodeAddress) =>
        new ILCodeVersionHandle(TargetPointer.Null, 0, ilCodeVersionNodeAddress);
    internal static ILCodeVersionHandle CreateSynthetic(TargetPointer module, uint methodDef) =>
        new ILCodeVersionHandle(module, methodDef, TargetPointer.Null);

    public static ILCodeVersionHandle Invalid { get; } = new(TargetPointer.Null, 0, TargetPointer.Null);

    public bool IsValid => Module != TargetPointer.Null || ILCodeVersionNode != TargetPointer.Null;

    internal bool IsExplicit => ILCodeVersionNode != TargetPointer.Null;
}

internal readonly struct NativeCodeVersionHandle
{
    // no public constructors
    internal readonly TargetPointer MethodDescAddress;
    internal readonly TargetPointer CodeVersionNodeAddress;
    private NativeCodeVersionHandle(TargetPointer methodDescAddress, TargetPointer codeVersionNodeAddress)
    {
        if (methodDescAddress != TargetPointer.Null && codeVersionNodeAddress != TargetPointer.Null)
        {
            throw new ArgumentException("Only one of methodDescAddress and codeVersionNodeAddress can be non-null");
        }
        MethodDescAddress = methodDescAddress;
        CodeVersionNodeAddress = codeVersionNodeAddress;
    }

    // for more information on Explicit/Synthetic code versions see docs/design/features/code-versioning.md
    internal static NativeCodeVersionHandle CreateExplicit(TargetPointer codeVersionNodeAddress) =>
        new NativeCodeVersionHandle(TargetPointer.Null, codeVersionNodeAddress);
    internal static NativeCodeVersionHandle CreateSynthetic(TargetPointer methodDescAddress) =>
        new NativeCodeVersionHandle(methodDescAddress, TargetPointer.Null);

    public static NativeCodeVersionHandle Invalid { get; } = new(TargetPointer.Null, TargetPointer.Null);

    public bool Valid => MethodDescAddress != TargetPointer.Null || CodeVersionNodeAddress != TargetPointer.Null;

    internal bool IsExplicit => CodeVersionNodeAddress != TargetPointer.Null;
}

internal readonly struct CodeVersions : ICodeVersions
{
    // throws NotImplementedException for all methods
}
