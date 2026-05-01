// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

/// <summary>
/// Classification of a signature type for GC scanning purposes.
/// </summary>
public enum GcTypeKind
{
    /// <summary>Not a GC reference (primitives, pointers).</summary>
    None,
    /// <summary>Object reference (class, string, array).</summary>
    Ref,
    /// <summary>Interior pointer (byref).</summary>
    Interior,
    /// <summary>Value type that may contain embedded GC references.</summary>
    Other,
}

public interface ISignatureDecoder : IContract
{
    static string IContract.Name { get; } = nameof(SignatureDecoder);

    TypeHandle DecodeFieldSignature(BlobHandle blobHandle, ModuleHandle moduleHandle, TypeHandle ctx) => throw new NotImplementedException();

    /// <summary>
    /// Decodes a method's signature for GC scanning purposes, classifying each parameter
    /// as a GC reference, interior pointer, value type, or non-GC type.
    /// Handles ELEMENT_TYPE_INTERNAL via the runtime type system.
    /// </summary>
    MethodSignature<GcTypeKind> DecodeMethodSignatureForGC(BlobHandle blobHandle, ModuleHandle moduleHandle) => throw new NotImplementedException();
}

public readonly struct SignatureDecoder : ISignatureDecoder
{
    // Everything throws NotImplementedException
}
