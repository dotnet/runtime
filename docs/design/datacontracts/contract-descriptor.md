# Contract Descritptor

## Summary

The [data contracts design](./datacontracts_design.md) is a mechanism that allows diagnostic tooling
to understand the behavior of certain .NET runtime subsystems and data structures.  In a typical
scenario, a diagnostic tool such as a debugger may have access to a target .NET process (or a memory
dump of such a process) from which it may request to read and write certain regions of memory.

This document describes a mechanism by which a diagnostic tool may acquire the following information:
* some details about the target process' architecture 
* a collection of types and their sizes and/or the offsets of certain fields within each type
* a collection of global values
* a collection of /algorithmic contracts/ that are satisfied by the target process

## Contract descriptor

The contract descriptor consists of the follow structure.  All multi-byte values are in target architecture endianness.
    
```c
struct DotNetRuntimeContractDescriptor
{
    uint64_t magic;
    uint32_t flags;
    uint32_t aux_data_count;
    char *data_descriptor;
    uint64_t *aux_data;
    char *compatible_contracts;
};
```

The `magic` is `0x44_4e_43_43_44_41_43_00` ("DNCCDAC\0") stored using the target architecture
endianness. (N.B. this is sufficient to discover the target arhcitecture endianness by comparing the
value in memory to `0x44_4e_43_43_44_41_43_00` and to `0x00_43_41_44_43_43_4e_44`)

Flags.  The following bits are defined:

| Bits 31-2 | Bit 1   | Bit 0 |
| --------- | ------- | ----- |
| Reserved  | ptrSize |   1   |

If `ptrSize` is 0, the architecture is 64-bit.  If it is 1, the architecture is 32-bit.  The
reserved bits should be written as zero.  Diagnostic tooling may ignore non-zero reserved bits.

The `data_descriptor` is a pointer to a json string described in [data descriptor physical layout](./data_descriptor.md#Physical_JSON_descriptor).

The auxiliary data for the JSON descriptor is stored at the location `aux_data` in `aux_data_count` 64-bit slots.

The `compatible_contracts` are a json string giving the [compatible contracts](./datacontracts_design.md#Compatible_Contract).  The compatible contracts are given as a json array where each element is a dictionary.  The dictionary will have a `c` key giving the name of the compatible contract as a string, and a `v` key giving the contract version as an integer.  For example:

``` jsonc
[{"c":"Thread","v":1},{"c":"GCHandle","v":1},...]
```
    
## Contract symbol

To aid in the discovery of the contract descriptor, the contract should be exported by the target
process with the name `DotNetRuntimeContractDescriptor`.  (Using the C symbol conventions of the
target platform.  That is, on platforms where such symbols typicall have an `_` prepended, this
symbol should be exported as `_DotNetRuntimeContractDescriptor`)

**FIXME** What about scenarios such as a NativeAOT library hosted inside a native process?  What if
there are two such libraries?
