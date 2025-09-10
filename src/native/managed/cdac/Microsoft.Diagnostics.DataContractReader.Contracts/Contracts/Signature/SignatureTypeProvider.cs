// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata;
using Microsoft.Diagnostics.DataContractReader.Data;
using System;
using System.Collections.Immutable;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public class SignatureTypeProvider<T> : ISignatureTypeProvider<TypeHandle, T>
{
    // All interface methods throw NotImplementedException for now
    private readonly Target _target;
    private readonly ModuleHandle _moduleHandle;

    public SignatureTypeProvider(Target target, ModuleHandle moduleHandle)
    {
        _target = target;
        _moduleHandle = moduleHandle;
    }
    public TypeHandle GetArrayType(TypeHandle elementType, ArrayShape shape)
        => _target.Contracts.RuntimeTypeSystem.IterateTypeParams(elementType, CorElementType.Array, shape.Rank, default);

    public TypeHandle GetByReferenceType(TypeHandle elementType)
        => _target.Contracts.RuntimeTypeSystem.IterateTypeParams(elementType, CorElementType.Byref, 0, default);

    public TypeHandle GetFunctionPointerType(MethodSignature<TypeHandle> signature)
        => GetPrimitiveType(PrimitiveTypeCode.IntPtr);

    public TypeHandle GetGenericInstantiation(TypeHandle genericType, ImmutableArray<TypeHandle> typeArguments)
        => _target.Contracts.RuntimeTypeSystem.IterateTypeParams(genericType, CorElementType.GenericInst, 0, typeArguments);

    public TypeHandle GetGenericMethodParameter(T context, int index)
    {
        if (typeof(T) == typeof(MethodDescHandle))
        {
            MethodDescHandle methodContext = (MethodDescHandle)(object)context!;
            return _target.Contracts.RuntimeTypeSystem.GetGenericMethodInstantiation(methodContext)[index];
        }
        throw new NotSupportedException();
    }
    public TypeHandle GetGenericTypeParameter(T context, int index)
    {
        TypeHandle typeContext;
        if (typeof(T) == typeof(TypeHandle))
        {
            typeContext = (TypeHandle)(object)context!;
            return _target.Contracts.RuntimeTypeSystem.GetInstantiation(typeContext)[index];
        }
        throw new NotImplementedException();
    }
    public TypeHandle GetModifiedType(TypeHandle modifier, TypeHandle unmodifiedType, bool isRequired)
        => unmodifiedType;

    public TypeHandle GetPinnedType(TypeHandle elementType)
        => elementType;

    public TypeHandle GetPointerType(TypeHandle elementType)
        => _target.Contracts.RuntimeTypeSystem.IterateTypeParams(elementType, CorElementType.Ptr, 0, default);

    public TypeHandle GetPrimitiveType(PrimitiveTypeCode typeCode)
        => _target.Contracts.RuntimeTypeSystem.GetPrimitiveType(typeCode);

    public TypeHandle GetSZArrayType(TypeHandle elementType)
    {
        TypeHandle typeHandle = default;
        if (elementType.Address == _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.ObjectMethodTable)))
        {
            TargetPointer arrayPtr = _target.ReadGlobalPointer(Constants.Globals.ObjectArrayMethodTable);
            typeHandle = _target.Contracts.RuntimeTypeSystem.GetTypeHandle(arrayPtr);
        }
        else if (elementType.Address == _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.StringMethodTable)))
        {
            TargetPointer arrayPtr = _target.ReadGlobalPointer(Constants.Globals.StringArrayMethodTable);
            typeHandle = _target.Contracts.RuntimeTypeSystem.GetTypeHandle(arrayPtr);
        }
        if (typeHandle.Address == TargetPointer.Null)
        {
            return _target.Contracts.RuntimeTypeSystem.IterateTypeParams(elementType, CorElementType.SzArray, 1, default);
        }
        return typeHandle;
    }

    public TypeHandle GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
    {
        Module module = _target.ProcessedData.GetOrAdd<Module>(_moduleHandle.Address);
        int token = MetadataTokens.GetToken((EntityHandle)handle);
        TargetPointer typeHandlePtr = _target.Contracts.Loader.GetModuleLookupMapElement(module.TypeDefToMethodTableMap, (uint)token, out _);
        return _target.Contracts.RuntimeTypeSystem.GetTypeHandle(typeHandlePtr);
    }

    public TypeHandle GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
    {
        Module module = _target.ProcessedData.GetOrAdd<Module>(_moduleHandle.Address);
        int token = MetadataTokens.GetToken((EntityHandle)handle);
        TargetPointer typeHandlePtr = _target.Contracts.Loader.GetModuleLookupMapElement(module.TypeRefToMethodTableMap, (uint)token, out _);
        return _target.Contracts.RuntimeTypeSystem.GetTypeHandle(typeHandlePtr);
    }

    public TypeHandle GetTypeFromSpecification(MetadataReader reader, T context, TypeSpecificationHandle handle, byte rawTypeKind)
        => throw new NotImplementedException();
}
