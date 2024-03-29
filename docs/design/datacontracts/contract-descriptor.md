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
    uint32_t size_and_flags;
    uint32_t aux_data_count;
    uint32_t descriptor_size;
    uint32_t reserved;
    const char *descriptor;
    uint64_t *aux_data;
};

struct DotNetRuntimeContractDescriptorList
{
    const struct DotNetRuntimeContractDescriptor *descriptor;
    const char *runtime_name;
    struct DotNetRuntimeContractDescriptorList *next_runtime;
}
```

The `magic` is `0x44_4e_43_43_44_41_43_00` ("DNCCDAC\0") stored using the target architecture
endianness. (N.B. this is sufficient to discover the target architecture endianness by comparing the
value in memory to `0x44_4e_43_43_44_41_43_00` and to `0x00_43_41_44_43_43_4e_44`)

Flags.  The following bits are defined:

| Bits 31-3 | Bit 2  | Bit 1   | Bit 0 |
| --------- | ------ | ------- | ----- |
| Reserved  | isList | ptrSize |   1   |

If `ptrSize` is 0, the architecture is 64-bit.  If it is 1, the architecture is 32-bit.  The
reserved bits should be written as zero.  Diagnostic tooling may ignore non-zero reserved bits.

If `isList` is 1, the descriptor is actually a `DotNetRuntimeContractDescriptorList` (that is, it
has a `next_runtime` field at the end. See "Non-Windows", below.) If `isList` is 0, the descriptor
does not have a `next_runtime` field.

The `descriptor` is a pointer to a json string described in [data descriptor physical layout](./data_descriptor.md#Physical_JSON_descriptor).  The total length (including nul terminator character) is given by `descriptor_size`.

The auxiliary data for the JSON descriptor is stored at the location `aux_data` in `aux_data_count` 64-bit slots.

The `next_runtime` field is used to support multiple .NET runtimes in a single process.  See below.

### Compatible contracts

The `descriptor` is a JSON dictionary that is used for storing the [in-memory data descriptor](./data_descriptor.md#Physical_JSON_Descriptor)
and the [compatible contracts](./datacontracts_design.md#Compatible_Contract).

The compatible contracts are stored in the toplevel key `"contracts"`.  The value will be a
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
    "s_pThreadStore": [ 0 ] // indirect from aux data offset 0
  },
  "contracts": {"Thread": 1,"GCHandle": 1, "ThreadStore": 1}
}
```

## Contract symbol

To aid in the discovery of the contract descriptor, the contract should be exported by the target
process with the name `DotNetRuntimeContractDescriptor`.

The meaning of the symbol differs on Windows and non-Windows platforms.

### Windows

Multiple DLLs loaded by a process may host a single .NET runtime.  Each DLL shall export the symbol
`DotNetRuntimeContractDescriptor` pointing to a `struct DotNetRuntimeContractDescriptor`.  It is
expected that `isList` will be 0.

### Non-Windows

In a process, each shared object containing a .NET runtime shall weakly-export the symbol
`DotNetRuntimeContractDescriptor` (Using the C symbol conventions of the target platform.  That is,
on platforms where such symbols typically have an `_` prepended, this symbol should be exported as
`_DotNetRuntimeContractDescriptor`) with a null initial value.  As each .NET runtime in the process starts
up, it shall atomically store a pointer to a `struct DotNetRuntimeContractDescriptorList` in
`DotNetRuntimeContractDescriptor` where `next_runtime` points to the previous value of
`DotNetRuntimeContractDescriptor` as by the C code below.

The `runtime_name` is an arbitrary identifier to aid diagnostic tooling in identifying the current
runtime.  (For example hosted runtimes may want to embed the name of the host; a desktop runtime may
use just the runtime flavor and version)

``` c
typedef struct DotNetRuntimeContractDescriptorList* DescPtr;
typedef _Atomic(DescPtr) AtomicDescPtr;

// global weak symbol
AtomicDescPtr  __attribute__((weak)) DotNetRuntimeContractDescriptor;

// predefined descriptor for current runtime
static const struct DotNetRuntimeContractDescriptor g_private_descriptor = { ... };

// install_descriptor will try to assign the address of s_runtime_descriptor to the global symbol
static struct DotNetRuntimeContractDescriptorList s_runtime_descriptor = {
    .descriptor = &g_private_descriptor,
    .runtime_name = NULL,
    .next_runtime = NULL
};

// to be called at startup
void
install_descriptor(const char *runtime_name)
{
    DescPtr descriptor = &s_runtime_descriptor;
    descriptor->runtime_name = runtime_name;
    descriptor->next_runtime = NULL;

    DescPtr prev = atomic_load(&DotNetRuntimeContractDescriptor);
    do
    {
        descriptor->next_runtime = prev;
    }
    while (!atomic_compare_exchange_weak(&DotNetRuntimeContractDescriptor, &prev, descriptor));
}
```

