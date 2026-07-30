// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public enum MetadataAddressKind
{
    ReadOnly,
    ReadWriteSavedCopy,
}

public interface IEcmaMetadata : IContract
{
    static string IContract.Name { get; } = nameof(EcmaMetadata);
    TargetSpan GetMetadataAddress(ModuleHandle handle, MetadataAddressKind kind) => throw new NotImplementedException();
    MetadataReader? GetMetadata(ModuleHandle module, bool requireReadWriteMetadata = false) => throw new NotImplementedException();
}

public readonly struct EcmaMetadata : IEcmaMetadata
{
    // Everything throws NotImplementedException
}
