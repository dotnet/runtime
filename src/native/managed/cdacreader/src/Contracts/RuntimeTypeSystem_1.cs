// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;



internal partial struct RuntimeTypeSystem_1 : IRuntimeTypeSystem
{
    private readonly Target _target;
    private readonly TargetPointer _freeObjectMethodTablePointer;

    // TODO(cdac): we mutate this dictionary - copies of the RuntimeTypeSystem_1 struct share this instance.
    // If we need to invalidate our view of memory, we should clear this dictionary.
    private readonly Dictionary<TargetPointer, MethodTable> _methodTables = new();


    internal struct MethodTable
    {
        internal MethodTableFlags Flags { get; }
        internal ushort NumInterfaces { get; }
        internal ushort NumVirtuals { get; }
        internal TargetPointer ParentMethodTable { get; }
        internal TargetPointer Module { get; }
        internal TargetPointer EEClassOrCanonMT { get; }
        internal TargetPointer PerInstInfo { get; }
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
            PerInstInfo = data.PerInstInfo;
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

    public ReadOnlySpan<MethodTableHandle> GetInstantiation(MethodTableHandle methodTableHandle)
    {
        MethodTable methodTable = _methodTables[methodTableHandle.Address];
        if (!methodTable.Flags.HasInstantiation)
            return default;

        TargetPointer perInstInfo = methodTable.PerInstInfo;
        var typeInfo = _target.GetTypeInfo(DataType.pointer);
        uint? size = typeInfo.Size;
        TargetPointer genericsDictInfo = _target.ReadPointer(perInstInfo - size!.Value);

        TargetPointer dictionaryPointer = _target.ReadPointer(perInstInfo);

        int numberOfGenericArgs = _target.ProcessedData.GetOrAdd<GenericsDictInfo>(genericsDictInfo).NumTypeArgs;
        MethodTableArray instantiation = _target.ProcessedData.GetOrAdd<(TargetPointer, int), MethodTableArray>
            ((dictionaryPointer, numberOfGenericArgs));

        return instantiation.Types.AsSpan();
    }

    public bool IsGenericTypeDefinition(MethodTableHandle methodTableHandle) => _methodTables[methodTableHandle.Address].Flags.IsGenericTypeDefinition;

    TypeHandle IRuntimeTypeSystem.TypeHandleFromAddress(TargetPointer address)
    {
        return TypeHandleFromAddress(address);
    }

    private static TypeHandle TypeHandleFromAddress(TargetPointer address)
    {
        if (address == 0)
            return default;

        if (((ulong)address & 2) == (ulong)2)
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
            var typeDesc = _target.ProcessedData.GetOrAdd<TypeDesc>(typeHandle.AsTypeDesc.Address);
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
            var typeDesc = _target.ProcessedData.GetOrAdd<TypeDesc>(typeHandle.AsTypeDesc.Address);
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
                    rank = _target.ProcessedData.GetOrAdd<Data.ArrayClass>(clsPtr).Rank;
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
            if (!methodTable.Flags.IsArray)
                throw new ArgumentException(nameof(typeHandle));

            return TypeHandleFromAddress(methodTable.PerInstInfo);
        }
        else if (typeHandle.IsTypeDesc)
        {
            var typeDesc = _target.ProcessedData.GetOrAdd<TypeDesc>(typeHandle.AsTypeDesc.Address);
            CorElementType elemType = (CorElementType)(typeDesc.TypeAndFlags & 0xFF);
            switch (elemType)
            {
                case CorElementType.ValueType:
                case CorElementType.Byref:
                case CorElementType.Ptr:
                    ParamTypeDesc paramTypeDesc = _target.ProcessedData.GetOrAdd<ParamTypeDesc>(typeHandle.AsTypeDesc.Address);
                    return TypeHandleFromAddress(paramTypeDesc.TypeArg);
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

        var typeDesc = _target.ProcessedData.GetOrAdd<TypeDesc>(typeHandle.AsTypeDesc.Address);
        CorElementType elemType = (CorElementType)(typeDesc.TypeAndFlags & 0xFF);
        switch (elemType)
        {
            case CorElementType.MVar:
            case CorElementType.Var:
                TypeVarTypeDesc typeVarTypeDesc = _target.ProcessedData.GetOrAdd<TypeVarTypeDesc>(typeHandle.AsTypeDesc.Address);
                module = typeVarTypeDesc.Module;
                token = typeVarTypeDesc.Token;
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

        var typeDesc = _target.ProcessedData.GetOrAdd<TypeDesc>(typeHandle.AsTypeDesc.Address);
        CorElementType elemType = (CorElementType)(typeDesc.TypeAndFlags & 0xFF);
        if (elemType != CorElementType.FnPtr)
            return false;

        FnPtrTypeDesc fnPtrTypeDesc = _target.ProcessedData.GetOrAdd<FnPtrTypeDesc>(typeHandle.AsTypeDesc.Address);

        TypeHandleArray retAndArgTypesArray = _target.ProcessedData.GetOrAdd<(TargetPointer, int), TypeHandleArray>
            ((fnPtrTypeDesc.RetAndArgTypes, checked((int)fnPtrTypeDesc.NumArgs + 1)));
        retAndArgTypes = retAndArgTypesArray.Types;
        callConv = (byte)fnPtrTypeDesc.CallConv;
        return true;
    }
}
