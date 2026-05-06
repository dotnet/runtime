// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

/// <summary>
/// Classification of a signature type for GC scanning purposes.
/// </summary>
internal enum GcTypeKind
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

/// <summary>
/// Classifies signature types for GC scanning purposes.
/// Implements <see cref="ISignatureTypeProvider{TType, TGenericContext}"/> for use
/// with SRM's <see cref="SignatureDecoder{TType, TGenericContext}"/>.
/// </summary>
internal sealed class GcSignatureTypeProvider
    : ISignatureTypeProvider<GcTypeKind, object?>
{
    public static readonly GcSignatureTypeProvider Instance = new();

    public GcTypeKind GetPrimitiveType(PrimitiveTypeCode typeCode)
        => typeCode switch
        {
            PrimitiveTypeCode.String or PrimitiveTypeCode.Object => GcTypeKind.Ref,
            PrimitiveTypeCode.TypedReference => GcTypeKind.Other,
            _ => GcTypeKind.None,
        };

    public GcTypeKind GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
        => rawTypeKind == (byte)SignatureTypeKind.ValueType ? GcTypeKind.Other : GcTypeKind.Ref;

    public GcTypeKind GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
        => rawTypeKind == (byte)SignatureTypeKind.ValueType ? GcTypeKind.Other : GcTypeKind.Ref;

    public GcTypeKind GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
        => rawTypeKind == (byte)SignatureTypeKind.ValueType ? GcTypeKind.Other : GcTypeKind.Ref;

    public GcTypeKind GetSZArrayType(GcTypeKind elementType) => GcTypeKind.Ref;
    public GcTypeKind GetArrayType(GcTypeKind elementType, ArrayShape shape) => GcTypeKind.Ref;
    public GcTypeKind GetByReferenceType(GcTypeKind elementType) => GcTypeKind.Interior;
    public GcTypeKind GetPointerType(GcTypeKind elementType) => GcTypeKind.None;

    public GcTypeKind GetGenericInstantiation(GcTypeKind genericType, ImmutableArray<GcTypeKind> typeArguments)
        => genericType;

    public GcTypeKind GetGenericMethodParameter(object? genericContext, int index) => GcTypeKind.Ref;
    public GcTypeKind GetGenericTypeParameter(object? genericContext, int index) => GcTypeKind.Ref;
    public GcTypeKind GetFunctionPointerType(MethodSignature<GcTypeKind> signature) => GcTypeKind.None;
    public GcTypeKind GetModifiedType(GcTypeKind modifier, GcTypeKind unmodifiedType, bool isRequired) => unmodifiedType;
    public GcTypeKind GetPinnedType(GcTypeKind elementType) => elementType;
}
