# Contract Signature

This contract describes the format of method, field, and local-variable signatures stored in target memory. Signatures use the ECMA-335 §II.23.2 format with CoreCLR-internal element types added by the runtime.

## Internal element types

The runtime extends the standard ECMA-335 element type encoding with values that may appear in signatures stored in target memory:

| Encoding | Value | Layout following the tag |
| --- | --- | --- |
| `ELEMENT_TYPE_INTERNAL` | `0x21` | a target-sized pointer to a runtime `TypeHandle` |
| `ELEMENT_TYPE_CMOD_INTERNAL` | `0x22` | one byte (`1` = required, `0` = optional), then a target-sized pointer to a runtime `TypeHandle` |

These tags are used in signatures generated internally by the runtime that are not persisted to a managed image. They are defined alongside the standard ECMA-335 element types in `src/coreclr/inc/corhdr.h`. Their literal values are part of this contract -- changing them is a breaking change.

## APIs of contract

```csharp
TypeHandle DecodeFieldSignature(BlobHandle blobHandle, ModuleHandle moduleHandle, TypeHandle ctx);

// Returns the address of the first argument of a vararg call relative to the cookie pointer location.
TargetPointer GetVarArgArgsBase(TargetPointer vaSigCookieAddr);

// Returns the address and length of the raw vararg signature blob held by the cookie.
void GetVarArgSignature(TargetPointer vaSigCookieAddr, out TargetPointer signatureAddress, out uint signatureLength);
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `VASigCookie` | `SizeOfArgs` | Total size in bytes of the pushed argument list. Used on x86 to locate the args base. |
| `VASigCookie` | `Signature` | The raw vararg signature (see `Signature`). |
| `Signature` | `SignaturePointer` | Target address of the raw signature blob. |
| `Signature` | `SignatureLength` | Length in bytes of the raw signature blob. |

Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| _none_ |  | |

Contracts used:
| Contract Name |
| --- |
| RuntimeTypeSystem |
| Loader |
| EcmaMetadata |
| RuntimeInfo |

Constants:
| Constant Name | Meaning | Value |
| --- | --- | --- |
| `ELEMENT_TYPE_INTERNAL` | runtime-internal element type tag for an internal `TypeHandle` | `0x21` |
| `ELEMENT_TYPE_CMOD_INTERNAL` | runtime-internal element type tag for an internal modified type | `0x22` |

Decoding a signature follows the ECMA-335 §II.23.2 grammar. For all standard element types, decoding behaves identically to `System.Reflection.Metadata.SignatureDecoder<TType, TGenericContext>`. When the decoder encounters one of the runtime-internal tags above, it reads the target-sized pointer (and optional `required` byte for `ELEMENT_TYPE_CMOD_INTERNAL`) from the signature blob and resolves it to a runtime `TypeHandle`.

The decoder is implemented as `RuntimeSignatureDecoder<TType, TGenericContext>` -- a clone of SRM's `SignatureDecoder<TType, TGenericContext>` with added support for the runtime-internal element types. The clone takes an additional `Target` so internal-type pointers can be sized for the target architecture. Provider implementations implement `IRuntimeSignatureTypeProvider<TType, TGenericContext>` -- a superset of `System.Reflection.Metadata.ISignatureTypeProvider<TType, TGenericContext>` -- adding methods for the runtime-internal element types:

```csharp
TType GetInternalType(TargetPointer typeHandlePointer);
TType GetInternalModifiedType(TargetPointer typeHandlePointer, TType unmodifiedType, bool isRequired);
```

The contract's provider resolves these pointers through `RuntimeTypeSystem.GetTypeHandle`. Standard ECMA-335 element types resolve through `RuntimeTypeSystem.GetPrimitiveType` and `RuntimeTypeSystem.GetConstructedType`. Generic type parameters (`VAR`) and generic method parameters (`MVAR`) resolve via `RuntimeTypeSystem.GetInstantiation` and `RuntimeTypeSystem.GetGenericMethodInstantiation` respectively, using a `TypeHandle` (for generic types) or `MethodDescHandle` (for generic methods) generic context. `GetTypeFromDefinition` and `GetTypeFromReference` resolve tokens via the module's `TypeDefToMethodTableMap` / `TypeRefToMethodTableMap`; cross-module references and `GetTypeFromSpecification` are not currently implemented.

```csharp
TypeHandle ISignature.DecodeFieldSignature(BlobHandle blobHandle, ModuleHandle moduleHandle, TypeHandle ctx)
{
    SignatureTypeProvider<TypeHandle> provider = new(_target, moduleHandle);
    MetadataReader mdReader = _target.Contracts.EcmaMetadata.GetMetadata(moduleHandle)!;
    BlobReader blobReader = mdReader.GetBlobReader(blobHandle);
    RuntimeSignatureDecoder<TypeHandle, TypeHandle> decoder = new(provider, _target, mdReader, ctx);
    return decoder.DecodeFieldSignature(ref blobReader);
}
```

### Other consumers

`RuntimeSignatureDecoder` is shared infrastructure within the cDAC. Other contracts construct their own decoder and provider directly when they need to decode method or local signatures rather than going through this contract. For example, the [StackWalk](./StackWalk.md) contract uses `RuntimeSignatureDecoder<GcTypeKind, GcSignatureContext>` with a GC-specific provider to classify method parameters during signature-based GC reference scanning.

### Vararg call cookies

`GetVarArgArgsBase` and `GetVarArgSignature` decode a `VASigCookie*` slot pushed by a vararg call site.

```csharp
TargetPointer ISignature.GetVarArgArgsBase(TargetPointer vaSigCookieAddr)
{
    // On x86 the args are pushed below the cookie pointer (stack grows down on the args walk),
    // so the first argument lies at vaSigCookieAddr + cookie.SizeOfArgs.
    // On every other platform the first argument follows the cookie pointer in memory
    // (stack grows up on the args walk), so its address is vaSigCookieAddr + sizeof(VASigCookie*).
    if (RuntimeInfo.GetTargetArchitecture() == X86)
    {
        TargetPointer vaSigCookie = _target.ReadPointer(vaSigCookieAddr);
        VASigCookie cookie = _target.ProcessedData.GetOrAdd<VASigCookie>(vaSigCookie);
        return vaSigCookieAddr + cookie.SizeOfArgs;
    }
    return vaSigCookieAddr + sizeof(TargetPointer);
}

void ISignature.GetVarArgSignature(TargetPointer vaSigCookieAddr, out TargetPointer signatureAddress, out uint signatureLength)
{
    TargetPointer vaSigCookie = _target.ReadPointer(vaSigCookieAddr);
    VASigCookie cookie = _target.ProcessedData.GetOrAdd<VASigCookie>(vaSigCookie);

    signatureAddress = cookie.Signature.SignaturePointer;
    signatureLength = cookie.Signature.SignatureLength;
}
```
