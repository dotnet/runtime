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
// This doesn't need as many properties as MethodTable because we don't want to be operating on
// a NonValidatedMethodTable for too long
internal struct NonValidatedMethodTable_1
{
    private readonly Target _target;
    private readonly Target.TypeInfo _type;
    internal TargetPointer Address { get; init; }

    private Metadata_1.MethodTableFlags? _methodTableFlags;

    internal NonValidatedMethodTable_1(Target target, TargetPointer methodTablePointer)
    {
        _target = target;
        _type = target.GetTypeInfo(DataType.MethodTable);
        Address = methodTablePointer;
        _methodTableFlags = null;
    }

    private Metadata_1.MethodTableFlags GetOrCreateFlags()
    {
        if (_methodTableFlags == null)
        {
            // note: may throw if the method table Address is corrupted
            Metadata_1.MethodTableFlags flags = new Metadata_1.MethodTableFlags
            {
                MTFlags = _target.Read<uint>(Address + (ulong)_type.Fields[nameof(Metadata_1.MethodTableFlags.MTFlags)].Offset),
                MTFlags2 = _target.Read<uint>(Address + (ulong)_type.Fields[nameof(Metadata_1.MethodTableFlags.MTFlags2)].Offset),
                BaseSize = _target.Read<uint>(Address + (ulong)_type.Fields[nameof(Metadata_1.MethodTableFlags.BaseSize)].Offset),
            };
            _methodTableFlags = flags;
        }
        return _methodTableFlags.Value;
    }

    internal Metadata_1.MethodTableFlags Flags => GetOrCreateFlags();

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

internal struct NonValidatedEEClass_1
{
    public readonly Target _target;
    private readonly Target.TypeInfo _type;

    internal TargetPointer Address { get; init; }

    internal NonValidatedEEClass_1(Target target, TargetPointer eeClassPointer)
    {
        _target = target;
        Address = eeClassPointer;
        _type = target.GetTypeInfo(DataType.EEClass);
    }

    internal TargetPointer MethodTable => _target.ReadPointer(Address + (ulong)_type.Fields[nameof(MethodTable)].Offset);
}


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

internal partial struct Metadata_1 : IMetadata
{
    private readonly Target _target;
    private readonly TargetPointer _freeObjectMethodTablePointer;

    // FIXME: we mutate this dictionary - copies of the Metadata_1 struct share this instance
    private readonly Dictionary<TargetPointer, MethodTable_1> _methodTables = new();

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

    internal TargetPointer FreeObjectMethodTablePointer => _freeObjectMethodTablePointer;

    private NonValidatedMethodTable_1 GetUntrustedMethodTableData(TargetPointer methodTablePointer)
    {
        return new NonValidatedMethodTable_1(_target, methodTablePointer);
    }

    private NonValidatedEEClass_1 GetUntrustedEEClassData(TargetPointer eeClassPointer)
    {
        return new NonValidatedEEClass_1(_target, eeClassPointer);
    }

    public MethodTableHandle GetMethodTableHandle(TargetPointer methodTablePointer)
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
            MethodTable_1 trustedMethodTable = new MethodTable_1(methodTableData);
            _ = _methodTables.TryAdd(methodTablePointer, trustedMethodTable);
            return new MethodTableHandle(methodTablePointer);
        }

        // Otherwse, don't trust it yet
        NonValidatedMethodTable_1 untrustedMethodTable = GetUntrustedMethodTableData(methodTablePointer);

        // if it's the free object method table, we can trust it
        if (methodTablePointer == FreeObjectMethodTablePointer)
        {
            Data.MethodTable freeObjectMethodTableData = _target.ProcessedData.GetOrAdd<Data.MethodTable>(methodTablePointer);
            MethodTable_1 trustedMethodTable = new MethodTable_1(freeObjectMethodTableData);
            _ = _methodTables.TryAdd(methodTablePointer, trustedMethodTable);
            return new MethodTableHandle(methodTablePointer);
        }
        if (!ValidateMethodTablePointer(untrustedMethodTable))
        {
            throw new ArgumentException("Invalid method table pointer");
        }
        // ok, we trust it, cache the data
        Data.MethodTable trustedMethodTableData = _target.ProcessedData.GetOrAdd<Data.MethodTable>(methodTablePointer);
        MethodTable_1 trustedMethodTableF = new MethodTable_1(trustedMethodTableData);
        _ = _methodTables.TryAdd(methodTablePointer, trustedMethodTableF);
        return new MethodTableHandle(methodTablePointer);
    }

    private bool ValidateMethodTablePointer(NonValidatedMethodTable_1 umt)
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
        catch (System.Exception)
        {
            // FIXME: maybe don't swallow all exceptions?
            return false;
        }
        return true;
    }

    private bool ValidateWithPossibleAV(NonValidatedMethodTable_1 methodTable)
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
            NonValidatedEEClass_1 eeClass = GetUntrustedEEClassData(eeClassPtr);
            TargetPointer methodTablePtrFromClass = GetMethodTableWithPossibleAV(eeClass);
            if (methodTable.Address == methodTablePtrFromClass)
            {
                return true;
            }
            if (methodTable.Flags.HasInstantiation || methodTable.Flags.IsArray)
            {
                NonValidatedMethodTable_1 methodTableFromClass = GetUntrustedMethodTableData(methodTablePtrFromClass);
                TargetPointer classFromMethodTable = GetClassWithPossibleAV(methodTableFromClass);
                return classFromMethodTable == eeClassPtr;
            }
        }
        return false;
    }

    private bool ValidateMethodTable(NonValidatedMethodTable_1 methodTable)
    {
        if (!methodTable.Flags.IsInterface && !methodTable.Flags.IsString)
        {
            if (methodTable.Flags.BaseSize == 0 || !_target.IsAlignedToPointerSize(methodTable.Flags.BaseSize))
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
    private TargetPointer GetClassWithPossibleAV(NonValidatedMethodTable_1 methodTable)
    {
        TargetPointer eeClassOrCanonMT = methodTable.EEClassOrCanonMT;

        if (GetEEClassOrCanonMTBits(eeClassOrCanonMT) == EEClassOrCanonMTBits.EEClass)
        {
            return methodTable.EEClass;
        }
        else
        {
            TargetPointer canonicalMethodTablePtr = methodTable.CanonMT;
            NonValidatedMethodTable_1 umt = GetUntrustedMethodTableData(canonicalMethodTablePtr);
            return umt.EEClass;
        }
    }

    private static TargetPointer GetMethodTableWithPossibleAV(NonValidatedEEClass_1 eeClass) => eeClass.MethodTable;

    public uint GetBaseSize(MethodTableHandle methodTableHandle) => _methodTables[methodTableHandle.Address].Flags.BaseSize;

    private static uint GetComponentSize(MethodTable_1 methodTable)
    {
        return methodTable.Flags.HasComponentSize ? methodTable.Flags.RawGetComponentSize() : 0u;
    }
    public uint GetComponentSize(MethodTableHandle methodTableHandle) => GetComponentSize(_methodTables[methodTableHandle.Address]);

    // only called on trusted method tables, so we always trust the resulting EEClass
    private Data.EEClass GetClassData(MethodTableHandle methodTableHandle)
    {
        TargetPointer clsPtr = GetClass(methodTableHandle);
        // Check if we cached it already
        if (_target.ProcessedData.TryGet(clsPtr, out Data.EEClass? eeClassData))
        {
            return eeClassData;
        }
        eeClassData = _target.ProcessedData.GetOrAdd<Data.EEClass>(clsPtr);
        return eeClassData;
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
    public bool ContainsGCPointers(MethodTableHandle methodTableHandle) => _methodTables[methodTableHandle.Address].Flags.ContainsGCPointers;

    public uint GetTypeDefToken(MethodTableHandle methodTableHandle)
    {
        MethodTable_1 methodTable = _methodTables[methodTableHandle.Address];
        return (uint)(methodTable.Flags.GetTypeDefRid() | ((int)TableIndex.TypeDef << 24));
    }

    public ushort GetNumMethods(MethodTableHandle methodTableHandle) => GetClassData(methodTableHandle).NumMethods;

    public ushort GetNumInterfaces(MethodTableHandle methodTableHandle) => _methodTables[methodTableHandle.Address].NumInterfaces;

    public uint GetTypeDefTypeAttributes(MethodTableHandle methodTableHandle) => GetClassData(methodTableHandle).AttrClass;

    public bool IsDynamicStatics(MethodTableHandle methodTableHandle) => _methodTables[methodTableHandle.Address].Flags.GetFlag(WFLAGS2_ENUM.DynamicStatics) != 0;

}
