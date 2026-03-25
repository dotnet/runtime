# Contract AuxiliarySymbols

This contract provides name resolution for JIT helper functions whose executing code
resides at dynamically-determined addresses.

## APIs of contract

``` csharp
// Attempts to resolve a code address to a JIT helper function name.
// Returns true if the address matches a known helper, with the name in helperName.
// Returns false if the address does not match any known helper.
bool TryGetJitHelperName(TargetPointer ip, out string helperName);
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `JitHelperInfo` | `Address` | Code pointer to the dynamically-located helper function |
| `JitHelperInfo` | `Name` | Pointer to a null-terminated wide string with the helper name |

Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| `InterestingJitHelpers` | TargetPointer | Pointer to an array of `JitHelperInfo` entries |
| `InterestingJitHelperCount` | TargetPointer | Pointer to the count of populated entries in the array |

Contracts used: none

``` csharp
bool TryGetJitHelperName(TargetPointer ip, out string helperName)
{
    helperName = null;

    TargetCodePointer codePointer = CodePointerFromAddress(ip);

    TargetPointer helperArray = target.ReadGlobalPointer("InterestingJitHelpers");
    int count = target.Read<int>(target.ReadGlobalPointer("InterestingJitHelperCount"));

    uint entrySize = /* JitHelperInfo size */;

    for (int i = 0; i < count; i++)
    {
        TargetPointer entryAddr = helperArray + (i * entrySize);
        TargetCodePointer address = target.ReadCodePointer(entryAddr + /* JitHelperInfo::Address offset */);
        TargetPointer namePointer = target.ReadPointer(entryAddr + /* JitHelperInfo::Name offset */);

        if (address == codePointer && namePointer != TargetPointer.Null)
        {
            helperName = target.ReadUtf16String(namePointer);
            return true;
        }
    }

    return false;
}
```
