# Data Contracts

The diagnostic data contract documents a subset of internal .NET runtime in-memory data structures. It enables diagnostic tools to inspect state of .NET runtime process by directly reading and interpreting process memory. It is meant to be used debuggers - for both live and post-mortem debugging, profilers, and other diagnostic tools. We expect it to enable innovative solutions like [unwinding through JITed code using eBPF filters](https://github.com/dotnet/runtime/issues/93550).

The diagnostic data contract addresses multiple problems of the established .NET runtime debugger architecture. The established CoreCLR debugger architecture requires debugger to acquire and load DAC and DBI libraries that exactly match the version of .NET runtime being debugged. It comes with multiple challenges:
- *Security*: The DBI and DAC libraries that match the exact .NET runtime may be untrusted (e.g. custom or 3rd party build of .NET runtime). https://github.com/dotnet/runtime/blob/main/docs/workflow/debugging/coreclr/debugging-runtime.md#resolving-signature-validation-errors-in-visual-studio has some additional context.
- *Servicing*: It is difficult to ship a debugger-only fix in DBI and DAC libraries without shipping a new runtime build. Instead, we create a new runtime build and debugger behavior only improves once the new runtime build is targeted.
- *Acquisition*: Where to acquire the DBI and DAC libraries that match the exact .NET runtime version from.
- *Cross-architecture*: The host/target of DBI and DAC libraries may not be available. https://github.com/dotnet/runtime/blob/main/docs/design/features/cross-dac.md has some additional context.

Diagnostic data contract addressed these challenges by eliminating the need for exactly matching DAC and DBI libraries.
Data contracts represent the manner in which a tool which is not the runtime can reliably understand and observe the behavior of the runtime. Contracts are defined by their documentation, and the runtime describes what contracts are applicable to understanding that runtime.

## Data Contract Descriptor
The physical layout of this data is defined in [the contract descriptor](./contract_descriptor.md) doc. Its practical effects are discussed here.

The Data Contract Descriptor has a set of records of the following forms.

### Data descriptor

The data descriptor is a logical entity that defines the layout of certain types relevant to one or
more algorithmic contracts, as well as global values known to the target runtime that may be
relevant to one or more algorithmic contracts.

More details are provided in the [data descriptor spec](./data_descriptor.md).  We highlight some important aspects below:

#### Global Values

Global values which can be either primitive integer constants or pointers.
All global values have a string describing their name, a type, and a value of one of the above types.

#### Data Structure Layout

Each data structure layout has a name for the type, followed by a list of fields. These fields can
be primitive integer types or pointers or another named data structure type. Each field descriptor
provides the offset of the field, the name of the field, and the type of the field.

Data structures may have a determinate size, specified in the descriptor, or an indeterminate size.
Determinate sizes are used by contracts for pointer arithmetic such as for iterating over arrays.
The determinate size of a structure may be larger than the sum of the sizes of the fields specified
in the data descriptor (that is, the data descriptor does not include every field and may not
include padding bytes).

### Compatible Contract

Each compatible contract is described by a string naming the contract, and a uint32 version. It is an ERROR if multiple versions of a contract are specified in the contract descriptor.


## Versioning of contracts
Contracts are described an integer version number. A higher version number is not more recent, it just means different. In order to avoid conflicts, all contracts should be documented in the main branch of the dotnet repository with a version number which does not conflict with any other. It is expected that every version of every contract describes the same functionality/data layout/set of global values.

## Contract data model
Logically a contract may refer to another contract. If it does so, it will typically refer to other contracts by names which do not include the contract version. This is to allow for version flexibility. Logically once the Data Contract Descriptor is fully processed, there is a single list of contracts that represents the set of contracts useable with whatever runtime instance is being processed.

## Algorithmic contracts

Algorithmic contracts define how to process a given set of data structures to produce useful results. These are effectively code snippets which utilize the abstracted data structures and global values provided by data descriptor to produce useful output about a given program. Descriptions of these contracts may refer to functionality provided by other contracts to do their work. The algorithms provided in these contracts are designed to operate given the ability to read various primitive types and defined data structures from the process memory space, as well as perform general purpose computation.

It is entirely reasonable for an algorithmic contract to have multiple entrypoints which take different inputs. For example imagine a contract which provides information about a `MethodTable`. It may provide the an api to get the `BaseSize` of a `MethodTable`, and an api to get the `DynamicTypeID` of a `MethodTable`. However, while the set of contracts which describe an older version of .NET may provide a means by which the `DynamicTypeID` may be acquired for a `MethodTable`, a newer runtime may not have that concept. In such a case, it is very reasonable to define that the `GetDynamicTypeID` api portion of that contract is defined to simply `throw new NotSupportedException();`

For simplicity, as it can be expected that all developers who work on the .NET runtime understand C# to a fair degree, it is preferred that the algorithms be defined in C#, or at least psuedocode that looks like C#. It is also considered entirely permissible to refer to other specifications if the algorithm is a general purpose one which is well defined by the OS or some other body. (For example, it is expected that the unwinding algorithms will be defined by references into either the DWARF spec, or various Windows Unwind specifications.)

For working with data from the target process/other contracts, the following C# interface is intended to be used within the algorithmic descriptions:

Best practice is to either write the algorithm in C# like psuedocode working on top of the [C# style api](contract_csharp_api_design.cs) or by reference to specifications which are not co-developed with the runtime, such as OS/architecture specifications. Within the contract algorithm specification, the intention is that all interesting api work is done by using an instance of the `Target` class.

Algorithmic contracts may include specifications for numbers which can be referred to in the contract or by other contracts. The intention is that these global values represent magic numbers and values which are useful for the operation of algorithmic contracts.

While not all versions of a data structure are required to have the same fields/type of fields,
algorithms may be built targeting the union of the set of field types defined in the data structure
descriptors of possible target runtimes. Access to a field which isn't defined on the current
runtime will produce an error.


## Arrangement of contract specifications in the repo

Specs shall be stored in the repo in a set of directories. `docs/design/datacontracts` Each one of them shall be a separate markdown file named with the name of contract. `docs/design/datacontracts/<contract_name>.md` Every version of each contract shall be located in the same file to facilitate understanding how variations between different contracts work.

### Algorithmic Contract

Algorithmic contracts describe how an algorithm that processes over data layouts work. Every version of an algorithmic contract presents a consistent api to consumers of the contract.

There are several sections:
1. The header, where a description of what the contract can do is placed.
2. The exposed data structures of the contract.
3. The api surface of the contract
4. The set of versions of the contract.

For each version of the contract, there shall be the set of versions that are associated with a particular implementation as well as some form of description of how the algorithm works for that version. Best practice is to either write the algorithm in C# like psuedocode working on top of the [C# style api](contract_csharp_api_design.cs) or by reference to specifications which are not co-developed with the runtime, such as OS/architecture specifications.

``````
# Contract `<contract_name>`

Insert description of contract, and what it can do here.

## Data structures defined by contract
``` csharp
record struct SomeStructUsedAsPartOfContractApi (int Value, int Value2);
```

## Apis of contract
``` csharp
SomeStructUsedAsPartOfContractApi GetStruct(TargetPointer pointerName);
int ComputeInterestingValue(TargetPointer pointerName);
int ComputeInterestingValue2(SomeStructUsedAsPartOfContractApi struct);
```

## Version 1

Version 1 is what we started with

``` csharp
SomeStructUsedAsPartOfContractApi GetStruct(TargetPointer pointerName)
{
    var runtimeDataStruct = new SomeRuntimeDataStructure(Target, pointerName);
    return new SomeStructUSedAsPartOfContractApi(runtimeDataStruct.Field1, runtimeDataStruct.Field2);
}
int ComputeInterestingValue(TargetPointer pointerName)
{
    var runtimeDataStruct = new SomeRuntimeDataStructure(Target, pointerName);
    return runtimeDataStruct.Field1 + runtimeDataStruct.Field2;
}
int ComputeInterestingValue2(SomeStructUsedAsPartOfContractApi struct)
{
    return struct.Value2;
}
```

## Version 2-5

Versions 2 to 5 are similar in most ways, but differ based on their ContractVersion in others.

``` csharp
SomeStructUsedAsPartOfContractApi GetStruct(TargetPointer pointerName)
{
    var runtimeDataStruct = new SomeRuntimeDataStructure(Target, pointerName);
    return new SomeStructUSedAsPartOfContractApi(runtimeDataStruct.Field1, runtimeDataStruct.Field2);
}
int ComputeInterestingValue(TargetPointer pointerName)
{
    var runtimeDataStruct = new SomeRuntimeDataStructure(Target, pointerName);
    if (ContractVersion > 3)
        return runtimeDataStruct.Field3 + runtimeDataStruct.Field2;
    else
        return runtimeDataStruct.Field3 ^ runtimeDataStruct.Field2;
}
int ComputeInterestingValue2(SomeStructUsedAsPartOfContractApi struct)
{
    if (ContractVersion > 4)
        return struct.Value2;
    else
        return struct.Value1;
}
```
``````

Which should format like:
# Contract `<contract_name>`

Insert description of contract, and what it can do here.

## Data structures defined by contract
``` csharp
record struct SomeStructUsedAsPartOfContractApi (int Value, int Value2);
```

## Apis of contract
``` csharp
SomeStructUsedAsPartOfContractApi GetStruct(TargetPointer pointerName);
int ComputeInterestingValue(TargetPointer pointerName);
int ComputeInterestingValue2(SomeStructUsedAsPartOfContractApi struct);
```

## Version 1

Version 1 is what we started with

``` csharp
SomeStructUsedAsPartOfContractApi GetStruct(TargetPointer pointerName)
{
    var runtimeDataStruct = new SomeRuntimeDataStructure(Target, pointerName);
    return new SomeStructUSedAsPartOfContractApi(runtimeDataStruct.Field1, runtimeDataStruct.Field2);
}
int ComputeInterestingValue(TargetPointer pointerName)
{
    var runtimeDataStruct = new SomeRuntimeDataStructure(Target, pointerName);
    return runtimeDataStruct.Field1 + runtimeDataStruct.Field2;
}
int ComputeInterestingValue2(SomeStructUsedAsPartOfContractApi struct)
{
    return struct.Value2;
}
```

## Version 2-5

Versions 2 to 5 are similar in most ways, but differ based on their ContractVersion in others.

``` csharp
SomeStructUsedAsPartOfContractApi GetStruct(TargetPointer pointerName)
{
    var runtimeDataStruct = new SomeRuntimeDataStructure(Target, pointerName);
    return new SomeStructUSedAsPartOfContractApi(runtimeDataStruct.Field1, runtimeDataStruct.Field2);
}
int ComputeInterestingValue(TargetPointer pointerName)
{
    var runtimeDataStruct = new SomeRuntimeDataStructure(Target, pointerName);
    if (ContractVersion > 3)
        return runtimeDataStruct.Field3 + runtimeDataStruct.Field2;
    else
        return runtimeDataStruct.Field3 ^ runtimeDataStruct.Field2;
}
int ComputeInterestingValue2(SomeStructUsedAsPartOfContractApi struct)
{
    if (ContractVersion > 4)
        return struct.Value2;
    else
        return struct.Value1;
}
```
