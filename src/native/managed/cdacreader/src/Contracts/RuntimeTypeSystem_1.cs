// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Diagnostics.DataContractReader.Data;
using Microsoft.Diagnostics.DataContractReader.Contracts.RuntimeTypeSystem_1_NS;
using System.Diagnostics;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal partial struct RuntimeTypeSystem_1 : IRuntimeTypeSystem
{
    private readonly Target _target;
    private readonly TargetPointer _freeObjectMethodTablePointer;
    private readonly ulong _methodDescAlignment;

    // TODO(cdac): we mutate this dictionary - copies of the RuntimeTypeSystem_1 struct share this instance.
    // If we need to invalidate our view of memory, we should clear this dictionary.
    private readonly Dictionary<TargetPointer, MethodTable> _methodTables = new();
    private readonly Dictionary<TargetPointer, MethodDesc> _methodDescs = new();


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

        // this MethodTable is a canonical MethodTable if its EEClassOrCanonMT is an EEClass
        internal bool IsCanonMT => GetEEClassOrCanonMTBits(EEClassOrCanonMT) == EEClassOrCanonMTBits.EEClass;
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

    // Low order bits of TypeHandle address.
    // If the low bits contain a 2, then it is a TypeDesc
    [Flags]
    internal enum TypeHandleBits
    {
        MethodTable = 0,
        TypeDesc = 2,
        ValidMask = 2,
    }

    [Flags]
    internal enum MethodDescFlags : ushort
    {
        HasNonVtableSlot = 0x0008,
    }

    internal struct MethodDesc
    {
        private readonly Data.MethodDesc _desc;
        private readonly Data.MethodDescChunk _chunk;
        internal TargetPointer Address { get; init; }
        internal MethodDesc(TargetPointer methodDescPointer, Data.MethodDesc desc, Data.MethodDescChunk chunk)
        {
            _desc = desc;
            _chunk = chunk;
            Address = methodDescPointer;
        }

        public TargetPointer MethodTable => _chunk.MethodTable;
        public ushort Slot => _desc.Slot;
    }

    internal RuntimeTypeSystem_1(Target target, TargetPointer freeObjectMethodTablePointer, ulong methodDescAlignment)
    {
        _target = target;
        _freeObjectMethodTablePointer = freeObjectMethodTablePointer;
        _methodDescAlignment = methodDescAlignment;
    }

    internal TargetPointer FreeObjectMethodTablePointer => _freeObjectMethodTablePointer;

    internal ulong MethodDescAlignment => _methodDescAlignment;

    public TypeHandle GetTypeHandle(TargetPointer typeHandlePointer)
    {
        TypeHandleBits addressLowBits = (TypeHandleBits)((ulong)typeHandlePointer & ((ulong)_target.PointerSize - 1));

        if ((addressLowBits != TypeHandleBits.MethodTable) && (addressLowBits != TypeHandleBits.TypeDesc))
        {
            throw new InvalidOperationException("Invalid type handle pointer");
        }

        // if we already validated this address, return a handle
        if (_methodTables.ContainsKey(typeHandlePointer))
        {
            return new TypeHandle(typeHandlePointer);
        }

        // Check for a TypeDesc
        if (addressLowBits == TypeHandleBits.TypeDesc)
        {
            // This is a TypeDesc
            return new TypeHandle(typeHandlePointer);
        }

        TargetPointer methodTablePointer = typeHandlePointer;

        // Check if we cached the underlying data already
        if (_target.ProcessedData.TryGet(methodTablePointer, out Data.MethodTable? methodTableData))
        {
            // we already cached the data, we must have validated the address, create the representation struct for our use
            MethodTable trustedMethodTable = new MethodTable(methodTableData);
            _ = _methodTables.TryAdd(methodTablePointer, trustedMethodTable);
            return new TypeHandle(methodTablePointer);
        }

        // If it's the free object method table, we trust it to be valid
        if (methodTablePointer == FreeObjectMethodTablePointer)
        {
            Data.MethodTable freeObjectMethodTableData = _target.ProcessedData.GetOrAdd<Data.MethodTable>(methodTablePointer);
            MethodTable trustedMethodTable = new MethodTable(freeObjectMethodTableData);
            _ = _methodTables.TryAdd(methodTablePointer, trustedMethodTable);
            return new TypeHandle(methodTablePointer);
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
        return new TypeHandle(methodTablePointer);
    }

    public uint GetBaseSize(TypeHandle typeHandle) => !typeHandle.IsMethodTable() ? (uint)0 : _methodTables[typeHandle.Address].Flags.BaseSize;

    public uint GetComponentSize(TypeHandle typeHandle) => !typeHandle.IsMethodTable() ? (uint)0 : _methodTables[typeHandle.Address].Flags.ComponentSize;

    private TargetPointer GetClassPointer(TypeHandle typeHandle)
    {
        MethodTable methodTable = _methodTables[typeHandle.Address];
        switch (GetEEClassOrCanonMTBits(methodTable.EEClassOrCanonMT))
        {
            case EEClassOrCanonMTBits.EEClass:
                return methodTable.EEClassOrCanonMT;
            case EEClassOrCanonMTBits.CanonMT:
                TargetPointer canonMTPtr = new TargetPointer((ulong)methodTable.EEClassOrCanonMT & ~(ulong)RuntimeTypeSystem_1.EEClassOrCanonMTBits.Mask);
                TypeHandle canonMTHandle = GetTypeHandle(canonMTPtr);
                MethodTable canonMT = _methodTables[canonMTHandle.Address];
                return canonMT.EEClassOrCanonMT; // canonical method table EEClassOrCanonMT is always EEClass
            default:
                throw new InvalidOperationException();
        }
    }

    // only called on validated method tables, so we don't need to re-validate the EEClass
    private Data.EEClass GetClassData(TypeHandle typeHandle)
    {
        TargetPointer clsPtr = GetClassPointer(typeHandle);
        return _target.ProcessedData.GetOrAdd<Data.EEClass>(clsPtr);
    }

    public TargetPointer GetCanonicalMethodTable(TypeHandle typeHandle) => !typeHandle.IsMethodTable() ? TargetPointer.Null : GetClassData(typeHandle).MethodTable;

    public TargetPointer GetModule(TypeHandle typeHandle)
    {
        if (typeHandle.IsMethodTable())
        {
            return _methodTables[typeHandle.Address].Module;
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
            else
            {
                System.Diagnostics.Debug.Assert(IsFunctionPointer(typeHandle, out _, out _));
                return TargetPointer.Null;
            }
        }
        else
        {
            return TargetPointer.Null;
        }
    }

    public TargetPointer GetParentMethodTable(TypeHandle typeHandle) => !typeHandle.IsMethodTable() ? TargetPointer.Null : _methodTables[typeHandle.Address].ParentMethodTable;

    public bool IsFreeObjectMethodTable(TypeHandle typeHandle) => FreeObjectMethodTablePointer == typeHandle.Address;

    public bool IsString(TypeHandle typeHandle) => !typeHandle.IsMethodTable() ? false : _methodTables[typeHandle.Address].Flags.IsString;
    public bool ContainsGCPointers(TypeHandle typeHandle) => !typeHandle.IsMethodTable() ? false : _methodTables[typeHandle.Address].Flags.ContainsGCPointers;

    public uint GetTypeDefToken(TypeHandle typeHandle)
    {
        if (!typeHandle.IsMethodTable())
            return 0;
        MethodTable methodTable = _methodTables[typeHandle.Address];
        return (uint)(methodTable.Flags.GetTypeDefRid() | ((int)TableIndex.TypeDef << 24));
    }

    public ushort GetNumMethods(TypeHandle typeHandle) => !typeHandle.IsMethodTable() ? (ushort)0 : GetClassData(typeHandle).NumMethods;

    public ushort GetNumInterfaces(TypeHandle typeHandle) => !typeHandle.IsMethodTable() ? (ushort)0 : _methodTables[typeHandle.Address].NumInterfaces;

    public uint GetTypeDefTypeAttributes(TypeHandle typeHandle) => !typeHandle.IsMethodTable() ? (uint)0 : GetClassData(typeHandle).CorTypeAttr;

    public bool IsDynamicStatics(TypeHandle typeHandle) => !typeHandle.IsMethodTable() ? false : _methodTables[typeHandle.Address].Flags.IsDynamicStatics;

    public ReadOnlySpan<TypeHandle> GetInstantiation(TypeHandle typeHandle)
    {
        if (!typeHandle.IsMethodTable())
            return default;

        MethodTable methodTable = _methodTables[typeHandle.Address];
        if (!methodTable.Flags.HasInstantiation)
            return default;

        return _target.ProcessedData.GetOrAdd<TypeInstantiation>(typeHandle.Address).TypeHandles;
    }

    private class TypeInstantiation : IData<TypeInstantiation>
    {
        public static TypeInstantiation Create(Target target, TargetPointer address) => new TypeInstantiation(target, address);

        public TypeHandle[] TypeHandles { get; }
        private TypeInstantiation(Target target, TargetPointer typePointer)
        {
            RuntimeTypeSystem_1 rts = (RuntimeTypeSystem_1)target.Contracts.RuntimeTypeSystem;
            MethodTable methodTable = rts._methodTables[typePointer];
            Debug.Assert(methodTable.Flags.HasInstantiation);

            TargetPointer perInstInfo = methodTable.PerInstInfo;
            TargetPointer genericsDictInfo = perInstInfo - (ulong)target.PointerSize;

            TargetPointer dictionaryPointer = target.ReadPointer(perInstInfo);


            int numberOfGenericArgs = target.ProcessedData.GetOrAdd<GenericsDictInfo>(genericsDictInfo).NumTypeArgs;

            TypeHandles = new TypeHandle[numberOfGenericArgs];
            for (int i = 0; i < numberOfGenericArgs; i++)
            {
                TypeHandles[i] = rts.GetTypeHandle(target.ReadPointer(dictionaryPointer + (ulong)target.PointerSize * (ulong)i));
            }
        }
    }

    public bool IsGenericTypeDefinition(TypeHandle typeHandle) => !typeHandle.IsMethodTable() ? false : _methodTables[typeHandle.Address].Flags.IsGenericTypeDefinition;

    public bool HasTypeParam(TypeHandle typeHandle)
    {
        if (typeHandle.IsMethodTable())
        {
            MethodTable methodTable = _methodTables[typeHandle.Address];
            return methodTable.Flags.IsArray;
        }
        else if (typeHandle.IsTypeDesc())
        {
            var typeDesc = _target.ProcessedData.GetOrAdd<TypeDesc>(typeHandle.TypeDescAddress());
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
        if (typeHandle.IsMethodTable())
        {
            MethodTable methodTable = _methodTables[typeHandle.Address];

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
                    return (CorElementType)GetClassData(typeHandle).InternalCorElementType;
                default:
                    return CorElementType.Class;
            }
        }
        else if (typeHandle.IsTypeDesc())
        {
            var typeDesc = _target.ProcessedData.GetOrAdd<TypeDesc>(typeHandle.TypeDescAddress());
            return (CorElementType)(typeDesc.TypeAndFlags & 0xFF);
        }

        return default;
    }

    // return true if the TypeHandle represents an array, and set the rank to either 0 (if the type is not an array), or the rank number if it is.
    public bool IsArray(TypeHandle typeHandle, out uint rank)
    {
        if (typeHandle.IsMethodTable())
        {
            MethodTable methodTable = _methodTables[typeHandle.Address];

            switch (methodTable.Flags.GetFlag(WFLAGS_HIGH.Category_Mask))
            {
                case WFLAGS_HIGH.Category_Array:
                    TargetPointer clsPtr = GetClassPointer(typeHandle);
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
        if (typeHandle.IsMethodTable())
        {
            MethodTable methodTable = _methodTables[typeHandle.Address];
            if (!methodTable.Flags.IsArray)
                throw new ArgumentException(nameof(typeHandle));

            return GetTypeHandle(methodTable.PerInstInfo);
        }
        else if (typeHandle.IsTypeDesc())
        {
            var typeDesc = _target.ProcessedData.GetOrAdd<TypeDesc>(typeHandle.TypeDescAddress());
            CorElementType elemType = (CorElementType)(typeDesc.TypeAndFlags & 0xFF);
            switch (elemType)
            {
                case CorElementType.ValueType:
                case CorElementType.Byref:
                case CorElementType.Ptr:
                    ParamTypeDesc paramTypeDesc = _target.ProcessedData.GetOrAdd<ParamTypeDesc>(typeHandle.TypeDescAddress());
                    return GetTypeHandle(paramTypeDesc.TypeArg);
            }
        }
        throw new ArgumentException(nameof(typeHandle));
    }

    public bool IsGenericVariable(TypeHandle typeHandle, out TargetPointer module, out uint token)
    {
        module = TargetPointer.Null;
        token = 0;

        if (!typeHandle.IsTypeDesc())
            return false;

        var typeDesc = _target.ProcessedData.GetOrAdd<TypeDesc>(typeHandle.TypeDescAddress());
        CorElementType elemType = (CorElementType)(typeDesc.TypeAndFlags & 0xFF);
        switch (elemType)
        {
            case CorElementType.MVar:
            case CorElementType.Var:
                TypeVarTypeDesc typeVarTypeDesc = _target.ProcessedData.GetOrAdd<TypeVarTypeDesc>(typeHandle.TypeDescAddress());
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

        if (!typeHandle.IsTypeDesc())
            return false;

        var typeDesc = _target.ProcessedData.GetOrAdd<TypeDesc>(typeHandle.TypeDescAddress());
        CorElementType elemType = (CorElementType)(typeDesc.TypeAndFlags & 0xFF);
        if (elemType != CorElementType.FnPtr)
            return false;

        FnPtrTypeDesc fnPtrTypeDesc = _target.ProcessedData.GetOrAdd<FnPtrTypeDesc>(typeHandle.TypeDescAddress());
        retAndArgTypes = _target.ProcessedData.GetOrAdd<FunctionPointerRetAndArgs>(typeHandle.TypeDescAddress()).TypeHandles;
        callConv = (byte)fnPtrTypeDesc.CallConv;
        return true;
    }

    private class FunctionPointerRetAndArgs : IData<FunctionPointerRetAndArgs>
    {
        public static FunctionPointerRetAndArgs Create(Target target, TargetPointer address) => new FunctionPointerRetAndArgs(target, address);

        public TypeHandle[] TypeHandles { get; }
        private FunctionPointerRetAndArgs(Target target, TargetPointer typePointer)
        {
            RuntimeTypeSystem_1 rts = (RuntimeTypeSystem_1)target.Contracts.RuntimeTypeSystem;
            FnPtrTypeDesc fnPtrTypeDesc = target.ProcessedData.GetOrAdd<FnPtrTypeDesc>(typePointer);

            TargetPointer retAndArgs = fnPtrTypeDesc.RetAndArgTypes;
            int numberOfRetAndArgTypes = checked((int)fnPtrTypeDesc.NumArgs + 1);

            TypeHandles = new TypeHandle[numberOfRetAndArgTypes];
            for (int i = 0; i < numberOfRetAndArgTypes; i++)
            {
                TypeHandles[i] = rts.GetTypeHandle(target.ReadPointer(retAndArgs + (ulong)target.PointerSize * (ulong)i));
            }
        }
    }

    internal ushort GetNumVtableSlots(TypeHandle typeHandle)
    {
        if (!typeHandle.IsMethodTable())
            return 0;
        MethodTable methodTable = _methodTables[typeHandle.Address];
        ushort numNonVirtualSlots = methodTable.IsCanonMT ? GetClassData(typeHandle).NumNonVirtualSlots : (ushort)0;
        return checked((ushort)(methodTable.NumVirtuals + numNonVirtualSlots));
    }

    public MethodDescHandle GetMethodDescHandle(TargetPointer methodDescPointer)
    {
        // if we already validated this address, return a handle
        if (_methodDescs.ContainsKey(methodDescPointer))
        {
            return new MethodDescHandle(methodDescPointer);
        }
        // Check if we cached the underlying data already
        if (_target.ProcessedData.TryGet(methodDescPointer, out Data.MethodDesc? methodDescData))
        {
            // we already cached the data, we must have validated the address, create the representation struct for our use
            TargetPointer mdescChunkPtr = GetMethodDescChunkPointerThrowing(methodDescPointer, methodDescData);
            // FIXME[cdac]: this isn't threadsafe
            if (!_target.ProcessedData.TryGet(mdescChunkPtr, out Data.MethodDescChunk? methodDescChunkData))
            {
                throw new InvalidOperationException("cached MethodDesc data but not its containing MethodDescChunk");
            }
            MethodDesc validatedMethodDesc = new MethodDesc(methodDescPointer, methodDescData, methodDescChunkData);
            _ = _methodDescs.TryAdd(methodDescPointer, validatedMethodDesc);
            return new MethodDescHandle(methodDescPointer);
        }

        if (!ValidateMethodDescPointer(methodDescPointer, out TargetPointer methodDescChunkPointer))
        {
            throw new InvalidOperationException("Invalid method desc pointer");
        }

        // ok, we validated it, cache the data and add the MethodDesc struct to the dictionary
        Data.MethodDescChunk validatedMethodDescChunkData = _target.ProcessedData.GetOrAdd<Data.MethodDescChunk>(methodDescChunkPointer);
        Data.MethodDesc validatedMethodDescData = _target.ProcessedData.GetOrAdd<Data.MethodDesc>(methodDescPointer);

        MethodDesc trustedMethodDescF = new MethodDesc(methodDescPointer, validatedMethodDescData, validatedMethodDescChunkData);
        _ = _methodDescs.TryAdd(methodDescPointer, trustedMethodDescF);
        return new MethodDescHandle(methodDescPointer);
    }

    public TargetPointer GetMethodTable(MethodDescHandle methodDescHandle) => _methodDescs[methodDescHandle.Address].MethodTable;
}
