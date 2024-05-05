# Contract Descriptor

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
    uint32_t descriptor_size;
    const char *descriptor;
    uint32_t pointer_data_count;
    uint32_t pad0;
    uintptr_t *pointer_data;
};
```

The `magic` is `0x44_4e_43_43_44_41_43_00` ("DNCCDAC\0") stored using the target architecture
endianness. This is sufficient to discover the target architecture endianness by comparing the
value in memory to `0x44_4e_43_43_44_41_43_00` and to `0x00_43_41_44_43_43_4e_44`.

The following `flags` bits are defined:

| Bits 31-2 | Bit 1   | Bit 0 |
| --------- | ------- | ----- |
| Reserved  | ptrSize |   1   |

If `ptrSize` is 0, the architecture is 64-bit.  If it is 1, the architecture is 32-bit.  The
reserved bits should be written as zero.  Diagnostic tooling may ignore non-zero reserved bits.

The `descriptor` is a pointer to a UTF-8 JSON string described in [data descriptor physical layout](./data_descriptor.md#Physical_JSON_descriptor).  The total number of bytes is given by `descriptor_size`.

The auxiliary data for the JSON descriptor is stored at the location `pointer_data` in `pointer_data_count` pointer-sized slots.

### Architecture properties

Although `DotNetRuntimeContractDescriptor` contains enough information to discover the target
architecture endianness pointer size, it is expected that in all scenarios diagnostic tooling will
already have this information available through other channels.  Diagnostic tools may use the
information derived from `DotNetRuntimeContractDescriptor` for validation.

### Compatible contracts

The `descriptor` is a JSON dictionary that is used for storing the [in-memory data descriptor](./data_descriptor.md#Physical_JSON_Descriptor)
and the [compatible contracts](./datacontracts_design.md#Compatible_Contract).

The compatible contracts are stored in the top-level key `"contracts"`.  The value will be a
dictionary that contains each contract name as a key.  Each value is the version of the contract as
a JSON integer constant.

**Contract example**:

``` jsonc
{"Thread":1,"GCHandle":1,...}
```

**Complete in-memory data descriptor example**:

``` jsonc
{
  "version": "0",
  "baseline": "example-64",
  "types":
  {
    "Thread": { "ThreadId": 32, "ThreadState": 0, "Next": 128 },
    "ThreadStore": { "ThreadCount": 32, "ThreadList": 8 }
  },
  "globals":
  {
    "FEATURE_COMINTEROP": 0,
    "s_pThreadStore": [ 0 ] // indirect from pointer data offset 0
  },
  "contracts": {"Thread": 1, "GCHandle": 1, "ThreadStore": 1}
}
```

## Contract symbol

To aid in discovery, the contract descriptor should be exported by the module hosting the .NET
runtime with the name `DotNetRuntimeContractDescriptor` using the C symbol naming conventions of the
target platform.

In scenarios where multiple .NET runtimes may be present in a single process, diagnostic tooling
should look for the symbol in each loaded module to discover all the runtimes.

