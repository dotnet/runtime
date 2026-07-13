// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.SignatureHelpers;

/// <summary>
/// A <see cref="SignatureTypeProvider{T}"/> that, when a generic instantiation's
/// exact closed <see cref="TypeHandle"/> is not loaded (so <c>GetConstructedType</c>
/// yields a null handle), falls back to the open generic type's handle.
/// </summary>
/// <remarks>
/// This mirrors the top-level open-generic fallback in
/// <c>CallingConvention_1</c>: the open generic (e.g. <c>Span&lt;T&gt;</c>) is a real,
/// loaded MethodTable whose FieldDescList exposes byref/pointer/interior fields at
/// the same, instantiation-independent, in-struct offsets. Recursing into the open
/// generic therefore yields the correct interior-pointer offsets even when the exact
/// instantiation (e.g. cross-module <c>Span&lt;byte&gt;</c>) isn't registered in the
/// searched module.
/// </remarks>
internal sealed class OpenGenericFallbackSignatureTypeProvider<T> : SignatureTypeProvider<T>
{
    public OpenGenericFallbackSignatureTypeProvider(Target target, Contracts.ModuleHandle moduleHandle)
        : base(target, moduleHandle)
    {
    }

    public override TypeHandle GetGenericInstantiation(TypeHandle genericType, ImmutableArray<TypeHandle> typeArguments)
    {
        TypeHandle constructed = base.GetGenericInstantiation(genericType, typeArguments);
        return constructed.Address != TargetPointer.Null ? constructed : genericType;
    }
}
