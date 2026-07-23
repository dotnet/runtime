// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.CallingConventionHelpers;

internal readonly struct SignatureTypeInfo
{
    public SignatureTypeInfo(
        CorElementType elementType,
        ITypeHandle? exactTypeHandle,
        ITypeHandle? genericTypeDefinition = null,
        ImmutableArray<SignatureTypeInfo> typeArguments = default)
    {
        ElementType = elementType;
        ExactTypeHandle = exactTypeHandle;
        GenericTypeDefinition = genericTypeDefinition;
        TypeArguments = typeArguments.IsDefault ? [] : typeArguments;
    }

    public CorElementType ElementType { get; }
    public ITypeHandle? ExactTypeHandle { get; }
    public ITypeHandle? GenericTypeDefinition { get; }
    public ImmutableArray<SignatureTypeInfo> TypeArguments { get; }
}
