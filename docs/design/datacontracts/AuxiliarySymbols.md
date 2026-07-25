# Contract AuxiliarySymbols

This contract provides name resolution for helper functions whose executing code
resides at dynamically-determined addresses.

## APIs of contract

``` csharp
// Attempts to resolve a code address to a helper function name.
// Returns true if the address matches a known helper, with the name in symbolName.
// Returns false if the address does not match any known helper.
bool TryGetAuxiliarySymbolName(TargetPointer ip, out string symbolName);
```

## Version 1

<!-- BEGIN GENERATED: usage contract=AuxiliarySymbols version=c1 -->
### Data descriptors used

| Data Descriptor | Field | Type | Meaning |
| --- | --- | --- | --- |
| `AuxiliarySymbolInfo` | *(type size)* | `uint32` | Size in bytes of each entry in the auxiliary symbol array |
| `AuxiliarySymbolInfo` | `Address` | `CodePointer` | Code pointer to the dynamically-located helper function |
| `AuxiliarySymbolInfo` | `Name` | `pointer` | Pointer to a null-terminated char string with the helper name |

### Global variables used

| Global | Type | Meaning |
| --- | --- | --- |
| `AuxiliarySymbolCount` | `pointer` | Pointer to the count of populated entries in the array |
| `AuxiliarySymbols` | `pointer` | Pointer to an array of AuxiliarySymbolInfo entries |

### Contracts used

| Contract Name |
| --- |
| `PlatformMetadata` |
<!-- END GENERATED: usage contract=AuxiliarySymbols version=c1 -->


``` csharp
bool TryGetAuxiliarySymbolName(TargetPointer ip, out string? symbolName)
{
    symbolName = null;

    TargetCodePointer codePointer = CodePointerFromAddress(ip);

    TargetPointer helperArray = target.ReadGlobalPointer("AuxiliarySymbols");
    uint count = target.Read<uint>(target.ReadGlobalPointer("AuxiliarySymbolCount"));

    uint entrySize = /* AuxiliarySymbolInfo size */;

    for (uint i = 0; i < count; i++)
    {
        TargetPointer entryAddr = helperArray + (i * entrySize);
        TargetCodePointer address = target.ReadCodePointer(entryAddr + /* AuxiliarySymbolInfo::Address offset */);
        TargetPointer namePointer = target.ReadPointer(entryAddr + /* AuxiliarySymbolInfo::Name offset */);

        if (address == codePointer && namePointer != TargetPointer.Null)
        {
            symbolName = target.ReadUtf8String(namePointer);
            return true;
        }
    }

    return false;
}
```
