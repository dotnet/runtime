# Contract RuntimeTypeSystem

This contract is for exploring the properties of the runtime types of values on the managed heap or on the stack in a .NET process.

## APIs of contract

### TypeHandle

A `TypeHandle` is the runtime representation of the type information about a value which is represented as a TypeHandle.
Given a `TargetPointer` address, the `RuntimeTypeSystem` contract provides a `TypeHandle` for querying the details of the `TypeHandle`.

``` csharp
struct TypeHandle
{
    // no public constructors

    public TargetPointer Address { get; }
    public bool IsNull => Address != 0;
}

internal enum CorElementType
{
    // Values defined in ECMA-335 - II.23.1.16 Element types used in signatures
    // +
    Internal = 0x21, // Indicates that the next pointer sized number of bytes is the address of a TypeHandle. Signatures that contain the Internal CorElementType cannot exist in metadata that is saved into a serialized format.
    CModInternal = 0x22, // Indicates that the next byte specifies if the modifier is required and the next pointer sized number of bytes after that is the address of a TypeHandle. Signatures that contain the CModInternal CorElementType cannot exist in metadata that is saved into a seralized format.
}
```

A `TypeHandle` is the runtime representation of the type information about a value.  This can be constructed from the address of a `TypeHandle` or a `MethodTable`.

``` csharp
partial interface IRuntimeTypeSystem : IContract
{
    #region TypeHandle inspection APIs
    public virtual TypeHandle GetTypeHandle(TargetPointer targetPointer);

    public virtual TargetPointer GetModule(TypeHandle typeHandle);
    // A canonical method table is either the MethodTable itself, or in the case of a generic instantiation, it is the
    // MethodTable of the prototypical instance.
    public virtual TargetPointer GetCanonicalMethodTable(TypeHandle typeHandle);
    public virtual TargetPointer GetParentMethodTable(TypeHandle typeHandle);

    public virtual TargetPointer GetMethodDescForSlot(TypeHandle typeHandle, ushort slot);
    public virtual IEnumerable<TargetPointer> GetIntroducedMethodDescs(TypeHandle methodTable);
    public virtual TargetCodePointer GetSlot(TypeHandle typeHandle, uint slot);

    public virtual uint GetBaseSize(TypeHandle typeHandle);
    // The component size is only available for strings and arrays.  It is the size of the element type of the array, or the size of an ECMA 335 character (2 bytes)
    public virtual uint GetComponentSize(TypeHandle typeHandle);

    // True if the MethodTable is the sentinel value associated with unallocated space in the managed heap
    public virtual bool IsFreeObjectMethodTable(TypeHandle typeHandle);
    public virtual bool IsString(TypeHandle typeHandle);
    // True if the MethodTable represents a type that contains managed references
    public virtual bool ContainsGCPointers(TypeHandle typeHandle);
    public virtual bool IsDynamicStatics(TypeHandle typeHandle);
    public virtual ushort GetNumInterfaces(TypeHandle typeHandle);

    // Returns an ECMA-335 TypeDef table token for this type, or for its generic type definition if it is a generic instantiation
    public virtual uint GetTypeDefToken(TypeHandle typeHandle);
    public virtual ushort GetNumVtableSlots(TypeHandle typeHandle);
    public virtual ushort GetNumMethods(TypeHandle typeHandle);
    // Returns the ECMA 335 TypeDef table Flags value (a bitmask of TypeAttributes) for this type,
    // or for its generic type definition if it is a generic instantiation
    public virtual uint GetTypeDefTypeAttributes(TypeHandle typeHandle);
    public ushort GetNumInstanceFields(TypeHandle typeHandle);
    public ushort GetNumStaticFields(TypeHandle typeHandle);
    public ushort GetNumThreadStaticFields(TypeHandle typeHandle);
    public TargetPointer GetGCThreadStaticsBasePointer(TypeHandle typeHandle, TargetPointer threadPtr);
    public TargetPointer GetNonGCThreadStaticsBasePointer(TypeHandle typeHandle, TargetPointer threadPtr);
    public TargetPointer GetFieldDescList(TypeHandle typeHandle);
    public TargetPointer GetGCStaticsBasePointer(TypeHandle typeHandle);
    public TargetPointer GetNonGCStaticsBasePointer(TypeHandle typeHandle);
    public virtual ReadOnlySpan<TypeHandle> GetInstantiation(TypeHandle typeHandle);
    public bool IsClassInited(TypeHandle typeHandle);
    public bool IsInitError(TypeHandle typeHandle);
    public virtual bool IsGenericTypeDefinition(TypeHandle typeHandle);

    public virtual bool IsCollectible(TypeHandle typeHandle);
    public virtual bool HasTypeParam(TypeHandle typeHandle);

    // Element type of the type. NOTE: this drops the CorElementType.GenericInst, and CorElementType.String is returned as CorElementType.Class.
    // NOTE: If this returns CorElementType.ValueType it may be a normal valuetype or a "NATIVE" valuetype used to represent an interop view of a structure
    // HasTypeParam will return true for cases where this is the interop view, and false for normal valuetypes.
    public virtual CorElementType GetSignatureCorElementType(TypeHandle typeHandle);

    // return true if the TypeHandle represents an array, and set the rank to either 0 (if the type is not an array), or the rank number if it is.
    public virtual bool IsArray(TypeHandle typeHandle, out uint rank);
    public virtual TypeHandle GetTypeParam(TypeHandle typeHandle);
    public virtual TypeHandle GetConstructedType(TypeHandle typeHandle, CorElementType corElementType, int rank, ImmutableArray<TypeHandle> typeArguments);
    public TypeHandle GetPrimitiveType(CorElementType typeCode);
    public virtual bool IsGenericVariable(TypeHandle typeHandle, out TargetPointer module, out uint token);
    public virtual bool IsFunctionPointer(TypeHandle typeHandle, out ReadOnlySpan<TypeHandle> retAndArgTypes, out byte callConv);
    public virtual bool IsPointer(TypeHandle typeHandle);
    public virtual TargetPointer GetLoaderModule(TypeHandle typeHandle);

    #endregion TypeHandle inspection APIs
}
```

### MethodDesc

A `MethodDesc` is the runtime representation of a managed method (either from IL, from reflection emit, or generated by the runtime).

```csharp
struct MethodDescHandle
{
    // no public properties or constructors

    internal TargetPointer Address { get; }
}
```

```csharp
public enum ArrayFunctionType
{
    Get = 0,
    Set = 1,
    Address = 2,
    Constructor = 3
}
```

```csharp
partial interface IRuntimeTypeSystem : IContract
{
    public virtual MethodDescHandle GetMethodDescHandle(TargetPointer methodDescPointer);

    public virtual TargetPointer GetMethodTable(MethodDescHandle methodDesc);

    // Return true for an uninstantiated generic method
    public virtual bool IsGenericMethodDefinition(MethodDescHandle methodDesc);

    public virtual ReadOnlySpan<TypeHandle> GetGenericMethodInstantiation(MethodDescHandle methodDesc);

    // Return mdTokenNil (0x06000000) if the method doesn't have a token, otherwise return the token of the method
    public virtual uint GetMethodToken(MethodDescHandle methodDesc);

    // Return true if a MethodDesc represents an array method
    // An array method is also a StoredSigMethodDesc
    public virtual bool IsArrayMethod(MethodDescHandle methodDesc, out ArrayFunctionType functionType);

    // Return true if a MethodDesc represents a method without metadata method, either an IL Stub dynamically
    // generated by the runtime, or a MethodDesc that describes a method represented by the System.Reflection.Emit.DynamicMethod class
    // Or something else similar.
    // A no metadata method is also a StoredSigMethodDesc
    public virtual bool IsNoMetadataMethod(MethodDescHandle methodDesc, out string methodName);

    // A StoredSigMethodDesc is a MethodDesc for which the signature isn't found in metadata.
    public virtual bool IsStoredSigMethodDesc(MethodDescHandle methodDesc, out ReadOnlySpan<byte> signature);

    // Return true for a MethodDesc that describes a method represented by the System.Reflection.Emit.DynamicMethod class
    // A DynamicMethod is also a StoredSigMethodDesc, and a NoMetadataMethod
    public virtual bool IsDynamicMethod(MethodDescHandle methodDesc);

    // Return true if a MethodDesc represents an IL Stub dynamically generated by the runtime
    // A IL Stub method is also a StoredSigMethodDesc, and a NoMetadataMethod
    public virtual bool IsILStub(MethodDescHandle methodDesc);

    // Return true if a MethodDesc is in a collectible module
    public virtual bool IsCollectibleMethod(MethodDescHandle methodDesc);

    // Return true if a MethodDesc supports mulitiple code versions
    public virtual bool IsVersionable(MethodDescHandle methodDesc);

    // Return a pointer to the IL versioning state of the MethodDesc
    public virtual TargetPointer GetMethodDescVersioningState(MethodDescHandle methodDesc);

    // Return the MethodTable slot number of the MethodDesc
    public virtual ushort GetSlotNumber(MethodDescHandle methodDesc);

    // Return true if the MethodDesc has space associated with it for storing a pointer to a code block
    public virtual bool HasNativeCodeSlot(MethodDescHandle methodDesc);

    // Return the address of the space that stores a pointer to a code block associated with the MethodDesc
    public virtual TargetPointer GetAddressOfNativeCodeSlot(MethodDescHandle methodDesc);

    // Get an instruction pointer that can be called to cause the MethodDesc to be executed
    // Requires the entry point to be stable
    public virtual TargetCodePointer GetNativeCode(MethodDescHandle methodDesc);

    // Get an instruction pointer that can be called to cause the MethodDesc to be executed
    public virtual TargetCodePointer GetMethodEntryPointIfExists(MethodDescHandle methodDesc);

    // Gets the GCStressCodeCopy pointer if available, otherwise returns TargetPointer.Null
    public virtual TargetPointer GetGCStressCodeCopy(MethodDescHandle methodDesc);
}
```

### FieldDesc
```csharp
TargetPointer GetMTOfEnclosingClass(TargetPointer fieldDescPointer);
uint GetFieldDescMemberDef(TargetPointer fieldDescPointer);
bool IsFieldDescThreadStatic(TargetPointer fieldDescPointer);
bool IsFieldDescStatic(TargetPointer fieldDescPointer);
uint GetFieldDescType(TargetPointer fieldDescPointer);
uint GetFieldDescOffset(TargetPointer fieldDescPointer, FieldDefinition fieldDef);
TargetPointer GetFieldDescNextField(TargetPointer fieldDescPointer);
```

## Version 1

### TypeHandle

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
        Category_Array_Mask = 0x000C0000,

        Category_IfArrayThenSzArray = 0x00020000,
        Category_Array = 0x00080000,
        Category_ValueType = 0x00040000,
        Category_Nullable = 0x00050000,
        Category_PrimitiveValueType = 0x00060000,
        Category_TruePrimitive = 0x00070000,
        Category_Interface = 0x000C0000,
        Collectible = 0x00200000,
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
        public bool IsCollectible => GetFlag(WFLAGS_HIGH.Collectible) != 0;
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

    // Low order bits of TypeHandle address.
    // If the low bits contain a 2, then it is a TypeDesc
    [Flags]
    internal enum TypeHandleBits
    {
        MethodTable = 0,
        TypeDesc = 2,
        ValidMask = 2,
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

Internally the contract uses extension methods on the `TypeHandle` api so that it can distinguish between `MethodTable` and `TypeDesc`
```csharp
static class RuntimeTypeSystem_1_Helpers
{
    public static bool IsTypeDesc(this TypeHandle type)
    {
        return type.Address != 0 && (type.Address & TypeHandleBits.ValidMask) == TypeHandleBits.TypeDesc;
    }

    public static bool IsMethodTable(this TypeHandle type)
    {
        return type.Address != 0 && (type.Address & TypeHandleBits.ValidMask) == TypeHandleBits.MethodTable;
    }

    public static TargetPointer TypeDescAddress(this TypeHandle type)
    {
        if (!type.IsTypeDesc())
            return 0;

        return (ulong)type.Address & ~(ulong)TypeHandleBits.ValidMask;
    }
}
```

The contract depends on the following globals

| Global name | Meaning |
| --- | --- |
| `FreeObjectMethodTablePointer` | A pointer to the address of a `MethodTable` used by the GC to indicate reclaimed memory
| `StaticsPointerMask` | For masking out a bit of DynamicStaticsInfo pointer fields

The contract additionally depends on these data descriptors

| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `MethodTable` | `MTFlags` | One of the flags fields on `MethodTable` |
| `MethodTable` | `MTFlags2` | One of the flags fields on `MethodTable` |
| `MethodTable` | `BaseSize` | BaseSize of a `MethodTable` |
| `MethodTable` | `EEClassOrCanonMT` | Path to both EEClass and canonical MethodTable of a MethodTable |
| `MethodTable` | `Module` | Module for `MethodTable` |
| `MethodTable` | `ParentMethodTable` | Parent type pointer of `MethodTable` |
| `MethodTable` | `NumInterfaces` | Number of interfaces of `MethodTable` |
| `MethodTable` | `NumVirtuals` | Number of virtual methods in `MethodTable` |
| `MethodTable` | `PerInstInfo` | Either the array element type, or pointer to generic information for `MethodTable` |
| `MethodTableAuxiliaryData` | `Flags` | Flags of `MethodTableAuxiliaryData` |
| `MethodTable` | `AuxiliaryData` | Pointer to the AuxiliaryData of a method table |
| `DynamicStaticsInfo` | `NonGCStatics` | Pointer to non-GC statics |
| `DynamicStaticsInfo` | `GCStatics` | Pointer to the GC statics |
| `DynamicStaticsInfo` | `Size` | Size of the data |
| `ThreadStaticsInfo` | `GCTlsIndex` | Pointer to GC thread local storage index |
| `ThreadStaticsInfo` | `NonGCTlsIndex` | Pointer to non-GC thread local storage index |
| `EEClass` | `InternalCorElementType` | An InternalCorElementType uses the enum values of a CorElementType to indicate some of the information about the type of the type which uses the EEClass In particular, all reference types are CorElementType.Class, Enums are the element type of their underlying type and ValueTypes which can exactly be represented as an element type are represented as such, all other values types are represented as CorElementType.ValueType. |
| `EEClass` | `MethodTable` | Pointer to the canonical MethodTable of this type |
| `EEClass` | `MethodDescChunk` | Pointer to the first MethodDescChunk of the EEClass |
| `EEClass` | `NumMethods` | Count of methods attached to the EEClass |
| `EEClass` | `NumNonVirtualSlots` | Count of non-virtual slots for the EEClass |
| `EEClass` | `CorTypeAttr` | Various flags |
| `EEClass` | `NumInstanceFields` | Count of instance fields of the EEClass |
| `EEClass` | `NumStaticFields` | Count of static fields of the EEClass |
| `EEClass` | `NumThreadStaticFields` | Count of threadstatic fields of the EEClass |
| `EEClass` | `FieldDescList` | A list of fields in the type |
| `ArrayClass` | `Rank` | Rank of the associated array MethodTable |
| `TypeDesc` | `TypeAndFlags` | The lower 8 bits are the CorElementType of the `TypeDesc`, the upper 24 bits are reserved for flags |
| `ParamTypeDesc` | `TypeArg` | Associated type argument |
| `TypeVarTypeDesc` | `Module` | Pointer to module which defines the type variable |
| `TypeVarTypeDesc` | `Token` | Token of the type variable |
| `FnPtrTypeDesc` | `NumArgs` | Number of arguments to the function described by the `TypeDesc` |
| `FnPtrTypeDesc` | `CallConv` | Lower 8 bits is the calling convention bit as extracted by the signature that defines this `TypeDesc` |
| `FnPtrTypeDesc` | `RetAndArgTypes` | Pointer to an array of TypeHandle addresses. This length of this is 1 more than `NumArgs` |
| `GenericsDictInfo` | `NumDicts` | Number of instantiation dictionaries, including inherited ones, in this `GenericsDictInfo` |
| `GenericsDictInfo` | `NumTypeArgs` | Number of type arguments in the type or method instantiation described by this `GenericsDictInfo` |

Contracts used:
| Contract Name |
| --- |
| `Thread` |


```csharp
    private readonly Dictionary<TargetPointer, MethodTable_1> _methodTables;

    internal TargetPointer FreeObjectMethodTablePointer {get; }

    public TypeHandle GetTypeHandle(TargetPointer typeHandlePointer)
    {
        ... // validate that typeHandlePointer points to something that looks like a MethodTable or a TypeDesc.
        ... // If this is a MethodTable
        ... //     read Data.MethodTable from typeHandlePointer.
        ... //     create a MethodTable_1 and add it to _methodTables.
        return TypeHandle { Address = typeHandlePointer }
    }

    public TargetPointer GetModule(TypeHandle TypeHandle)
    {
        if (typeHandle.IsMethodTable())
        {
            return _methodTables[TypeHandle.Address].Module;
        }
        else if (typeHandle.IsTypeDesc())
        {
            if (HasTypeParam(typeHandle))
            {
                return GetModule(GetTypeParam(typeHandle));
            }
            else if (IsGenericVariable(typeHandle, out TargetPointer genericParamModule, out _))
            {
                return genericParamModule;
            }
        }
        return TargetPointer.Null;
    }

    internal static EEClassOrCanonMTBits GetEEClassOrCanonMTBits(TargetPointer eeClassOrCanonMTPtr)
    {
        return (EEClassOrCanonMTBits)(eeClassOrCanonMTPtr & (ulong)EEClassOrCanonMTBits.Mask);
    }

    public TargetPointer GetCanonicalMethodTable(TypeHandle TypeHandle) => !typeHandle.IsMethodTable() ? TargetPointer.Null : GetClassData(TypeHandle).MethodTable;

    public TargetPointer GetParentMethodTable(TypeHandle TypeHandle) => !typeHandle.IsMethodTable() ? TargetPointer.Null : _methodTables[TypeHandle.Address].ParentMethodTable;

    public uint GetBaseSize(TypeHandle TypeHandle) => !typeHandle.IsMethodTable() ? (uint)0 : _methodTables[TypeHandle.Address].Flags.BaseSize;

    public uint GetComponentSize(TypeHandle TypeHandle) =>!typeHandle.IsMethodTable() ? (uint)0 :  GetComponentSize(_methodTables[TypeHandle.Address]);

    private TargetPointer GetClassPointer(TypeHandle TypeHandle)
    {
        ... // if the MethodTable stores a pointer to the EEClass, return it
            // otherwise the MethodTable stores a pointer to the canonical MethodTable
            // in that case, return the canonical MethodTable's EEClass.
            // Canonical MethodTables always store an EEClass pointer.
    }

    private Data.EEClass GetClassData(TypeHandle TypeHandle)
    {
        TargetPointer eeClassPtr = GetClassPointer(TypeHandle);
        ... // read Data.EEClass data from eeClassPtr
    }

    public bool IsFreeObjectMethodTable(TypeHandle TypeHandle) => FreeObjectMethodTablePointer == TypeHandle.Address;

    public bool IsString(TypeHandle TypeHandle) => !typeHandle.IsMethodTable() ? false : _methodTables[TypeHandle.Address].Flags.IsString;

    public bool ContainsGCPointers(TypeHandle TypeHandle) => !typeHandle.IsMethodTable() ? false : _methodTables[TypeHandle.Address].Flags.ContainsGCPointers;

    public bool IsDynamicStatics(TypeHandle TypeHandle) => !typeHandle.IsMethodTable() ? false : _methodTables[TypeHandle.Address].Flags.IsDynamicStatics;

    public ushort GetNumInterfaces(TypeHandle TypeHandle) => !typeHandle.IsMethodTable() ? 0 : _methodTables[TypeHandle.Address].NumInterfaces;

    public uint GetTypeDefToken(TypeHandle TypeHandle)
    {
        if (!typeHandle.IsMethodTable())
            return 0;

        MethodTable_1 typeHandle = _methodTables[TypeHandle.Address];
        return (uint)(typeHandle.Flags.GetTypeDefRid() | ((int)TableIndex.TypeDef << 24));
    }

    public ushort GetNumVtableSlots(TypeHandle typeHandle)
    {
        if (!typeHandle.IsMethodTable())
            return 0;
        MethodTable methodTable = _methodTables[typeHandle.Address];
        ushort numNonVirtualSlots = methodTable.IsCanonMT ? GetClassData(typeHandle).NumNonVirtualSlots : (ushort)0;
        return checked((ushort)(methodTable.NumVirtuals + numNonVirtualSlots));
    }

    public ushort GetNumMethods(TypeHandle TypeHandle) => !typeHandle.IsMethodTable() ? 0 : GetClassData(TypeHandle).NumMethods;

    public uint GetTypeDefTypeAttributes(TypeHandle TypeHandle) => !typeHandle.IsMethodTable() ? 0 : GetClassData(TypeHandle).CorTypeAttr;

    public ushort GetNumInstanceFields(TypeHandle typeHandle) => !typeHandle.IsMethodTable() ? (ushort)0 : GetClassData(typeHandle).NumInstanceFields;

    public ushort GetNumStaticFields(TypeHandle typeHandle) => !typeHandle.IsMethodTable() ? (ushort)0 : GetClassData(typeHandle).NumStaticFields;

    public ushort GetNumThreadStaticFields(TypeHandle typeHandle) => !typeHandle.IsMethodTable() ? (ushort)0 : GetClassData(typeHandle).NumThreadStaticFields;

    public TargetPointer GetFieldDescList(TypeHandle typeHandle) => !typeHandle.IsMethodTable() ? TargetPointer.Null : GetClassData(typeHandle).FieldDescList;

    public TargetPointer GetGCStaticsBasePointer(TypeHandle typeHandle)
    {
        if (!typeHandle.IsMethodTable())
            return TargetPointer.Null;

        MethodTable methodTable = _methodTables[typeHandle.Address];
        if (!methodTable.Flags.IsDynamicStatics)
            return TargetPointer.Null;
        TargetPointer dynamicStaticsInfoSize = target.GetTypeInfo(DataType.DynamicStaticsInfo).Size!.Value;
        TargetPointer mask = target.ReadGlobalPointer("StaticsPointerMask");

        TargetPointer dynamicStaticsInfo = methodTable.AuxiliaryData - dynamicStaticsInfoSize;
        return (target.ReadPointer(dynamicStaticsInfo + /* DynamicStaticsInfo::GCStatics offset */) & (ulong)mask);
    }

    public TargetPointer GetNonGCStaticsBasePointer(TypeHandle typeHandle)
    {
        if (!typeHandle.IsMethodTable())
            return TargetPointer.Null;

        MethodTable methodTable = _methodTables[typeHandle.Address];
        if (!methodTable.Flags.IsDynamicStatics)
            return TargetPointer.Null;
        TargetPointer dynamicStaticsInfoSize = target.GetTypeInfo(DataType.DynamicStaticsInfo).Size!.Value;
        TargetPointer mask = target.ReadGlobalPointer("StaticsPointerMask");

        TargetPointer dynamicStaticsInfo = methodTable.AuxiliaryData - dynamicStaticsInfoSize;
        return (target.ReadPointer(dynamicStaticsInfo + /* DynamicStaticsInfo::NonGCStatics offset */) & (ulong)mask);
    }

    public TargetPointer GetGCThreadStaticsBasePointer(TypeHandle typeHandle, TargetPointer threadPtr)
    {
        if (!typeHandle.IsMethodTable())
            return TargetPointer.Null;
        MethodTable_1 methodTable = _methodTables[typeHandle.Address];
        TargetPointer threadStaticsInfoSize = target.GetTypeInfo(DataType.ThreadStaticsInfo).Size;
        TargetPointer threadStaticsInfoAddr = methodTable.AuxiliaryData - threadStaticsInfoSize;

        TargetPointer tlsIndexAddr = threadStaticsInfoAddr + /* ThreadStaticsInfo::GCTlsIndex offset */;
        Contracts.IThread threadContract = target.Contracts.Thread;
        return threadContract.GetThreadLocalStaticBase(threadPtr, tlsIndexAddr);
    }

    public TargetPointer GetNonGCThreadStaticsBasePointer(TypeHandle typeHandle, TargetPointer threadPtr)
    {
        if (!typeHandle.IsMethodTable())
            return TargetPointer.Null;
        MethodTable_1 methodTable = _methodTables[typeHandle.Address];
        TargetPointer threadStaticsInfoSize = target.GetTypeInfo(DataType.ThreadStaticsInfo).Size;
        TargetPointer threadStaticsInfoAddr = methodTable.AuxiliaryData - threadStaticsInfoSize;

        TargetPointer tlsIndexAddr = threadStaticsInfoAddr + /* ThreadStaticsInfo::NonGCTlsIndex offset */;
        Contracts.IThread threadContract = target.Contracts.Thread;
        return threadContract.GetThreadLocalStaticBase(threadPtr, tlsIndexAddr);
    }

    public ReadOnlySpan<TypeHandle> GetInstantiation(TypeHandle TypeHandle)
    {
        if (!typeHandle.IsMethodTable())
            return default;

        MethodTable_1 typeHandle = _methodTables[TypeHandle.Address];
        if (!typeHandle.Flags.HasInstantiation)
            return default;

        TargetPointer perInstInfo = typeHandle.PerInstInfo;
        TargetPointer genericsDictInfo = perInstInfo - (ulong)_target.PointerSize;
        TargetPointer dictionaryPointer = _target.ReadPointer(perInstInfo);

        int NumTypeArgs = // Read NumTypeArgs from genericsDictInfo using GenericsDictInfo contract
        TypeHandle[] instantiation = new TypeHandle[NumTypeArgs];
        for (int i = 0; i < NumTypeArgs; i++)
            instantiation[i] = GetTypeHandle(_target.ReadPointer(dictionaryPointer + _target.PointerSize * i));

        return instantiation;
    }

    public bool IsClassInited(TypeHandle typeHandle)
    {
        if (!typeHandle.IsMethodTable())
            return false;
        TargetPointer auxiliaryDataPtr = target.ReadPointer(typeHandle.Address + /* MethodTable.AuxiliaryData offset */);
        TargetPointer flagsPtr = target.ReadPointer(auxiliaryDataPtr + /* MethodTableAuxiliaryData::Flags offset */);
        uint flags = target.Read<uint>(flagsPtr);
        return (flags & (uint)MethodTableAuxiliaryFlags.Initialized) != 0;
    }

    public bool IsInitError(TypeHandle typeHandle)
    {
        if (!typeHandle.IsMethodTable())
            return false;
        TargetPointer auxiliaryDataPtr = target.ReadPointer(typeHandle.Address + /* MethodTable.AuxiliaryData offset */);
        TargetPointer flagsPtr = target.ReadPointer(auxiliaryDataPtr + /* MethodTableAuxiliaryData::Flags offset */);
        uint flags = target.Read<uint>(flagsPtr);
        return (flags & (uint)MethodTableAuxiliaryFlags.IsInitError) != 0;
    }

    public bool IsDynamicStatics(TypeHandle TypeHandle) => !typeHandle.IsMethodTable() ? false : _methodTables[TypeHandle.Address].Flags.IsDynamicStatics;

    public bool IsCollectible(TypeHandle typeHandle)
    {
        if (!typeHandle.IsMethodTable())
            return false;
        MethodTable typeHandle = _methodTables[typeHandle.Address];
        return typeHandle.Flags.IsCollectible;
    }

    public bool HasTypeParam(TypeHandle typeHandle)
    {
        if (typeHandle.IsMethodTable())
        {
            MethodTable typeHandle = _methodTables[typeHandle.Address];
            return typeHandle.Flags.IsArray;
        }
        else if (typeHandle.IsTypeDesc())
        {
            int TypeAndFlags = // Read TypeAndFlags field from TypeDesc contract using address typeHandle.TypeDescAddress()
            CorElementType elemType = (CorElementType)(TypeAndFlags & 0xFF);
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
        if (typeHandle.IsMethodTable())
        {
            MethodTable typeHandle = _methodTables[typeHandle.Address];

            switch (typeHandle.Flags.GetFlag(WFLAGS_HIGH.Category_Mask))
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
                    return (CorElementType)GetClassData(typeHandle).InternalCorElementType;
                default:
                    return CorElementType.Class;
            }
        }
        else if (typeHandle.IsTypeDesc())
        {
            int TypeAndFlags = // Read TypeAndFlags field from TypeDesc contract using address typeHandle.TypeDescAddress()
            return (CorElementType)(TypeAndFlags & 0xFF);
        }
        return default(CorElementType);
    }

    // return true if the TypeHandle represents an array, and set the rank to either 0 (if the type is not an array), or the rank number if it is.
    public bool IsArray(TypeHandle typeHandle, out uint rank)
    {
        if (typeHandle.IsMethodTable())
        {
            MethodTable typeHandle = _methodTables[typeHandle.Address];

            switch (typeHandle.Flags.GetFlag(WFLAGS_HIGH.Category_Mask))
            {
                case WFLAGS_HIGH.Category_Array:
                    TargetPointer clsPtr = GetClassPointer(typeHandle);
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
        if (typeHandle.IsMethodTable())
        {
            MethodTable typeHandle = _methodTables[typeHandle.Address];

            // Validate that this is an array
            if (!typeHandle.Flags.IsArray)
                throw new ArgumentException(nameof(typeHandle));

            return GetTypeHandle(typeHandle.PerInstInfo);
        }
        else if (typeHandle.IsTypeDesc())
        {
            int TypeAndFlags = // Read TypeAndFlags field from TypeDesc contract using address typeHandle.TypeDescAddress()
            CorElementType elemType = (CorElementType)(typeDesc.TypeAndFlags & 0xFF);

            switch (elemType)
            {
                case CorElementType.ValueType:
                case CorElementType.Byref:
                case CorElementType.Ptr:
                    TargetPointer typeArgPointer = // Read TypeArg field from ParamTypeDesc contract using address typeHandle.TypeDescAddress()
                    return GetTypeHandle(typeArgPointer);
            }
        }
        throw new ArgumentException(nameof(typeHandle));
    }

    // helper functions

    private bool GenericInstantiationMatch(TypeHandle genericType, TypeHandle potentialMatch, ImmutableArray<TypeHandle> typeArguments)
    {
        ReadOnlySpan<TypeHandle> instantiation = GetInstantiation(potentialMatch);
        if (instantiation.Length != typeArguments.Length)
            return false;

        if (GetTypeDefToken(genericType) != GetTypeDefToken(potentialMatch))
            return false;

        if (GetModule(genericType) != GetModule(potentialMatch))
            return false;

        for (int i = 0; i < instantiation.Length; i++)
        {
            if (!(instantiation[i].Address == typeArguments[i].Address))
                return false;
        }
        return true;
    }

    private bool ArrayPtrMatch(TypeHandle elementType, CorElementType corElementType, int rank, TypeHandle potentialMatch)
    {
        IsArray(potentialMatch, out uint typeHandleRank);
        return GetSignatureCorElementType(potentialMatch) == corElementType &&
                GetTypeParam(potentialMatch).Address == elementType.Address &&
                (corElementType == CorElementType.SzArray || corElementType == CorElementType.Byref ||
                corElementType == CorElementType.Ptr || (rank == typeHandleRank));

    }

    public TypeHandle GetConstructedType(TypeHandle typeHandle, CorElementType corElementType, int rank, ImmutableArray<TypeHandle> typeArguments)
    {
        if (typeHandle.Address == TargetPointer.Null)
            return new TypeHandle(TargetPointer.Null);
        ILoader loaderContract = _target.Contracts.Loader;
        TargetPointer loaderModule = GetLoaderModule(typeHandle);
        ModuleHandle moduleHandle = loaderContract.GetModuleHandleFromModulePtr(loaderModule);
        foreach (TargetPointer ptr in loaderContract.GetAvailableTypeParams(moduleHandle))
        {
            TypeHandle potentialMatch = GetTypeHandle(ptr);
            if (corElementType == CorElementType.GenericInst)
            {
                if (GenericInstantiationMatch(typeHandle, potentialMatch, typeArguments))
                {
                    return potentialMatch;
                }
            }
            else if (ArrayPtrMatch(typeHandle, corElementType, rank, potentialMatch))
            {
                return potentialMatch;
            }
        }
        return new TypeHandle(TargetPointer.Null);
    }

    public TypeHandle GetPrimitiveType(CorElementType typeCode)
    {
        TargetPointer coreLib = _target.ReadGlobalPointer("CoreLib");
        TargetPointer classes = _target.ReadPointer(coreLib + /* CoreLibBinder::Classes offset */);
        TargetPointer typeHandlePtr = _target.ReadPointer(classes + (ulong)typeCode * (ulong)_target.PointerSize);
        return GetTypeHandle(typeHandlePtr);
    }

    public bool IsGenericVariable(TypeHandle typeHandle, out TargetPointer module, out uint token)
    {
        module = TargetPointer.Null;
        token = 0;

        if (!typeHandle.IsTypeDesc())
            return false;

        int TypeAndFlags = // Read TypeAndFlags field from TypeDesc contract using address typeHandle.TypeDescAddress()
        CorElementType elemType = (CorElementType)(typeDesc.TypeAndFlags & 0xFF);

        switch (elemType)
        {
            case CorElementType.MVar:
            case CorElementType.Var:
                module = // Read Module field from TypeVarTypeDesc contract using address typeHandle.TypeDescAddress()
                token = // Read Module field from TypeVarTypeDesc contract using address typeHandle.TypeDescAddress()
                return true;
        }
        return false;
    }

    public bool IsFunctionPointer(TypeHandle typeHandle, out ReadOnlySpan<TypeHandle> retAndArgTypes, out byte callConv)
    {
        retAndArgTypes = default;
        callConv = default;

        if (!typeHandle.IsTypeDesc())
            return false;

        int TypeAndFlags = // Read TypeAndFlags field from TypeDesc contract using address typeHandle.TypeDescAddress()
        CorElementType elemType = (CorElementType)(TypeAndFlags & 0xFF);

        if (elemType != CorElementType.FnPtr)
            return false;

        int NumArgs = // Read NumArgs field from FnPtrTypeDesc contract using address typeHandle.TypeDescAddress()
        TargetPointer RetAndArgTypes = // Read NumArgs field from FnPtrTypeDesc contract using address typeHandle.TypeDescAddress()

        TypeHandle[] retAndArgTypesArray = new TypeHandle[NumTypeArgs + 1];
        for (int i = 0; i <= NumTypeArgs; i++)
            retAndArgTypesArray[i] = GetTypeHandle(_target.ReadPointer(RetAndArgTypes + _target.PointerSize * i));

        retAndArgTypes = retAndArgTypesArray;
        callConv = (byte) // Read CallConv field from FnPtrTypeDesc contract using address typeHandle.TypeDescAddress(), and then ignore all but the low 8 bits.
        return true;
    }

    public bool IsPointer(TypeHandle typeHandle)
    {
        if (!typeHandle.IsTypeDesc())
            return false;

        int TypeAndFlags = // Read TypeAndFlags field from TypeDesc contract using address typeHandle.TypeDescAddress()
        CorElementType elemType = (CorElementType)(TypeAndFlags & 0xFF);
        return elemType == CorElementType.Ptr;
    }

    public TargetPointer GetLoaderModule(TypeHandle typeHandle)
    {
        if (typeHandle.IsTypeDesc())
        {
            // TypeDesc::GetLoaderModule
            if (HasTypeParam(typeHandle))
            {
                return GetLoaderModule(GetTypeParam(typeHandle));
            }
            else if (IsGenericVariable(typeHandle, out TargetPointer genericParamModule, out _))
            {
                return genericParamModule;
            }
            else
            {
                Debug.Assert(IsFunctionPointer(typeHandle, out _, out _));
                return target.ReadPointer(typeHandle.TypeDescAddress() + /* FnPtrTypeDesc::LoaderModule offset */);
            }
        }
        return target.ReadPointer(mt.AuxiliaryData + /* MethodTableAuxiliaryData::LoaderModule offset */);
    }
```

### MethodDesc

The version 1 `MethodDesc` APIs depend on the following globals:

| Global name | Meaning |
| --- | --- |
| `MethodDescAlignment` | `MethodDescChunk` trailing data is allocated in multiples of this constant.  The size (in bytes) of each `MethodDesc` (or subclass) instance is a multiple of this constant. |
| `MethodDescTokenRemainderBitCount` | Number of bits in the token remainder in `MethodDesc` |
| `MethodDescSizeTable` | A pointer to the MethodDesc size table. The MethodDesc flags are used as an offset into this table to lookup the MethodDesc size. |


In the runtime a `MethodDesc` implicitly belongs to a single `MethodDescChunk` and some common data is shared between method descriptors that belong to the same chunk.  A single method table
will typically have multiple chunks.  There are subkinds of MethodDescs at runtime of varying sizes (but the sizes must be mutliples of `MethodDescAlignment`) and each chunk contains method descriptors of the same size.

We depend on the following data descriptors:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `MethodDesc` | `ChunkIndex` | Offset of this `MethodDesc` relative to the end of its containing `MethodDescChunk` - in multiples of `MethodDescAlignment` |
| `MethodDesc` | `Slot` | The method's slot |
| `MethodDesc` | `Flags` | The method's flags |
| `MethodDesc` | `Flags3AndTokenRemainder` | More flags for the method, and the low bits of the method's token's RID |
| `MethodDesc` | `GCCoverageInfo` | The method's GCCover debug info, if supported |
| `MethodDescCodeData` | `VersioningState` | The IL versioning state associated with a method descriptor
| `MethodDescChunk` | `MethodTable` | The method table set of methods belongs to |
| `MethodDescChunk` | `Next` | The next chunk of methods |
| `MethodDescChunk` | `Size` | The size of this `MethodDescChunk`  following this `MethodDescChunk` header, minus 1. In multiples of `MethodDescAlignment` |
| `MethodDescChunk` | `Count` | The number of `MethodDesc` entries in this chunk, minus 1. |
| `MethodDescChunk` | `FlagsAndTokenRange` | `MethodDescChunk` flags, and the upper bits of the method token's RID |
| `MethodTable` | `AuxiliaryData` | Auxiliary data associated with the method table. See `MethodTableAuxiliaryData` |
| `MethodTableAuxiliaryData` | `LoaderModule` | The loader module associated with a method table
| `MethodTableAuxiliaryData` | `OffsetToNonVirtualSlots` | Offset from the auxiliary data address to the array of non-virtual slots |
| `InstantiatedMethodDesc` | `PerInstInfo` | The pointer to the method's type arguments |
| `InstantiatedMethodDesc` | `Flags2` | Flags for the `InstantiatedMethodDesc` |
| `InstantiatedMethodDesc` | `NumGenericArgs` | How many generic args the method has |
| `StoredSigMethodDesc` | `Sig` | Pointer to a metadata signature |
| `StoredSigMethodDesc` | `cSig` | Count of bytes in the metadata signature |
| `StoredSigMethodDesc` | `ExtendedFlags` | Flags field for the `StoredSigMethodDesc` |
| `DynamicMethodDesc` | `MethodName` | Pointer to Null-terminated UTF8 string describing the Method desc |
| `GCCoverageInfo` | `SavedCode` | Pointer to the GCCover saved code copy, if supported |


The contract depends on the following other contracts

| Contract |
| --- |
| CodeVersions |
| Loader |
| PlatformMetadata |
| ReJIT |
| ExecutionManager |
| PrecodeStubs |

And the following enumeration definitions

```csharp
    internal enum MethodDescClassification
    {
        IL = 0, // IL
        FCall = 1, // FCall (also includes tlbimped ctor, Delegate ctor)
        PInvoke = 2, // PInvoke method
        EEImpl = 3, // special method; implementation provided by EE (like Delegate Invoke)
        Array = 4, // Array ECall
        Instantiated = 5, // Instantiated generic methods, including descriptors
                            // for both shared and unshared code (see InstantiatedMethodDesc)
        ComInterop = 6,
        Dynamic = 7, // for method desc with no metadata behind
    }

    [Flags]
    internal enum MethodDescFlags : ushort
    {
        ClassificationMask = 0x7,
        #region Additonal pointers
        // The below flags each imply that there's an extra pointer-sized piece of data after the MethodDesc in the MethodDescChunk
        HasNonVtableSlot = 0x0008,
        HasMethodImpl = 0x0010,
        HasNativeCodeSlot = 0x0020,
        HasAsyncMethodData = 0x0040,
        // Mask for the above flags
        MethodDescAdditionalPointersMask = 0x0038,
        #endredion Additional pointers
    }

    internal enum InstantiatedMethodDescFlags2 : ushort
    {
        KindMask = 0x07,
        GenericMethodDefinition = 0x01,
        UnsharedMethodInstantiation = 0x02,
        SharedMethodInstantiation = 0x03,
        WrapperStubWithInstantiations = 0x04,
    }

    [Flags]
    internal enum DynamicMethodDescExtendedFlags : uint
    {
        IsLCGMethod = 0x00004000,
        IsILStub = 0x00008000,
    }

    [Flags]
    internal enum MethodDescFlags3 : ushort
    {
        // HasPrecode implies that HasStableEntryPoint is set.
        HasStableEntryPoint = 0x1000, // The method entrypoint is stable (either precode or actual code)
        HasPrecode = 0x2000, // Precode has been allocated for this method
        IsUnboxingStub = 0x4000,
        IsEligibleForTieredCompilation = 0x8000,
    }

    [Flags]
    internal enum MethodDescEntryPointFlags : byte
    {
        TemporaryEntryPointAssigned = 0x04,
    }

    internal enum MethodTableAuxiliaryFlags : uint
    {
        Initialized = 0x0001,
        IsInitError = 0x0100,
    }

```

Internal to the contract in order to answer queries about method descriptors,
we collect the information in a `MethodDesc` struct:

```csharp
internal struct MethodDesc
{
    private readonly Data.MethodDesc _desc;
    private readonly Data.MethodDescChunk _chunk;
    private readonly Target _target;
    internal TargetPointer Address { get; init; }
    internal TargetPointer ChunkAddress { get; init; }

    internal MethodDesc(Target target, TargetPointer methodDescPointer, Data.MethodDesc desc, TargetPointer methodDescChunkAddress, Data.MethodDescChunk chunk)
    {
        _target = target;
        _desc = desc;
        _chunk = chunk;
        ChunkAddress = methodDescChunkAddress;
        Address = methodDescPointer;
    }

    public MethodClassification Classification => (MethodClassification)(_desc.Flags & MethodDescFlags.ClassificationMask);
    public bool IsIL => Classification == MethodClassification.IL || Classification == MethodClassification.Instantiated;

    public TargetPointer MethodTable => _chunk.MethodTable;

    public ushort Slot => _desc.Slot;


    internal bool HasFlags(MethodDescChunkFlags flags) => (_chunk.FlagsAndTokenRange & (ushort)flags) != 0;
    internal bool HasFlags(MethodDescFlags flags) => (_desc.Flags & (ushort)flags) != 0;
    internal bool HasFlags(MethodDescFlags3 flags) => (_desc.Flags3AndTokenRemainder & (ushort)flags) != 0;

    public bool IsLoaderModuleAttachedToChunk => HasFlags(MethodDescChunkFlags.LoaderModuleAttachedToChunk);

    public ulong SizeOfChunk
    {
        get
        {
            ulong typeSize = _target.GetTypeInfo(DataType.MethodDescChunk).Size;
            ulong chunkSize = (ulong)(_chunk.Size + 1) * _target.ReadGlobal<ulong>("MethodDescAlignment");
            ulong extra = IsLoaderModuleAttachedToChunk ? (ulong)_target.PointerSize : 0;
            return typeSize + chunkSize + extra;
        }
    }

    public bool IsEligibleForTieredCompilation => HasFlags(MethodDescFlags3.IsEligibleForTieredCompilation);

    // non-vtable slot, native code slot and MethodImpl slots are stored after the MethodDesc itself, packed tightly
    // in the order: [non-vtable; methhod impl; native code].
    internal int NonVtableSlotIndex => HasNonVtableSlot ? 0 : throw new InvalidOperationException();
    internal int MethodImplIndex => HasMethodImpl ? /* 0 or 1 */ : throw new InvalidOperationException();
    internal int NativeCodeSlotIndex => HasNativeCodeSlot ? /* 0, 1 or 2 */ : throw new InvalidOperationException();

    internal bool HasNativeCodeSlot => HasFlags(MethodDescFlags.HasNativeCodeSlot);
    internal bool HasNonVtableSlot => HasFlags(MethodDescFlags.HasNonVtableSlot);
    internal bool HasMethodImpl => HasFlags(MethodDescFlags.HasMethodImpl);

    internal bool HasStableEntryPoint => HasFlags(MethodDescFlags3.HasStableEntryPoint);
    internal bool HasPrecode => HasFlags(MethodDescFlags3.HasPrecode);

}
```

Method descriptor handles are instantiated by caching the relevant data in a `_methodDescs` dictionary:

```csharp
    public MethodDescHandle GetMethodDescHandle(TargetPointer methodDescPointer)
    {
        // Validate that methodDescPointer points at a MethodDesc
        // Get the corresponding MethodDescChunk pointer
        // Load the relevant Data.MethodDesc and Data.MethodDescChunk structures
        // and caching the results in _methodDescs
        return new MethodDescHandle() { Address = methodDescPointer };
    }
```

And the various apis are implemented with the following algorithms

```csharp
    public TargetPointer GetMethodTable(MethodDescHandle methodDescHandle)
    {
        return _methodDescs[methodDescHandle.Address].MethodTable;
    }

    public bool IsGenericMethodDefinition(MethodDescHandle methodDescHandle)
    {
        MethodDesc methodDesc = _methodDescs[methodDescHandle.Address];

        if (methodDesc.Classification != MethodDescClassification.Instantiated)
            return false;

        ushort Flags2 = // Read Flags2 field from InstantiatedMethodDesc contract using address methodDescHandle.Address

        return ((int)Flags2 & (int)InstantiatedMethodDescFlags2.KindMask) == (int)InstantiatedMethodDescFlags2.GenericMethodDefinition;
    }

    public ReadOnlySpan<TypeHandle> GetGenericMethodInstantiation(MethodDescHandle methodDescHandle)
    {
        MethodDesc methodDesc = _methodDescs[methodDescHandle.Address];

        if (methodDesc.Classification != MethodDescClassification.Instantiated)
            return default;

        TargetPointer dictionaryPointer = // Read PerInstInfo field from InstantiatedMethodDesc contract using address methodDescHandle.Address
        if (dictionaryPointer == 0)
            return default;

        int NumTypeArgs = // Read NumGenericArgs from methodDescHandle.Address using InstantiatedMethodDesc contract
        TypeHandle[] instantiation = new TypeHandle[NumTypeArgs];
        for (int i = 0; i < NumTypeArgs; i++)
            instantiation[i] = GetTypeHandle(_target.ReadPointer(dictionaryPointer + _target.PointerSize * i));

        return instantiation;
    }

    public uint GetMethodToken(MethodDescHandle methodDescHandle)
    {
        MethodDesc methodDesc = _methodDescs[methodDescHandle.Address];

        TargetPointer methodDescChunk = // Using ChunkIndex from methodDesc, compute the wrapping MethodDescChunk

        ushort Flags3AndTokenRemainder = // Read Flags3AndTokenRemainder field from MethodDesc contract using address methodDescHandle.Address

        ushort FlagsAndTokenRange = // Read FlagsAndTokenRange field from MethodDescChunk contract using address methodDescChunk

        int tokenRemainderBitCount = _target.ReadGlobal<byte>("MethodDescTokenRemainderBitCount");
        int tokenRangeBitCount = 24 - tokenRemainderBitCount;
        uint allRidBitsSet = 0xFFFFFF;
        uint tokenRemainderMask = allRidBitsSet >> tokenRangeBitCount;
        uint tokenRangeMask = allRidBitsSet >> tokenRemainderBitCount;

        uint tokenRemainder = (uint)(_desc.Flags3AndTokenRemainder & tokenRemainderMask);
        uint tokenRange = ((uint)(_chunk.FlagsAndTokenRange & tokenRangeMask)) << tokenRemainderBitCount;

        return 0x06000000 | tokenRange | tokenRemainder;
    }

    public uint GetMethodDescSize(MethodDescHandle methodDescHandle)
    {
        MethodDesc methodDesc = _methodDescs[methodDescHandle.Address];

        // the runtime generates a table to lookup the size of a MethodDesc based on the flags
        // read the location of the table and index into it using certain bits of MethodDesc.Flags
        TargetPointer methodDescSizeTable = target.ReadGlobalPointer(Constants.Globals.MethodDescSizeTable);

        ushort arrayOffset = (ushort)(methodDesc.Flags & (ushort)(
            MethodDescFlags.ClassificationMask |
            MethodDescFlags.HasNonVtableSlot |
            MethodDescFlags.HasMethodImpl |
            MethodDescFlags.HasNativeCodeSlot |
            MethodDescFlags.HasAsyncMethodData));
        return target.Read<byte>(methodDescSizeTable + arrayOffset);
    }

    public bool IsArrayMethod(MethodDescHandle methodDescHandle, out ArrayFunctionType functionType)
    {
        MethodDesc methodDesc = _methodDescs[methodDescHandle.Address];

        if (methodDesc.Classification != MethodDescClassification.Array)
        {
            functionType = default;
            return false;
        }

        int arrayMethodIndex = methodDesc.Slot - GetNumVtableSlots(GetTypeHandle(methodDesc.MethodTable));

        functionType = arrayMethodIndex switch
        {
            0 => ArrayFunctionType.Get,
            1 => ArrayFunctionType.Set,
            2 => ArrayFunctionType.Address,
            > 3 => ArrayFunctionType.Constructor,
            _ => throw new InvalidOperationException()
        };

        return true;
    }

    public bool IsNoMetadataMethod(MethodDescHandle methodDescHandle, out ReadOnlySpan<byte> methodName)
    {
        MethodDesc methodDesc = _methodDescs[methodDescHandle.Address];

        if (methodDesc.Classification != MethodDescClassification.Dynamic)
        {
            methodName = default;
            return false;
        }

        TargetPointer methodNamePointer = // Read MethodName field from DynamicMethodDesc contract using address methodDescHandle.Address

        methodName = // ReadBuffer from target of a utf8 null terminated string, starting at address methodNamePointer
        return true;
    }

    public bool IsStoredSigMethodDesc(MethodDescHandle methodDescHandle, out ReadOnlySpan<byte> signature)
    {
        MethodDesc methodDesc = _methodDescs[methodDescHandle.Address];

        switch (methodDesc.Classification)
        {
            case MethodDescClassification.Dynamic:
            case MethodDescClassification.EEImpl:
            case MethodDescClassification.Array:
                break; // These have stored sigs

            default:
                signature = default;
                return false;
        }

        TargetPointer Sig = // Read Sig field from StoredSigMethodDesc contract using address methodDescHandle.Address
        uint cSig = // Read cSig field from StoredSigMethodDesc contract using address methodDescHandle.Address

        TargetPointer methodNamePointer = // Read S field from DynamicMethodDesc contract using address methodDescHandle.Address
        signature = // Read buffer from target memory starting at address Sig, with cSig bytes in it.
        return true;
    }

    public bool IsDynamicMethod(MethodDescHandle methodDescHandle)
    {
        MethodDesc methodDesc = _methodDescs[methodDescHandle.Address];

        if (methodDesc.Classification != MethodDescClassification.Dynamic)
        {
            return false;
        }

        uint ExtendedFlags = // Read ExtendedFlags field from StoredSigMethodDesc contract using address methodDescHandle.Address

        return ((DynamicMethodDescExtendedFlags)ExtendedFlags).HasFlag(DynamicMethodDescExtendedFlags.IsLCGMethod);
    }

    public bool IsILStub(MethodDescHandle methodDescHandle)
    {
        MethodDesc methodDesc = _methodDescs[methodDescHandle.Address];

        if (methodDesc.Classification != MethodDescClassification.Dynamic)
        {
            return false;
        }

        uint ExtendedFlags = // Read ExtendedFlags field from StoredSigMethodDesc contract using address methodDescHandle.Address

        return ((DynamicMethodDescExtendedFlags)ExtendedFlags).HasFlag(DynamicMethodDescExtendedFlags.IsILStub);
    }

    public ushort GetSlotNumber(MethodDescHandle methodDesc) => _methodDescs[methodDesc.Addres]._desc.Slot;
```

Determining if a method is in a collectible module:

```csharp
    private TargetPointer GetLoaderModule(MethodDesc md)
    {
        if (md.IsLoaderModuleAttachedToChunk)
        {
            TargetPointer methodDescChunkPointer = md.ChunkAddress;
            TargetPointer endOfChunk = methodDescChunkPointer + md.SizeOfChunk;
            TargetPointer ppLoaderModule = endOfChunk - (ulong)_target.PointerSize;
            return _target.ReadPointer(ppLoaderModule);
        }
        else
        {
            TargetPointer mtAddr = GetMethodTable(new MethodDescHandle(md.Address));
            TypeHandle mt = GetTypeHandle(mtAddr);
            return GetLoaderModule(mt);
        }
    }

    private TargetPointer GetLoaderModule(TypeHandle typeHandle)
    {
        if (!typeHandle.IsMethodTable())
        {
            // FIXME[cdac]: TypeDesc::GetLoaderModule()
        }
        else
        {
            MethodTable mt = _methodTables[typeHandle.Address];
            Data.MethodTableAuxiliaryData mtAuxData = /* get the AuxiliaryData from the Method Table*/;
            return mtAuxData.LoaderModule;
        }
    }

    public bool IsCollectibleMethod(MethodDescHandle methodDesc)
    {
        MethodDesc md = _methodDescs[methodDesc.Address];
        TargetPointer loaderModuleAddr = GetLoaderModule(md);
        ModuleHandle mod = _target.Contracts.Loader.GetModuleHandleFromModulePtr(loaderModuleAddr);
        return _target.Contracts.Loader.IsCollectible(mod);

    }
```

Determining if a method supports multiple code versions:

```csharp
    private bool IsWrapperStub(MethodDesc md)
    {
        return md.IsUnboxingStub || IsInstantiatingStub(md);
    }

    private bool IsInstantiatingStub(MethodDesc md)
    {
        return md.Classification == MethodClassification.Instantiated && !md.IsUnboxingStub && IsWrapperStubWithInstantiations(md);
    }

    private bool IsWrapperStubWithInstantiations(MethodDesc methodDesc)
    {
        return /*Flags2 of InstantiatedMethodDesc at methodDesc.Address has WrapperStubWithInstantiations set*/;
    }

    public bool IsVersionable(MethodDescHandle methodDesc)
    {
        MethodDesc md = _methodDescs[methodDesc.Address];
        if (md.IsEligibleForTieredCompilation)
            return true;
        // MethodDesc::IsEligibleForReJIT
        if (_target.Contracts.ReJIT.IsEnabled())
        {
            if (!md.IsIL)
                return false;
            if (IsWrapperStub(md))
                return false;
            return _target.Contracts.CodeVersions.CodeVersionManagerSupportsMethod(methodDesc.Address);
        }
        return false;
    }
```

Extracting a pointer to the `MethodDescVersioningState` data for a given method

```csharp
    public TargetPointer GetMethodDescVersioningState(MethodDescHandle methodDesc)
    {
        MethodDesc md = _methodDescs[methodDesc.Address];
        TargetPointer codeDataAddress = md._desc.CodeData;
        return /* VersioningState field of MethodDescCodeData at codeDataAddress */;
    }
```

Checking if a method has a native code slot and getting its address

```csharp
    public bool HasNativeCodeSlot(MethodDescHandle methodDesc) =>_methodDescs[methodDesc.Address].HasNativeCodeSlot;

    uint GetMethodClassificationBaseSize (MethodClassification classification)
    => classification switch
    {
        MethodClassification.IL => /*size of MethodDesc*/,
        MethodClassification.FCall => /* size of FCallMethodDesc */
        MethodClassification.PInvoke => /* size of PInvokeMethodDesc */
        MethodClassification.EEImpl => /* size of EEImplMethodDesc */
        MethodClassification.Array => /* size of ArrayMethodDesc */
        MethodClassification.Instantiated => /* size of InstantiatedMethodDesc */
        MethodClassification.ComInterop => /* size of CLRToCOMCallMethodDesc */
        MethodClassification.Dynamic => /* size of DynamicMethodDesc */
    };

    private uint MethodDescAdditionalPointersOffset(MethodDesc md) => GetMethodClassificationBaseSize(md.Classification);

    public TargetPointer GetAddressOfNativeCodeSlot(MethodDescHandle methodDesc)
    {
        MethodDesc md = _methodDescs[methodDesc.Address];
        uint offset = MethodDescAdditionalPointersOffset(md);
        offset += (uint)(_target.PointerSize * md.NativeCodeSlotIndex);
        return methodDesc.Address + offset;
    }
```

Getting the native code pointer for methods with a NativeCodeSlot or a stable entry point

```csharp
    private TargetCodePointer GetStableEntryPoint(TargetPointer methodDescAddress, MethodDesc md)
    {
        return GetMethodEntryPointIfExists(methodDescAddress, md);
    }

    private TargetCodePointer GetMethodEntryPointIfExists(TargetPointer methodDescAddress, MethodDesc md)
    {
        if (md.HasNonVtableSlot)
        {
            TargetPointer pSlot = GetAddressOfNonVtableSlot(methodDescAddress, md);

            return _target.ReadCodePointer(pSlot);
        }

        TargetPointer methodTablePointer = md.MethodTable;
        TypeHandle typeHandle = GetTypeHandle(methodTablePointer);
        TargetPointer addrOfSlot = GetAddressOfSlot(typeHandle, md.Slot);
        return _target.ReadCodePointer(addrOfSlot);
    }

    private TargetPointer GetAddressOfNonVtableSlot(TargetPointer methodDescPointer, MethodDesc md)
    {
        uint offset = MethodDescAdditionalPointersOffset(md);
        offset += (uint)(_target.PointerSize * md.NonVtableSlotIndex);
        return methodDescPointer.Value + offset;
    }

    private TargetPointer GetAddressOfSlot(TypeHandle typeHandle, uint slotNum)
    {
        if (!typeHandle.IsMethodTable())
            throw new InvalidOperationException("typeHandle is not a MethodTable");
        MethodTable mt = _methodTables[typeHandle.Address];

        if (slotNum < mt.NumVirtuals)
        {
            // Virtual slots live in chunks pointed to by vtable indirections
            return GetVTableIndirectionsAddressOfSlot(typeHandle.Address, slotNum);
        }
        else
        {
            // Non-virtual slots < GetNumVtableSlots live before the MethodTableAuxiliaryData. The array grows backwards
            TargetPointer auxDataPtr = _target.ReadPointer(typeHandle.Address + /* MethodTable::AuxiliaryData offset */);
            TargetPointer nonVirtualSlotsArray = auxDataPtr + _target.Read<short>(/* MethodTableAuxiliaryData::OffsetToNonVirtualSlots offset */);
            return nonVirtualSlotsArray - (1 + (slotNum - mt.NumVirtuals));
        }

    }

    private TargetPointer GetVTableIndirectionsAddressOfSlot (TargetPointer methodTablePointer, uint slot)
    {
        private const int NumPointersPerIndirection = 8;
        private const int NumPointersPerIndirectionLog2 = 3;
        TargetPointer indirectionPointer = methodTablePointer + /*size of MethodTable*/ + (ulong)(slotNum >> NumPointersPerIndirectionLog2) * (ulong)_target.PointerSize;
        TargetPointer slotsStart = _target.ReadPointer(indirectionPointer);
        return slotsStart + (ulong)(slotNum & (NumPointersPerIndirection - 1)) * (ulong)_target.PointerSize;
    }

    public TargetCodePointer GetNativeCode(MethodDescHandle methodDescHandle)
    {
        MethodDesc md = _methodDescs[methodDescHandle.Address];
        if (md.HasNativeCodeSlot)
        {
            TargetPointer ppCode = GetAddressOfNativeCodeSlot(methodDescHandle);
            return _target.ReadCodePointer(ppCode);
        }

        if (!md.HasStableEntryPoint || md.HasPrecode)
            return TargetCodePointer.Null;

        return GetStableEntryPoint(methodDescHandle.Address, md);
    }

    public TargetCodePointer IRuntimeTypeSystem.GetMethodEntryPointIfExists(MethodDescHandle methodDescHandle)
    {
        MethodDesc md = _methodDescs[methodDescHandle.Address];
        return GetMethodEntryPointIfExists(methodDescHandle.Address, md);
    }
```

Getting the value of a slot of a MethodTable
```csharp
    public TargetCodePointer GetSlot(TypeHandle typeHandle, uint slot)
    {
        // based on MethodTable::GetSlot(uint slotNumber)
        if (!typeHandle.IsMethodTable())
            throw new ArgumentException($"{nameof(typeHandle)} is not a MethodTable");

        if (slot < GetNumVtableSlots(typeHandle))
        {
            TargetPointer slotPtr = GetAddressOfSlot(typeHandle, slot);
            return _target.ReadCodePointer(slotPtr);
        }

        return TargetCodePointer.Null;
    }
```

Getting a MethodDesc for a certain slot in a MethodTable
```csharp
    // Based on MethodTable::IntroducedMethodIterator
    private IEnumerable<MethodDescHandle> GetIntroducedMethods(TypeHandle typeHandle)
    {
        // typeHandle must represent a MethodTable

        EEClass eeClass = GetClassData(typeHandle);

        // pointer to the first MethodDescChunk
        TargetPointer chunkAddr = eeClass.MethodDescChunk;
        while (chunkAddr != TargetPointer.Null)
        {
            MethodDescChunk chunk = // read Data.MethodDescChunk data from chunkAddr
            TargetPointer methodDescPtr = chunk.FirstMethodDesc;

            // chunk.Count is the number of MethodDescs in the chunk - 1
            // add 1 to get the actual number of MethodDescs within the chunk
            for (int i = 0; i < chunk.Count + 1; i++)
            {
                MethodDescHandle methodDescHandle = GetMethodDescHandle(methodDescPtr);

                // increment pointer to the beginning of the next MethodDesc
                methodDescPtr += GetMethodDescSize(methodDescHandle);
                yield return methodDescHandle;
            }

            // go to the next chunk
            chunkAddr = chunk.Next;
        }
    }

    private readonly TargetPointer GetMethodDescForEntrypoint(TargetCodePointer pCode)
    {
        // Standard path, ask ExecutionManager for the MethodDesc
        IExecutionManager executionManager = _target.Contracts.ExecutionManager;
        if (executionManager.GetCodeBlockHandle(pCode) is CodeBlockHandle cbh)
        {
            TargetPointer methodDescPtr = executionManager.GetMethodDesc(cbh);
            return methodDescPtr;
        }

        // Stub path, read address as a Precode and get the MethodDesc from it
        {
            TargetPointer methodDescPtr = _target.Contracts.PrecodeStubs.GetMethodDescFromStubAddress(pCode);
            return methodDescPtr;
        }
    }

    public IEnumerable<TargetPointer> GetIntroducedMethodDescs(TypeHandle typeHandle)
    {
        if (!typeHandle.IsMethodTable())
            throw new ArgumentException($"{nameof(typeHandle)} is not a MethodTable");

        TypeHandle canonMT = GetTypeHandle(GetCanonicalMethodTable(typeHandle));
        foreach (MethodDescHandle mdh in GetIntroducedMethods(canonMT))
        {
            yield return mdh.Address;
        }
    }

    // Uses GetMethodDescForVtableSlot if slot is less than the number of vtable slots
    // otherwise looks for the slot in the introduced methods
    public TargetPointer GetMethodDescForSlot(TypeHandle typeHandle, ushort slot)
    {
        if (!typeHandle.IsMethodTable())
            throw new ArgumentException($"{nameof(typeHandle)} is not a MethodTable");

        TypeHandle canonMT = GetTypeHandle(GetCanonicalMethodTable(typeHandle));
        if (slot < GetNumVtableSlots(canonMT))
        {
            return GetMethodDescForVtableSlot(canonMT, slot);
        }
        else
        {
            foreach (MethodDescHandle mdh in GetIntroducedMethods(canonMT))
            {
                MethodDesc md = _methodDescs[mdh.Address];
                if (md.Slot == slot)
                {
                    return mdh.Address;
                }
            }
            return TargetPointer.Null;
        }
    }

    private TargetPointer GetMethodDescForVtableSlot(TypeHandle methodTable, ushort slot)
    {
        // based on MethodTable::GetMethodDescForSlot_NoThrow
        if (!typeHandle.IsMethodTable())
            throw new ArgumentException($"{nameof(typeHandle)} is not a MethodTable");

        TargetPointer cannonMTPTr = GetCanonicalMethodTable(typeHandle);
        TypeHandle canonMT = GetTypeHandle(cannonMTPTr);
        if (slot >= GetNumVtableSlots(canonMT))
            throw new ArgumentException(nameof(slot), "Slot number is greater than the number of slots");

        TargetPointer slotPtr = GetAddressOfSlot(canonMT, slot);
        TargetCodePointer pCode = _target.ReadCodePointer(slotPtr);

        if (pCode == TargetCodePointer.Null)
        {
            TargetPointer lookupMTPtr = cannonMTPTr;
            while (lookupMTPtr != TargetPointer.Null)
            {
                // if pCode is null, we iterate through the method descs in the MT.
                TypeHandle lookupMT = GetTypeHandle(lookupMTPtr);
                foreach (MethodDescHandle mdh in GetIntroducedMethods(lookupMT))
                {
                    MethodDesc md = _methodDescs[mdh.Address];
                    if (md.Slot == slot)
                    {
                        return mdh.Address;
                    }
                }
                lookupMTPtr = GetParentMethodTable(lookupMT);
                if (lookupMTPtr != TargetPointer.Null)
                    lookupMTPtr = GetCanonicalMethodTable(GetTypeHandle(lookupMTPtr));
            }
            return TargetPointer.Null;
        }

        return GetMethodDescForEntrypoint(pCode);
    }
```

### FieldDesc

The version 1 FieldDesc APIs depend on the following data descriptors:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `FieldDesc` | `MTOfEnclosingClass` | Pointer to method table of enclosing class |
| `FieldDesc` | `DWord1` | The FD's flags and token |
| `FieldDesc` | `DWord2` | The FD's kind and offset |

```csharp
internal enum FieldDescFlags1 : uint
{
    TokenMask = 0xffffff,
    IsStatic = 0x1000000,
    IsThreadStatic = 0x2000000,
}

internal enum FieldDescFlags2 : uint
{
    TypeMask = 0xf8000000,
    OffsetMask = 0x07ffffff,
}

TargetPointer GetMTOfEnclosingClass(TargetPointer fieldDescPointer)
{
    return target.ReadPointer(fieldDescPointer + /* FieldDesc::MTOfEnclosingClass offset */);
}

uint GetFieldDescMemberDef(TargetPointer fieldDescPointer)
{
    uint DWord1 = target.Read<uint>(fieldDescPointer + /* FieldDesc::DWord1 offset */);
    return EcmaMetadataUtils.CreateFieldDef(DWord1 & (uint)FieldDescFlags1.TokenMask);
}

bool IsFieldDescThreadStatic(TargetPointer fieldDescPointer)
{
    uint DWord1 = target.Read<uint>(fieldDescPointer + /* FieldDesc::DWord1 offset */);
    return (DWord1 & (uint)FieldDescFlags1.IsThreadStatic) != 0;
}

bool IsFieldDescStatic(TargetPointer fieldDescPointer)
{
    uint DWord1 = target.Read<uint>(fieldDescPointer + /* FieldDesc::DWord1 offset */);
    return (DWord1 & (uint)FieldDescFlags1.IsStatic) != 0;
}

uint GetFieldDescType(TargetPointer fieldDescPointer)
{
    uint DWord2 = target.Read<uint>(fieldDescPointer + /* FieldDesc::DWord2 offset */);
    return (DWord2 & (uint)FieldDescFlags2.TypeMask) >> 27;
}

uint GetFieldDescOffset(TargetPointer fieldDescPointer)
{
    uint DWord2 = target.Read<uint>(fieldDescPointer + /* FieldDesc::DWord2 offset */);
    if (DWord2 == _target.ReadGlobal<uint>("FieldOffsetBigRVA"))
    {
        return (uint)fieldDef.GetRelativeVirtualAddress();
    }
    return DWord2 & (uint)FieldDescFlags2.OffsetMask;
}

TargetPointer GetFieldDescNextField(TargetPointer fieldDescPointer)
    => fieldDescPointer + _target.GetTypeInfo(DataType.FieldDesc).Size!.Value;
```