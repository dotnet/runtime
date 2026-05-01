// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Reflection.Metadata;
using Microsoft.Diagnostics.DataContractReader.SignatureHelpers;

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
/// Implements <see cref="IRuntimeSignatureTypeProvider{TType, TGenericContext}"/> which
/// is a superset of SRM's <see cref="ISignatureTypeProvider{TType, TGenericContext}"/>,
/// adding support for <c>ELEMENT_TYPE_INTERNAL</c>.
/// </summary>
internal sealed class GcSignatureTypeProvider
    : IRuntimeSignatureTypeProvider<GcTypeKind, object?>
{
    private readonly Target _target;
    private readonly ModuleHandle _moduleHandle;

    public GcSignatureTypeProvider(Target target, ModuleHandle moduleHandle)
    {
        _target = target;
        _moduleHandle = moduleHandle;
    }

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
    public GcTypeKind GetInternalModifiedType(Target target, TargetPointer typeHandlePointer, GcTypeKind unmodifiedType, bool isRequired) => unmodifiedType;
    public GcTypeKind GetPinnedType(GcTypeKind elementType) => elementType;

    public GcTypeKind GetInternalType(Target target, TargetPointer typeHandlePointer)
    {
        if (typeHandlePointer == TargetPointer.Null)
            return GcTypeKind.None;

        try
        {
            IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
            TypeHandle th = rts.GetTypeHandle(typeHandlePointer);
            CorElementType corType = rts.GetSignatureCorElementType(th);

            return corType switch
            {
                CorElementType.Void or CorElementType.Boolean or CorElementType.Char
                    or CorElementType.I1 or CorElementType.U1
                    or CorElementType.I2 or CorElementType.U2
                    or CorElementType.I4 or CorElementType.U4
                    or CorElementType.I8 or CorElementType.U8
                    or CorElementType.R4 or CorElementType.R8
                    or CorElementType.I or CorElementType.U
                    or CorElementType.FnPtr or CorElementType.Ptr
                    => GcTypeKind.None,

                CorElementType.Byref => GcTypeKind.Interior,
                CorElementType.ValueType => GcTypeKind.Other,

                _ => GcTypeKind.Ref,
            };
        }
        catch
        {
            return GcTypeKind.Ref;
        }
    }
}
