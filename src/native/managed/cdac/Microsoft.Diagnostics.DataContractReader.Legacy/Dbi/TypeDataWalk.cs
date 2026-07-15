// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

// Port of native DacDbiInterfaceImpl::TypeDataWalk
//
// Walks the flattened DebuggerIPCE_TypeArgData[] tree that the right side built in
// CordbType::GatherTypeData and produces an ITypeHandle for the loaded representation
// (exact, or canonical when generic code-sharing collapses reference type-args to
// System.__Canon and value type-args to their canonical form).
//
internal unsafe ref struct TypeDataWalk
{
    private readonly Target _target;
    private readonly IRuntimeTypeSystem _rts;
    private readonly ITypeHandle _canonTh;
    private DebuggerIPCE_TypeArgData* _pCurrent;
    private uint _remaining;

    public TypeDataWalk(Target target, IRuntimeTypeSystem rts, ITypeHandle canonTh, DebuggerIPCE_TypeArgData* pData, uint nData)
    {
        _target = target;
        _rts = rts;
        _canonTh = canonTh;
        _pCurrent = pData;
        _remaining = nData;
    }

    // Pop a single node from the head of the list, or null if exhausted.
    private DebuggerIPCE_TypeArgData* ReadOne()
    {
        if (_remaining == 0)
            return null;
        _remaining--;
        DebuggerIPCE_TypeArgData* p = _pCurrent;
        _pCurrent++;
        return p;
    }

    // Skip a single node and all of its descendants.
    private void Skip()
    {
        DebuggerIPCE_TypeArgData* p = ReadOne();
        if (p != null)
        {
            uint n = DacDbiImpl.ReadLittleEndian(p->numTypeArgs);
            for (uint i = 0; i < n; i++)
                Skip();
        }
    }

    public ITypeHandle ReadLoadedTypeHandle()
    {
        DebuggerIPCE_TypeArgData* p = ReadOne();
        if (p == null)
            return ITypeHandle.Null;

        CorElementType et = (CorElementType)DacDbiImpl.ReadLittleEndian(p->data.elementType);
        switch (et)
        {
            case CorElementType.Array:
            case CorElementType.SzArray:
                return ArrayTypeArg(p);

            case CorElementType.Ptr:
            case CorElementType.Byref:
                return PtrOrByRefTypeArg(p);

            case CorElementType.Class:
            case CorElementType.ValueType:
                {
                    ulong vmAssembly = DacDbiImpl.ReadLittleEndian(p->data.ClassTypeData_vmAssembly);
                    uint metadataToken = DacDbiImpl.ReadLittleEndian(p->data.ClassTypeData_metadataToken);
                    return ReadLoadedInstantiation(vmAssembly, metadataToken, DacDbiImpl.ReadLittleEndian(p->numTypeArgs));
                }

            case CorElementType.FnPtr:
                return FnPtrTypeArg(p);

            default:
                return _rts.GetPrimitiveType(et);
        }
    }

    // Read a single type argument in canonicalization-aware fashion.
    private ITypeHandle ReadLoadedTypeArg()
    {
        DebuggerIPCE_TypeArgData* p = ReadOne();
        if (p == null)
            return ITypeHandle.Null;

        CorElementType et = (CorElementType)DacDbiImpl.ReadLittleEndian(p->data.elementType);
        switch (et)
        {
            case CorElementType.Ptr:
                return PtrOrByRefTypeArg(p);

            case CorElementType.Class:
            case CorElementType.ValueType:
                return ClassTypeArg(p);

            case CorElementType.FnPtr:
                return FnPtrTypeArg(p);

            default:
                return ObjRefOrPrimitiveTypeArg(p, et);
        }
    }

    // Read an instantiation and ask the runtime-type-system for the loaded handle.
    private ITypeHandle ReadLoadedInstantiation(ulong vmAssembly, uint metadataToken, uint nTypeArgs)
    {
        ITypeHandle typeDef = TryLookupTypeDefOrRefInAssembly(vmAssembly, metadataToken);
        if (typeDef.IsNull)
            return ITypeHandle.Null;

        if (nTypeArgs == 0)
            return typeDef;

        ImmutableArray<ITypeHandle>.Builder builder = ImmutableArray.CreateBuilder<ITypeHandle>((int)nTypeArgs);
        bool allOK = true;
        for (uint i = 0; i < nTypeArgs; i++)
        {
            ITypeHandle th = ReadLoadedTypeArg();
            allOK &= !th.IsNull;
            builder.Add(th);
        }
        if (!allOK)
            return ITypeHandle.Null;

        return _rts.GetConstructedType(typeDef, CorElementType.GenericInst, 0, builder.MoveToImmutable());
    }

    private ITypeHandle ArrayTypeArg(DebuggerIPCE_TypeArgData* pInfo)
    {
        ITypeHandle elem = ReadLoadedTypeArg();
        if (elem.IsNull)
            return ITypeHandle.Null;
        CorElementType et = (CorElementType)DacDbiImpl.ReadLittleEndian(pInfo->data.elementType);
        int rank = (int)DacDbiImpl.ReadLittleEndian(pInfo->data.ArrayTypeData_arrayRank);
        return _rts.GetConstructedType(elem, et, rank, ImmutableArray<ITypeHandle>.Empty);
    }

    private ITypeHandle PtrOrByRefTypeArg(DebuggerIPCE_TypeArgData* pInfo)
    {
        ITypeHandle referent = ReadLoadedTypeArg();
        if (referent.IsNull)
            return ITypeHandle.Null;
        CorElementType et = (CorElementType)DacDbiImpl.ReadLittleEndian(pInfo->data.elementType);
        return _rts.GetConstructedType(referent, et, 0, ImmutableArray<ITypeHandle>.Empty);
    }

    // A generic reference type collapses to System.__Canon
    // (and its type arguments are skipped); a value-type instantiation is recursively
    // resolved.
    private ITypeHandle ClassTypeArg(DebuggerIPCE_TypeArgData* pInfo)
    {
        ulong vmAssembly = DacDbiImpl.ReadLittleEndian(pInfo->data.ClassTypeData_vmAssembly);
        uint metadataToken = DacDbiImpl.ReadLittleEndian(pInfo->data.ClassTypeData_metadataToken);
        uint numTypeArgs = DacDbiImpl.ReadLittleEndian(pInfo->numTypeArgs);
        CorElementType et = (CorElementType)DacDbiImpl.ReadLittleEndian(pInfo->data.elementType);

        ITypeHandle typeDef = TryLookupTypeDefOrRefInAssembly(vmAssembly, metadataToken);

        if ((!typeDef.IsNull && _rts.IsValueType(typeDef)) || et == CorElementType.ValueType)
        {
            return ReadLoadedInstantiation(vmAssembly, metadataToken, numTypeArgs);
        }
        else
        {
            for (uint i = 0; i < numTypeArgs; i++)
                Skip();
            return _canonTh;
        }
    }

    private ITypeHandle FnPtrTypeArg(DebuggerIPCE_TypeArgData* pInfo)
    {
        uint numTypeArgs = DacDbiImpl.ReadLittleEndian(pInfo->numTypeArgs);
        ImmutableArray<ITypeHandle>.Builder builder = ImmutableArray.CreateBuilder<ITypeHandle>((int)numTypeArgs);
        bool allOK = true;
        for (uint i = 0; i < numTypeArgs; i++)
        {
            ITypeHandle th = ReadLoadedTypeArg();
            allOK &= !th.IsNull;
            builder.Add(th);
        }
        if (!allOK)
            return ITypeHandle.Null;

        // Non-default calling conventions are not supported (matches the exact-handle path).
        return _rts.GetConstructedType(ITypeHandle.Null, CorElementType.FnPtr, 0, builder.MoveToImmutable());
    }

    private ITypeHandle ObjRefOrPrimitiveTypeArg(DebuggerIPCE_TypeArgData* pInfo, CorElementType elementType)
    {
        // Skip any children: they are part of a reference-typed argument that canonicalizes to __Canon.
        uint numTypeArgs = DacDbiImpl.ReadLittleEndian(pInfo->numTypeArgs);
        for (uint i = 0; i < numTypeArgs; i++)
            Skip();

        if (_rts.IsCorElementTypeObjRef(elementType))
            return _canonTh;
        return _rts.GetPrimitiveType(elementType);
    }

    private ITypeHandle TryLookupTypeDefOrRefInAssembly(ulong vmAssembly, uint metadataToken)
    {
        ILoader loader = _target.Contracts.Loader;
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        Contracts.ModuleHandle moduleHandle = loader.GetModuleHandleFromAssemblyPtr(new TargetPointer(vmAssembly));
        ModuleLookupTables lookupTables = loader.GetLookupTables(moduleHandle);
        TargetPointer mt;
        switch ((EcmaMetadataUtils.TokenType)(metadataToken & EcmaMetadataUtils.TokenTypeMask))
        {
            case EcmaMetadataUtils.TokenType.mdtTypeDef:
                mt = loader.GetModuleLookupMapElement(lookupTables.TypeDefToMethodTable, metadataToken, out _);
                break;
            case EcmaMetadataUtils.TokenType.mdtTypeRef:
                mt = loader.GetModuleLookupMapElement(lookupTables.TypeRefToMethodTable, metadataToken, out _);
                break;
            default:
                return ITypeHandle.Null;
        }
        if (mt == TargetPointer.Null)
            return ITypeHandle.Null;
        return rts.GetTypeHandle(mt);
    }
}
