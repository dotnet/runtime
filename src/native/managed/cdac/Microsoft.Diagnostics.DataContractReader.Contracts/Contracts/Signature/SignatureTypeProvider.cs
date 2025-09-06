// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata;
using Microsoft.Diagnostics.DataContractReader.Data;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using System;
using System.Collections.Immutable;
using Microsoft.Diagnostics.DataContractReader;

namespace Microsoft.Diagnostics.Contracts;

public class SignatureTypeProvider<T> : ISignatureTypeProvider<TypeHandle, T>
{
    // All interface methods throw NotImplementedException for now
    private readonly Target _target;
    private readonly DataContractReader.Contracts.ModuleHandle _moduleHandle;

    public SignatureTypeProvider(Target target, DataContractReader.Contracts.ModuleHandle moduleHandle)
    {
        _target = target;
        _moduleHandle = moduleHandle;
    }
    public TypeHandle GetArrayType(TypeHandle elementType, ArrayShape shape)
        => IterateTypeParams(elementType, CorElementType.Array, shape.Rank, default);

    public TypeHandle GetByReferenceType(TypeHandle elementType)
        => IterateTypeParams(elementType, CorElementType.Byref, 0, default);

    private bool GenericInstantiationMatch(TypeHandle genericType, TypeHandle potentialMatch, ImmutableArray<TypeHandle> typeArguments)
    {
        IRuntimeTypeSystem rtsContract = _target.Contracts.RuntimeTypeSystem;
        ReadOnlySpan<TypeHandle> instantiation = rtsContract.GetInstantiation(potentialMatch);
        if (instantiation.Length != typeArguments.Length)
            return false;

        if (rtsContract.GetTypeDefToken(genericType) != rtsContract.GetTypeDefToken(potentialMatch))
            return false;

        if (rtsContract.GetModule(genericType) != rtsContract.GetModule(potentialMatch))
            return false;

        for (int i = 0; i < instantiation.Length; i++)
        {
            if (!(instantiation[i].Address == typeArguments[i].Address))
                return false;
        }
        return true;
    }

    private bool ArrayPtrMatch(TypeHandle elementType, CorElementType corElementType, int rank, TypeHandle potentialMatch)
    {
        IRuntimeTypeSystem rtsContract = _target.Contracts.RuntimeTypeSystem;
        rtsContract.IsArray(potentialMatch, out uint typeHandleRank);
        return rtsContract.GetSignatureCorElementType(potentialMatch) == corElementType &&
                rtsContract.GetTypeParam(potentialMatch).Address == elementType.Address &&
                (corElementType == CorElementType.SzArray || corElementType == CorElementType.Byref ||
                corElementType == CorElementType.Ptr || (rank == typeHandleRank));

    }

    private TypeHandle IterateTypeParams(TypeHandle typeHandle, CorElementType corElementType, int rank, ImmutableArray<TypeHandle> typeArguments)
    {
        ILoader loaderContract = _target.Contracts.Loader;
        IRuntimeTypeSystem rtsContract = _target.Contracts.RuntimeTypeSystem;
        TargetPointer loaderModule = rtsContract.GetLoaderModule(typeHandle);
        DataContractReader.Contracts.ModuleHandle moduleHandle = loaderContract.GetModuleHandleFromModulePtr(loaderModule);
        foreach (TargetPointer ptr in loaderContract.GetAvailableTypeParams(moduleHandle))
        {
            TypeHandle potentialMatch = rtsContract.GetTypeHandle(ptr);
            if (corElementType == CorElementType.GenericInst)
            {
                if (GenericInstantiationMatch(typeHandle, potentialMatch, typeArguments))
                {
                    return potentialMatch;
                }
            }
            else if (ArrayPtrMatch(typeHandle, corElementType, rank, potentialMatch))
            {
                return potentialMatch;
            }
        }
        return new TypeHandle(TargetPointer.Null);
    }
    public TypeHandle GetFunctionPointerType(MethodSignature<TypeHandle> signature)
        => GetPrimitiveType(PrimitiveTypeCode.IntPtr);

    public TypeHandle GetGenericInstantiation(TypeHandle genericType, ImmutableArray<TypeHandle> typeArguments)
        => IterateTypeParams(genericType, CorElementType.GenericInst, 0, typeArguments);

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
        => IterateTypeParams(elementType, CorElementType.Ptr, 0, default);

    public TypeHandle GetPrimitiveType(PrimitiveTypeCode typeCode)
    {
        TargetPointer coreLib = _target.ReadGlobalPointer(Constants.Globals.CoreLib);
        CoreLibBinder coreLibData = _target.ProcessedData.GetOrAdd<CoreLibBinder>(coreLib);
        TargetPointer typeHandlePtr = _target.ReadPointer(coreLibData.Classes + (ulong)typeCode * (ulong)_target.PointerSize);
        return _target.Contracts.RuntimeTypeSystem.GetTypeHandle(typeHandlePtr);
    }

    private TypeHandle GetPrimitiveArrayType(CorElementType elementType)
    {
        TargetPointer arrayPtr = _target.ReadGlobalPointer(Constants.Globals.PredefinedArrayTypes);
        TargetPointer typeHandlePtr = _target.ReadPointer(arrayPtr + (ulong)elementType * (ulong)_target.PointerSize);
        return _target.Contracts.RuntimeTypeSystem.GetTypeHandle(typeHandlePtr);
    }

    public TypeHandle GetSZArrayType(TypeHandle elementType)
    {
        IRuntimeTypeSystem rtsContract = _target.Contracts.RuntimeTypeSystem;
        CorElementType corElementType = rtsContract.GetSignatureCorElementType(elementType);
        TypeHandle typeHandle = default;
        if (corElementType <= CorElementType.R8)
        {
            typeHandle = GetPrimitiveArrayType(corElementType);
        }
        else if (elementType.Address == _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.ObjectMethodTable)))
        {
            typeHandle = GetPrimitiveArrayType(CorElementType.Object);
        }
        else if (elementType.Address == _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.StringMethodTable)))
        {
            typeHandle = GetPrimitiveArrayType(CorElementType.String);
        }
        if (typeHandle.Address == TargetPointer.Null)
        {
            return IterateTypeParams(elementType, CorElementType.SzArray, 1, default);
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
