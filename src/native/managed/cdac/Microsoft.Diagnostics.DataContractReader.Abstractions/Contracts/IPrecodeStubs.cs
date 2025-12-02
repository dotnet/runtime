// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public interface IPrecodeStubs : IContract
{
    static string IContract.Name { get; } = nameof(PrecodeStubs);
    TargetPointer GetMethodDescFromStubAddress(TargetCodePointer entryPoint) => throw new NotImplementedException();
}

public readonly struct PrecodeStubs : IPrecodeStubs
{

}
