// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public interface IEcmaMetadata : IContract
{
    static string IContract.Name { get; } = nameof(EcmaMetadata);
    TargetSpan GetReadOnlyMetadataAddress(ModuleHandle handle) => throw new NotImplementedException();
    MetadataReader? GetMetadata(ModuleHandle module) => throw new NotImplementedException();
}

public readonly struct EcmaMetadata : IEcmaMetadata
{
    // Everything throws NotImplementedException
}
