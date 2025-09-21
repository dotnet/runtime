// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct ComWrappers_1 : IComWrappers
{
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
        vtable = TargetPointer.Null;
        qiAddress = TargetPointer.Null;
        try
        {
            TargetPointer vtableAddress = _target.ReadPointer(ccw);
            if (vtableAddress == TargetPointer.Null)
                return false;
            qiAddress = _target.ReadPointer(vtableAddress);
            if (qiAddress == TargetPointer.Null)
                return false;
        }
        catch (VirtualReadException)
        {
            return false;
        }
        qiAddress = CodePointerUtils.AddressFromCodePointer(qiAddress.Value, _target);
        return true;
    }

    private bool IsComWrappersCCW(TargetPointer ccw)
    {
        if (!GetComWrappersCCWVTableQIAddress(ccw, out _, out TargetPointer qiAddress))
            return false;

        return qiAddress == _target.ReadGlobalPointer(Constants.Globals.MOWQueryInterface) ||
               qiAddress == _target.ReadGlobalPointer(Constants.Globals.TrackerTargetQueryInterface);
    }

    public TargetPointer GetManagedObjectWrapperFromCCW(TargetPointer ccw)
    {
        if (!IsComWrappersCCW(ccw))
            return TargetPointer.Null;
        try
        {
            return _target.ReadPointer(ccw & _target.ReadGlobalPointer(Constants.Globals.DispatchThisPtrMask));
        }
        catch (VirtualReadException)
        {
            return TargetPointer.Null;
        }
    }

    public TargetPointer GetComWrappersObjectFromMOW(TargetPointer mow)
    {
        TargetPointer objHandle = _target.ReadPointer(mow);
        Data.ObjectHandle handle = _target.ProcessedData.GetOrAdd<Data.ObjectHandle>(objHandle);
        Data.ManagedObjectWrapperHolderObject mowHolderObject = _target.ProcessedData.GetOrAdd<Data.ManagedObjectWrapperHolderObject>(handle.Object);
        return mowHolderObject.WrappedObject;
    }

    public long GetMOWReferenceCount(TargetPointer mow)
    {
        Data.ManagedObjectWrapperLayout layout = _target.ProcessedData.GetOrAdd<Data.ManagedObjectWrapperLayout>(mow);
        return layout.RefCount;
    }

    public bool IsComWrappersRCW(TargetPointer rcw)
    {
        Contracts.IObject objContract = _target.Contracts.Object;
        TargetPointer mt = objContract.GetMethodTableAddress(rcw);
        return mt != TargetPointer.Null;
    }
}
