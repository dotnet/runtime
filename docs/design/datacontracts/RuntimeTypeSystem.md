# Contract RuntimeTypeSystem

This contract is for exploring the properties of the runtime types of values on the managed heap or on the stack in a .NET process.

## APIs of contract

A `MethodTable` is the runtime representation of the type information about a value which is represented as a TypeHandle.
Given a `TargetPointer` address, the `RuntimeTypeSystem` contract provides a `MethodTableHandle` for querying the `MethodTable`.
Some other apis are available while using `TypeHandle` which is provided similarly to a MethodTable


``` csharp
struct MethodTableHandle
{
    // no public properties or constructors

    public TargetPointer Address { get; }
}

struct TypeHandle
{
}


internal enum CorElementType
{
    Void = 1,
    Boolean = 2,
    Char = 3,
    I1 = 4,
    U1 = 5,
    I2 = 6,
    U2 = 7,
    I4 = 8,
    U4 = 9,
    I8 = 0xa,
    U8 = 0xb,
    R4 = 0xc,
    R8 = 0xd,
    String = 0xe,
    Ptr = 0xf,
    Byref = 0x10,
    ValueType = 0x11,
    Class = 0x12,
    Var = 0x13,
    Array = 0x14,
    GenericInst = 0x15,
    TypedByRef = 0x16,
    I = 0x18,
    U = 0x19,
    FnPtr = 0x1b,
    Object = 0x1c,
    SzArray = 0x1d,
    MVar = 0x1e,
    CModReqd = 0x1f,
    CModOpt = 0x20,
    Internal = 0x21,
    Sentinel = 0x41,
}
```

A `TypeHandle` is the runtime representation of the type information about a value.  This can be constructed from either a `MethodTableHandle` or 
struct TypeHandle

``` csharp
    #region MethodTable inspection APIs
    public virtual MethodTableHandle GetMethodTableHandle(TargetPointer targetPointer);

    public virtual TargetPointer GetModule(MethodTableHandle methodTable);
    // A canonical method table is either the MethodTable itself, or in the case of a generic instantiation, it is the
    // MethodTable of the prototypical instance.
    public virtual TargetPointer GetCanonicalMethodTable(MethodTableHandle methodTable);
    public virtual TargetPointer GetParentMethodTable(MethodTableHandle methodTable);

    public virtual uint GetBaseSize(MethodTableHandle methodTable);
    // The component size is only available for strings and arrays.  It is the size of the element type of the array, or the size of an ECMA 335 character (2 bytes)
    public virtual uint GetComponentSize(MethodTableHandle methodTable);

    // True if the MethodTable is the sentinel value associated with unallocated space in the managed heap
    public virtual bool IsFreeObjectMethodTable(MethodTableHandle methodTable);
    public virtual bool IsString(MethodTableHandle methodTable);
    // True if the MethodTable represents a type that contains managed references
    public virtual bool ContainsGCPointers(MethodTableHandle methodTable);
    public virtual bool IsDynamicStatics(MethodTableHandle methodTable);
    public virtual ushort GetNumMethods(MethodTableHandle methodTable);
    public virtual ushort GetNumInterfaces(MethodTableHandle methodTable);

    // Returns an ECMA-335 TypeDef table token for this type, or for its generic type definition if it is a generic instantiation
    public virtual uint GetTypeDefToken(MethodTableHandle methodTable);
    // Returns the ECMA 335 TypeDef table Flags value (a bitmask of TypeAttributes) for this type,
    // or for its generic type definition if it is a generic instantiation
    public virtual uint GetTypeDefTypeAttributes(MethodTableHandle methodTable);
    public virtual ReadOnlySpan<MethodTableHandle> GetInstantiation(MethodTableHandle methodTable);
    public virtual bool IsGenericTypeDefinition(MethodTableHandle methodTable);

    #endregion MethodTable inspection APIs

    #region TypeHandle inspection APIs
    public virtual TypeHandle TypeHandleFromAddress(TargetPointer address);
    public virtual bool HasTypeParam(TypeHandle typeHandle);
    // Element type of the type. NOTE: this drops the CorElementType.GenericInst for generics, and CorElementType.String is returned as CorElementType.Class
    public virtual CorElementType GetSignatureCorElementType(TypeHandle typeHandle);

    // return true if the TypeHandle represents an array, and set the rank to either 0 (if the type is not an array), or the rank number if it is.
    public virtual bool IsArray(TypeHandle typeHandle, out uint rank);
    public virtual TypeHandle GetTypeParam(TypeHandle typeHandle);
    public virtual bool IsGenericVariable(TypeHandle typeHandle, out TargetPointer module, out uint token);
    public virtual bool IsFunctionPointer(TypeHandle typeHandle, out ReadOnlySpan<TypeHandle> retAndArgTypes, out byte callConv);

    #endregion TypeHandle inspection APIs
```

## Version 1

The `MethodTable` inspection APIs are implemented in terms of the following flags on the runtime `MethodTable` structure:

``` csharp
internal partial struct RuntimeTypeSystem_1
{
    // The lower 16-bits of the MTFlags field are used for these flags,
    // if WFLAGS_HIGH.HasComponentSize is unset
    [Flags]
    internal enum WFLAGS_LOW : uint
    {
        GenericsMask = 0x00000030,
        GenericsMask_NonGeneric = 0x00000000,   // no instantiation
        GenericsMask_TypicalInstantiation = 0x00000030,   // the type instantiated at its formal parameters, e.g. List<T>

        StringArrayValues = GenericsMask_NonGeneric,
    }

    // Upper bits of MTFlags
    [Flags]
    internal enum WFLAGS_HIGH : uint
    {
        Category_Mask = 0x000F0000,
        Category_ElementType_Mask = 0x000E0000,
        Category_IfArrayThenSzArray = 0x00020000,
        Category_Array = 0x00080000,
        Category_ValueType = 0x00040000,
        Category_Nullable = 0x00050000,
        Category_PrimitiveValueType = 0x00060000,
        Category_TruePrimitive = 0x00070000,
        Category_Interface = 0x000C0000,
        Category_Array_Mask = 0x000C0000,
        ContainsGCPointers = 0x01000000,
        HasComponentSize = 0x80000000, // This is set if lower 16 bits is used for the component size,
                                       // otherwise the lower bits are used for WFLAGS_LOW
    }

    [Flags]
    internal enum WFLAGS2_ENUM : uint
    {
        DynamicStatics = 0x0002,
    }

    // Encapsulates the MethodTable flags v1 uses
    internal struct MethodTableFlags
    {
        public uint MTFlags { get; }
        public uint MTFlags2 { get; }
        public uint BaseSize { get; }

        public WFLAGS_LOW GetFlag(WFLAGS_LOW mask) { ... /* mask & lower 16 bits of MTFlags */ }
        public WFLAGS_HIGH GetFlag(WFLAGS_HIGH mask) { ... /* mask & upper 16 bits of MTFlags */ }

        public WFLAGS2_ENUM GetFlag(WFLAGS2_ENUM mask) { ... /* mask & MTFlags2*/ }

        private bool TestFlagWithMask(WFLAGS_LOW mask, WFLAGS_LOW flag)
        {
            if (IsStringOrArray)
            {
                return (WFLAGS_LOW.StringArrayValues & mask) == flag;
            }
            else
            {
                return (FlagsLow & mask) == flag;
            }
        }

        public ushort ComponentSizeBits => (ushort)(MTFlags & 0x0000ffff); // only meaningful if HasComponentSize is set

        public bool HasComponentSize => GetFlag(WFLAGS_HIGH.HasComponentSize) != 0;
        public bool IsInterface => GetFlag(WFLAGS_HIGH.Category_Mask) == WFLAGS_HIGH.Category_Interface;
        public bool IsString => HasComponentSize && !IsArray && ComponentSizeBits == 2;
        public bool IsArray => GetFlag(WFLAGS_HIGH.Category_Array_Mask) == WFLAGS_HIGH.Category_Array;
        public bool IsStringOrArray => HasComponentSize;
        public ushort ComponentSize => HasComponentSize ? ComponentSizeBits : (ushort)0;
        public bool HasInstantiation => !TestFlagWithMask(WFLAGS_LOW.GenericsMask, WFLAGS_LOW.GenericsMask_NonGeneric);
        public bool ContainsGCPointers => GetFlag(WFLAGS_HIGH.ContainsGCPointers) != 0;
        public bool IsDynamicStatics => GetFlag(WFLAGS2_ENUM.DynamicStatics) != 0;
        public bool IsGenericTypeDefinition => TestFlagWithMask(WFLAGS_LOW.GenericsMask, WFLAGS_LOW.GenericsMask_TypicalInstantiation);
    }

    [Flags]
    internal enum EEClassOrCanonMTBits
    {
        EEClass = 0,
        CanonMT = 1,
        Mask = 1,
    }
}
```

Internally the contract has a `MethodTable_1` struct that depends on the `MethodTable` data descriptor

```csharp
internal struct MethodTable_1
{
    internal RuntimeTypeSystem_1.MethodTableFlags Flags { get; }
    internal ushort NumInterfaces { get; }
    internal ushort NumVirtuals { get; }
    internal TargetPointer ParentMethodTable { get; }
    internal TargetPointer Module { get; }
    internal TargetPointer EEClassOrCanonMT { get; }
    internal TargetPointer PerInstInfo { get; }
    internal MethodTable_1(Data.MethodTable data)
    {
        Flags = new RuntimeTypeSystem_1.MethodTableFlags
        {
            MTFlags = data.MTFlags,
            MTFlags2 = data.MTFlags2,
            BaseSize = data.BaseSize,
        };
        NumInterfaces = data.NumInterfaces;
        NumVirtuals = data.NumVirtuals;
        EEClassOrCanonMT = data.EEClassOrCanonMT;
        Module = data.Module;
        ParentMethodTable = data.ParentMethodTable;
        PerInstInfo = data.PerInstInfo;
    }
}
```

Internally the contract depends on a `TypeDescHandle` structure that represents the address of something that implements the TypeDesc data descriptor.

Internally the contract depends on the `TypeHandle` structure being capable of holding a TypeDescHandle or a MethodTableHandle.
```csharp
struct TypeHandle
{
    internal TypeHandle(TypeDescHandle typeDescHandle);
    internal TypeHandle(MethodTableHandle methodTableHandle);
    internal bool IsMethodTable { get; }
    internal MethodTableHandle AsMethodTable { get; }
    internal bool IsTypeDesc { get; }
    internal TypeDescHandle AsTypeDesc { get; }
}
```

The contract depends on the global pointer value `FreeObjectMethodTablePointer`.
The contract additionally depends on these data descriptors

| Data Descriptor Name |
| --- |
| `EEClass` |
| `ArraayClass` |
| `TypeDesc` |
| `ParamTypeDesc` |
| `TypeVarTypeDesc` |
| `FnPtrTypeDesc` |
| `GenericsDictInfo` |


```csharp
    private readonly Dictionary<TargetPointer, MethodTable_1> _methodTables;

    internal TargetPointer FreeObjectMethodTablePointer {get; }

    public MethodTableHandle GetMethodTableHandle(TargetPointer methodTablePointer)
    {
        ... // validate that methodTablePointer points to something that looks like a MethodTable.
        ... // read Data.MethodTable from methodTablePointer.
        ... // create a MethodTable_1 and add it to _methodTables.
        return MethodTableHandle { Address = methodTablePointer }
    }

    internal static EEClassOrCanonMTBits GetEEClassOrCanonMTBits(TargetPointer eeClassOrCanonMTPtr)
    {
        return (EEClassOrCanonMTBits)(eeClassOrCanonMTPtr & (ulong)EEClassOrCanonMTBits.Mask);
    }

    public uint GetBaseSize(MethodTableHandle methodTableHandle) => _methodTables[methodTableHandle.Address].Flags.BaseSize;

    public uint GetComponentSize(MethodTableHandle methodTableHandle) => GetComponentSize(_methodTables[methodTableHandle.Address]);

    private TargetPointer GetClassPointer(MethodTableHandle methodTableHandle)
    {
        ... // if the MethodTable stores a pointer to the EEClass, return it
            // otherwise the MethodTable stores a pointer to the canonical MethodTable
            // in that case, return the canonical MethodTable's EEClass.
            // Canonical MethodTables always store an EEClass pointer.
    }

    private Data.EEClass GetClassData(MethodTableHandle methodTableHandle)
    {
        TargetPointer eeClassPtr = GetClassPointer(methodTableHandle);
        ... // read Data.EEClass data from eeClassPtr
    }


    public TargetPointer GetCanonicalMethodTable(MethodTableHandle methodTableHandle) => GetClassData(methodTableHandle).MethodTable;

    public TargetPointer GetModule(MethodTableHandle methodTableHandle) => _methodTables[methodTableHandle.Address].Module;
    public TargetPointer GetParentMethodTable(MethodTableHandle methodTableHandle) => _methodTables[methodTableHandle.Address].ParentMethodTable;

    public bool IsFreeObjectMethodTable(MethodTableHandle methodTableHandle) => FreeObjectMethodTablePointer == methodTableHandle.Address;

    public bool IsString(MethodTableHandle methodTableHandle) => _methodTables[methodTableHandle.Address].Flags.IsString;
    public bool ContainsGCPointers(MethodTableHandle methodTableHandle) => _methodTables[methodTableHandle.Address].Flags.ContainsGCPointers;

    public uint GetTypeDefToken(MethodTableHandle methodTableHandle)
    {
        MethodTable_1 methodTable = _methodTables[methodTableHandle.Address];
        return (uint)(methodTable.Flags.GetTypeDefRid() | ((int)TableIndex.TypeDef << 24));
    }

    public ushort GetNumMethods(MethodTableHandle methodTableHandle) => GetClassData(methodTableHandle).NumMethods;

    public ushort GetNumInterfaces(MethodTableHandle methodTableHandle) => _methodTables[methodTableHandle.Address].NumInterfaces;

    public uint GetTypeDefTypeAttributes(MethodTableHandle methodTableHandle) => GetClassData(methodTableHandle).CorTypeAttr;

    public bool IsDynamicStatics(MethodTableHandle methodTableHandle) => _methodTables[methodTableHandle.Address].Flags.IsDynamicStatics;

    public ReadOnlySpan<MethodTableHandle> GetInstantiation(MethodTableHandle methodTableHandle)
    {
        MethodTable_1 methodTable = _methodTables[methodTableHandle.Address];
        if (!methodTable.Flags.HasInstantiation)
            return default;

        TargetPointer perInstInfo = methodTable.PerInstInfo;
        TargetPointer genericsDictInfo = perInstInfo - (ulong)_target.PointerSize;
        TargetPointer dictionaryPointer = _target.ReadPointer(perInstInfo);

        int NumTypeArgs = // Read NumTypeArgs from genericsDictInfo using GenericsDictInfo contract
        MethodTableHandle[] instantiation = new MethodTableHandle[NumTypeArgs];
        for (int i = 0; i < NumTypeArgs; i++)
            instantiation[i] = GetMethodTableHandle(_target.ReadPointer(dictionaryPointer + _target.PointerSize * i));

        return instantiation;
    }

    public bool IsDynamicStatics(MethodTableHandle methodTableHandle) => _methodTables[methodTableHandle.Address].Flags.IsGenericTypeDefinition;

    public TypeHandle TypeHandleFromAddress(TargetPointer address)
    {
        // validate that address points to something that looks like a TypeHandle.

        if (address & 2 == 2)
        {
            return new TypeHandle(new TypeDescHandle(address - 2));
        }
        else
        {
            return new TypeHandle(new MethodTableHandle(address));
        }
    }

    public bool HasTypeParam(TypeHandle typeHandle)
    {
        if (typeHandle.IsMethodTable)
        {
            MethodTable methodTable = _methodTables[typeHandle.AsMethodTable.Address];
            return methodTable.Flags.IsArray;
        }
        else if (typeHandle.IsTypeDesc)
        {
            int TypeAndFlags = // Read TypeAndFlags field from TypeDesc contract using address typeHandle.AsTypeDesc.Address
            CorElementType elemType = (CorElementType)(typeDesc.TypeAndFlags & 0xFF);
            switch (elemType)
            {
                case CorElementType.ValueType:
                case CorElementType.Byref:
                case CorElementType.Ptr:
                    return true;
            }
        }
        return false;
    }

    public CorElementType GetSignatureCorElementType(TypeHandle typeHandle)
    {
        if (typeHandle.IsMethodTable)
        {
            MethodTable methodTable = _methodTables[typeHandle.AsMethodTable.Address];

            switch (methodTable.Flags.GetFlag(WFLAGS_HIGH.Category_Mask))
            {
                case WFLAGS_HIGH.Category_Array:
                    return CorElementType.Array;
                case WFLAGS_HIGH.Category_Array | WFLAGS_HIGH.Category_IfArrayThenSzArray:
                    return CorElementType.SzArray;
                case WFLAGS_HIGH.Category_ValueType:
                case WFLAGS_HIGH.Category_Nullable:
                case WFLAGS_HIGH.Category_PrimitiveValueType:
                    return CorElementType.ValueType;
                case WFLAGS_HIGH.Category_TruePrimitive:
                    return (CorElementType)GetClassData(typeHandle.AsMethodTable).InternalCorElementType;
                default:
                    return CorElementType.Class;
            }
        }
        else if (typeHandle.IsTypeDesc)
        {
            int TypeAndFlags = // Read TypeAndFlags field from TypeDesc contract using address typeHandle.AsTypeDesc.Address
            return (CorElementType)(typeDesc.TypeAndFlags & 0xFF);
        }
        return default(CorElementType);
    }

    // return true if the TypeHandle represents an array, and set the rank to either 0 (if the type is not an array), or the rank number if it is.
    public bool IsArray(TypeHandle typeHandle, out uint rank)
    {
        if (typeHandle.IsMethodTable)
        {
            MethodTable methodTable = _methodTables[typeHandle.AsMethodTable.Address];

            switch (methodTable.Flags.GetFlag(WFLAGS_HIGH.Category_Mask))
            {
                case WFLAGS_HIGH.Category_Array:
                    TargetPointer clsPtr = GetClassPointer(typeHandle.AsMethodTable);
                    rank = // Read Rank field from ArrayClass contract using address clsPtr
                    return true;

                case WFLAGS_HIGH.Category_Array | WFLAGS_HIGH.Category_IfArrayThenSzArray:
                    rank = 1;
                    return true;
            }
        }

        rank = 0;
        return false;
    }

    public TypeHandle GetTypeParam(TypeHandle typeHandle)
    {
        if (typeHandle.IsMethodTable)
        {
            MethodTable methodTable = _methodTables[typeHandle.AsMethodTable.Address];

            // Validate that this is an array
            if (!methodTable.Flags.IsArray)
                throw new ArgumentException(nameof(typeHandle));

            return TypeHandleFromAddress(methodTable.PerInstInfo);
        }
        else if (typeHandle.IsTypeDesc)
        {
            int TypeAndFlags = // Read TypeAndFlags field from TypeDesc contract using address typeHandle.AsTypeDesc.Address
            CorElementType elemType = (CorElementType)(typeDesc.TypeAndFlags & 0xFF);

            switch (elemType)
            {
                case CorElementType.ValueType:
                case CorElementType.Byref:
                case CorElementType.Ptr:
                    TargetPointer typeArgPointer = // Read TypeArg field from ParamTypeDesc contract using address typeHandle.AsTypeDesc.Address
                    return TypeHandleFromAddress(typeArgPointer);
            }
        }
        throw new ArgumentException(nameof(typeHandle));
    }

    public bool IsGenericVariable(TypeHandle typeHandle, out TargetPointer module, out uint token)
    {
        module = TargetPointer.Null;
        token = 0;

        if (!typeHandle.IsTypeDesc)
            return false;

        int TypeAndFlags = // Read TypeAndFlags field from TypeDesc contract using address typeHandle.AsTypeDesc.Address
        CorElementType elemType = (CorElementType)(typeDesc.TypeAndFlags & 0xFF);

        switch (elemType)
        {
            case CorElementType.MVar:
            case CorElementType.Var:
                TypeVarTypeDesc typeVarTypeDesc = _target.ProcessedData.GetOrAdd<TypeVarTypeDesc>(typeHandle.AsTypeDesc.Address);
                module = // Read Module field from TypeVarTypeDesc contract using address typeHandle.AsTypeDesc.Address
                token = // Read Module field from TypeVarTypeDesc contract using address typeHandle.AsTypeDesc.Address
                return true;
        }
        return false;
    }

    public bool IsFunctionPointer(TypeHandle typeHandle, out ReadOnlySpan<TypeHandle> retAndArgTypes, out byte callConv)
    {
        retAndArgTypes = default;
        callConv = default;

        if (!typeHandle.IsTypeDesc)
            return false;

        int TypeAndFlags = // Read TypeAndFlags field from TypeDesc contract using address typeHandle.AsTypeDesc.Address
        CorElementType elemType = (CorElementType)(typeDesc.TypeAndFlags & 0xFF);

        if (elemType != CorElementType.FnPtr)
            return false;

        int NumArgs = // Read NumArgs field from FnPtrTypeDesc contract using address typeHandle.AsTypeDesc.Address
        TargetPointer RetAndArgTypes = // Read NumArgs field from FnPtrTypeDesc contract using address typeHandle.AsTypeDesc.Address

        MethodTableHandle[] retAndArgTypesArray = new TypeHandle[NumTypeArgs + 1];
        for (int i = 0; i <= NumTypeArgs; i++)
            retAndArgTypesArray[i] = TypeHandleFromAddress(_target.ReadPointer(RetAndArgTypes + _target.PointerSize * i));

        TypeHandleArray retAndArgTypesArray = _target.ProcessedData.GetOrAdd<(TargetPointer, int), TypeHandleArray>
            ((fnPtrTypeDesc.RetAndArgTypes, checked((int)fnPtrTypeDesc.NumArgs + 1)));
        retAndArgTypes = retAndArgTypesArray;
        callConv = (byte) // Read CallConv field from FnPtrTypeDesc contract using address typeHandle.AsTypeDesc.Address, and then ignore all but the low 8 bits.
        return true;
    }
```
