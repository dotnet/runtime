// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata;
using Microsoft.Diagnostics.DataContractReader.Data;
using System;
using System.Collections.Immutable;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.SignatureHelpers;

public class SignatureTypeProvider<T> : ISignatureTypeProvider<TypeHandle, T>
{
    private readonly Target _target;
    private readonly Contracts.ModuleHandle _moduleHandle;
    private readonly Contracts.ILoader _loader;
    private readonly Contracts.IRuntimeTypeSystem _runtimeTypeSystem;

    public SignatureTypeProvider(Target target, Contracts.ModuleHandle moduleHandle)
    {
        _target = target;
        _moduleHandle = moduleHandle;
        _loader = target.Contracts.Loader;
        _runtimeTypeSystem = target.Contracts.RuntimeTypeSystem;
    }

    public TypeHandle GetArrayType(TypeHandle elementType, ArrayShape shape)
        => _runtimeTypeSystem.GetConstructedType(elementType, CorElementType.Array, shape.Rank, []);

    public TypeHandle GetByReferenceType(TypeHandle elementType)
        => _runtimeTypeSystem.GetConstructedType(elementType, CorElementType.Byref, 0, []);

    public TypeHandle GetFunctionPointerType(MethodSignature<TypeHandle> signature)
        => GetPrimitiveType(PrimitiveTypeCode.IntPtr);

    public TypeHandle GetGenericInstantiation(TypeHandle genericType, ImmutableArray<TypeHandle> typeArguments)
        => _runtimeTypeSystem.GetConstructedType(genericType, CorElementType.GenericInst, 0, typeArguments);

    public TypeHandle GetGenericMethodParameter(T context, int index)
    {
        if (typeof(T) == typeof(MethodDescHandle))
        {
            MethodDescHandle methodContext = (MethodDescHandle)(object)context!;
            return _runtimeTypeSystem.GetGenericMethodInstantiation(methodContext)[index];
        }
        throw new NotSupportedException();
    }
    public TypeHandle GetGenericTypeParameter(T context, int index)
    {
        TypeHandle typeContext;
        if (typeof(T) == typeof(TypeHandle))
        {
            typeContext = (TypeHandle)(object)context!;
            return _runtimeTypeSystem.GetInstantiation(typeContext)[index];
        }
        throw new NotImplementedException();
    }
    public TypeHandle GetModifiedType(TypeHandle modifier, TypeHandle unmodifiedType, bool isRequired)
        => unmodifiedType;

    public TypeHandle GetPinnedType(TypeHandle elementType)
        => elementType;

    public TypeHandle GetPointerType(TypeHandle elementType)
        => _runtimeTypeSystem.GetConstructedType(elementType, CorElementType.Ptr, 0, []);

    public TypeHandle GetPrimitiveType(PrimitiveTypeCode typeCode)
        => _runtimeTypeSystem.GetBinderType((ushort)typeCode);

    public TypeHandle GetSZArrayType(TypeHandle elementType)
        => _runtimeTypeSystem.GetConstructedType(elementType, CorElementType.SzArray, 1, []);

    public TypeHandle GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
    {
        int token = MetadataTokens.GetToken((EntityHandle)handle);
        TargetPointer typeDefToMethodTable = _loader.GetLookupTables(_moduleHandle).TypeDefToMethodTable;
        TargetPointer typeHandlePtr = _loader.GetModuleLookupMapElement(typeDefToMethodTable, (uint)token, out _);
        return typeHandlePtr == TargetPointer.Null ? new TypeHandle(TargetPointer.Null) : _runtimeTypeSystem.GetTypeHandle(typeHandlePtr);
    }

    public TypeHandle GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
    {
        int token = MetadataTokens.GetToken((EntityHandle)handle);
        TargetPointer typeRefToMethodTable = _loader.GetLookupTables(_moduleHandle).TypeRefToMethodTable;
        TargetPointer typeHandlePtr = _loader.GetModuleLookupMapElement(typeRefToMethodTable, (uint)token, out _);
        return typeHandlePtr == TargetPointer.Null ? new TypeHandle(TargetPointer.Null) : _runtimeTypeSystem.GetTypeHandle(typeHandlePtr);
    }

    public TypeHandle GetTypeFromSpecification(MetadataReader reader, T context, TypeSpecificationHandle handle, byte rawTypeKind)
        => throw new NotImplementedException();
}
