# Contract Metadata

This contract is for exploring the properties of the metadata of values on the heap or on the stack in a .NET process.

## APIs of contract

A `MethodTable` is the runtime representation of the type information about a value.  Given a `TargetPointer` address, the `Metadata` contract provides a `MethodTableHandle` for querying the `MethodTable`.

``` csharp
struct MethodTableHandle
{
    // no public properties or constructors

    internal TargetPointer Address { get; }
}
```

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
    public virtual ushort GetNumVirtuals(MethodTableHandle methodTable);
    public virtual ushort GetNumVtableSlots(MethodTableHandle methodTable);

    // Returns an ECMA-335 TypeDef table token for this type, or for its generic type definition if it is a generic instantiation
    public virtual uint GetTypeDefToken(MethodTableHandle methodTable);
    // Returns the ECMA 335 TypeDef table Flags value (a bitmask of TypeAttributes) for this type,
    // or for its generic type definition if it is a generic instantiation
    public virtual uint GetTypeDefTypeAttributes(MethodTableHandle methodTable);
    #endregion MethodTable inspection APIs
```

## Version 1

The `MethodTable` inspection APIs are implemented in terms of the following flags on the runtime `MethodTable` structure:

``` csharp
internal partial struct Metadata_1
{
    [Flags]
    internal enum WFLAGS_LOW : uint
    {
        GenericsMask = 0x00000030,
        GenericsMask_NonGeneric = 0x00000000,   // no instantiation

        StringArrayValues = GenericsMask_NonGeneric,
    }

    [Flags]
    internal enum WFLAGS_HIGH : uint
    {
        Category_Mask = 0x000F0000,
        Category_Array = 0x00080000,
        Category_Array_Mask = 0x000C0000,
        Category_Interface = 0x000C0000,
        ContainsPointers = 0x01000000, // Contains object references
        HasComponentSize = 0x80000000, // This is set if component size is used for flags.
    }

    [Flags]
    internal enum WFLAGS2_ENUM : uint
    {
        DynamicStatics = 0x0002,
    }

    // Encapsulates the MethodTable flags v1 uses
    internal struct MethodTableFlags
    {
        public WFLAGS_LOW GetFlag(WFLAGS_LOW mask) { ... }
        public WFLAGS_HIGH GetFlag(WFLAGS_HIGH mask) { ... }

        public WFLAGS2_ENUM GetFlag(WFLAGS2_ENUM mask) { ... }
        public bool IsInterface => GetFlag(WFLAGS_HIGH.Category_Mask) == WFLAGS_HIGH.Category_Interface;
        public bool IsString => HasComponentSize && !IsArray && RawGetComponentSize() == 2;

        public bool HasComponentSize => GetFlag(WFLAGS_HIGH.HasComponentSize) != 0;

        public bool IsArray => GetFlag(WFLAGS_HIGH.Category_Array_Mask) == WFLAGS_HIGH.Category_Array;

        public bool IsStringOrArray => HasComponentSize;
        public ushort RawGetComponentSize() => (ushort)(MTFlags >> 16);

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

        public bool HasInstantiation => !TestFlagWithMask(WFLAGS_LOW.GenericsMask, WFLAGS_LOW.GenericsMask_NonGeneric);

        public bool ContainsGCPointers => GetFlag(WFLAGS_HIGH.ContainsGCPointers) != 0;
    }

    [Flags]
    internal enum EEClassOrCanonMTBits
    {
        EEClass = 0,
        CanonMT = 1,
        Mask = 1,
    }
```

Internally the contract has a `MethodTable_1` struct that depends on the `MethodTable` data descriptor

```csharp
internal struct MethodTable_1
{
    internal Metadata_1.MethodTableFlags Flags { get; }
    internal ushort NumInterfaces { get; }
    internal ushort NumVirtuals { get; }
    internal TargetPointer ParentMethodTable { get; }
    internal TargetPointer Module { get; }
    internal TargetPointer EEClassOrCanonMT { get; }
    internal MethodTable_1(Data.MethodTable data)
    {
        Flags = new Metadata_1.MethodTableFlags
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
    }
}
```

The contract depends on the global pointer value `FreeObjectMethodTablePointer`.
The contract additionally depends on the `EEClass` data descriptor.

```csharp
    private readonly Dictionary<TargetPointer, MethodTable_1> _methodTables;

    internal TargetPointer FreeObjectMethodTablePointer {get; }

    public MethodTableHandle GetMethodTableHandle(TargetPointer methodTablePointer)
    {
        ...
    }

    internal static EEClassOrCanonMTBits GetEEClassOrCanonMTBits(TargetPointer eeClassOrCanonMTPtr)
    {
        return (EEClassOrCanonMTBits)(eeClassOrCanonMTPtr & (ulong)EEClassOrCanonMTBits.Mask);
    }

    public uint GetBaseSize(MethodTableHandle methodTableHandle) => _methodTables[methodTableHandle.Address].Flags.BaseSize;

    public uint GetComponentSize(MethodTableHandle methodTableHandle) => GetComponentSize(_methodTables[methodTableHandle.Address]);

    private Data.EEClass GetClassData(MethodTableHandle methodTableHandle)
    {
        ...
    }

    private TargetPointer GetClass(MethodTableHandle methodTableHandle)
    {
        ... // if the MethodTable stores a pointer to the EEClass, return it
            // otherwise the MethodTable stores a pointer to the canonical MethodTable
            // in that case, return the canonical MethodTable's EEClass.
            // Canonical MethodTables always store an EEClass pointer.
    }

    public TargetPointer GetCanonicalMethodTable(MethodTableHandle methodTableHandle) => GetClassData(methodTableHandle).MethodTable;

    public TargetPointer GetModule(MethodTableHandle methodTableHandle) => _methodTables[methodTableHandle.Address].Module;
    public TargetPointer GetParentMethodTable(MethodTableHandle methodTableHandle) => _methodTables[methodTableHandle.Address].ParentMethodTable;

    public bool IsFreeObjectMethodTable(MethodTableHandle methodTableHandle) => FreeObjectMethodTablePointer == methodTableHandle.Address;

    public bool IsString(MethodTableHandle methodTableHandle) => _methodTables[methodTableHandle.Address].Flags.IsString;
    public bool ContainsPointers(MethodTableHandle methodTableHandle) => _methodTables[methodTableHandle.Address].Flags.ContainsPointers;

    public uint GetTypeDefToken(MethodTableHandle methodTableHandle)
    {
        MethodTable_1 methodTable = _methodTables[methodTableHandle.Address];
        return (uint)(methodTable.Flags.GetTypeDefRid() | ((int)TableIndex.TypeDef << 24));
    }

    public ushort GetNumMethods(MethodTableHandle methodTableHandle)
    {
        EEClass_1 cls = GetClassData(methodTableHandle);
        return cls.NumMethods;
    }

    public ushort GetNumInterfaces(MethodTableHandle methodTableHandle) => _methodTables[methodTableHandle.Address].NumInterfaces;

    public ushort GetNumVirtuals(MethodTableHandle methodTableHandle) => _methodTables[methodTableHandle.Address].NumVirtuals;
    private ushort GetNumNonVirtualSlots(MethodTableHandle methodTableHandle)
    {
        MethodTable_1 methodTable = _methodTables[methodTableHandle.Address];
        TargetPointer eeClassOrCanonMT = methodTable.EEClassOrCanonMT;
        if (GetEEClassOrCanonMTBits(eeClassOrCanonMT) == EEClassOrCanonMTBits.EEClass)
        {
            return GetClassData(methodTableHandle).NumNonVirtualSlots;
        }
        else
        {
            return 0;
        }
    }

    public ushort GetNumVtableSlots(MethodTableHandle methodTableHandle)
    {
        return checked((ushort)(GetNumVirtuals(methodTableHandle) + GetNumNonVirtualSlots(methodTableHandle)));
    }

    public uint GetTypeDefTypeAttributes(MethodTableHandle methodTableHandle)
    {
        return GetClassData(methodTableHandle).AttrClass;
    }

    public bool IsDynamicStatics(MethodTableHandle methodTableHandle) => _methodTables[methodTableHandle.Address].Flags.GetFlag(WFLAGS2_ENUM.DynamicStatics) != 0;
```
