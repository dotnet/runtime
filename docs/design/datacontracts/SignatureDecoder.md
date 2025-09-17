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


Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |


Contracts used:
| Contract Name |
| --- |
| RuntimeTypeSystem |
| Loader |

### SignatureTypeProvider
The cDAC implements the ISignatureTypeProvider<TType,TGenericContext> with TType=TypeHandle. TGenericContext can either be a MethodDescHandle or TypeHandle; MethodDescHandle context is used to look up generic method parameters, and TypeHandle context is used to look up generic type parameters.

A cDAC SignatureTypeProvider is instantiated over a Module which is used to lookup types.

The following ISignatureTypeProvider APIs are trivially implemented using RuntimeTypeSystem.GetPrimitiveType and RuntimeTypeSystem.GetConstructedType:

* GetArrayType - GetConstructedType
* GetByReferenceType - GetConstructedType
* GetFunctionPointerType - Implemented as primitive IntPtr type
* GetGenericInstantiation - GetConstructedType
* GetModifiedType - Returns unmodified type
* GetPinnedType - Returns unpinned type
* GetPointerType - GetConstructedType
* GetPrimitiveType - GetConstructedType
* GetSZArrayType - GetConstructedType

GetGenericMethodParameter is only supported when TGenericContext=MethodDescHandle and looks up the method parameters from the context using RuntimeTypeSystem.GetGenericMethodInstantiation.

GetGenericTypeParameter is only supported when TGenericContext=TypeHandle and looks up the type parameters from the context using RuntimeTypeSystem.GetInstantiation.

GetTypeFromDefinition uses the SignatureTypeProvider's ModuleHandle to lookup the given Token in the Module's TypeDefToMethodTableMap. If a value is not found return null.

GetTypeFromReference uses the SignatureTypeProvider's ModuleHandle to lookup the given Token in the Module's TypeRefToMethodTableMap. If a value is not found return null.The implementation when the type exists in a different module is incomplete.

GetTypeFromSpecification is not currently implemented.


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
