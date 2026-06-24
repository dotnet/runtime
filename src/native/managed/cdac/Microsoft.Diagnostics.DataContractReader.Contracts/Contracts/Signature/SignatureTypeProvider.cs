// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata;
using Microsoft.Diagnostics.DataContractReader.Data;
using System;
using System.Collections.Immutable;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.SignatureHelpers;

public class SignatureTypeProvider<T> : IRuntimeSignatureTypeProvider<ITypeHandle, T>
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

    public ITypeHandle GetArrayType(ITypeHandle elementType, ArrayShape shape)
        => _runtimeTypeSystem.GetConstructedType(elementType, CorElementType.Array, shape.Rank, []);

    public ITypeHandle GetByReferenceType(ITypeHandle elementType)
        => _runtimeTypeSystem.GetConstructedType(elementType, CorElementType.Byref, 0, []);

    public ITypeHandle GetFunctionPointerType(MethodSignature<ITypeHandle> signature)
        => GetPrimitiveType(PrimitiveTypeCode.IntPtr);

    public ITypeHandle GetGenericInstantiation(ITypeHandle genericType, ImmutableArray<ITypeHandle> typeArguments)
        => _runtimeTypeSystem.GetConstructedType(genericType, CorElementType.GenericInst, 0, typeArguments);

    public ITypeHandle GetGenericMethodParameter(T context, int index)
    {
        if (typeof(T) == typeof(MethodDescHandle))
        {
            MethodDescHandle methodContext = (MethodDescHandle)(object)context!;
            return _runtimeTypeSystem.GetGenericMethodInstantiation(methodContext)[index];
        }
        throw new NotSupportedException();
    }
    public ITypeHandle GetGenericTypeParameter(T context, int index)
    {
        ITypeHandle typeContext;
        if (typeof(T) == typeof(ITypeHandle))
        {
            typeContext = (ITypeHandle)(object)context!;
            return _runtimeTypeSystem.GetInstantiation(typeContext)[index];
        }
        throw new NotImplementedException();
    }
    public ITypeHandle GetModifiedType(ITypeHandle modifier, ITypeHandle unmodifiedType, bool isRequired)
        => unmodifiedType;

    public ITypeHandle GetPinnedType(ITypeHandle elementType)
        => elementType;

    public ITypeHandle GetPointerType(ITypeHandle elementType)
        => _runtimeTypeSystem.GetConstructedType(elementType, CorElementType.Ptr, 0, []);

    public ITypeHandle GetPrimitiveType(PrimitiveTypeCode typeCode)
        => _runtimeTypeSystem.GetPrimitiveType((CorElementType)typeCode);

    public ITypeHandle GetSZArrayType(ITypeHandle elementType)
        => _runtimeTypeSystem.GetConstructedType(elementType, CorElementType.SzArray, 1, []);

    public ITypeHandle GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
    {
        int token = MetadataTokens.GetToken((EntityHandle)handle);
        TargetPointer typeDefToMethodTable = _loader.GetLookupTables(_moduleHandle).TypeDefToMethodTable;
        TargetPointer typeHandlePtr = _loader.GetModuleLookupMapElement(typeDefToMethodTable, (uint)token, out _);
        return typeHandlePtr == TargetPointer.Null ? ITypeHandle.Null : _runtimeTypeSystem.GetTypeHandle(typeHandlePtr);
    }

    public ITypeHandle GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
    {
        int token = MetadataTokens.GetToken((EntityHandle)handle);
        TargetPointer typeRefToMethodTable = _loader.GetLookupTables(_moduleHandle).TypeRefToMethodTable;
        TargetPointer typeHandlePtr = _loader.GetModuleLookupMapElement(typeRefToMethodTable, (uint)token, out _);
        return typeHandlePtr == TargetPointer.Null ? ITypeHandle.Null : _runtimeTypeSystem.GetTypeHandle(typeHandlePtr);
    }

    public ITypeHandle GetTypeFromSpecification(MetadataReader reader, T context, TypeSpecificationHandle handle, byte rawTypeKind)
        => throw new NotImplementedException();

    public ITypeHandle GetInternalType(TargetPointer typeHandlePointer)
        => typeHandlePointer == TargetPointer.Null
            ? ITypeHandle.Null
            : _runtimeTypeSystem.GetTypeHandle(typeHandlePointer);

    public ITypeHandle GetInternalModifiedType(TargetPointer typeHandlePointer, ITypeHandle unmodifiedType, bool isRequired)
        => unmodifiedType;
}
