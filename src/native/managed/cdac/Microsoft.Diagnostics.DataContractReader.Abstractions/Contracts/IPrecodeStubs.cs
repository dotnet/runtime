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

    /// <summary>
    /// If the given code pointer is an interpreter precode, returns the actual interpreter code
    /// address (ByteCodeAddr). Otherwise returns the original address unchanged.
    /// This method never throws; it returns the original address on any failure.
    /// Mirrors GetInterpreterCodeFromInterpreterPrecodeIfPresent in native code (precode.cpp).
    /// </summary>
    TargetCodePointer GetInterpreterCodeFromInterpreterPrecodeIfPresent(TargetCodePointer entryPoint) => entryPoint;
}

public readonly struct PrecodeStubs : IPrecodeStubs
{

}
