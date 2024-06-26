// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct ModuleHandle
{
    internal ModuleHandle(TargetPointer address)
    {
        Address = address;
    }

    internal TargetPointer Address { get; }
}

internal interface ILoader : IContract
{
    static string IContract.Name => nameof(Loader);
    static IContract IContract.Create(Target target, int version)
    {
        return version switch
        {
            1 => new Loader_1(target),
            _ => default(Loader),
        };
    }

    public virtual ModuleHandle GetModuleHandle(TargetPointer targetPointer) => throw new NotImplementedException();
}

internal readonly struct Loader : ILoader
{
    // Everything throws NotImplementedException
}
