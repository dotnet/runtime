# Contract ECall

This contract is for fetching information related to native calls into the runtime.

## APIs of contract

``` csharp
// Given an FCall entrypoint returns the corresponding MethodDesc.
// If the address does not correspond to an FCall, returns TargetPointer.Null.
TargetPointer MapTargetBackToMethodDesc(TargetCodePointer address);
```

## Version 1

Global variables used
| Global Name | Type | Purpose |
| --- | --- | --- |
| FCallMethods | ECHash[] | Hash table containing ECHash structures |
| FCallHashSize | uint | Number of buckets in the hash table |


Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `ECHash` | `Next` | Pointer to the next ECHash in the chain |
| `ECHash` | `Implementation` | FCall's Entrypoint address |
| `ECHash` | `MethodDesc` | Pointer to the FCall's method desc |


``` csharp
TargetPointer IECall.MapTargetBackToMethodDesc(TargetCodePointer codePointer)
```

To map an FCall entrypoint back to a MethodDesc, we read the global `FCallMethods` hash table. This is a array of pointers to `ECHash` objects. The length of this array is defined by the global `FCallHashSize` where each element is an `ECHash` which can form a chain. It uses a simple hash function: `<hash> = codePointer % FCallHashSize` to map code entry points to buckets. To map a `codePointer` back to a MethodDesc pointer:

1. Calculate the `<hash>` corresponding to the given `codePointer`.
2. Take the `<hash>` offset into the `FCallMethods` array.
3. Now that we have the correct `ECHash` chain, iterate the chain using the `ECHash.Next` pointer until we find an `ECHash` where the `Implementation` field matches the `codePointer`. If found, return the `MethodDesc` field.
4. If no `ECHash` matches return `TargetPointer.Null` to indicate a MethodDesc was not found.
