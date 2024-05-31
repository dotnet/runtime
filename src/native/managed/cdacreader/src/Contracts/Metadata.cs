// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using UntrustedMethodTable = Microsoft.Diagnostics.DataContractReader.Contracts.UntrustedMethodTable_1;
using MethodTable = Microsoft.Diagnostics.DataContractReader.Contracts.MethodTable_1;
using UntrustedEEClass = Microsoft.Diagnostics.DataContractReader.Contracts.UntrustedEEClass_1;
using EEClass = Microsoft.Diagnostics.DataContractReader.Contracts.EEClass_1;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal interface IMetadata : IContract
{
    static string IContract.Name => nameof(Metadata);
    static IContract IContract.Create(Target target, int version)
    {
        TargetPointer targetPointer = target.ReadGlobalPointer(Constants.Globals.FreeObjectMethodTable);
        TargetPointer freeObjectMethodTable = target.ReadPointer(targetPointer);
        return version switch
        {
            1 => new Metadata_1(target, freeObjectMethodTable),
            _ => default(Metadata),
        };
    }

    public virtual MethodTable GetMethodTableData(TargetPointer targetPointer) => throw new NotImplementedException();
}

internal struct Metadata : IMetadata
{
    // Everything throws NotImplementedException
}

internal interface IMethodTableFlags
{
    public uint DwFlags { get; }
    public uint DwFlags2 { get; }
    public uint BaseSize { get; }
    private int GetTypeDefRid() => (int)(DwFlags2 >> Metadata_1.Constants.MethodTableDwFlags2TypeDefRidShift);

    public uint GetFlag(Metadata_1.WFLAGS_HIGH mask) => DwFlags & (uint)mask;
    public bool IsInterface => GetFlag(Metadata_1.WFLAGS_HIGH.Category_Mask) == (uint)Metadata_1.WFLAGS_HIGH.Category_Interface;
    public bool IsString => HasComponentSize() && !IsArray() && RawGetComponentSize() == 2;

    public bool HasComponentSize() => GetFlag(Metadata_1.WFLAGS_HIGH.HasComponentSize) != 0;

    public bool IsArray() => GetFlag(Metadata_1.WFLAGS_HIGH.Category_Array_Mask) == (uint)Metadata_1.WFLAGS_HIGH.Category_Array;

    public ushort RawGetComponentSize() => (ushort)(DwFlags >> 16);

    public bool HasInstantiation() => throw new NotImplementedException();
}

// GC Heap corruption may create situations where a putative pointer to a MethodTable
// may point to garbage. So this struct represents a MethodTable that we don't necessarily
// trust to be valid.
// see Metadata_1.ValidateMethodTablePointer
// This doesn't need as many properties as MethodTable because we dont' want to be operating on
// an UntrustedMethodTable for too long
internal struct UntrustedMethodTable_1 : IMethodTableFlags
{
    private readonly Target _target;
    private readonly Target.TypeInfo _type;
    public TargetPointer MethodTablePointer { get; init; }

    internal UntrustedMethodTable_1(Target target, TargetPointer methodTablePointer)
    {
        _target = target;
        _type = target.GetTypeInfo(DataType.MethodTable);
        MethodTablePointer = methodTablePointer;
    }

    // all these accessors might throw if MethodTablePointer is invalid
    public uint DwFlags => _target.Read<uint>(MethodTablePointer + (ulong)_type.Fields[nameof(DwFlags2)].Offset);
    public uint DwFlags2 => _target.Read<uint>(MethodTablePointer + (ulong)_type.Fields[nameof(DwFlags)].Offset);
    public uint BaseSize => _target.Read<uint>(MethodTablePointer + (ulong)_type.Fields[nameof(BaseSize)].Offset);

    public TargetPointer EEClassOrCanonMT => _target.ReadPointer(MethodTablePointer + (ulong)_type.Fields[nameof(EEClassOrCanonMT)].Offset);
    public TargetPointer EEClass => (EEClassOrCanonMT & (ulong)Metadata_1.EEClassOrCanonMTBits.Mask) == (ulong)Metadata_1.EEClassOrCanonMTBits.EEClass ? EEClassOrCanonMT : throw new InvalidOperationException("not an EEClass");

}

internal struct UntrustedEEClass_1
{
    public readonly Target _target;
    private readonly Target.TypeInfo _type;

    public TargetPointer EEClassPointer { get; init; }

    internal UntrustedEEClass_1(Target target, TargetPointer eeClassPointer)
    {
        _target = target;
        EEClassPointer = eeClassPointer;
        _type = target.GetTypeInfo(DataType.EEClass);
    }

    public TargetPointer MethodTable => _target.ReadPointer(EEClassPointer + (ulong)_type.Fields[nameof(MethodTable)].Offset);
}


internal struct MethodTable_1 : IMethodTableFlags
{
    public Data.MethodTable MethodTableData { get; init; }
    public bool IsFreeObjectMethodTable { get; init; }
    internal MethodTable_1(Data.MethodTable data, bool isFreeObjectMT)
    {
        MethodTableData = data;
        IsFreeObjectMethodTable = isFreeObjectMT;
    }

    public uint DwFlags => MethodTableData.DwFlags;
    public uint DwFlags2 => MethodTableData.DwFlags2;
    public uint BaseSize => MethodTableData.BaseSize;
    public TargetPointer EEClassOrCanonMT => MethodTableData.EEClassOrCanonMT;

    public TargetPointer EEClass => (EEClassOrCanonMT & (ulong)Metadata_1.EEClassOrCanonMTBits.Mask) == (ulong)Metadata_1.EEClassOrCanonMTBits.EEClass ? EEClassOrCanonMT : throw new InvalidOperationException("not an EEClass");
}

internal struct EEClass_1
{
    public Data.EEClass EEClassData { get; init; }
    internal EEClass_1(Data.EEClass eeClassData)
    {
        EEClassData = eeClassData;
    }

    public TargetPointer MethodTable => EEClassData.MethodTable;
}


internal struct Metadata_1 : IMetadata
{
    private readonly Target _target;
    private readonly TargetPointer _freeObjectMethodTablePointer;

    internal static class Constants
    {
        internal const int MethodTableDwFlags2TypeDefRidShift = 8;
    }

    [Flags]
    internal enum WFLAGS_HIGH : uint
    {
        Category_Mask = 0x000F0000,

        Category_Class = 0x00000000,
        Category_Unused_1 = 0x00010000,
        Category_Unused_2 = 0x00020000,
        Category_Unused_3 = 0x00030000,

        Category_ValueType = 0x00040000,
        Category_ValueType_Mask = 0x000C0000,
        Category_Nullable = 0x00050000, // sub-category of ValueType
        Category_PrimitiveValueType = 0x00060000, // sub-category of ValueType, Enum or primitive value type
        Category_TruePrimitive = 0x00070000, // sub-category of ValueType, Primitive (ELEMENT_TYPE_I, etc.)

        Category_Array = 0x00080000,
        Category_Array_Mask = 0x000C0000,
        // Category_IfArrayThenUnused                 = 0x00010000, // sub-category of Array
        Category_IfArrayThenSzArray = 0x00020000, // sub-category of Array

        Category_Interface = 0x000C0000,
        Category_Unused_4 = 0x000D0000,
        Category_Unused_5 = 0x000E0000,
        Category_Unused_6 = 0x000F0000,

        Category_ElementTypeMask = 0x000E0000, // bits that matter for element type mask

        // GC depends on this bit
        HasFinalizer = 0x00100000, // instances require finalization

        IDynamicInterfaceCastable = 0x10000000, // class implements IDynamicInterfaceCastable interface

        ICastable = 0x00400000, // class implements ICastable interface

        RequiresAlign8 = 0x00800000, // Type requires 8-byte alignment (only set on platforms that require this and don't get it implicitly)

        ContainsPointers = 0x01000000,

        HasTypeEquivalence = 0x02000000, // can be equivalent to another type

        IsTrackedReferenceWithFinalizer = 0x04000000,

        // GC depends on this bit
        Collectible = 0x00200000,
        ContainsGenericVariables = 0x20000000,   // we cache this flag to help detect these efficiently and
                                                 // to detect this condition when restoring

        ComObject = 0x40000000, // class is a com object

        HasComponentSize = 0x80000000,   // This is set if component size is used for flags.

        // Types that require non-trivial interface cast have this bit set in the category
        NonTrivialInterfaceCast = Category_Array
                                             | ComObject
                                             | ICastable
                                             | IDynamicInterfaceCastable
                                             | Category_ValueType

    }

    [Flags]
    internal enum EEClassOrCanonMTBits
    {
        EEClass = 0,
        CanonMT = 1,
        Mask = 1,
    }

    internal Metadata_1(Target target, TargetPointer freeObjectMethodTablePointer)
    {
        _target = target;
        _freeObjectMethodTablePointer = freeObjectMethodTablePointer;
    }

    public TargetPointer FreeObjectMethodTablePointer => _freeObjectMethodTablePointer;

    private UntrustedMethodTable GetUntrustedMethodTableData(TargetPointer methodTablePointer)
    {
        return new UntrustedMethodTable(_target, methodTablePointer);
    }

    private UntrustedEEClass GetUntrustedEEClassData(TargetPointer eeClassPointer)
    {
        return new UntrustedEEClass(_target, eeClassPointer);
    }

    public MethodTable GetMethodTableData(TargetPointer methodTablePointer)
    {
        // Check if we cached it already
        if (_target.ProcessedData.TryGet(methodTablePointer, out Data.MethodTable? methodTableData))
        {
            return new MethodTable(methodTableData, methodTablePointer == FreeObjectMethodTablePointer);
        }

        // Otherwse, don't trust it yet
        UntrustedMethodTable untrustedMethodTable = GetUntrustedMethodTableData(methodTablePointer);

        // if it's the free object method table, we can trust it
        if (methodTablePointer == FreeObjectMethodTablePointer)
        {
            Data.MethodTable freeObjectMethodTableData = _target.ProcessedData.GetOrAdd<Data.MethodTable>(methodTablePointer);
            return new MethodTable(freeObjectMethodTableData, isFreeObjectMT: true);
        }
        if (!ValidateMethodTablePointer(in untrustedMethodTable))
        {
            throw new ArgumentException("Invalid method table pointer");
        }
        // ok, we trust it, cache the data
        Data.MethodTable trustedMethodTableData = _target.ProcessedData.GetOrAdd<Data.MethodTable>(methodTablePointer);
        return new MethodTable(trustedMethodTableData, isFreeObjectMT: false);
    }

    private bool ValidateMethodTablePointer(ref readonly UntrustedMethodTable umt)
    {
        // FIXME: is methodTablePointer properly sign-extended from 32-bit targets?
        // FIXME2: do we need this? Data.MethodTable probably would throw if methodTablePointer is invalid
        //if (umt.MethodTablePointer == TargetPointer.Null || umt.MethodTablePointer == TargetPointer.MinusOne)
        //{
        //    return false;
        //}
        try
        {
            if (!ValidateWithPossibleAV(in umt))
            {
                return false;
            }
            if (!ValidateMethodTable(in umt))
            {
                return false;
            }
        }
        catch (Exception)
        {
            // FIXME: maybe don't swallow all exceptions?
            return false;
        }
        return true;
    }

    private bool ValidateWithPossibleAV(ref readonly UntrustedMethodTable methodTable)
    {
        TargetPointer eeClassPtr = GetClassWithPossibleAV(in methodTable);
        if (eeClassPtr != TargetPointer.Null)
        {
            UntrustedEEClass eeClass = GetUntrustedEEClassData(eeClassPtr);
            TargetPointer methodTablePtrFromClass = GetMethodTablePointerWithPossibleAV(in eeClass);
            if (methodTable.MethodTablePointer == methodTablePtrFromClass)
            {
                return true;
            }
            if (((IMethodTableFlags)methodTable).HasInstantiation() || ((IMethodTableFlags)methodTable).IsArray())
            {
                UntrustedMethodTable methodTableFromClass = GetUntrustedMethodTableData(methodTablePtrFromClass);
                TargetPointer classFromMethodTable = GetClassWithPossibleAV(in methodTableFromClass);
                return classFromMethodTable == eeClassPtr;
            }
        }
        return false;
    }

    internal bool ValidateMethodTable(ref readonly UntrustedMethodTable methodTable)
    {
        if (!((IMethodTableFlags)methodTable).IsInterface && !((IMethodTableFlags)methodTable).IsString)
        {
            if (methodTable.BaseSize == 0 || !_target.IsAlignedToPointerSize(methodTable.BaseSize))
            {
                return false;
            }
        }
        return true;
    }

    private TargetPointer GetClassWithPossibleAV(ref readonly UntrustedMethodTable methodTable)
    {
        throw new NotImplementedException("TODO");
    }

    private TargetPointer GetMethodTablePointerWithPossibleAV(ref readonly UntrustedEEClass eeClass)
    {
        throw new NotImplementedException("TODO");
    }
}
