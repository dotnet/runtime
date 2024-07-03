// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;



internal partial struct RuntimeTypeSystem_1 : IRuntimeTypeSystem
{
    private readonly Target _target;
    private readonly TargetPointer _freeObjectMethodTablePointer;

    // TODO(cdac): we mutate this dictionary - copies of the RuntimeTypeSystem_1 struct share this instance.
    // If we need to invalidate our view of memory, we shoudl clear this dictionary.
    private readonly Dictionary<TargetPointer, MethodTable> _methodTables = new();


    internal struct MethodTable
    {
        internal MethodTableFlags Flags { get; }
        internal ushort NumInterfaces { get; }
        internal ushort NumVirtuals { get; }
        internal TargetPointer ParentMethodTable { get; }
        internal TargetPointer Module { get; }
        internal TargetPointer EEClassOrCanonMT { get; }
        internal MethodTable(Data.MethodTable data)
        {
            Flags = new MethodTableFlags
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

    // Low order bit of EEClassOrCanonMT.
    // See MethodTable::LowBits UNION_EECLASS / UNION_METHODABLE
    [Flags]
    internal enum EEClassOrCanonMTBits
    {
        EEClass = 0,
        CanonMT = 1,
        Mask = 1,
    }

    internal RuntimeTypeSystem_1(Target target, TargetPointer freeObjectMethodTablePointer)
    {
        _target = target;
        _freeObjectMethodTablePointer = freeObjectMethodTablePointer;
    }

    internal TargetPointer FreeObjectMethodTablePointer => _freeObjectMethodTablePointer;


    public MethodTableHandle GetMethodTableHandle(TargetPointer methodTablePointer)
    {
        // if we already validated this address, return a handle
        if (_methodTables.ContainsKey(methodTablePointer))
        {
            return new MethodTableHandle(methodTablePointer);
        }
        // Check if we cached the underlying data already
        if (_target.ProcessedData.TryGet(methodTablePointer, out Data.MethodTable? methodTableData))
        {
            // we already cached the data, we must have validated the address, create the representation struct for our use
            MethodTable trustedMethodTable = new MethodTable(methodTableData);
            _ = _methodTables.TryAdd(methodTablePointer, trustedMethodTable);
            return new MethodTableHandle(methodTablePointer);
        }

        // If it's the free object method table, we trust it to be valid
        if (methodTablePointer == FreeObjectMethodTablePointer)
        {
            Data.MethodTable freeObjectMethodTableData = _target.ProcessedData.GetOrAdd<Data.MethodTable>(methodTablePointer);
            MethodTable trustedMethodTable = new MethodTable(freeObjectMethodTableData);
            _ = _methodTables.TryAdd(methodTablePointer, trustedMethodTable);
            return new MethodTableHandle(methodTablePointer);
        }

        // Otherwse, get ready to validate
        NonValidated.MethodTable nonvalidatedMethodTable = NonValidated.GetMethodTableData(_target, methodTablePointer);

        if (!ValidateMethodTablePointer(nonvalidatedMethodTable))
        {
            throw new InvalidOperationException("Invalid method table pointer");
        }
        // ok, we validated it, cache the data and add the MethodTable_1 struct to the dictionary
        Data.MethodTable trustedMethodTableData = _target.ProcessedData.GetOrAdd<Data.MethodTable>(methodTablePointer);
        MethodTable trustedMethodTableF = new MethodTable(trustedMethodTableData);
        _ = _methodTables.TryAdd(methodTablePointer, trustedMethodTableF);
        return new MethodTableHandle(methodTablePointer);
    }


    public uint GetBaseSize(MethodTableHandle methodTableHandle) => _methodTables[methodTableHandle.Address].Flags.BaseSize;

    public uint GetComponentSize(MethodTableHandle methodTableHandle) => _methodTables[methodTableHandle.Address].Flags.ComponentSize;

    private TargetPointer GetClassPointer(MethodTableHandle methodTableHandle)
    {
        MethodTable methodTable = _methodTables[methodTableHandle.Address];
        switch (GetEEClassOrCanonMTBits(methodTable.EEClassOrCanonMT))
        {
            case EEClassOrCanonMTBits.EEClass:
                return methodTable.EEClassOrCanonMT;
            case EEClassOrCanonMTBits.CanonMT:
                TargetPointer canonMTPtr = new TargetPointer((ulong)methodTable.EEClassOrCanonMT & ~(ulong)RuntimeTypeSystem_1.EEClassOrCanonMTBits.Mask);
                MethodTableHandle canonMTHandle = GetMethodTableHandle(canonMTPtr);
                MethodTable canonMT = _methodTables[canonMTHandle.Address];
                return canonMT.EEClassOrCanonMT; // canonical method table EEClassOrCanonMT is always EEClass
            default:
                throw new InvalidOperationException();
        }
    }

    // only called on validated method tables, so we don't need to re-validate the EEClass
    private Data.EEClass GetClassData(MethodTableHandle methodTableHandle)
    {
        TargetPointer clsPtr = GetClassPointer(methodTableHandle);
        return _target.ProcessedData.GetOrAdd<Data.EEClass>(clsPtr);
    }

    public TargetPointer GetCanonicalMethodTable(MethodTableHandle methodTableHandle) => GetClassData(methodTableHandle).MethodTable;

    public TargetPointer GetModule(MethodTableHandle methodTableHandle) => _methodTables[methodTableHandle.Address].Module;
    public TargetPointer GetParentMethodTable(MethodTableHandle methodTableHandle) => _methodTables[methodTableHandle.Address].ParentMethodTable;

    public bool IsFreeObjectMethodTable(MethodTableHandle methodTableHandle) => FreeObjectMethodTablePointer == methodTableHandle.Address;

    public bool IsString(MethodTableHandle methodTableHandle) => _methodTables[methodTableHandle.Address].Flags.IsString;
    public bool ContainsGCPointers(MethodTableHandle methodTableHandle) => _methodTables[methodTableHandle.Address].Flags.ContainsGCPointers;

    public uint GetTypeDefToken(MethodTableHandle methodTableHandle)
    {
        MethodTable methodTable = _methodTables[methodTableHandle.Address];
        return (uint)(methodTable.Flags.GetTypeDefRid() | ((int)TableIndex.TypeDef << 24));
    }

    public ushort GetNumMethods(MethodTableHandle methodTableHandle) => GetClassData(methodTableHandle).NumMethods;

    public ushort GetNumInterfaces(MethodTableHandle methodTableHandle) => _methodTables[methodTableHandle.Address].NumInterfaces;

    public uint GetTypeDefTypeAttributes(MethodTableHandle methodTableHandle) => GetClassData(methodTableHandle).CorTypeAttr;

    public bool IsDynamicStatics(MethodTableHandle methodTableHandle) => _methodTables[methodTableHandle.Address].Flags.IsDynamicStatics;

}
