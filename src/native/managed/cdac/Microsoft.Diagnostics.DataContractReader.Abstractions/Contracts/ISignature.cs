// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public interface ISignature : IContract
{
    static string IContract.Name { get; } = nameof(Signature);
    TypeHandle DecodeFieldSignature(BlobHandle blobHandle, ModuleHandle moduleHandle, TypeHandle ctx) => throw new NotImplementedException();
}

public readonly struct Signature : ISignature
{
    // Everything throws NotImplementedException
}
