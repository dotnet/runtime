namespace DataContracts
{

    // Indicate that this type is a DataContractType which should have the DataContractTypeSourceGenerator applied to it
    // Also that any types nested in this type with the DataContractLayout define particular versioned layouts for data structures
    class DataContractTypeAttribute : System.Attribute {}


    // Defined on each specific data layout, the fields of the type are defined by the fields of the class
    class DataContractLayoutAttribute : System.Attribute
    {
        public DataContractLayoutAttribute(uint version, uint typeSize) { Version = version; TypeSize = typeSize; }
        public uint Version;
        public uint TypeSize;
    }

    // Defined on the class that contains global fields for a contract. The name and version are used to identify the contract
    class DataContractGlobalsAttribute : System.Attribute
    {
        public DataContractGlobalsAttribute(string name, uint version) { Name = name; Version = version; }
        public string Name;
        public uint Version;
    }

    // Defined on the class that contains an algorithmic contract. The version, and base type of the associated type are used to identify the contract,
    // there must exist a constructor of the type with the following signature (DataContracts.Target target, uint contractVersion)
    class DataContractAlgorithmAttribute : System.Attribute
    {
        public DataContractAlgorithmAttribute(params uint []version) { Name = name; Version = version; }
        public uint[] Version;
    }

    struct TargetPointer
    {
        public ulong Value;
        public static TargetPointer Null = new TargetPointer(0);
        // Add a full set of operators to support pointer arithmetic
    }

    struct TargetNInt
    {
        public long Value;
        // Add a full set of operators to support arithmetic as well as casting to/from TargetPointer 
    }

    struct TargetNUInt
    {
        public ulong Value;
        // Add a full set of operators to support arithmetic as well as casting to/from TargetPointer
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
        NIntType,
        NUIntType,
        PointerType,

        // Other values are dynamically assigned by the type definition rules
    }

    struct FieldLayout
    {
        public int Offset;
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
        // Users of the data contract may adjust this number to force re-reading of all data
        public int CurrentEpoch = 0;

        sbyte ReadInt8(TargetPointer pointer);
        byte ReadUInt8(TargetPointer pointer);
        short ReadInt16(TargetPointer pointer);
        ushort ReadUInt16(TargetPointer pointer);
        int ReadInt32(TargetPointer pointer);
        uint ReadUInt32(TargetPointer pointer);
        long ReadInt64(TargetPointer pointer);
        ulong ReadUInt64(TargetPointer pointer);
        TargetPointer ReadTargetPointer(TargetPointer pointer);
        TargetNInt ReadNInt(TargetPointer pointer);
        TargetNUInt ReadNUint(TargetPointer pointer);
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
        bool TryReadNInt(TargetPointer pointer, out TargetNInt value);
        bool TryReadNUInt(TargetPointer pointer, out TargetNUInt value);
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

        partial class Contracts
        {
            FieldLayout GetFieldLayout(string typeName, string fieldName);
            bool TryGetFieldLayout(string typeName, string fieldName, out FieldLayout layout);
            int GetTypeSize(string typeName);
            bool TryGetTypeSize(string typeName, out int size);

            object GetContract(string contractName);
            bool TryGetContract(string contractName, out object contract);

            // Every contract that is defined has a field here. As an example this document defines a MethodTableContract
            // If the contract is not supported by the runtime in use, then the implementation of the contract will be the base type which
            // is defined to throw if it is ever used.

            // List of contracts will be inserted here by source generator
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
        class MethodTableContract
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
                //
                // This lookup can either be reflection based, or we can do it based on a source generator.
            }
        }

        [DataContractGlobals("FeatureFlags", 1)]
        public class FeatureFlags_1
        {
            public const int FeatureComInterop = 0;
        }

        [DataContractGlobals("FeatureFlags", 2)]
        public class FeatureFlags_2
        {
            public const int FeatureComInterop = 1;
        }

        [DataContractAlgorithm(1)]
        class MethodTableContract_1 : ContractDefinitions.MethodTableContract, IAlgorithmContract
        {
            DataContracts.Target Target;
            readonly uint ContractVersion;
            public MethodTableContract_1(DataContracts.Target target, uint contractVersion) { Target = target; ContractVersion = contractVersion; }

            public virtual int DynamicTypeID(TargetPointer methodTablePointer) { return new MethodTable(_target, methodTablePointer).dynamicTypeId; }
            public virtual int BaseSize(TargetPointer methodTablePointer) { return new MethodTable(_target, methodTablePointer).baseSizeAndFlags & 0x3FFFFFFF; }
        }

        // This is used for version 2 and 3 of the contract, where the dynamic type id is no longer present, and baseSize has a new limitation in that it can only be a value up to 0x1FFFFFFF in v3
        [DataContractAlgorithm(2, 3)]
        class MethodTableContract_2 : ContractDefinitions.MethodTableContract, IAlgorithmContract
        {
            DataContracts.Target Target;
            readonly uint ContractVersion;
            public MethodTableContract_2(DataContracts.Target target, uint contractVersion) { Target = target; }

            public virtual int DynamicTypeID(TargetPointer methodTablePointer)
            {
                throw new NotImplementedException();
            }
            public virtual int BaseSize(TargetPointer methodTablePointer)
            {
                return new MethodTable(_target, methodTablePointer).baseSizeAndFlags & ((ContractVersion == 3) ? 0x1FFFFFFF : 0x3FFFFFFF);
            }
        }

        // We use a source generator to generate the actual runtime properties, and api for working with the fields on this type.
        //
        // The source generator would fill in most of the apis, and provide a bunch of properties that give a granular failure model where if a particular field isn't defined, it fails at the access point
        // This example shows access to a type.
        [DataContractType]
        partial struct MethodTable
        {
            partial void Get_dynamicTypeId_optional(ref int value);
            partial void Get_baseSizeAndFlags(ref int value);

            [DataContractLayout(1, 8)]
            public class DataLayout1
            {
                [FieldOffset(0)]
                public int dynamicTypeId;
                [FieldOffset(4)]
                public int baseSize;
            }
            [DataContractLayout(2, 4)]
            public class DataLayout2
            {
                [FieldOffset(0)]
                public int baseSize;
            }

            // The rest of this is generated by a source generator
            public uint TypeSize => _layout.TypeSize;
            void Get_dynamicTypeId_optional(ref int value)
            {
                value = dynamicTypeId;
            }
            void Get_baseSizeAndFlags(ref int value)
            {
                value = baseSizeAndFlags;
            }

            private static int LayoutIndex = DataContracts.Target.RegisterLayout(MethodTableLayout.GetLayoutByTarget);

            public readonly TargetPointer Pointer;
            private int _epoch;
            private readonly MethodTableLayout _layout;

            public MethodTable(DataContracts.Target target, TargetPointer pointer)
            {
                Pointer = pointer;
                _epoch = Int32.MinInt;
                _layout = target.GetLayoutByIndex(LayoutIndex);
            }
            class MethodTableLayout
            {
                public static object GetLayoutByTarget(DataContracts.Target target)
                {
                    return new MethodTableLayout(target);
                }

                public readonly uint TypeSize;

                private MethodTableLayout(DataContracts.Target target)
                {
                    Target = target;
                    TypeSize = target.Contract.GetTypeSize("MethodTable");
                    if (!_target.Contract.TryGetFieldLayout("MethodTable", "dynamicTypeId", out var dynamicTypeIdField))
                    {
                        dynamicTypeId_Offset = -1;
                    }
                    else
                    {
                        if (dynamicTypeIdField.Type != FieldType.Int32Type)
                            dynamicTypeId_Offset = -2;
                        else
                            dynamicTypeId_Offset = dynamicTypeIdField.Offset;
                    }
                    if (!_target.Contract.TryGetFieldLayout("MethodTable", "baseSizeAndFlags", out var baseSizeAndFlagsField))
                    {
                        baseSizeAndFlags_Offset = -1;
                    }
                    else
                    {
                        if (baseSizeAndFlagsField.Type != FieldType.Int32Type)
                            baseSizeAndFlags_Offset = -2;
                        else
                            baseSizeAndFlags_Offset = baseSizeAndFlagsField.Offset;
                    }
                }
                public readonly DataContracts.Target Target;

                int dynamicTypeId_Offset;
                public int dynamicTypeId(TargetPointer pointer)
                {
                    if (dynamicTypeId_Offset == -1)
                    {
                        throw new Exception("MethodTable has no field dynamicTypeId");
                    }
                    if (dynamicTypeId_Offset == -2)
                    {
                        throw new Exception("MethodTable field dynamicTypeId does not have type int32");
                    }
                    return _target.ReadInt32(pointer + dynamicTypeId_Offset);
                }
                public bool Has_dynamicTypeId => dynamicTypeId_Offset >= 0;

                int baseSizeAndFlags_Offset;
                public int baseSizeAndFlags(TargetPointer pointer)
                {
                    if (baseSizeAndFlags_Offset == -1)
                    {
                        throw new Exception("MethodTable has no field baseSizeAndFlags");
                    }
                    if (baseSizeAndFlags_Offset == -2)
                    {
                        throw new Exception("MethodTable field baseSizeAndFlags does not have type int32");
                    }
                    return _target.ReadInt32(pointer + baseSizeAndFlags_Offset);
                }
            }

            private int _dynamicTypeId;
            public int dynamicTypeId
            {
                get
                {
                    int currentEpoch = _layout.Target.CurrentEpoch;
                    if (_epoch != currentEpoch)
                    {
                        _dynamicTypeId = _layout.dynamicTypeId(Pointer);
                        _epoch = currentEpoch;
                    }
                    return _dynamicTypeId;
                }
            }
            public bool Has_dynamicTypeId => layout.Has_dynamicTypeId;

            private int _baseSizeAndFlags;
            public int baseSizeAndFlags
            {
                get
                {
                    int currentEpoch = _layout.Target.CurrentEpoch;
                    if (_epoch != currentEpoch)
                    {
                        _baseSizeAndFlags = _layout.baseSizeAndFlags(Pointer);
                        _epoch = currentEpoch;
                    }
                    return _baseSizeAndFlags;
                }
            }
        }
    }
}
