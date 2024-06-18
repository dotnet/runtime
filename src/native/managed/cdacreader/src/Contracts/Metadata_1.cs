// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

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
    public TargetPointer Address { get; init; }

    internal UntrustedMethodTable_1(Target target, TargetPointer methodTablePointer)
    {
        _target = target;
        _type = target.GetTypeInfo(DataType.MethodTable);
        Address = methodTablePointer;
    }

    // all these accessors might throw if MethodTablePointer is invalid
    public uint DwFlags => _target.Read<uint>(Address + (ulong)_type.Fields[nameof(DwFlags2)].Offset);
    public uint DwFlags2 => _target.Read<uint>(Address + (ulong)_type.Fields[nameof(DwFlags)].Offset);
    public uint BaseSize => _target.Read<uint>(Address + (ulong)_type.Fields[nameof(BaseSize)].Offset);

    internal TargetPointer EEClassOrCanonMT => _target.ReadPointer(Address + (ulong)_type.Fields[nameof(EEClassOrCanonMT)].Offset);
    internal TargetPointer EEClass => Metadata_1.GetEEClassOrCanonMTBits(EEClassOrCanonMT) == Metadata_1.EEClassOrCanonMTBits.EEClass ? EEClassOrCanonMT : throw new InvalidOperationException("not an EEClass");
    internal TargetPointer CanonMT
    {
        get
        {
            if (Metadata_1.GetEEClassOrCanonMTBits(EEClassOrCanonMT) == Metadata_1.EEClassOrCanonMTBits.CanonMT)
            {
                return new TargetPointer((ulong)EEClassOrCanonMT & ~(ulong)Metadata_1.EEClassOrCanonMTBits.Mask);
            }
            else
            {
                throw new InvalidOperationException("not a canonical method table");
            }
        }
    }
}

internal struct UntrustedEEClass_1
{
    public readonly Target _target;
    private readonly Target.TypeInfo _type;

    public TargetPointer Address { get; init; }

    internal UntrustedEEClass_1(Target target, TargetPointer eeClassPointer)
    {
        _target = target;
        Address = eeClassPointer;
        _type = target.GetTypeInfo(DataType.EEClass);
    }

    public TargetPointer MethodTable => _target.ReadPointer(Address + (ulong)_type.Fields[nameof(MethodTable)].Offset);
}


internal struct MethodTable_1 : IMethodTableFlags
{
    private Data.MethodTable MethodTableData { get; init; }
    internal bool IsFreeObjectMethodTable { get; init; }
    internal MethodTable_1(Data.MethodTable data, bool isFreeObjectMT)
    {
        MethodTableData = data;
        IsFreeObjectMethodTable = isFreeObjectMT;
    }

    public uint DwFlags => MethodTableData.DwFlags;
    public uint DwFlags2 => MethodTableData.DwFlags2;
    public uint BaseSize => MethodTableData.BaseSize;
    internal TargetPointer EEClassOrCanonMT => MethodTableData.EEClassOrCanonMT;
    internal TargetPointer Module => MethodTableData.Module;

    public TargetPointer EEClass => Metadata_1.GetEEClassOrCanonMTBits(EEClassOrCanonMT) == Metadata_1.EEClassOrCanonMTBits.EEClass ? EEClassOrCanonMT : throw new InvalidOperationException("not an EEClass");


    public TargetPointer ParentMethodTable => MethodTableData.ParentMethodTable;
    public ushort NumInterfaces => MethodTableData.NumInterfaces;
    public ushort NumVirtuals => MethodTableData.NumVirtuals;

}

internal struct EEClass_1
{
    public Data.EEClass EEClassData { get; init; }
    internal EEClass_1(Data.EEClass eeClassData)
    {
        EEClassData = eeClassData;
    }

    public TargetPointer MethodTable => EEClassData.MethodTable;
    public ushort NumMethods => EEClassData.NumMethods;
    public ushort NumNonVirtualSlots => EEClassData.NumNonVirtualSlots;

    public uint TypeDefTypeAttributes => EEClassData.DwAttrClass;
}


internal partial struct Metadata_1 : IMetadata
{
    private readonly Target _target;
    private readonly TargetPointer _freeObjectMethodTablePointer;

    private readonly Dictionary<TargetPointer, MethodTable_1> _methodTables = new();

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

    internal Metadata_1(Target target, TargetPointer freeObjectMethodTablePointer)
    {
        _target = target;
        _freeObjectMethodTablePointer = freeObjectMethodTablePointer;
    }

    public TargetPointer FreeObjectMethodTablePointer => _freeObjectMethodTablePointer;

    private UntrustedMethodTable_1 GetUntrustedMethodTableData(TargetPointer methodTablePointer)
    {
        return new UntrustedMethodTable_1(_target, methodTablePointer);
    }

    private UntrustedEEClass_1 GetUntrustedEEClassData(TargetPointer eeClassPointer)
    {
        return new UntrustedEEClass_1(_target, eeClassPointer);
    }

    public MethodTableHandle GetMethodTableData(TargetPointer methodTablePointer)
    {
        // if we already trust that address, return a handle
        if (_methodTables.ContainsKey(methodTablePointer))
        {
            return new MethodTableHandle(methodTablePointer);
        }
        // Check if we cached the underlying data already
        if (_target.ProcessedData.TryGet(methodTablePointer, out Data.MethodTable? methodTableData))
        {
            // we already cached the data, we trust it, create the representation struct for our use
            MethodTable_1 trustedMethodTable = new MethodTable_1(methodTableData, methodTablePointer == FreeObjectMethodTablePointer);
            _ = _methodTables.TryAdd(methodTablePointer, trustedMethodTable);
            return new MethodTableHandle(methodTablePointer);
        }

        // Otherwse, don't trust it yet
        UntrustedMethodTable_1 untrustedMethodTable = GetUntrustedMethodTableData(methodTablePointer);

        // if it's the free object method table, we can trust it
        if (methodTablePointer == FreeObjectMethodTablePointer)
        {
            Data.MethodTable freeObjectMethodTableData = _target.ProcessedData.GetOrAdd<Data.MethodTable>(methodTablePointer);
            MethodTable_1 trustedMethodTable = new MethodTable_1(freeObjectMethodTableData, isFreeObjectMT: true);
            _ = _methodTables.TryAdd(methodTablePointer, trustedMethodTable);
            return new MethodTableHandle(methodTablePointer);
        }
        if (!ValidateMethodTablePointer(in untrustedMethodTable))
        {
            throw new ArgumentException("Invalid method table pointer");
        }
        // ok, we trust it, cache the data
        Data.MethodTable trustedMethodTableData = _target.ProcessedData.GetOrAdd<Data.MethodTable>(methodTablePointer);
        MethodTable_1 trustedMethodTableF = new MethodTable_1(trustedMethodTableData, isFreeObjectMT: false);
        _ = _methodTables.TryAdd(methodTablePointer, trustedMethodTableF);
        return new MethodTableHandle(methodTablePointer);
    }

    private bool ValidateMethodTablePointer(in UntrustedMethodTable_1 umt)
    {
        // FIXME: is methodTablePointer properly sign-extended from 32-bit targets?
        // FIXME2: do we need this? Data.MethodTable probably would throw if methodTablePointer is invalid
        //if (umt.MethodTablePointer == TargetPointer.Null || umt.MethodTablePointer == TargetPointer.MinusOne)
        //{
        //    return false;
        //}
        try
        {
            if (!ValidateWithPossibleAV(umt))
            {
                return false;
            }
            if (!ValidateMethodTable(umt))
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

    private bool ValidateWithPossibleAV(in UntrustedMethodTable_1 methodTable)
    {
        // For non-generic classes, we can rely on comparing
        //    object->methodtable->class->methodtable
        // to
        //    object->methodtable
        //
        //  However, for generic instantiation this does not work. There we must
        //  compare
        //
        //    object->methodtable->class->methodtable->class
        // to
        //    object->methodtable->class
        TargetPointer eeClassPtr = GetClassWithPossibleAV(methodTable);
        if (eeClassPtr != TargetPointer.Null)
        {
            UntrustedEEClass_1 eeClass = GetUntrustedEEClassData(eeClassPtr);
            TargetPointer methodTablePtrFromClass = GetMethodTableWithPossibleAV(in eeClass);
            if (methodTable.Address == methodTablePtrFromClass)
            {
                return true;
            }
            if (((IMethodTableFlags)methodTable).HasInstantiation || ((IMethodTableFlags)methodTable).IsArray)
            {
                UntrustedMethodTable_1 methodTableFromClass = GetUntrustedMethodTableData(methodTablePtrFromClass);
                TargetPointer classFromMethodTable = GetClassWithPossibleAV(in methodTableFromClass);
                return classFromMethodTable == eeClassPtr;
            }
        }
        return false;
    }

    private bool ValidateMethodTable(in UntrustedMethodTable_1 methodTable)
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

    internal static EEClassOrCanonMTBits GetEEClassOrCanonMTBits(TargetPointer eeClassOrCanonMTPtr)
    {
        return (EEClassOrCanonMTBits)(eeClassOrCanonMTPtr & (ulong)EEClassOrCanonMTBits.Mask);
    }
    private TargetPointer GetClassWithPossibleAV(in UntrustedMethodTable_1 methodTable)
    {
        TargetPointer eeClassOrCanonMT = methodTable.EEClassOrCanonMT;

        if (GetEEClassOrCanonMTBits(eeClassOrCanonMT) == EEClassOrCanonMTBits.EEClass)
        {
            return methodTable.EEClass;
        }
        else
        {
            TargetPointer canonicalMethodTablePtr = methodTable.CanonMT;
            UntrustedMethodTable_1 umt = GetUntrustedMethodTableData(canonicalMethodTablePtr);
            return umt.EEClass;
        }
    }

    private static TargetPointer GetMethodTableWithPossibleAV(in UntrustedEEClass_1 eeClass)
    {
        return eeClass.MethodTable;
    }

    public uint GetBaseSize(MethodTableHandle methodTableHandle) => ((IMethodTableFlags)_methodTables[methodTableHandle.Address]).BaseSize;

    private static uint GetComponentSize(MethodTable_1 methodTable)
    {
        return ((IMethodTableFlags)methodTable).HasComponentSize ? ((IMethodTableFlags)methodTable).RawGetComponentSize() : 0u;
    }
    public uint GetComponentSize(MethodTableHandle methodTableHandle) => GetComponentSize(_methodTables[methodTableHandle.Address]);
    public TargetPointer GetClass(MethodTableHandle methodTableHandle)
    {
        MethodTable_1 methodTable = _methodTables[methodTableHandle.Address];
        switch (GetEEClassOrCanonMTBits(methodTable.EEClassOrCanonMT))
        {
            case EEClassOrCanonMTBits.EEClass:
                return methodTable.EEClassOrCanonMT;
            case EEClassOrCanonMTBits.CanonMT:
                TargetPointer canonMTPtr = new TargetPointer((ulong)methodTable.EEClassOrCanonMT & ~(ulong)Metadata_1.EEClassOrCanonMTBits.Mask);
                MethodTableHandle canonMTHandle = GetMethodTableData(canonMTPtr);
                MethodTable_1 canonMT = _methodTables[canonMTHandle.Address];
                return canonMT.EEClassOrCanonMT; // canonical method table EEClassOrCanonMT is always EEClass
            default:
                throw new InvalidOperationException();
        }
    }

    public TargetPointer GetModule(MethodTableHandle methodTableHandle) => _methodTables[methodTableHandle.Address].Module;
    public TargetPointer GetParentMethodTable(MethodTableHandle methodTableHandle) => _methodTables[methodTableHandle.Address].ParentMethodTable;

    // only called on trusted method tables, so we always trust the resulting EEClass
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

    public bool IsFreeObjectMethodTable(MethodTableHandle methodTableHandle)
    {
        // TODO: just store the MethodTableHandle of the free object MT in the contract and do an equality comparison
        // no need to store it on every MethodTable_1 instance
        return _methodTables[methodTableHandle.Address].IsFreeObjectMethodTable;
    }
    public bool IsString(MethodTableHandle methodTableHandle) => ((IMethodTableFlags)_methodTables[methodTableHandle.Address]).IsString;
    public bool ContainsPointers(MethodTableHandle methodTableHandle) => ((IMethodTableFlags)_methodTables[methodTableHandle.Address]).ContainsPointers;

    public uint GetTypeDefToken(MethodTableHandle methodTableHandle)
    {
        MethodTable_1 methodTable = _methodTables[methodTableHandle.Address];
        return (uint)(((IMethodTableFlags)methodTable).GetTypeDefRid() | ((int)TableIndex.TypeDef << 24));
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

    public bool IsDynamicStatics(MethodTableHandle methodTableHandle) => ((IMethodTableFlags)_methodTables[methodTableHandle.Address]).GetFlag(WFLAGS2_ENUM.DynamicStatics) != 0;

    [Flags]
    internal enum MethodTableAuxiliaryDataFlags : uint
    {
        Initialized = 0x0001,
        HasCheckedCanCompareBitsOrUseFastGetHashCode = 0x0002,  // Whether we have checked the overridden Equals or GetHashCode
        CanCompareBitsOrUseFastGetHashCode = 0x0004,     // Is any field type or sub field type overridden Equals or GetHashCode

        HasApproxParent = 0x0010,
        // enum_unused                      = 0x0020,
        IsNotFullyLoaded = 0x0040,
        DependenciesLoaded = 0x0080,     // class and all dependencies loaded up to CLASS_LOADED_BUT_NOT_VERIFIED

        IsInitError = 0x0100,
        IsStaticDataAllocated = 0x0200,
        // unum_unused                      = 0x0400,
        IsTlsIndexAllocated = 0x0800,
        MayHaveOpenInterfaceInInterfaceMap = 0x1000,
        // enum_unused                      = 0x2000,

        // ifdef _DEBUG
        DEBUG_ParentMethodTablePointerValid = 0x4000,
        DEBUG_HasInjectedInterfaceDuplicates = 0x8000,
    }
}
