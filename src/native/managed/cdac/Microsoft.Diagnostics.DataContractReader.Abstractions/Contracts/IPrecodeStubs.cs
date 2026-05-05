// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public interface IPrecodeStubs : IContract
{
    static string IContract.Name { get; } = nameof(PrecodeStubs);
    TargetPointer GetMethodDescFromStubAddress(TargetCodePointer entryPoint) => throw new NotImplementedException();

    // Given an interior address within a precode stub and the kind of stub (StubPrecode or FixupPrecode),
    // computes the entry point of the precode.
    TargetPointer GetPrecodeEntryPointFromInteriorAddress(TargetCodePointer interiorAddress, bool isFixupPrecode) => throw new NotImplementedException();
}

public readonly struct PrecodeStubs : IPrecodeStubs
{

}
