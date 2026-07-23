# Contract TypeInformation

This reader-provided contract decodes runtime signatures into type information
without requiring every type in the signature to have an exact loaded runtime
type.

`ITypeHandle` continues to represent only an exact target-backed `MethodTable`
or `TypeDesc`. `SignatureTypeInfo` is a separate representation for facts that
remain available from the signature:

- the outermost `CorElementType`
- an optional exact loaded `ITypeHandle`
- the loaded generic type definition, when applicable
- recursively decoded generic type arguments

This separation lets GC signature consumers classify reference and byref
arguments without fabricating an `ITypeHandle`. Operations that require exact
runtime layout, such as value-type size, GCDesc, HFA, alignment, or ABI
classification, still require `SignatureTypeInfo.ExactTypeHandle`.

## APIs of contract

```csharp
MethodSignature<SignatureTypeInfo> DecodeMethodSignature(MethodDescHandle methodDesc);

SignatureTypeInfo GetFieldTypeInfo(
    TargetPointer fieldDesc,
    SignatureTypeInfo owningType);
```

## Version 1

CoreCLR advertises version `c1` in its contract descriptor. The contract
derives its results from existing contracts and does not require type, field,
or global data descriptor entries of its own.

Data descriptors used:

| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| _none_ |  | |

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

`DecodeMethodSignature` obtains the exact owning type and raw method signature
from `RuntimeTypeSystem`. It decodes the signature with a provider that keeps
the raw `Class` or `ValueType` kind even when a TypeDef or TypeRef lookup does
not produce an exact loaded type.

For a generic instantiation, the provider asks
`RuntimeTypeSystem.GetConstructedType` for the exact loaded type. If no exact
type is available, the result retains the generic type definition and decoded
type arguments while leaving `ExactTypeHandle` null.

`GetFieldTypeInfo` decodes a field signature using the supplied owning
`SignatureTypeInfo` as the generic type context. This preserves exact generic
argument facts while recursively inspecting fields of a generic value type,
even when an exact loaded type for a nested constructed field is unavailable.
