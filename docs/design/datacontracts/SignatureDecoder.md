# Contract SignatureDecoder

This contract encapsulates signature decoding in the cDAC.

## APIs of contract

```csharp
TypeHandle DecodeFieldSignature(BlobHandle blobHandle, ModuleHandle moduleHandle, TypeHandle ctx);
```

## Version 1

In version 1 of the SignatureDecoder contract we use a cDAC-internal `RuntimeSignatureDecoder` that closely mirrors the API and behavior of `System.Reflection.Metadata.SignatureDecoder`. The cDAC decoder exists because runtime-internal signatures may contain `ELEMENT_TYPE_INTERNAL` (`0x21`) and `ELEMENT_TYPE_CMOD_INTERNAL` (`0x22`) elements -- target-pointer references to runtime type handles that are not part of ECMA-335 and that SRM does not understand.

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
| EcmaMetadata |

### RuntimeSignatureDecoder

`RuntimeSignatureDecoder<TType, TGenericContext>` is a cDAC polyfill that mirrors `System.Reflection.Metadata.SignatureDecoder<TType, TGenericContext>`:

* Same constructor shape (`provider`, `metadataReader`, `genericContext`) plus an additional `Target` parameter so that internal-type pointer values can be sized correctly for the target.
* Same public surface: `DecodeType(ref BlobReader, bool allowTypeSpecifications = false)`, `DecodeFieldSignature(ref BlobReader)`, `DecodeMethodSignature(ref BlobReader)`, and `DecodeLocalSignature(ref BlobReader)`.
* Same parsing rules for ECMA-335 element types.

The decoder additionally recognizes:

* `ELEMENT_TYPE_INTERNAL` (`0x21`): followed by a target-sized pointer to a runtime `TypeHandle`. Provider returns a type via `GetInternalType(target, typeHandlePointer)`.
* `ELEMENT_TYPE_CMOD_INTERNAL` (`0x22`): followed by a single byte indicating required (`1`) or optional (`0`), then a target-sized pointer to a runtime `TypeHandle`. Provider returns a modifier-applied type via `GetInternalModifiedType(target, typeHandlePointer, unmodifiedType, isRequired)`.

Tag `3` in `TypeDefOrRefOrSpec` encoding throws `BadImageFormatException`, matching SRM behavior. The element type code is read as a compressed integer per ECMA-335 §II.23.2.

### IRuntimeSignatureTypeProvider

Provider implementations implement `IRuntimeSignatureTypeProvider<TType, TGenericContext>`, which is a superset of `System.Reflection.Metadata.ISignatureTypeProvider<TType, TGenericContext>` adding two methods to handle the runtime-only encodings:

```csharp
TType GetInternalType(TargetPointer typeHandlePointer);
TType GetInternalModifiedType(TargetPointer typeHandlePointer, TType unmodifiedType, bool isRequired);
```

Providers that need a `Target` to resolve type-handle pointers capture it themselves (typically as a constructor argument). The decoder does not pass the target through to provider methods.

### SignatureTypeProvider

The cDAC implements `IRuntimeSignatureTypeProvider<TType, TGenericContext>` with `TType=TypeHandle`. `TGenericContext` can either be a `MethodDescHandle` or `TypeHandle`; `MethodDescHandle` context is used to look up generic method parameters, and `TypeHandle` context is used to look up generic type parameters.

A cDAC `SignatureTypeProvider` is instantiated over a `Module` which is used to lookup types.

The following `ISignatureTypeProvider` APIs are trivially implemented using `RuntimeTypeSystem.GetPrimitiveType` and `RuntimeTypeSystem.GetConstructedType`:

* `GetArrayType` - `GetConstructedType`
* `GetByReferenceType` - `GetConstructedType`
* `GetFunctionPointerType` - Implemented as primitive `IntPtr` type
* `GetGenericInstantiation` - `GetConstructedType`
* `GetModifiedType` - Returns unmodified type
* `GetPinnedType` - Returns unpinned type
* `GetPointerType` - `GetConstructedType`
* `GetPrimitiveType` - `GetConstructedType`
* `GetSZArrayType` - `GetConstructedType`

`GetGenericMethodParameter` is only supported when `TGenericContext=MethodDescHandle` and looks up the method parameters from the context using `RuntimeTypeSystem.GetGenericMethodInstantiation`.

`GetGenericTypeParameter` is only supported when `TGenericContext=TypeHandle` and looks up the type parameters from the context using `RuntimeTypeSystem.GetInstantiation`.

`GetTypeFromDefinition` uses the `SignatureTypeProvider`'s `ModuleHandle` to lookup the given Token in the Module's `TypeDefToMethodTableMap`. If a value is not found returns null.

`GetTypeFromReference` uses the `SignatureTypeProvider`'s `ModuleHandle` to lookup the given Token in the Module's `TypeRefToMethodTableMap`. If a value is not found returns null. The implementation when the type exists in a different module is incomplete.

`GetTypeFromSpecification` is not currently implemented.

`GetInternalType` resolves the `TargetPointer` to a `TypeHandle` via the captured `Target`'s `RuntimeTypeSystem.GetTypeHandle`.

`GetInternalModifiedType` returns the unmodified type, matching the behavior of `GetModifiedType`.

### APIs
```csharp
TypeHandle ISignatureDecoder.DecodeFieldSignature(BlobHandle blobHandle, ModuleHandle moduleHandle, TypeHandle ctx)
{
    SignatureTypeProvider<TypeHandle> provider = new(_target, moduleHandle);
    MetadataReader mdReader = _target.Contracts.EcmaMetadata.GetMetadata(moduleHandle)!;
    BlobReader blobReader = mdReader.GetBlobReader(blobHandle);
    RuntimeSignatureDecoder<TypeHandle, TypeHandle> decoder = new(provider, _target, mdReader, ctx);
    return decoder.DecodeFieldSignature(ref blobReader);
}
```

### Other consumers

`RuntimeSignatureDecoder` is shared infrastructure within the cDAC. Other contracts construct their own decoder + provider directly when they need to decode method or local signatures rather than going through this contract. For example, the [StackWalk](./StackWalk.md) contract uses `RuntimeSignatureDecoder<GcTypeKind, object?>` with a GC-specific provider to classify method parameters during signature-based GC reference scanning.
