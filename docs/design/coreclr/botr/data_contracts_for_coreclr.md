# Data Contracts for CoreCLR, and the data contract stream

Data contracts represent the manner in which a tool which is not the runtime can reliably understand and observe the behavior of the runtime. Contracts are defined by their documentation, and the runtime describes what contracts are applicable to understanding that runtime.

## Data Contract Stream
The physical layout of this stream is not defined in this document, but its practical effects are.

The data contract stream has a set of records of the following forms.

### Global Values
Global values which can be of types (int8, uint8, int16, uint16, int32, uint32, int64, uint64, pointer, unicode string)
All global values have a unicode string describing their name, and a value of one of the above types.

### Compatible Contract
Each compatible contract is described by a unicode string naming the contract, and a uint32 version. It is an ERROR if multiple versions of a contract are specified in the stream.

### Data Structure Layout
Each data structure layout has a unicode name for the type, followed by a list of fields. These fields can be of primitive types (int8, uint8, int16, uint16, int32, uint32, int64, uint64, pointer) or of another named data structure type. Each field descriptor provides the offset of the field, the name of the field, and the type of the field.

## Versioning of contracts
Contracts are described an integer version number. A higher version number is not more recent, it just means different. In order to avoid conflicts, all contracts should be documented in the main branch of the dotnet repository with a version number which does not conflict with any other. It is expected that every version of every contract describes the same functionality/data layout/set of global values.

## Contract data model
Logically a contract may refer to another contract. If it does so, it will typically refer to other contracts by names which do not include the contract version. This is to allow for version flexibility. Logically once the contract data stream is fully processed, there is a single list of contracts that represents the set of contracts useable with whatever runtime instance is being processed.

## Types of contracts

There are 3 different types of contracts each representing a different phase of execution of the data contract system.

### Composition contracts
These contracts indicate the version numbers of other contracts. This is done to reduce the size of contract list needed in the runtime data stream. In general it is intended that as a runtime nears shipping, the product team can gather up all of the current versions of the contracts into a single magic value, which can be used to initialize most of the contract versions of the data contract system. A specific version number in the contract data stream for a given contract will override any composition contracts specified in the contract data stream. If there are multiple composition contracts in a stream which specify the same contract to have a different version, the first composition contract linearly in the runtime data stream wins. This is intended to allow for a composite contract for the architecture/os indepedent work, and a separate composite contract for the non independent work. If a contract is specified explicitly in the Data Contract Stream and a different version is specified via the composition contract mechanism, the explicitly specified contract takes precedence.

### Fixed value contracts
These contracts represent data which is entirely determined by the contract version + contract name. There are 2 subtypes of this form of contract.

#### Global Value Contract
A global value contract specifies numbers which can be referred to by other contracts. If a global value is specified directly in the Contract Data Stream, then the global value defintion in the Contract Data Stream takes precedence. The intention is that these global variable contracts represent magic numbers and values which are useful for the operation of algorithmic contracts. For instance, we will likely have a `TargetPointerSize` global value represented via a contract, and things like `FEATURE_SUPPORTS_COM` can also be a global value contract, with a value of 1.

#### Data Structure Definition Contract
A data structure definition contract defines a single type's physical layout. It MUST be named "MyDataStructureType_layout". If a data structure layout is specified directly in the Contract Data Stream, then the data structure defintion in the Contract Data Stream takes precedence. These contracts are responsible for declaring the field layout of individual fields.

### Algorithmic contracts
Algorithmic contracts define how to process a given set of data structures to produce useful results. These are effectively code snippets which utilize the abstracted data structures provided by Data Structure Definition Contracts and Global Value Contract to produce useful output about a given program. Descriptions of these contracts may refer to functionality provided by other contracts to do their work. The algorithms provided in these contracts are designed to operate given the ability to read various primitive types and defined data structures from the process memory space, as well as perform general purpose computation.

It is entirely reasonable for an algorithmic contract to have multiple entrypoints which take different inputs. For example imagine a contract which provides information about a `MethodTable`. It may provide the an api to get the `BaseSize` of a `MethodTable`, and an api to get the `DynamicTypeID` of a `MethodTable`. However, while the set of contracts which describe an older version of .NET may provide a means by which the `DynamicTypeID` may be acquired for a `MethodTable`, a newer runtime may not have that concept. In such a case, it is very reasonable to define that the `GetDynamicTypeID` api portion of that contract is defined to simply `throw new NotSupportedException();`

For simplicity, as it can be expected that all developers who work on the .NET runtime understand C# to a fair degree, it is preferred that the algorithms be defined in C#, or at least psuedocode that looks like C#. It is also condsidered entirely permissable to refer to other specifications if the algorithm is a general purpose one which is well defined by the OS or some other body. (For example, it is expected that the unwinding algorithms will be defined by references into either the DWARF spec, or various Windows Unwind specifications.)

For working with data from the target process/other contracts, the following C# interface is intended to be used within the algorithmic descriptions:

``` csharp
namespace DataContracts
{
    struct TargetPointer
    {
        public ulong Value;
    }

    enum FieldType
    {
        Int8Type,
        UInt8Type,
        Int16Type,
        UInt16Type,
        Int32Type,
        UInt32Type,
        Int64Type,
        UInt64Type,
        PointerType,

        // Other values are dynamically assigned by the type definition rules
    }

    struct FieldLayout
    {
        public uint Offset;
        public FieldType Type;
    }

    interface IAlgorithmContract
    {
        void Init();
    }

    interface IContract
    {
        string Name { get; }
        uint Version { get; }
    }

    class Target
    {
        sbyte ReadInt8(TargetPointer pointer);
        byte ReadUInt8(TargetPointer pointer);
        short ReadInt16(TargetPointer pointer);
        ushort ReadUInt16(TargetPointer pointer);
        int ReadInt32(TargetPointer pointer);
        uint ReadUInt32(TargetPointer pointer);
        long ReadInt64(TargetPointer pointer);
        ulong ReadUInt64(TargetPointer pointer);
        TargetPointer ReadTargetPointer(TargetPointer pointer);
        byte[] ReadByteArray(TargetPointer pointer, ulong size);
        void FillByteArray(TargetPointer pointer, byte[] array, ulong size);

        bool TryReadInt8(TargetPointer pointer, out sbyte value);
        bool TryReadUInt8(TargetPointer pointer, out byte value);
        bool TryReadInt16(TargetPointer pointer, out short value);
        bool TryReadUInt16(TargetPointer pointer, out ushort value);
        bool TryReadInt32(TargetPointer pointer, out int value);
        bool TryReadUInt32(TargetPointer pointer, out uint value);
        bool TryReadInt64(TargetPointer pointer, out long value);
        bool TryReadUInt64(TargetPointer pointer, out ulong value);
        bool TryReadTargetPointer(TargetPointer pointer, out TargetPointer value);
        bool TryReadByteArray(TargetPointer pointer, ulong size, out byte[] value);
        bool TryFillByteArray(TargetPointer pointer, byte[] array, ulong size);

        // If pointer is 0, then the return value will be 0
        TargetPointer GetTargetPointerForField(TargetPointer pointer, FieldLayout fieldLayout);

        sbyte ReadGlobalInt8(string globalName);
        byte ReadGlobalUInt8(string globalName);
        short ReadGlobalInt16(string globalName);
        ushort ReadGlobalUInt16(string globalName);
        int ReadGlobalInt32(string globalName);
        uint ReadGlobalUInt32(string globalName);
        long ReadGlobalInt64(string globalName);
        ulong ReadGlobalUInt64(string globalName);
        TargetPointer ReadGlobalTargetPointer(string globalName);

        bool TryReadGlobalInt8(string globalName, out sbyte value);
        bool TryReadGlobalUInt8(string globalName, out byte value);
        bool TryReadGlobalInt16(string globalName, out short value);
        bool TryReadGlobalUInt16(string globalName, out ushort value);
        bool TryReadGlobalInt32(string globalName, out int value);
        bool TryReadGlobalUInt32(string globalName, out uint value);
        bool TryReadGlobalInt64(string globalName, out long value);
        bool TryReadGlobalUInt64(string globalName, out ulong value);
        bool TryReadGlobalTargetPointer(string globalName, out TargetPointer value);

        Contracts Contract { get; }

        class Contracts
        {
            FieldLayout GetFieldLayout(string typeName, string fieldName);
            bool TryGetFieldLayout(string typeName, string fieldName, out FieldLayout layout);

            object GetContract(string contractName);
            bool TryGetContract(string contractName, out object contract);

            // Every contract that is defined has a field here. As an example this document defines a MethodTableContract
            // If the contract is not supported by the runtime in use, then the implementation of the contract will be the base type which
            // is defined to throw if it is ever used.

            // Insert list of contracts here
            MethodTableContract MethodTableContract;
        }
    }

    // Types defined by contracts live here
    namespace ContractDefinitions
    {
        class CompositeContract
        {
            List<Tuple<string, uint>> Subcontracts;
        }

        class DataStructureContract
        {
            string MethodTableName {get;}
            List<Tuple<string, FieldLayout>> FieldData;
        }

        // Insert Algorithmic Contract definitions here
        class MethodTableContract : Init
        {
            public virtual int DynamicTypeID(TargetPointer methodTablePointer) { throw new NotImplementedException(); }
            public virtual int BaseSize(TargetPointer methodTablePointer) { throw new NotImplementedException(); }
        }
    }

    namespace ContractImplementation
    {
        // Get contract from the predefined contract database
        static class PredefinedContracts
        {
            public static IContract GetContract(string name, uint version, Target target)
            {
                // Do some lookup and allocate an instance of the contract requested
            }
        }

        class MethodTableContract_1 : IAlgorithmContract
        {
            Target _target;
            FieldLayout _dynamicTypeIdField;
            FieldLayout _baseSizeField;

            public MethodTableContract_1(Target target) { _target = target; }

            void IAlgorithmContract.Init()
            {
                _dynamicTypeIdField = _target.Contract.GetFieldLayout("MethodTable", "dynamicTypeId");
                if (_dynamicTypeIdField.Type != FieldType.Int32Type)
                    throw new InvalidContractSpec();
                _baseSizeField = _target.Contract.GetFieldLayout("MethodTable", "baseSize");
                if (_baseSizeField.Type != FieldType.Int32Type)
                    throw new InvalidContractSpec();
            }

            public virtual int DynamicTypeID(TargetPointer methodTablePointer) { return _target.ReadInt32(_target.GetTargetPointerForField(methodTablePointer, _dynamicTyepIdField)); }
            public virtual int BaseSize(TargetPointer methodTablePointer) { return _target.ReadInt32(_target.GetTargetPointerForField(methodTablePointer, _baseSizeField)); }
        }

        // This is used for version 2 of the contract, where the dynamic type id is no longer present
        class MethodTableContract_2 : IAlgorithmContract
        {
            Target _target;
            FieldLayout _baseSizeField;

            public MethodTableContract_2(Target target) { _target = target; }

            void IAlgorithmContract.Init()
            {
                _baseSizeField = _target.Contract.GetFieldLayout("MethodTable", "baseSize");
                if (_baseSizeField.Type != FieldType.Int32Type)
                    throw new InvalidContractSpec();
            }

            public virtual int DynamicTypeID(TargetPointer methodTablePointer) { throw new NotImplementedException(); }

            public virtual int BaseSize(TargetPointer methodTablePointer) { return _target.ReadInt32(_target.GetTargetPointerForField(methodTablePointer, _baseSizeField)); }
        }
    }
}
```

Within the contract algorithm specification, the intention is that all interesting api work is done by using an instance of the `Target` class.

## Arrangement of contract specifications in the repo

Specs shall be stored in the repo in a set of directories. `docs/design/datacontracts`

### Global Value Contracts
For global value contracts they shall be stored in `docs/design/datacontracts/globals/<contract_name>/<version>`

The name of each contract shall be `docs/design/datacontracts/globals/<contract_name>/<contract_name>_<version>.md`

The format of each contract spec shall be


```
# Contract '<contract_name>' Version '<version_number>'

Insert description of contract, and what its for here.

## Values
| Global Name | Type | Value |
| --- | --- | --- |
| SomeGlobal | Int32 | 1 |
| SomeOtherGlobal | Int8 | 0 |
```

Which should format like:
# Contract '<contract_name>' Version '<version_number>'

Insert description of contract, and what its for here.

## Values
| Global Name | Type | Value |
| --- | --- | --- |
| SomeGlobal | Int32 | 1 |
| SomeOtherGlobal | Int8 | 0 |


### Data Structure Contracts
For global value contracts they shall be stored in `docs/design/datacontracts/datalayout/<contract_name>/<version>`

The name of each contract shall be `docs/design/datacontracts/datalayout/<contract_name>/<contract_name>_<version>.md`

The format of each contract spec shall be


```
# Contract '<type_name>_layout' Version '<version_number>'

Insert description of type, and what its for here.

## Values

Type name '<type_name>'

| Field Name | Type | Offset |
| --- | --- | --- |
| FirstField | Int32 | 0 |
| SecondField | Int8 | 4 |
```

Which should format like:
# Contract '<type_name>_layout' Version '<version_number>'

Insert description of type, and what its for here.

## Values

Type name '<type_name>'

| Field Name | Type | Offset |
| --- | --- | --- |
| FirstField | Int32 | 0 |
| SecondField | Int8 | 4 |

### Algorthmic Contract

Algorithmic contracts these describe how a single contract works.

For these we will have 2 different documents. The first document shall describe the api to the contract, and the second shall describe the actual implementations.

#### Algorithmic Contract api
For algorithmic contract apis they shall be stored in `docs/design/datacontracts/<contract_name>.md`

The format of each contract definition spec shall be

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

Versions
| Versions |
| --- |
| [1](contract_name/contract_name_1.md) |
| [2-5](contract_name/contract_name_2.md) |
``````

Which should format like:
# Contract `<contract_name>`

Insert description of contract, and what it can do here.

## Data structures defined by contract
``` csharp
struct SomeStructUsedAsPartOfContractApi {}
```

## Apis of contract
``` csharp
SomeStructUsedAsPartOfContractApi GetStruct(TargetPointer pointerName);
int ComputeInterestingValue(TargetPointer pointerName);
int ComputeInterestingValue2(SomeStructUsedAsPartOfContractApi struct);
```

Versions
| Versions |
| --- |
| [1](contract_name/contract_name_1.md) |
| [2-5](contract_name/contract_name_2.md) |

#### Algorithmic Contract Implementation
For algorithmic contract implementations they shall be stored in `docs/design/datacontracts/<contract_name>/<contract_name>_<first_version_implemented>.md`

Implementations of contracts must implement the full contract; however, they are permitted to implement the contract as "throw new NotImplementedException();"
Each implementation of a contract will be defined by a document like the follows. Note that for contract implementations it is legal for a single document to describe multiple versions. In that case the `<version_number>` shall be a sequence of numbers, and the implementation will likely have conditions based on the version of the contract in use. Where the contract is defined not by C# code, but instead by reference to another specification, the bodies of the various functions shall be whatever psuedocode makes sense.

``````
# Contract `<contract_name>` Version `<version_number>`

Insert description of contract, and what it can do here.

## Contract global definitions
``` csharp
fieldLayout = _target.Contract.GetFieldLayout("SomeType", "InterestingField");
fieldLayout2 = _target.Contract.GetFieldLayout("SomeType", "InterestingField2");
```

## Apis of contract
``` csharp
SomeStructUsedAsPartOfContractApi GetStruct(TargetPointer pointerName)
{
    return new SomeStructUSedAsPartOfContractApi(_target.ReadInt32(_target.GetTargetPointerForField(methodTablePointer, fieldLayout)),
                                                 _target.ReadInt32(_target.GetTargetPointerForField(methodTablePointer, fieldLayout2)));
}
int ComputeInterestingValue(TargetPointer pointerName)
{
    return _target.ReadInt32(_target.GetTargetPointerForField(methodTablePointer, fieldLayout));
}
int ComputeInterestingValue2(SomeStructUsedAsPartOfContractApi struct)
{
    return struct.Value2;
}
```
``````
Which should format like:
# Contract `<contract_name>` Version `<version_number>`

Insert description of contract, and what it can do here.

## Contract global definitions
``` csharp
fieldLayout = _target.Contract.GetFieldLayout("SomeType", "InterestingField");
fieldLayout2 = _target.Contract.GetFieldLayout("SomeType", "InterestingField2");
```

## Apis of contract
``` csharp
SomeStructUsedAsPartOfContractApi GetStruct(TargetPointer pointerName)
{
    return new SomeStructUSedAsPartOfContractApi(_target.ReadInt32(_target.GetTargetPointerForField(methodTablePointer, fieldLayout)),
                                                 _target.ReadInt32(_target.GetTargetPointerForField(methodTablePointer, fieldLayout2)));
}
int ComputeInterestingValue(TargetPointer pointerName)
{
    return _target.ReadInt32(_target.GetTargetPointerForField(methodTablePointer, fieldLayout));
}
int ComputeInterestingValue2(SomeStructUsedAsPartOfContractApi struct)
{
    return struct.Value2;
}
```
