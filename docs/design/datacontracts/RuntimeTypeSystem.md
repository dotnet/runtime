# Contract RuntimeTypeSystem

This contract is for exploring the properties of the runtime types of values on the managed heap or on the stack in a .NET process.

## APIs of contract

A `MethodTable` is the runtime representation of the type information about a value.  Given a `TargetPointer` address, the `RuntimeTypeSystem` contract provides a `MethodTableHandle` for querying the `MethodTable`.

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
internal partial struct RuntimeTypeSystem_1
{
    // The lower 16-bits of the MTFlags field are used for these flags,
    // if WFLAGS_HIGH.HasComponentSize is unset
    [Flags]
    internal enum WFLAGS_LOW : uint
    {
        GenericsMask = 0x00000030,
        GenericsMask_NonGeneric = 0x00000000,   // no instantiation

        StringArrayValues = GenericsMask_NonGeneric,
    }

    // Upper bits of MTFlags
    [Flags]
    internal enum WFLAGS_HIGH : uint
    {
        Category_Mask = 0x000F0000,
        Category_Array = 0x00080000,
        Category_Array_Mask = 0x000C0000,
        Category_Interface = 0x000C0000,
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
```
