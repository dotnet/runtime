// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct ComWrappers_1 : IComWrappers
{
    private const string NativeObjectWrapperNamespace = "System.Runtime.InteropServices";
    private const string NativeObjectWrapperName = "ComWrappers+NativeObjectWrapper";
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

        TargetPointer comWrappersVtablePtrs = _target.ReadGlobalPointer(Constants.Globals.ComWrappersVtablePtrs);
        Data.ComWrappersVtablePtrs comWrappersVtableStruct = _target.ProcessedData.GetOrAdd<Data.ComWrappersVtablePtrs>(comWrappersVtablePtrs);
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

    public IEnumerable<RCWCleanupData> GetRCWCleanupList(TargetPointer cleanupListAddress)
    {
        // FEATURE_COMINTEROP is Windows-only; return empty if the feature is not enabled
        if (_target.ReadGlobal<byte>(Constants.Globals.FeatureCOMInterop) == 0)
            yield break;

        TargetPointer listAddress = cleanupListAddress;
        if (listAddress == TargetPointer.Null)
        {
            // Use the global cleanup list if no explicit address was provided
            if (!_target.TryReadGlobalPointer(Constants.Globals.RCWCleanupList, out TargetPointer? globalListPtr)
                || globalListPtr is null)
            {
                yield break;
            }
            listAddress = globalListPtr.Value;
        }

        if (listAddress == TargetPointer.Null)
            yield break;

        Data.RCWCleanupList cleanupList = _target.ProcessedData.GetOrAdd<Data.RCWCleanupList>(listAddress);
        TargetPointer bucketAddress = cleanupList.FirstBucket;

        while (bucketAddress != TargetPointer.Null)
        {
            // Each bucket-head RCW shares context/thread/freethreaded state with all
            // RCWs in its bucket (they all belong to the same COM apartment/context).
            Data.RCW bucketHead = _target.ProcessedData.GetOrAdd<Data.RCW>(bucketAddress);
            TargetPointer ctxCookie = bucketHead.CtxCookie;
            TargetPointer staThread = GetSTAThreadFromRCW(bucketHead);
            bool isFreeThreaded = GetIsFreeThreaded(bucketHead.Flags);

            TargetPointer rcwAddress = bucketAddress;
            while (rcwAddress != TargetPointer.Null)
            {
                yield return new RCWCleanupData(rcwAddress, ctxCookie, staThread, isFreeThreaded);

                Data.RCW rcw = _target.ProcessedData.GetOrAdd<Data.RCW>(rcwAddress);
                rcwAddress = rcw.NextRCW;
            }

            bucketAddress = bucketHead.NextCleanupBucket;
        }
    }

    private TargetPointer GetSTAThreadFromRCW(Data.RCW rcw)
    {
        // The low bit of m_pCtxEntry is used for synchronization; mask it off to get the real pointer.
        const ulong CtxEntrySyncBitMask = ~(ulong)1;
        TargetPointer ctxEntryAddress = rcw.CtxEntry & CtxEntrySyncBitMask;
        if (ctxEntryAddress == TargetPointer.Null)
            return TargetPointer.Null;

        Data.CtxEntry ctxEntry = _target.ProcessedData.GetOrAdd<Data.CtxEntry>(ctxEntryAddress);
        return ctxEntry.STAThread;
    }

    private static bool GetIsFreeThreaded(uint flags)
    {
        // MarshalingType occupies bits 7-8 of the flags DWORD; MarshalingType_FreeThreaded = 2
        const uint marshalingTypeMask = 3u;
        const int marshalingTypeShift = 7;
        const uint marshalingTypeFreeThreaded = 2u;
        return ((flags >> marshalingTypeShift) & marshalingTypeMask) == marshalingTypeFreeThreaded;
    }
}
