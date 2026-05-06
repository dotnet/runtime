// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal struct ComWrappers_1 : IComWrappers
{
    private const string NativeObjectWrapperNamespace = "System.Runtime.InteropServices";
    private const string NativeObjectWrapperName = "ComWrappers+NativeObjectWrapper";
    private const string ComWrappersNamespace = "System.Runtime.InteropServices";
    private const string ComWrappersName = "ComWrappers";
    private const string NativeObjectWrapperCWTFieldName = "s_nativeObjectWrapperTable";
    private const string AllManagedObjectWrapperTableFieldName = "s_allManagedObjectWrapperTable";
    private const string ListNamespace = "System.Collections.Generic";
    private const string ListName = "List`1";
    private const string ListItemsFieldName = "_items";
    private const string ListSizeFieldName = "_size";
    private static readonly Guid IID_IUnknown = new Guid("00000000-0000-0000-C000-000000000046");
    private const int CallerDefinedIUnknown = 1;
    private TargetPointer? _mowTableAddr = null;
    private TargetPointer? _nativeObjectWrapperCWTAddr = null;
    private uint? _listItemsOffset = null;
    private uint? _listSizeOffset = null;
    private readonly Target _target;

    public ComWrappers_1(Target target)
    {
        _target = target;
    }

    public TargetPointer GetComWrappersIdentity(TargetPointer address)
    {
        Data.NativeObjectWrapperObject wrapper = _target.ProcessedData.GetOrAdd<Data.NativeObjectWrapperObject>(address);
        return wrapper.ExternalComObject;
    }

    private bool GetComWrappersCCWVTableQIAddress(TargetPointer ccw, out TargetPointer vtable, out TargetPointer qiAddress)
    {
        qiAddress = TargetPointer.Null;
        if (!_target.TryReadPointer(ccw, out vtable))
            return false;
        if (!_target.TryReadCodePointer(vtable, out TargetCodePointer qiCodePtr))
            return false;
        qiAddress = CodePointerUtils.AddressFromCodePointer(qiCodePtr, _target);
        return true;
    }

    private bool IsComWrappersCCW(TargetPointer ccw)
    {
        if (!GetComWrappersCCWVTableQIAddress(ccw, out _, out TargetPointer qiAddress))
            return false;

        if (!_target.TryReadGlobalPointer(Constants.Globals.ComWrappersVtablePtrs, out TargetPointer? comWrappersVtablePtrs))
            return false;
        Data.ComWrappersVtablePtrs comWrappersVtableStruct = _target.ProcessedData.GetOrAdd<Data.ComWrappersVtablePtrs>(comWrappersVtablePtrs.Value);
        return comWrappersVtableStruct.ComWrappersInterfacePointers.Contains(CodePointerUtils.CodePointerFromAddress(qiAddress, _target));
    }

    public TargetPointer GetManagedObjectWrapperFromCCW(TargetPointer ccw)
    {
        if (!IsComWrappersCCW(ccw))
            return TargetPointer.Null;
        if (!_target.TryReadPointer(ccw & _target.ReadGlobalPointer(Constants.Globals.DispatchThisPtrMask), out TargetPointer MOWWrapper))
            return TargetPointer.Null;
        return MOWWrapper;
    }

    public TargetPointer GetComWrappersObjectFromMOW(TargetPointer mow)
    {
        Data.ObjectHandle handle = _target.ProcessedData.GetOrAdd<Data.ObjectHandle>(mow);
        Data.ManagedObjectWrapperHolderObject mowHolderObject = _target.ProcessedData.GetOrAdd<Data.ManagedObjectWrapperHolderObject>(handle.Object);
        return mowHolderObject.WrappedObject;
    }

    public long GetMOWReferenceCount(TargetPointer mow)
    {
        Data.ManagedObjectWrapperLayout layout = _target.ProcessedData.GetOrAdd<Data.ManagedObjectWrapperLayout>(mow);
        return layout.RefCount;
    }

    private TargetPointer IndexIntoDispatchSection(int index, TargetPointer dispatches)
    {
        Target.TypeInfo dispatchTypeInfo = _target.GetTypeInfo(DataType.InternalComInterfaceDispatch);
        uint dispatchSize = dispatchTypeInfo.Size!.Value;
        uint entriesPerThisPtr = (dispatchSize / (uint)_target.PointerSize) - 1;

        TargetPointer dispatchAddress = dispatches + (ulong)((uint)(index / (int)entriesPerThisPtr) * dispatchSize);
        Data.InternalComInterfaceDispatch dispatch = _target.ProcessedData.GetOrAdd<Data.InternalComInterfaceDispatch>(dispatchAddress);

        return dispatch.Entries + (ulong)((uint)(index % (int)entriesPerThisPtr) * (uint)_target.PointerSize);
    }

    public TargetPointer GetIdentityForMOW(TargetPointer mow)
    {
        Data.ManagedObjectWrapperLayout layout = _target.ProcessedData.GetOrAdd<Data.ManagedObjectWrapperLayout>(mow);

        if ((layout.Flags & CallerDefinedIUnknown) == 0)
        {
            return IndexIntoDispatchSection(layout.UserDefinedCount, layout.Dispatches);
        }

        Target.TypeInfo entryTypeInfo = _target.GetTypeInfo(DataType.ComInterfaceEntry);
        uint entrySize = entryTypeInfo.Size!.Value;

        for (int i = 0; i < layout.UserDefinedCount; i++)
        {
            TargetPointer entryAddress = layout.UserDefined + (ulong)((uint)i * entrySize);
            Data.ComInterfaceEntry entry = _target.ProcessedData.GetOrAdd<Data.ComInterfaceEntry>(entryAddress);
            if (entry.IID == IID_IUnknown)
            {
                return IndexIntoDispatchSection(i, layout.Dispatches);
            }
        }

        return TargetPointer.Null;
    }

    public List<TargetPointer> GetMOWs(TargetPointer obj, out bool hasMOWTable)
    {
        hasMOWTable = false;
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        if (_mowTableAddr is null)
        {
            rts.GetCoreLibFieldDescAndDef(ComWrappersNamespace, ComWrappersName, AllManagedObjectWrapperTableFieldName, out TargetPointer fieldDescAddr, out _);
            _mowTableAddr = _target.ReadPointer(rts.GetFieldDescStaticAddress(fieldDescAddr));
        }

        List<TargetPointer> mows = new List<TargetPointer>();

        if (_mowTableAddr.Value == TargetPointer.Null)
            return mows;
        IConditionalWeakTable cwt = _target.Contracts.ConditionalWeakTable;
        if (cwt.TryGetValue(_mowTableAddr.Value, obj, out TargetPointer mowListObj))
        {
            hasMOWTable = true;
            Data.Object listObj = _target.ProcessedData.GetOrAdd<Data.Object>(mowListObj);
            if (_listItemsOffset is null)
            {
                rts.GetCoreLibFieldDescAndDef(ListNamespace, ListName, ListItemsFieldName, out TargetPointer itemsFieldDescAddr, out FieldDefinition itemsFieldDef);
                _listItemsOffset = rts.GetFieldDescOffset(itemsFieldDescAddr, itemsFieldDef);
            }
            TargetPointer listItemsPtr = _target.ReadPointer(listObj.Data + _listItemsOffset.Value);

            if (_listSizeOffset is null)
            {
                rts.GetCoreLibFieldDescAndDef(ListNamespace, ListName, ListSizeFieldName, out TargetPointer sizeFieldDescAddr, out FieldDefinition sizeFieldDef);
                _listSizeOffset = rts.GetFieldDescOffset(sizeFieldDescAddr, sizeFieldDef);
            }
            int size = _target.Read<int>(listObj.Data + _listSizeOffset.Value);

            if (size > 0 && listItemsPtr != TargetPointer.Null)
            {
                Data.Array listItemsArray = _target.ProcessedData.GetOrAdd<Data.Array>(listItemsPtr);
                for (int i = 0; i < size; i++)
                {
                    TargetPointer mow = _target.ReadPointer(listItemsArray.DataPointer + (ulong)(i * _target.PointerSize));
                    Data.ManagedObjectWrapperHolderObject mowHolderObject = _target.ProcessedData.GetOrAdd<Data.ManagedObjectWrapperHolderObject>(mow);
                    mows.Add(mowHolderObject.Wrapper);
                }
            }
        }
        return mows;
    }

    public bool IsComWrappersRCW(TargetPointer rcw)
    {
        TargetPointer mt = _target.Contracts.Object.GetMethodTableAddress(rcw);

        // get system module
        ILoader loader = _target.Contracts.Loader;
        TargetPointer systemAssembly = loader.GetSystemAssembly();
        ModuleHandle moduleHandle = loader.GetModuleHandleFromAssemblyPtr(systemAssembly);

        // lookup by name
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        TargetPointer typeHandlePtr = rts.GetTypeByNameAndModule(NativeObjectWrapperName, NativeObjectWrapperNamespace, moduleHandle).Address;
        return mt == typeHandlePtr;
    }

    public TargetPointer GetComWrappersRCWForObject(TargetPointer obj)
    {
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        if (_nativeObjectWrapperCWTAddr is null)
        {
            rts.GetCoreLibFieldDescAndDef(ComWrappersNamespace, ComWrappersName, NativeObjectWrapperCWTFieldName, out TargetPointer fieldDescAddr, out _);
            _nativeObjectWrapperCWTAddr = _target.ReadPointer(rts.GetFieldDescStaticAddress(fieldDescAddr));
        }
        if (_nativeObjectWrapperCWTAddr.Value == TargetPointer.Null)
            return TargetPointer.Null;
        IConditionalWeakTable cwt = _target.Contracts.ConditionalWeakTable;
        _ = cwt.TryGetValue(_nativeObjectWrapperCWTAddr.Value, obj, out TargetPointer rcw);
        return rcw;
    }
}
