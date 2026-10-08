// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.SignatureHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.CallingConventionHelpers;

internal readonly record struct SignatureTypeContext(
    MethodDescHandle? Method,
    SignatureTypeInfo OwningType);

internal sealed class SignatureTypeInfoProvider
    : IRuntimeSignatureTypeProvider<SignatureTypeInfo, SignatureTypeContext>
{
    private readonly Target _target;
    private readonly ModuleHandle _moduleHandle;
    private readonly ILoader _loader;
    private readonly IRuntimeTypeSystem _rts;

    internal SignatureTypeInfoProvider(Target target, ModuleHandle moduleHandle)
    {
        _target = target;
        _moduleHandle = moduleHandle;
        _loader = target.Contracts.Loader;
        _rts = target.Contracts.RuntimeTypeSystem;
    }

    public SignatureTypeInfo GetArrayType(SignatureTypeInfo elementType, ArrayShape shape)
        => CreateConstructedType(elementType, CorElementType.Array, shape.Rank);

    public SignatureTypeInfo GetByReferenceType(SignatureTypeInfo elementType)
        => CreateConstructedType(elementType, CorElementType.Byref, rank: 0);

    public SignatureTypeInfo GetFunctionPointerType(MethodSignature<SignatureTypeInfo> signature)
        => new(CorElementType.FnPtr, _rts.GetPrimitiveType(CorElementType.I));

    public SignatureTypeInfo GetGenericInstantiation(
        SignatureTypeInfo genericType,
        ImmutableArray<SignatureTypeInfo> typeArguments)
    {
        ITypeHandle? exactType = null;
        if (genericType.ExactTypeHandle is not null)
        {
            ImmutableArray<ITypeHandle?> exactArguments = typeArguments
                .Select(static typeArgument => typeArgument.ExactTypeHandle)
                .ToImmutableArray();
            exactType = _rts.GetConstructedType(
                genericType.ExactTypeHandle,
                CorElementType.GenericInst,
                rank: 0,
                exactArguments);
        }

        return new SignatureTypeInfo(
            genericType.ElementType,
            exactType,
            genericType.ExactTypeHandle ?? genericType.GenericTypeDefinition,
            typeArguments);
    }

    public SignatureTypeInfo GetGenericMethodParameter(SignatureTypeContext context, int index)
    {
        if (context.Method is not MethodDescHandle method)
        {
            return new SignatureTypeInfo(CorElementType.MVar, exactTypeHandle: null);
        }

        ITypeHandle exactType = _rts.GetGenericMethodInstantiation(method)[index];
        return FromExactType(exactType);
    }

    public SignatureTypeInfo GetGenericTypeParameter(SignatureTypeContext context, int index)
    {
        if ((uint)index < (uint)context.OwningType.TypeArguments.Length)
        {
            return context.OwningType.TypeArguments[index];
        }

        if (context.OwningType.ExactTypeHandle is ITypeHandle exactOwningType)
        {
            ITypeHandle exactType = _rts.GetInstantiation(exactOwningType)[index];
            return FromExactType(exactType);
        }

        return new SignatureTypeInfo(CorElementType.Var, exactTypeHandle: null);
    }

    public SignatureTypeInfo GetModifiedType(
        SignatureTypeInfo modifier,
        SignatureTypeInfo unmodifiedType,
        bool isRequired)
        => unmodifiedType;

    public SignatureTypeInfo GetPinnedType(SignatureTypeInfo elementType)
        => elementType;

    public SignatureTypeInfo GetPointerType(SignatureTypeInfo elementType)
        => CreateConstructedType(elementType, CorElementType.Ptr, rank: 0);

    public SignatureTypeInfo GetPrimitiveType(PrimitiveTypeCode typeCode)
    {
        CorElementType elementType = (CorElementType)typeCode;
        return new SignatureTypeInfo(elementType, _rts.GetPrimitiveType(elementType));
    }

    public SignatureTypeInfo GetSZArrayType(SignatureTypeInfo elementType)
        => CreateConstructedType(elementType, CorElementType.SzArray, rank: 1);

    public SignatureTypeInfo GetTypeFromDefinition(
        MetadataReader reader,
        TypeDefinitionHandle handle,
        byte rawTypeKind)
        => GetTypeFromToken(
            MetadataTokens.GetToken(handle),
            ModuleLookupMapKind.TypeDefToMethodTable,
            rawTypeKind);

    public SignatureTypeInfo GetTypeFromReference(
        MetadataReader reader,
        TypeReferenceHandle handle,
        byte rawTypeKind)
        => GetTypeFromToken(
            MetadataTokens.GetToken(handle),
            ModuleLookupMapKind.TypeRefToMethodTable,
            rawTypeKind);

    public SignatureTypeInfo GetTypeFromSpecification(
        MetadataReader reader,
        SignatureTypeContext context,
        TypeSpecificationHandle handle,
        byte rawTypeKind)
    {
        BlobReader blobReader = reader.GetBlobReader(reader.GetTypeSpecification(handle).Signature);
        RuntimeSignatureDecoder<SignatureTypeInfo, SignatureTypeContext> decoder =
            new(this, _target, reader, context);
        return decoder.DecodeType(ref blobReader, allowTypeSpecifications: true);
    }

    public SignatureTypeInfo GetInternalType(TargetPointer typeHandlePointer)
        => typeHandlePointer == TargetPointer.Null
            ? new SignatureTypeInfo(CorElementType.Internal, exactTypeHandle: null)
            : FromExactType(_rts.GetTypeHandle(typeHandlePointer));

    public SignatureTypeInfo GetInternalModifiedType(
        TargetPointer typeHandlePointer,
        SignatureTypeInfo unmodifiedType,
        bool isRequired)
        => unmodifiedType;

    internal SignatureTypeInfo FromExactType(ITypeHandle exactType)
    {
        ReadOnlySpan<ITypeHandle> exactArguments = _rts.GetInstantiation(exactType);
        ImmutableArray<SignatureTypeInfo>.Builder typeArguments =
            ImmutableArray.CreateBuilder<SignatureTypeInfo>(exactArguments.Length);
        for (int i = 0; i < exactArguments.Length; i++)
        {
            typeArguments.Add(FromExactType(exactArguments[i]));
        }

        return new SignatureTypeInfo(
            _rts.GetSignatureCorElementType(exactType),
            exactType,
            genericTypeDefinition: null,
            typeArguments.MoveToImmutable());
    }

    private SignatureTypeInfo CreateConstructedType(
        SignatureTypeInfo elementType,
        CorElementType constructedElementType,
        int rank)
    {
        ITypeHandle? exactType = elementType.ExactTypeHandle is null
            ? null
            : _rts.GetConstructedType(elementType.ExactTypeHandle, constructedElementType, rank, []);
        return new SignatureTypeInfo(constructedElementType, exactType);
    }

    private SignatureTypeInfo GetTypeFromToken(int token, ModuleLookupMapKind lookupMapKind, byte rawTypeKind)
    {
        TargetPointer typeHandlePointer = _loader.GetModuleLookupMapElement(
            _moduleHandle,
            lookupMapKind,
            (uint)token,
            out _);
        ITypeHandle? exactType = typeHandlePointer == TargetPointer.Null
            ? null
            : _rts.GetTypeHandle(typeHandlePointer);

        CorElementType elementType = rawTypeKind switch
        {
            (byte)SignatureTypeKind.Class => CorElementType.Class,
            (byte)SignatureTypeKind.ValueType => CorElementType.ValueType,
            _ when exactType is not null => _rts.GetSignatureCorElementType(exactType),
            _ => default,
        };

        return new SignatureTypeInfo(elementType, exactType);
    }
}
