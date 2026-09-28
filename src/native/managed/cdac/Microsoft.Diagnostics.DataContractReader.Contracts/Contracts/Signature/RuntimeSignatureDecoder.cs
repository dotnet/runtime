// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.Diagnostics.DataContractReader.SignatureHelpers;

/// <summary>
/// Decodes signature blobs. Behaves identically to SRM's
/// <see cref="SignatureDecoder{TType, TGenericContext}"/> for standard ECMA-335 type codes,
/// with added support for runtime-internal types
/// (<c>ELEMENT_TYPE_INTERNAL</c> 0x21 and <c>ELEMENT_TYPE_CMOD_INTERNAL</c> 0x22).
/// </summary>
internal readonly struct RuntimeSignatureDecoder<TType, TGenericContext>
{
    private const int ELEMENT_TYPE_CMOD_INTERNAL = 0x22;
    private const int ELEMENT_TYPE_INTERNAL = 0x21;

    private readonly IRuntimeSignatureTypeProvider<TType, TGenericContext> _provider;
    private readonly MetadataReader _metadataReader;
    private readonly TGenericContext _genericContext;
    private readonly int _pointerSize;

    public RuntimeSignatureDecoder(
        IRuntimeSignatureTypeProvider<TType, TGenericContext> provider,
        Target target,
        MetadataReader metadataReader,
        TGenericContext genericContext)
    {
        _provider = provider;
        _metadataReader = metadataReader;
        _genericContext = genericContext;
        _pointerSize = target.PointerSize;
    }

    /// <summary>
    /// Decodes a type embedded in a signature and advances the reader past the type.
    /// </summary>
    public TType DecodeType(ref BlobReader blobReader, bool allowTypeSpecifications = false)
    {
        return DecodeType(ref blobReader, allowTypeSpecifications, blobReader.ReadCompressedInteger());
    }

    private TType DecodeType(ref BlobReader blobReader, bool allowTypeSpecifications, int typeCode)
    {
        TType elementType;
        int index;

        switch (typeCode)
        {
            case (int)SignatureTypeCode.Boolean:
            case (int)SignatureTypeCode.Char:
            case (int)SignatureTypeCode.SByte:
            case (int)SignatureTypeCode.Byte:
            case (int)SignatureTypeCode.Int16:
            case (int)SignatureTypeCode.UInt16:
            case (int)SignatureTypeCode.Int32:
            case (int)SignatureTypeCode.UInt32:
            case (int)SignatureTypeCode.Int64:
            case (int)SignatureTypeCode.UInt64:
            case (int)SignatureTypeCode.Single:
            case (int)SignatureTypeCode.Double:
            case (int)SignatureTypeCode.IntPtr:
            case (int)SignatureTypeCode.UIntPtr:
            case (int)SignatureTypeCode.Object:
            case (int)SignatureTypeCode.String:
            case (int)SignatureTypeCode.Void:
            case (int)SignatureTypeCode.TypedReference:
                return _provider.GetPrimitiveType((PrimitiveTypeCode)typeCode);

            case (int)SignatureTypeCode.Pointer:
                elementType = DecodeType(ref blobReader);
                return _provider.GetPointerType(elementType);

            case (int)SignatureTypeCode.ByReference:
                elementType = DecodeType(ref blobReader);
                return _provider.GetByReferenceType(elementType);

            case (int)SignatureTypeCode.Pinned:
                elementType = DecodeType(ref blobReader);
                return _provider.GetPinnedType(elementType);

            case (int)SignatureTypeCode.SZArray:
                elementType = DecodeType(ref blobReader);
                return _provider.GetSZArrayType(elementType);

            case (int)SignatureTypeCode.FunctionPointer:
                MethodSignature<TType> methodSignature = DecodeMethodSignature(ref blobReader);
                return _provider.GetFunctionPointerType(methodSignature);

            case (int)SignatureTypeCode.Array:
                return DecodeArrayType(ref blobReader);

            case (int)SignatureTypeCode.RequiredModifier:
                return DecodeModifiedType(ref blobReader, isRequired: true);

            case (int)SignatureTypeCode.OptionalModifier:
                return DecodeModifiedType(ref blobReader, isRequired: false);

            case (int)SignatureTypeCode.GenericTypeInstance:
                return DecodeGenericTypeInstance(ref blobReader);

            case (int)SignatureTypeCode.GenericTypeParameter:
                index = blobReader.ReadCompressedInteger();
                return _provider.GetGenericTypeParameter(_genericContext, index);

            case (int)SignatureTypeCode.GenericMethodParameter:
                index = blobReader.ReadCompressedInteger();
                return _provider.GetGenericMethodParameter(_genericContext, index);

            case (int)SignatureTypeKind.Class:
            case (int)SignatureTypeKind.ValueType:
                return DecodeTypeHandle(ref blobReader, (byte)typeCode, allowTypeSpecifications);

            case ELEMENT_TYPE_INTERNAL:
                return DecodeInternalType(ref blobReader);

            case ELEMENT_TYPE_CMOD_INTERNAL:
                return DecodeInternalModifiedType(ref blobReader);

            default:
                throw new BadImageFormatException($"Unexpected signature type code: 0x{typeCode:X2}");
        }
    }

    /// <summary>
    /// Decodes a list of types, with at least one instance that is preceded by its count as a compressed integer.
    /// </summary>
    private ImmutableArray<TType> DecodeTypeSequence(ref BlobReader blobReader)
    {
        int count = blobReader.ReadCompressedInteger();
        if (count == 0)
        {
            throw new BadImageFormatException("Signature type sequence must have at least one element");
        }

        var types = ImmutableArray.CreateBuilder<TType>(count);
        for (int i = 0; i < count; i++)
        {
            types.Add(DecodeType(ref blobReader));
        }
        return types.MoveToImmutable();
    }

    /// <summary>
    /// Decodes a method (definition, reference, or standalone) or property signature blob.
    /// </summary>
    public MethodSignature<TType> DecodeMethodSignature(ref BlobReader blobReader)
    {
        SignatureHeader header = blobReader.ReadSignatureHeader();
        CheckMethodOrPropertyHeader(header);

        int genericParameterCount = 0;
        if (header.IsGeneric)
        {
            genericParameterCount = blobReader.ReadCompressedInteger();
        }

        int parameterCount = blobReader.ReadCompressedInteger();
        TType returnType = DecodeType(ref blobReader);
        ImmutableArray<TType> parameterTypes;
        int requiredParameterCount;

        if (parameterCount == 0)
        {
            requiredParameterCount = 0;
            parameterTypes = ImmutableArray<TType>.Empty;
        }
        else
        {
            var parameterBuilder = ImmutableArray.CreateBuilder<TType>(parameterCount);
            int parameterIndex;

            for (parameterIndex = 0; parameterIndex < parameterCount; parameterIndex++)
            {
                int typeCode = blobReader.ReadCompressedInteger();
                if (typeCode == (int)SignatureTypeCode.Sentinel)
                {
                    break;
                }
                parameterBuilder.Add(DecodeType(ref blobReader, allowTypeSpecifications: false, typeCode: typeCode));
            }

            requiredParameterCount = parameterIndex;
            for (; parameterIndex < parameterCount; parameterIndex++)
            {
                parameterBuilder.Add(DecodeType(ref blobReader));
            }
            parameterTypes = parameterBuilder.MoveToImmutable();
        }

        return new MethodSignature<TType>(header, returnType, requiredParameterCount, genericParameterCount, parameterTypes);
    }

    /// <summary>
    /// Decodes a local variable signature blob and advances the reader past the signature.
    /// </summary>
    public ImmutableArray<TType> DecodeLocalSignature(ref BlobReader blobReader)
    {
        SignatureHeader header = blobReader.ReadSignatureHeader();
        CheckHeader(header, SignatureKind.LocalVariables);
        return DecodeTypeSequence(ref blobReader);
    }

    /// <summary>
    /// Decodes a field signature blob and advances the reader past the signature.
    /// </summary>
    public TType DecodeFieldSignature(ref BlobReader blobReader)
    {
        SignatureHeader header = blobReader.ReadSignatureHeader();
        CheckHeader(header, SignatureKind.Field);
        return DecodeType(ref blobReader);
    }

    private TType DecodeArrayType(ref BlobReader blobReader)
    {
        TType elementType = DecodeType(ref blobReader);
        int rank = blobReader.ReadCompressedInteger();
        var sizes = ImmutableArray<int>.Empty;
        var lowerBounds = ImmutableArray<int>.Empty;

        int sizesCount = blobReader.ReadCompressedInteger();
        if (sizesCount > 0)
        {
            var builder = ImmutableArray.CreateBuilder<int>(sizesCount);
            for (int i = 0; i < sizesCount; i++)
            {
                builder.Add(blobReader.ReadCompressedInteger());
            }
            sizes = builder.MoveToImmutable();
        }

        int lowerBoundsCount = blobReader.ReadCompressedInteger();
        if (lowerBoundsCount > 0)
        {
            var builder = ImmutableArray.CreateBuilder<int>(lowerBoundsCount);
            for (int i = 0; i < lowerBoundsCount; i++)
            {
                builder.Add(blobReader.ReadCompressedSignedInteger());
            }
            lowerBounds = builder.MoveToImmutable();
        }

        return _provider.GetArrayType(elementType, new ArrayShape(rank, sizes, lowerBounds));
    }

    private TType DecodeGenericTypeInstance(ref BlobReader blobReader)
    {
        TType genericType = DecodeType(ref blobReader);
        ImmutableArray<TType> types = DecodeTypeSequence(ref blobReader);
        return _provider.GetGenericInstantiation(genericType, types);
    }

    private TType DecodeModifiedType(ref BlobReader blobReader, bool isRequired)
    {
        // A standard modifier may be followed by an internal modifier; allow type specifications
        // for the modifier handle (matches SRM behavior).
        TType modifier = DecodeTypeHandle(ref blobReader, 0, allowTypeSpecifications: true);
        TType unmodifiedType = DecodeType(ref blobReader);
        return _provider.GetModifiedType(modifier, unmodifiedType, isRequired);
    }

    private TType DecodeInternalType(ref BlobReader blobReader)
    {
        ulong val = ReadPointerSized(ref blobReader);
        return _provider.GetInternalType(new TargetPointer(val));
    }

    private TType DecodeInternalModifiedType(ref BlobReader blobReader)
    {
        bool isRequired = blobReader.ReadByte() != 0;
        ulong val = ReadPointerSized(ref blobReader);
        TType unmodifiedType = DecodeType(ref blobReader);
        return _provider.GetInternalModifiedType(new TargetPointer(val), unmodifiedType, isRequired);
    }

    private TType DecodeTypeHandle(ref BlobReader blobReader, byte rawTypeKind, bool allowTypeSpecifications)
    {
        EntityHandle handle = blobReader.ReadTypeHandle();
        if (!handle.IsNil)
        {
            switch (handle.Kind)
            {
                case HandleKind.TypeDefinition:
                    return _provider.GetTypeFromDefinition(_metadataReader, (TypeDefinitionHandle)handle, rawTypeKind);

                case HandleKind.TypeReference:
                    return _provider.GetTypeFromReference(_metadataReader, (TypeReferenceHandle)handle, rawTypeKind);

                case HandleKind.TypeSpecification:
                    if (!allowTypeSpecifications)
                    {
                        throw new BadImageFormatException("TypeSpecification handle not allowed in this context");
                    }
                    return _provider.GetTypeFromSpecification(_metadataReader, _genericContext, (TypeSpecificationHandle)handle, rawTypeKind);
            }
        }

        throw new BadImageFormatException("Expected TypeDef, TypeRef, or TypeSpec handle");
    }

    private ulong ReadPointerSized(ref BlobReader blobReader)
    {
        return _pointerSize == 8 ? blobReader.ReadUInt64() : blobReader.ReadUInt32();
    }

    private static void CheckHeader(SignatureHeader header, SignatureKind expectedKind)
    {
        if (header.Kind != expectedKind)
        {
            throw new BadImageFormatException($"Expected signature header {expectedKind}, got {header.Kind} (raw 0x{header.RawValue:X2})");
        }
    }

    private static void CheckMethodOrPropertyHeader(SignatureHeader header)
    {
        SignatureKind kind = header.Kind;
        if (kind != SignatureKind.Method && kind != SignatureKind.Property)
        {
            throw new BadImageFormatException($"Expected Method or Property signature header, got {kind} (raw 0x{header.RawValue:X2})");
        }
    }
}
