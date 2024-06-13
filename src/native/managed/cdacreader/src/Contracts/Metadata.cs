// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata.Ecma335;
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

    public virtual TargetPointer GetClass(in MethodTable methodTable) => throw new NotImplementedException();

    public virtual bool IsString(in MethodTable methodTable) => throw new NotImplementedException();
    public virtual bool ContainsPointers(in MethodTable methodTable) => throw new NotImplementedException();
    public virtual bool IsDynamicStatics(in MethodTable methodTable) => throw new NotImplementedException();
    public virtual uint GetTypeDefToken(in MethodTable methodTable) => throw new NotImplementedException();
    public virtual ushort GetNumMethods(in MethodTable methodTable) => throw new NotImplementedException();

    public virtual ushort GetNumVtableSlots(in MethodTable methodTable) => throw new NotImplementedException();

    public virtual uint GetTypeDefTypeAttributes(in MethodTable methodTable) => throw new NotImplementedException();
}

internal struct Metadata : IMetadata
{
    // Everything throws NotImplementedException
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


    public int GetComponentSize() => ((IMethodTableFlags)this).HasComponentSize ? ((IMethodTableFlags)this).RawGetComponentSize() : 0;

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

    private bool ValidateMethodTablePointer(in UntrustedMethodTable umt)
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

    private bool ValidateWithPossibleAV(in UntrustedMethodTable methodTable)
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
            UntrustedEEClass eeClass = GetUntrustedEEClassData(eeClassPtr);
            TargetPointer methodTablePtrFromClass = GetMethodTableWithPossibleAV(in eeClass);
            if (methodTable.Address == methodTablePtrFromClass)
            {
                return true;
            }
            if (((IMethodTableFlags)methodTable).HasInstantiation || ((IMethodTableFlags)methodTable).IsArray)
            {
                UntrustedMethodTable methodTableFromClass = GetUntrustedMethodTableData(methodTablePtrFromClass);
                TargetPointer classFromMethodTable = GetClassWithPossibleAV(in methodTableFromClass);
                return classFromMethodTable == eeClassPtr;
            }
        }
        return false;
    }

    private bool ValidateMethodTable(in UntrustedMethodTable methodTable)
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
    private TargetPointer GetClassWithPossibleAV(in UntrustedMethodTable methodTable)
    {
        TargetPointer eeClassOrCanonMT = methodTable.EEClassOrCanonMT;

        if (GetEEClassOrCanonMTBits(eeClassOrCanonMT) == EEClassOrCanonMTBits.EEClass)
        {
            return methodTable.EEClass;
        }
        else
        {
            TargetPointer canonicalMethodTablePtr = methodTable.CanonMT;
            UntrustedMethodTable umt = GetUntrustedMethodTableData(canonicalMethodTablePtr);
            return umt.EEClass;
        }
    }

    private static TargetPointer GetMethodTableWithPossibleAV(in UntrustedEEClass eeClass)
    {
        return eeClass.MethodTable;
    }

    public TargetPointer GetClass(in MethodTable methodTable)
    {
        switch (GetEEClassOrCanonMTBits(methodTable.EEClassOrCanonMT))
        {
            case EEClassOrCanonMTBits.EEClass:
                return methodTable.EEClassOrCanonMT;
            case EEClassOrCanonMTBits.CanonMT:
                TargetPointer canonMTPtr = new TargetPointer((ulong)methodTable.EEClassOrCanonMT & ~(ulong)Metadata_1.EEClassOrCanonMTBits.Mask);
                MethodTable canonMT = GetMethodTableData(canonMTPtr);
                return canonMT.EEClassOrCanonMT; // canonical method table EEClassOrCanonMT is always EEClass
            default:
                throw new InvalidOperationException();
        }
    }

    // only called on trusted method tables, so we always trust the resulting EEClass
    private EEClass GetClassData(in MethodTable methodTable)
    {
        TargetPointer clsPtr = GetClass(in methodTable);
        // Check if we cached it already
        if (_target.ProcessedData.TryGet(clsPtr, out Data.EEClass? eeClassData))
        {
            return new EEClass_1(eeClassData);
        }
        eeClassData = _target.ProcessedData.GetOrAdd<Data.EEClass>(clsPtr);
        return new EEClass_1(eeClassData);
    }

    public bool IsString(in MethodTable methodTable) => ((IMethodTableFlags)methodTable).IsString;
    public bool ContainsPointers(in MethodTable methodTable) => ((IMethodTableFlags)methodTable).ContainsPointers;
    public bool IsDynamicStatics(in MethodTable methodTable) => ((IMethodTableFlags)methodTable).IsDynamicStatics;

    public uint GetTypeDefToken(in MethodTable methodTable)
    {
        return (uint)(((IMethodTableFlags)methodTable).GetTypeDefRid() | ((int)TableIndex.TypeDef << 24));
    }

    public ushort GetNumMethods(in MethodTable methodTable)
    {
        EEClass cls = GetClassData(in methodTable);
        return cls.NumMethods;
    }

    private ushort GetNumNonVirtualSlots(in MethodTable methodTable)
    {
        TargetPointer eeClassOrCanonMT = methodTable.EEClassOrCanonMT;
        if (GetEEClassOrCanonMTBits(eeClassOrCanonMT) == EEClassOrCanonMTBits.EEClass)
        {
            return GetClassData(methodTable).NumNonVirtualSlots;
        }
        else
        {
            return 0;
        }
    }

    public ushort GetNumVtableSlots(in MethodTable methodTable)
    {
        return checked((ushort)(methodTable.NumVirtuals + GetNumNonVirtualSlots(methodTable)));
    }

    public uint GetTypeDefTypeAttributes(in MethodTable methodTable)
    {
        return GetClassData(methodTable).TypeDefTypeAttributes;
    }

    [Flags]
    internal enum MethodTableAuxiliaryDataFlags : uint
    {
        CanCompareBitsOrUseFastGetHashCode = 0x0001,     // Is any field type or sub field type overrode Equals or GetHashCode
        HasCheckedCanCompareBitsOrUseFastGetHashCode = 0x0002,  // Whether we have checked the overridden Equals or GetHashCode
        HasApproxParent = 0x0010,
        IsNotFullyLoaded = 0x0040,
        DependenciesLoaded = 0x0080,     // class and all dependencies loaded up to CLASS_LOADED_BUT_NOT_VERIFIED
        MayHaveOpenInterfaceInInterfaceMap = 0x0100,
        DebugOnly_ParentMethodTablePointerValid = 0x4000,
        DebugOnly_HasInjectedInterfaceDuplicates = 0x8000,
    }
}
