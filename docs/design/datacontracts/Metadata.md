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
    public virtual MethodTableHandle GetMethodTableHandle(TargetPointer targetPointer) => throw new NotImplementedException();

    public virtual TargetPointer GetModule(MethodTableHandle methodTable) => throw new NotImplementedException();
    // A canonical method table is either the MethodTable itself, or in the case of a generic instantiation, it is the
    // MethodTable of the prototypical instance.
    public virtual TargetPointer GetCanonicalMethodTable(MethodTableHandle methodTable) => throw new NotImplementedException();
    public virtual TargetPointer GetParentMethodTable(MethodTableHandle methodTable) => throw new NotImplementedException();

    public virtual uint GetBaseSize(MethodTableHandle methodTable) => throw new NotImplementedException();
    // The component size is only available for strings and arrays.  It is the size of the element type of the array, or the size of an ECMA 335 character (2 bytes)
    public virtual uint GetComponentSize(MethodTableHandle methodTable) => throw new NotImplementedException();

    // True if the MethodTable is the sentinel value associated with unallocated space in the managed heap
    public virtual bool IsFreeObjectMethodTable(MethodTableHandle methodTable) => throw new NotImplementedException();
    public virtual bool IsString(MethodTableHandle methodTable) => throw new NotImplementedException();
    // True if the MethodTable represents a type that contains managed references
    public virtual bool ContainsPointers(MethodTableHandle methodTable) => throw new NotImplementedException();
    public virtual bool IsDynamicStatics(MethodTableHandle methodTable) => throw new NotImplementedException();
    public virtual ushort GetNumMethods(MethodTableHandle methodTable) => throw new NotImplementedException();
    public virtual ushort GetNumInterfaces(MethodTableHandle methodTable) => throw new NotImplementedException();
    public virtual ushort GetNumVirtuals(MethodTableHandle methodTable) => throw new NotImplementedException();
    public virtual ushort GetNumVtableSlots(MethodTableHandle methodTable) => throw new NotImplementedException();

    // Returns an ECMA-335 TypeDef table token for this type, or for its generic type definition if it is a generic instantiation
    public virtual uint GetTypeDefToken(MethodTableHandle methodTable) => throw new NotImplementedException();
    // Returns the ECMA 335 TypeDef table Flags value (a bitmask of TypeAttributes) for this type,
    // or for its generic type definition if it is a generic instantiation
    public virtual uint GetTypeDefTypeAttributes(MethodTableHandle methodTable) => throw new NotImplementedException();
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

        StringArrayValues =
            GenericsMask_NonGeneric |
            0,
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

    internal struct MethodTableFlags
    {
        public uint DwFlags { get; init; }
        public uint DwFlags2 { get; init; }
        public uint BaseSize { get; init; }

        private WFLAGS_HIGH FlagsHigh => (WFLAGS_HIGH)DwFlags;
        private WFLAGS_LOW FlagsLow => (WFLAGS_LOW)DwFlags;
        public int GetTypeDefRid() => (int)(DwFlags2 >> Constants.MethodTableDwFlags2TypeDefRidShift);

        public WFLAGS_LOW GetFlag(WFLAGS_LOW mask) => throw new NotImplementedException("TODO");
        public WFLAGS_HIGH GetFlag(WFLAGS_HIGH mask) => FlagsHigh & mask;

        public WFLAGS2_ENUM GetFlag(WFLAGS2_ENUM mask) => (WFLAGS2_ENUM)DwFlags2 & mask;
        public bool IsInterface => GetFlag(WFLAGS_HIGH.Category_Mask) == WFLAGS_HIGH.Category_Interface;
        public bool IsString => HasComponentSize && !IsArray && RawGetComponentSize() == 2;

        public bool HasComponentSize => GetFlag(WFLAGS_HIGH.HasComponentSize) != 0;

        public bool IsArray => GetFlag(WFLAGS_HIGH.Category_Array_Mask) == WFLAGS_HIGH.Category_Array;

        public bool IsStringOrArray => HasComponentSize;
        public ushort RawGetComponentSize() => (ushort)(DwFlags >> 16);

        public bool TestFlagWithMask(WFLAGS_LOW mask, WFLAGS_LOW flag)
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

        public bool TestFlagWithMask(WFLAGS2_ENUM mask, WFLAGS2_ENUM flag)
        {
            return ((WFLAGS2_ENUM)DwFlags2 & mask) == flag;
        }

        public bool HasInstantiation => !TestFlagWithMask(WFLAGS_LOW.GenericsMask, WFLAGS_LOW.GenericsMask_NonGeneric);

        public bool ContainsPointers => GetFlag(WFLAGS_HIGH.ContainsPointers) != 0;
    }

    internal static class Constants
    {
        internal const int MethodTableDwFlags2TypeDefRidShift = 8;
    }

    [Flags]
    internal enum EEClassOrCanonMTBits
    {
        EEClass = 0,
        CanonMT = 1,
        Mask = 1,
    }
```

Internally the contract has structs `MethodTable_1` and `EEClass_1`

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
            DwFlags = data.DwFlags,
            DwFlags2 = data.DwFlags2,
            BaseSize = data.BaseSize,
        };
        NumInterfaces = data.NumInterfaces;
        NumVirtuals = data.NumVirtuals;
        EEClassOrCanonMT = data.EEClassOrCanonMT;
        Module = data.Module;
        ParentMethodTable = data.ParentMethodTable;
    }
}

internal struct EEClass_1
{
    internal TargetPointer MethodTable { get; }
    internal ushort NumMethods { get; }
    internal ushort NumNonVirtualSlots { get; }
    internal uint TypeDefTypeAttributes { get; }
    internal EEClass_1(Data.EEClass eeClassData)
    {
        MethodTable = eeClassData.MethodTable;
        NumMethods = eeClassData.NumMethods;
        NumNonVirtualSlots = eeClassData.NumNonVirtualSlots;
        TypeDefTypeAttributes = eeClassData.DwAttrClass;
    }
}
```

```csharp
    private readonly Dictionary<TargetPointer, MethodTable_1> _methodTables;
    private readonly TargetPointer _freeObjectMethodTablePointer;

    internal TargetPointer FreeObjectMethodTablePointer => _freeObjectMethodTablePointer;

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

    private EEClass_1 GetClassData(MethodTableHandle methodTableHandle)
    {
        TargetPointer clsPtr = GetClass(methodTableHandle);
        // Check if we cached it already
        if (_target.ProcessedData.TryGet(clsPtr, out Data.EEClass? eeClassData))
        {
            return new EEClass_1(eeClassData);
        }
        eeClassData = _target.ProcessedData.GetOrAdd<Data.EEClass>(clsPtr);
        return new EEClass_1(eeClassData);
    }

    private TargetPointer GetClass(MethodTableHandle methodTableHandle)
    {
        MethodTable_1 methodTable = _methodTables[methodTableHandle.Address];
        switch (GetEEClassOrCanonMTBits(methodTable.EEClassOrCanonMT))
        {
            case EEClassOrCanonMTBits.EEClass:
                return methodTable.EEClassOrCanonMT;
            case EEClassOrCanonMTBits.CanonMT:
                TargetPointer canonMTPtr = new TargetPointer((ulong)methodTable.EEClassOrCanonMT & ~(ulong)Metadata_1.EEClassOrCanonMTBits.Mask);
                MethodTableHandle canonMTHandle = GetMethodTableHandle(canonMTPtr);
                MethodTable_1 canonMT = _methodTables[canonMTHandle.Address];
                return canonMT.EEClassOrCanonMT; // canonical method table EEClassOrCanonMT is always EEClass
            default:
                throw new InvalidOperationException();
        }
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
        return GetClassData(methodTableHandle).TypeDefTypeAttributes;
    }

    public bool IsDynamicStatics(MethodTableHandle methodTableHandle) => _methodTables[methodTableHandle.Address].Flags.GetFlag(WFLAGS2_ENUM.DynamicStatics) != 0;
```
