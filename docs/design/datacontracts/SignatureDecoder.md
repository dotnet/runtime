# Contract SignatureDecoder

This contract encapsulates signature decoding in the cDAC.

## APIs of contract

```csharp
TypeHandle DecodeFieldSignature(BlobHandle blobHandle, ModuleHandle moduleHandle, TypeHandle ctx);
```

## Version 1

In version 1 of the SignatureDecoder contract we take advantage of the System.Reflection.Metadata signature decoding. We implement a SignatureTypeProvider that inherits from System.Reflection.Metadata ISignatureTypeProvider.

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| Module | TypeDefToMethodTableMap | Mapping table |
| Module | TypeRefToMethodTableMap | Mapping table |

Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| ObjectMethodTable | TargetPointer | pointer to the MT of `object` |
| StringMethodTable | TargetPointer | pointer to the MT of `string` |

Contracts used:
| Contract Name |
| --- |
| RuntimeTypeSystem |
| Loader |

### SignatureTypeProvider
```csharp
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

public class SignatureTypeProvider<T> : ISignatureTypeProvider<TypeHandle, T>
{
    private readonly Target _target;
    private readonly DataContractReader.Contracts.ModuleHandle _moduleHandle;

    public SignatureTypeProvider(Target target, DataContractReader.Contracts.ModuleHandle moduleHandle)
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
        => _target.Contracts.RuntimeTypeSystem.IterateTypeParams(elementType, CorElementType.SzArray, 1, default);

    public TypeHandle GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
    {
        int token = MetadataTokens.GetToken((EntityHandle)handle);
        TargetPointer typeHandlePtr = _target.Contracts.Loader.GetModuleLookupMapElement(_target.ReadPointer(moduleHandle.Address +  /* Module::TypeDefToMethodTableMap offset */, (uint)token, out _);
        return typeHandlePtr == TargetPointer.Null ? new TypeHandle(TargetPointer.Null) : _runtimeTypeSystem.GetTypeHandle(typeHandlePtr);
    }

    public TypeHandle GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
    {
        int token = MetadataTokens.GetToken((EntityHandle)handle);
        TargetPointer typeHandlePtr = _target.Contracts.Loader.GetModuleLookupMapElement(_target.ReadPointer(moduleHandle.Address +  /* Module::TypeRefToMethodTableMap offset */, (uint)token, out _);
        return typeHandlePtr == TargetPointer.Null ? new TypeHandle(TargetPointer.Null) : _runtimeTypeSystem.GetTypeHandle(typeHandlePtr);
    }

    public TypeHandle GetTypeFromSpecification(MetadataReader reader, T context, TypeSpecificationHandle handle, byte rawTypeKind)
        => throw new NotImplementedException();
}

```

### APIs
```csharp
TypeHandle ISignatureDecoder.DecodeFieldSignature(BlobHandle blobHandle, ModuleHandle moduleHandle, TypeHandle ctx)
{
    SignatureTypeProvider<TypeHandle> provider = new(_target, moduleHandle);
    MetadataReader mdReader = _target.Contracts.EcmaMetadata.GetMetadata(moduleHandle)!;
    BlobReader blobReader = mdReader.GetBlobReader(blobHandle);
    SignatureDecoder<TypeHandle, TypeHandle> decoder = new(provider, mdReader, ctx);
    return decoder.DecodeFieldSignature(ref blobReader);
}
```
