// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal partial struct RuntimeTypeSystem_1 : IRuntimeTypeSystem
{
    private readonly Target _target;
    private readonly TargetPointer _freeObjectMethodTablePointer;
    private readonly ulong _methodDescAlignment;
    private readonly TypeValidation _typeValidation;
    private readonly MethodValidation _methodValidation;

    // TODO(cdac): we mutate this dictionary - copies of the RuntimeTypeSystem_1 struct share this instance.
    // If we need to invalidate our view of memory, we should clear this dictionary.
    private readonly Dictionary<TargetPointer, MethodTable> _methodTables = new();
    private readonly Dictionary<TargetPointer, MethodDesc> _methodDescs = new();

    internal struct MethodTable
    {
        internal MethodTableFlags_1 Flags { get; }
        internal ushort NumInterfaces { get; }
        internal ushort NumVirtuals { get; }
        internal TargetPointer ParentMethodTable { get; }
        internal TargetPointer Module { get; }
        internal TargetPointer EEClassOrCanonMT { get; }
        internal TargetPointer PerInstInfo { get; }
        internal TargetPointer AuxiliaryData { get; }
        internal MethodTable(Data.MethodTable data)
        {
            Flags = new MethodTableFlags_1
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
            AuxiliaryData = data.AuxiliaryData;
        }

        // this MethodTable is a canonical MethodTable if its EEClassOrCanonMT is an EEClass
        internal bool IsCanonMT => MethodTableFlags_1.GetEEClassOrCanonMTBits(EEClassOrCanonMT) == MethodTableFlags_1.EEClassOrCanonMTBits.EEClass;
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

    // on MethodDescChunk.FlagsAndTokenRange
    [Flags]
    internal enum MethodDescChunkFlags : ushort
    {
        // Has this chunk had its methods been determined eligible for tiered compilation or not
        DeterminedIsEligibleForTieredCompilation = 0x4000,
        // Is this chunk associated with a LoaderModule directly? If this flag is set, then the LoaderModule pointer is placed at the end of the chunk.
        LoaderModuleAttachedToChunk              = 0x8000,
    }

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

            Token = ComputeToken(target, desc, chunk);
        }

        public TargetPointer MethodTable => _chunk.MethodTable;
        public ushort Slot => _desc.Slot;
        public uint Token { get; }

        private static uint ComputeToken(Target target, Data.MethodDesc desc, Data.MethodDescChunk chunk)
        {
            int tokenRemainderBitCount = target.ReadGlobal<byte>(Constants.Globals.MethodDescTokenRemainderBitCount);
            int tokenRangeBitCount = EcmaMetadataUtils.RowIdBitCount - tokenRemainderBitCount;
            uint allRidBitsSet = EcmaMetadataUtils.RIDMask;
            uint tokenRemainderMask = allRidBitsSet >> tokenRangeBitCount;
            uint tokenRangeMask = allRidBitsSet >> tokenRemainderBitCount;

            uint tokenRemainder = (uint)(desc.Flags3AndTokenRemainder & tokenRemainderMask);
            uint tokenRange = ((uint)(chunk.FlagsAndTokenRange & tokenRangeMask)) << tokenRemainderBitCount;
            return EcmaMetadataUtils.CreateMethodDef(tokenRange | tokenRemainder);
        }

        public MethodClassification Classification => (MethodClassification)((int)_desc.Flags & (int)MethodDescFlags_1.MethodDescFlags.ClassificationMask);

        private bool HasFlags(MethodDescFlags_1.MethodDescFlags flags) => (_desc.Flags & (ushort)flags) != 0;
        internal bool HasFlags(MethodDescFlags_1.MethodDescFlags3 flags) => (_desc.Flags3AndTokenRemainder & (ushort)flags) != 0;

        internal bool HasFlags(MethodDescChunkFlags flags) => (_chunk.FlagsAndTokenRange & (ushort)flags) != 0;

        public bool IsEligibleForTieredCompilation => HasFlags(MethodDescFlags_1.MethodDescFlags3.IsEligibleForTieredCompilation);


        public bool IsUnboxingStub => HasFlags(MethodDescFlags_1.MethodDescFlags3.IsUnboxingStub);

        public TargetPointer CodeData => _desc.CodeData;

        public TargetPointer? GCCoverageInfo => _desc.GCCoverageInfo;

        public bool IsIL => Classification == MethodClassification.IL || Classification == MethodClassification.Instantiated;

        internal bool HasNonVtableSlot => MethodDescOptionalSlots.HasNonVtableSlot(_desc.Flags);
        internal bool HasNativeCodeSlot => MethodDescOptionalSlots.HasNativeCodeSlot(_desc.Flags);

        internal bool HasStableEntryPoint => HasFlags(MethodDescFlags_1.MethodDescFlags3.HasStableEntryPoint);
        internal bool HasPrecode => HasFlags(MethodDescFlags_1.MethodDescFlags3.HasPrecode);

        internal TargetPointer GetAddressOfNonVtableSlot() => MethodDescOptionalSlots.GetAddressOfNonVtableSlot(Address, Classification, _desc.Flags, _target);
        internal TargetPointer GetAddressOfNativeCodeSlot() => MethodDescOptionalSlots.GetAddressOfNativeCodeSlot(Address, Classification, _desc.Flags, _target);

        internal bool IsLoaderModuleAttachedToChunk => HasFlags(MethodDescChunkFlags.LoaderModuleAttachedToChunk);

        public ulong SizeOfChunk
        {
            get
            {
                ulong typeSize = _target.GetTypeInfo(DataType.MethodDescChunk).Size!.Value;
                ulong chunkSize = (ulong)(_chunk.Size + 1) * _target.ReadGlobal<ulong>(Constants.Globals.MethodDescAlignment);
                ulong extra = IsLoaderModuleAttachedToChunk ? (ulong)_target.PointerSize : 0;
                return typeSize + chunkSize + extra;
            }
        }

    }

    private sealed class InstantiatedMethodDesc : IData<InstantiatedMethodDesc>
    {
        public static InstantiatedMethodDesc Create(Target target, TargetPointer address) => new InstantiatedMethodDesc(target, address);

        private readonly TargetPointer _address;
        private readonly Data.InstantiatedMethodDesc _desc;

        private InstantiatedMethodDesc(Target target, TargetPointer methodDescPointer)
        {
            _address = methodDescPointer;
            RuntimeTypeSystem_1 rts = (RuntimeTypeSystem_1)target.Contracts.RuntimeTypeSystem;
            _desc = target.ProcessedData.GetOrAdd<Data.InstantiatedMethodDesc>(methodDescPointer);

            int numGenericArgs = _desc.NumGenericArgs;
            TargetPointer perInstInfo = _desc.PerInstInfo;
            if ((perInstInfo == TargetPointer.Null) || (numGenericArgs == 0))
            {
                Instantiation = System.Array.Empty<TypeHandle>();
            }
            else
            {
                Instantiation = new TypeHandle[numGenericArgs];
                for (int i = 0; i < numGenericArgs; i++)
                {
                    Instantiation[i] = rts.GetTypeHandle(target.ReadPointer(perInstInfo + (ulong)target.PointerSize * (ulong)i));
                }
            }
        }

        private bool HasFlags(InstantiatedMethodDescFlags2 mask, InstantiatedMethodDescFlags2 flags) => (_desc.Flags2 & (ushort)mask) == (ushort)flags;
        internal bool IsWrapperStubWithInstantiations => HasFlags(InstantiatedMethodDescFlags2.KindMask, InstantiatedMethodDescFlags2.WrapperStubWithInstantiations);
        internal bool IsGenericMethodDefinition => HasFlags(InstantiatedMethodDescFlags2.KindMask, InstantiatedMethodDescFlags2.GenericMethodDefinition);
        internal bool HasPerInstInfo => _desc.PerInstInfo != TargetPointer.Null;
        internal bool HasMethodInstantiation => IsGenericMethodDefinition || HasPerInstInfo;
        public TypeHandle[] Instantiation { get; }
    }

    private sealed class DynamicMethodDesc : IData<DynamicMethodDesc>
    {
        public static DynamicMethodDesc Create(Target target, TargetPointer address) => new DynamicMethodDesc(target, address);

        private readonly TargetPointer _address;
        private readonly Data.DynamicMethodDesc _desc;
        private readonly Data.StoredSigMethodDesc _storedSigDesc;

        private DynamicMethodDesc(Target target, TargetPointer methodDescPointer)
        {
            _address = methodDescPointer;
            _desc = target.ProcessedData.GetOrAdd<Data.DynamicMethodDesc>(methodDescPointer);

            MethodName = _desc.MethodName != TargetPointer.Null
                ? target.ReadUtf8String(_desc.MethodName)
                : string.Empty;

            _storedSigDesc = target.ProcessedData.GetOrAdd<Data.StoredSigMethodDesc>(methodDescPointer);
        }

        public string MethodName { get; }
        public DynamicMethodDescExtendedFlags ExtendedFlags => (DynamicMethodDescExtendedFlags)_storedSigDesc.ExtendedFlags;

        public bool IsDynamicMethod => ExtendedFlags.HasFlag(DynamicMethodDescExtendedFlags.IsLCGMethod);
        public bool IsILStub => ExtendedFlags.HasFlag(DynamicMethodDescExtendedFlags.IsILStub);
    }

    private sealed class StoredSigMethodDesc : IData<StoredSigMethodDesc>
    {
        public static StoredSigMethodDesc Create(Target target, TargetPointer address) => new StoredSigMethodDesc(target, address);

        public byte[] Signature { get; }
        private StoredSigMethodDesc(Target target, TargetPointer methodDescPointer)
        {
            Data.StoredSigMethodDesc storedSigMethodDesc = target.ProcessedData.GetOrAdd<Data.StoredSigMethodDesc>(methodDescPointer);
            Signature = new byte[storedSigMethodDesc.cSig];
            target.ReadBuffer(storedSigMethodDesc.Sig, Signature.AsSpan());
        }
    }

    internal RuntimeTypeSystem_1(Target target, TypeValidation typeValidation, MethodValidation methodValidation, TargetPointer freeObjectMethodTablePointer, ulong methodDescAlignment)
    {
        _target = target;
        _freeObjectMethodTablePointer = freeObjectMethodTablePointer;
        _methodDescAlignment = methodDescAlignment;
        _typeValidation = typeValidation;
        _methodValidation = methodValidation;
        _methodValidation.SetMethodTableQueries(new NonValidatedMethodTableQueries(this));
    }

    internal TargetPointer FreeObjectMethodTablePointer => _freeObjectMethodTablePointer;

    internal ulong MethodDescAlignment => _methodDescAlignment;

    public TypeHandle GetTypeHandle(TargetPointer typeHandlePointer)
    {
        TypeHandleBits addressLowBits = (TypeHandleBits)((ulong)typeHandlePointer & ((ulong)_target.PointerSize - 1));

        if ((addressLowBits != TypeHandleBits.MethodTable) && (addressLowBits != TypeHandleBits.TypeDesc))
        {
            throw new ArgumentException("Invalid type handle pointer", nameof(typeHandlePointer));
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
        if (!_typeValidation.TryValidateMethodTablePointer(methodTablePointer))
        {
            throw new ArgumentException("Invalid method table pointer", nameof(typeHandlePointer));
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
        switch (MethodTableFlags_1.GetEEClassOrCanonMTBits(methodTable.EEClassOrCanonMT))
        {
            case MethodTableFlags_1.EEClassOrCanonMTBits.EEClass:
                return methodTable.EEClassOrCanonMT;
            case MethodTableFlags_1.EEClassOrCanonMTBits.CanonMT:
                TargetPointer canonMTPtr =MethodTableFlags_1.UntagEEClassOrCanonMT(methodTable.EEClassOrCanonMT);
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

    private sealed class TypeInstantiation : IData<TypeInstantiation>
    {
        public static TypeInstantiation Create(Target target, TargetPointer address) => new TypeInstantiation(target, address);

        public TypeHandle[] TypeHandles { get; }
        private TypeInstantiation(Target target, TargetPointer typePointer)
        {
            RuntimeTypeSystem_1 rts = (RuntimeTypeSystem_1)target.Contracts.RuntimeTypeSystem;
            MethodTable methodTable = rts._methodTables[typePointer];
            Debug.Assert(methodTable.Flags.HasInstantiation);

            TargetPointer perInstInfo = methodTable.PerInstInfo;
            TargetPointer genericsDictInfoAddr = perInstInfo - (ulong)target.PointerSize;
            GenericsDictInfo genericsDictInfo = target.ProcessedData.GetOrAdd<GenericsDictInfo>(genericsDictInfoAddr);

            // Use the last dictionary. This corresponds to the specific type - any previous ones are for superclasses
            // See PerInstInfo in methodtable.h for details in coreclr
            TargetPointer dictionaryPointer = target.ReadPointer(perInstInfo + (ulong)target.PointerSize * (ulong)(genericsDictInfo.NumDicts - 1));

            int numberOfGenericArgs = genericsDictInfo.NumTypeArgs;
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

            switch (methodTable.Flags.GetFlag(MethodTableFlags_1.WFLAGS_HIGH.Category_Mask))
            {
                case MethodTableFlags_1.WFLAGS_HIGH.Category_Array:
                    return CorElementType.Array;
                case MethodTableFlags_1.WFLAGS_HIGH.Category_Array | MethodTableFlags_1.WFLAGS_HIGH.Category_IfArrayThenSzArray:
                    return CorElementType.SzArray;
                case MethodTableFlags_1.WFLAGS_HIGH.Category_ValueType:
                case MethodTableFlags_1.WFLAGS_HIGH.Category_Nullable:
                case MethodTableFlags_1.WFLAGS_HIGH.Category_PrimitiveValueType:
                    return CorElementType.ValueType;
                case MethodTableFlags_1.WFLAGS_HIGH.Category_TruePrimitive:
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

            switch (methodTable.Flags.GetFlag(MethodTableFlags_1.WFLAGS_HIGH.Category_Mask))
            {
                case MethodTableFlags_1.WFLAGS_HIGH.Category_Array:
                    TargetPointer clsPtr = GetClassPointer(typeHandle);
                    rank = _target.ProcessedData.GetOrAdd<Data.ArrayClass>(clsPtr).Rank;
                    return true;

                case MethodTableFlags_1.WFLAGS_HIGH.Category_Array | MethodTableFlags_1.WFLAGS_HIGH.Category_IfArrayThenSzArray:
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

    private sealed class FunctionPointerRetAndArgs : IData<FunctionPointerRetAndArgs>
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

    private ushort GetNumVtableSlots(TypeHandle typeHandle)
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
            TargetPointer mdescChunkPtr = _methodValidation.GetMethodDescChunkPointerThrowing(methodDescPointer, methodDescData);
            // FIXME[cdac]: this isn't threadsafe
            if (!_target.ProcessedData.TryGet(mdescChunkPtr, out Data.MethodDescChunk? methodDescChunkData))
            {
                throw new InvalidOperationException("cached MethodDesc data but not its containing MethodDescChunk");
            }
            MethodDesc validatedMethodDesc = new MethodDesc(_target, methodDescPointer, methodDescData, mdescChunkPtr, methodDescChunkData);
            _ = _methodDescs.TryAdd(methodDescPointer, validatedMethodDesc);
            return new MethodDescHandle(methodDescPointer);
        }

        if (!_methodValidation.ValidateMethodDescPointer(methodDescPointer, out TargetPointer methodDescChunkPointer))
        {
            throw new ArgumentException("Invalid method desc pointer", nameof(methodDescPointer));
        }

        // ok, we validated it, cache the data and add the MethodDesc struct to the dictionary
        Data.MethodDescChunk validatedMethodDescChunkData = _target.ProcessedData.GetOrAdd<Data.MethodDescChunk>(methodDescChunkPointer);
        Data.MethodDesc validatedMethodDescData = _target.ProcessedData.GetOrAdd<Data.MethodDesc>(methodDescPointer);

        MethodDesc trustedMethodDescF = new MethodDesc(_target, methodDescPointer, validatedMethodDescData, methodDescChunkPointer, validatedMethodDescChunkData);
        _ = _methodDescs.TryAdd(methodDescPointer, trustedMethodDescF);
        return new MethodDescHandle(methodDescPointer);
    }

    public TargetPointer GetMethodTable(MethodDescHandle methodDescHandle) => _methodDescs[methodDescHandle.Address].MethodTable;

    private InstantiatedMethodDesc AsInstantiatedMethodDesc(MethodDesc methodDesc)
    {
        Debug.Assert(methodDesc.Classification == MethodClassification.Instantiated);
        return _target.ProcessedData.GetOrAdd<InstantiatedMethodDesc>(methodDesc.Address);
    }

    private DynamicMethodDesc AsDynamicMethodDesc(MethodDesc methodDesc)
    {
        Debug.Assert(methodDesc.Classification == MethodClassification.Dynamic);
        return _target.ProcessedData.GetOrAdd<DynamicMethodDesc>(methodDesc.Address);
    }

    private StoredSigMethodDesc AsStoredSigMethodDesc(MethodDesc methodDesc)
    {
        Debug.Assert(methodDesc.Classification == MethodClassification.Dynamic ||
                     methodDesc.Classification == MethodClassification.EEImpl ||
                     methodDesc.Classification == MethodClassification.Array);
        return _target.ProcessedData.GetOrAdd<StoredSigMethodDesc>(methodDesc.Address);
    }

    public bool IsGenericMethodDefinition(MethodDescHandle methodDescHandle)
    {
        MethodDesc methodDesc = _methodDescs[methodDescHandle.Address];

        if (methodDesc.Classification != MethodClassification.Instantiated)
            return false;
        return AsInstantiatedMethodDesc(methodDesc).IsGenericMethodDefinition;
    }

    public ReadOnlySpan<TypeHandle> GetGenericMethodInstantiation(MethodDescHandle methodDescHandle)
    {
        MethodDesc methodDesc = _methodDescs[methodDescHandle.Address];

        if (methodDesc.Classification != MethodClassification.Instantiated)
            return default;

        return AsInstantiatedMethodDesc(methodDesc).Instantiation;
    }

    public uint GetMethodToken(MethodDescHandle methodDescHandle)
    {
        MethodDesc methodDesc = _methodDescs[methodDescHandle.Address];
        return methodDesc.Token;
    }

    public bool IsArrayMethod(MethodDescHandle methodDescHandle, out ArrayFunctionType functionType)
    {
        MethodDesc methodDesc = _methodDescs[methodDescHandle.Address];

        if (methodDesc.Classification != MethodClassification.Array)
        {
            functionType = default;
            return false;
        }

        // To get the array function index, subtract the number of virtuals from the method's slot
        // The array vtable looks like:
        //    System.Object vtable
        //    System.Array vtable
        //    type[] vtable
        //    Get
        //    Set
        //    Address
        //    .ctor        // possibly more
        // See ArrayMethodDesc for details in coreclr
        MethodTable methodTable = GetOrCreateMethodTable(methodDesc);
        int arrayMethodIndex = methodDesc.Slot - methodTable.NumVirtuals;
        functionType = arrayMethodIndex switch
        {
            0 => ArrayFunctionType.Get,
            1 => ArrayFunctionType.Set,
            2 => ArrayFunctionType.Address,
            >= 3 => ArrayFunctionType.Constructor,
            _ => throw new InvalidOperationException()
        };

        return true;
    }

    public bool IsNoMetadataMethod(MethodDescHandle methodDescHandle, out string methodName)
    {
        MethodDesc methodDesc = _methodDescs[methodDescHandle.Address];

        if (methodDesc.Classification != MethodClassification.Dynamic)
        {
            methodName = string.Empty;
            return false;
        }

        methodName = AsDynamicMethodDesc(methodDesc).MethodName;
        return true;
    }

    public bool IsStoredSigMethodDesc(MethodDescHandle methodDescHandle, out ReadOnlySpan<byte> signature)
    {
        MethodDesc methodDesc = _methodDescs[methodDescHandle.Address];

        switch (methodDesc.Classification)
        {
            case MethodClassification.Dynamic:
            case MethodClassification.EEImpl:
            case MethodClassification.Array:
                break; // These have stored sigs

            default:
                signature = default;
                return false;
        }

        signature = AsStoredSigMethodDesc(methodDesc).Signature;
        return true;
    }

    public bool IsDynamicMethod(MethodDescHandle methodDescHandle)
    {
        MethodDesc methodDesc = _methodDescs[methodDescHandle.Address];

        if (methodDesc.Classification != MethodClassification.Dynamic)
        {
            return false;
        }

        return AsDynamicMethodDesc(methodDesc).IsDynamicMethod;
    }

    public bool IsILStub(MethodDescHandle methodDescHandle)
    {
        MethodDesc methodDesc = _methodDescs[methodDescHandle.Address];

        if (methodDesc.Classification != MethodClassification.Dynamic)
        {
            return false;
        }

        return AsDynamicMethodDesc(methodDesc).IsILStub;
    }

    private MethodTable GetOrCreateMethodTable(MethodDesc methodDesc)
    {
        // Ensures that the method table is valid, created, and cached
        _ = GetTypeHandle(methodDesc.MethodTable);
        return _methodTables[methodDesc.MethodTable];
    }

    private struct VtableIndirections
    {
        // See methodtable.inl  VTABLE_SLOTS_PER_CHUNK and the comment on it
        private const int NumPointersPerIndirection = 8;
        private const int NumPointersPerIndirectionLog2 = 3;
        private readonly Target _target;
        public readonly TargetPointer Address;
        public VtableIndirections(Target target, TargetPointer address)
        {
            _target = target;
            Address = address;
        }

        public TargetPointer GetAddressOfSlot(uint slotNum)
        {
            TargetPointer indirectionPointer = Address + (ulong)(slotNum >> NumPointersPerIndirectionLog2) * (ulong)_target.PointerSize;
            TargetPointer slotsStart = _target.ReadPointer(indirectionPointer);
            return slotsStart + (ulong)(slotNum & (NumPointersPerIndirection - 1)) * (ulong)_target.PointerSize;
        }
    }

    private VtableIndirections GetVTableIndirections(TargetPointer methodTableAddress)
    {
        var typeInfo = _target.GetTypeInfo(DataType.MethodTable);
        return new VtableIndirections(_target, methodTableAddress + typeInfo.Size!.Value);
    }

    private TargetPointer GetAddressOfSlot(TypeHandle typeHandle, uint slotNum)
    {
        if (!typeHandle.IsMethodTable())
            throw new InvalidOperationException($"nameof{typeHandle} is not a MethodTable");

        Debug.Assert(slotNum < GetNumVtableSlots(typeHandle), "Slot number is greater than the number of slots");

        // MethodTable::GetSlotPtrRaw
        MethodTable mt = _methodTables[typeHandle.Address];
        if (slotNum < mt.NumVirtuals)
        {
            // Virtual slots live in chunks pointed to by vtable indirections
            return GetVTableIndirections(typeHandle.Address).GetAddressOfSlot(slotNum);
        }
        else
        {
            Debug.Assert(mt.NumVirtuals < GetNumVtableSlots(typeHandle), "Method table does not have non-virtual slots");

            // Non-virtual slots < GetNumVtableSlots live before the MethodTableAuxiliaryData. The array grows backwards
            TargetPointer auxDataPtr = mt.AuxiliaryData;
            Data.MethodTableAuxiliaryData auxData = _target.ProcessedData.GetOrAdd<Data.MethodTableAuxiliaryData>(auxDataPtr);
            TargetPointer nonVirtualSlotsArray = auxDataPtr + (ulong)auxData.OffsetToNonVirtualSlots;
            return nonVirtualSlotsArray - (1 + (slotNum - mt.NumVirtuals));
        }
    }

    private bool IsWrapperStub(MethodDesc md)
    {
        return md.IsUnboxingStub || IsInstantiatingStub(md);
    }

    private bool IsInstantiatingStub(MethodDesc md)
    {
        return md.Classification == MethodClassification.Instantiated && !md.IsUnboxingStub && AsInstantiatedMethodDesc(md).IsWrapperStubWithInstantiations;
    }

    private bool HasMethodInstantiation(MethodDesc md)
    {
        return md.Classification == MethodClassification.Instantiated && AsInstantiatedMethodDesc(md).HasMethodInstantiation;
    }

    private TargetPointer GetLoaderModule(TypeHandle typeHandle)
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
                System.Diagnostics.Debug.Assert(IsFunctionPointer(typeHandle, out _, out _));
                FnPtrTypeDesc fnPtrTypeDesc = _target.ProcessedData.GetOrAdd<FnPtrTypeDesc>(typeHandle.TypeDescAddress());
                return fnPtrTypeDesc.LoaderModule;
            }
        }

        MethodTable mt = _methodTables[typeHandle.Address];
        Data.MethodTableAuxiliaryData mtAuxData = _target.ProcessedData.GetOrAdd<Data.MethodTableAuxiliaryData>(mt.AuxiliaryData);
        return mtAuxData.LoaderModule;
    }

    private bool IsGenericMethodDefinition(MethodDesc md)
    {
        return md.Classification == MethodClassification.Instantiated && AsInstantiatedMethodDesc(md).IsGenericMethodDefinition;
    }

    private TargetPointer GetLoaderModule(MethodDesc md)
    {
        // MethodDesc::GetLoaderModule:
        // return GetMethodDescChunk()->GetLoaderModule();
        // MethodDescChunk::GetLoaderModule:
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

    bool IRuntimeTypeSystem.IsCollectibleMethod(MethodDescHandle methodDesc)
    {
        MethodDesc md = _methodDescs[methodDesc.Address];
        TargetPointer loaderModuleAddr = GetLoaderModule(md);
        ModuleHandle mod = _target.Contracts.Loader.GetModuleHandle(loaderModuleAddr);
        return _target.Contracts.Loader.IsCollectible(mod);
    }

    bool IRuntimeTypeSystem.IsVersionable(MethodDescHandle methodDesc)
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

    TargetPointer IRuntimeTypeSystem.GetMethodDescVersioningState(MethodDescHandle methodDesc)
    {
        MethodDesc md = _methodDescs[methodDesc.Address];
        TargetPointer codeDataAddress = md.CodeData;
        if (codeDataAddress == TargetPointer.Null)
            return TargetPointer.Null;

        Data.MethodDescCodeData codeData = _target.ProcessedData.GetOrAdd<Data.MethodDescCodeData>(codeDataAddress);
        return codeData.VersioningState;
    }

    uint IRuntimeTypeSystem.GetMethodToken(MethodDescHandle methodDescHandle)
    {
        MethodDesc methodDesc = _methodDescs[methodDescHandle.Address];
        return methodDesc.Token;
    }

    ushort IRuntimeTypeSystem.GetSlotNumber(MethodDescHandle methodDesc)
    {
        MethodDesc md = _methodDescs[methodDesc.Address];
        return md.Slot;
    }
    bool IRuntimeTypeSystem.HasNativeCodeSlot(MethodDescHandle methodDesc)
    {
        MethodDesc md = _methodDescs[methodDesc.Address];
        return md.HasNativeCodeSlot;
    }

    TargetPointer IRuntimeTypeSystem.GetAddressOfNativeCodeSlot(MethodDescHandle methodDesc)
    {
        MethodDesc md = _methodDescs[methodDesc.Address];
        return md.GetAddressOfNativeCodeSlot();
    }

    TargetCodePointer IRuntimeTypeSystem.GetNativeCode(MethodDescHandle methodDescHandle)
    {
        MethodDesc md = _methodDescs[methodDescHandle.Address];
        // TODO(cdac): _ASSERTE(!IsDefaultInterfaceMethod() || HasNativeCodeSlot());
        if (md.HasNativeCodeSlot)
        {
            // When profiler is enabled, profiler may ask to rejit a code even though we
            // we have ngen code for this MethodDesc.  (See MethodDesc::DoPrestub).
            // This means that *ppCode is not stable. It can turn from non-zero to zero.
            TargetPointer ppCode = md.GetAddressOfNativeCodeSlot();
            TargetCodePointer pCode = _target.ReadCodePointer(ppCode);
            return CodePointerUtils.CodePointerFromAddress(pCode.AsTargetPointer, _target);
        }

        if (!md.HasStableEntryPoint || md.HasPrecode)
            return TargetCodePointer.Null;

        return GetStableEntryPoint(md);
    }

    private TargetCodePointer GetStableEntryPoint(MethodDesc md)
    {
        // TODO(cdac): _ASSERTE(!IsVersionableWithVtableSlotBackpatch());
        Debug.Assert(md.HasStableEntryPoint);

        return GetMethodEntryPointIfExists(md);
    }

    private TargetCodePointer GetMethodEntryPointIfExists(MethodDesc md)
    {
        if (md.HasNonVtableSlot)
        {
            TargetPointer pSlot = md.GetAddressOfNonVtableSlot();
            return _target.ReadCodePointer(pSlot);
        }

        TargetPointer methodTablePointer = md.MethodTable;
        TypeHandle typeHandle = GetTypeHandle(methodTablePointer);
        Debug.Assert(_methodTables[typeHandle.Address].IsCanonMT);
        TargetPointer addrOfSlot = GetAddressOfSlot(typeHandle, md.Slot);
        return _target.ReadCodePointer(addrOfSlot);
    }

    TargetPointer IRuntimeTypeSystem.GetGCStressCodeCopy(MethodDescHandle methodDesc)
    {
        MethodDesc md = _methodDescs[methodDesc.Address];
        if (md.GCCoverageInfo is TargetPointer gcCoverageInfoAddr && gcCoverageInfoAddr != TargetPointer.Null)
        {
            Target.TypeInfo gcCoverageInfoType = _target.GetTypeInfo(DataType.GCCoverageInfo);
            return gcCoverageInfoAddr + (ulong)gcCoverageInfoType.Fields["SavedCode"].Offset;
        }
        return TargetPointer.Null;
    }

    private sealed class NonValidatedMethodTableQueries : MethodValidation.IMethodTableQueries
    {
        private readonly RuntimeTypeSystem_1 _rts;
        public NonValidatedMethodTableQueries(RuntimeTypeSystem_1 rts)
        {
            _rts = rts;
        }

        public bool SlotIsVtableSlot(TargetPointer methodTablePointer, uint slot)
        {
            return _rts.SlotIsVtableSlot(methodTablePointer, slot);
        }

        public TargetPointer GetAddressOfMethodTableSlot(TargetPointer methodTablePointer, uint slot)
        {
            return _rts.GetAddressOfMethodTableSlot(methodTablePointer, slot);
        }
    }

    // for the benefit of MethodValidation
    private TargetPointer GetAddressOfMethodTableSlot(TargetPointer methodTablePointer, uint slot)
    {
        TypeHandle typeHandle = GetTypeHandle(methodTablePointer);
        Debug.Assert(_methodTables[typeHandle.Address].IsCanonMT);
        TargetPointer addrOfSlot = GetAddressOfSlot(typeHandle, slot);
        return addrOfSlot;
    }

    private bool SlotIsVtableSlot(TargetPointer methodTablePointer, uint slot)
    {
        TypeHandle typeHandle = GetTypeHandle(methodTablePointer);
        return slot < GetNumVtableSlots(typeHandle);
    }

}
