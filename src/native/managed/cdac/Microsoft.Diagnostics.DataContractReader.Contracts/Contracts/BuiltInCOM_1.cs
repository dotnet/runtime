// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct BuiltInCOM_1 : IBuiltInCOM
{
    private readonly Target _target;

    private enum CCWFlags
    {
        IsHandleWeak = 0x4,
    }

    // Matches the bit position of m_MarshalingType within RCW::RCWFlags::m_dwFlags.
    private const int MarshalingTypeShift = 7;
    private const uint MarshalingTypeMask = 0x3u << MarshalingTypeShift;
    private const uint MarshalingTypeFreeThreaded = 2u;

    internal BuiltInCOM_1(Target target)
    {
        _target = target;
    }

    public ulong GetRefCount(TargetPointer address)
    {
        Data.ComCallWrapper wrapper = _target.ProcessedData.GetOrAdd<Data.ComCallWrapper>(address);
        Data.SimpleComCallWrapper simpleWrapper = _target.ProcessedData.GetOrAdd<Data.SimpleComCallWrapper>(wrapper.SimpleWrapper);
        return simpleWrapper.RefCount & (ulong)_target.ReadGlobal<long>(Constants.Globals.ComRefcountMask);
    }

    public bool IsHandleWeak(TargetPointer address)
    {
        Data.ComCallWrapper wrapper = _target.ProcessedData.GetOrAdd<Data.ComCallWrapper>(address);
        Data.SimpleComCallWrapper simpleWrapper = _target.ProcessedData.GetOrAdd<Data.SimpleComCallWrapper>(wrapper.SimpleWrapper);
        return (simpleWrapper.Flags & (uint)CCWFlags.IsHandleWeak) != 0;
    }

    public IEnumerable<RCWCleanupInfo> GetRCWCleanupList(TargetPointer cleanupListPtr)
    {
        TargetPointer listAddress;
        if (cleanupListPtr != TargetPointer.Null)
        {
            listAddress = cleanupListPtr;
        }
        else
        {
            TargetPointer globalPtr = _target.ReadGlobalPointer(Constants.Globals.RCWCleanupList);
            listAddress = _target.ReadPointer(globalPtr);
        }

        if (listAddress == TargetPointer.Null)
            yield break;

        Data.RCWCleanupList list = _target.ProcessedData.GetOrAdd<Data.RCWCleanupList>(listAddress);
        TargetPointer bucketPtr = list.FirstBucket;
        while (bucketPtr != TargetPointer.Null)
        {
            Data.RCW bucket = _target.ProcessedData.GetOrAdd<Data.RCW>(bucketPtr);
            bool isFreeThreaded = (bucket.Flags & MarshalingTypeMask) == MarshalingTypeFreeThreaded << MarshalingTypeShift;
            TargetPointer ctxCookie = bucket.CtxCookie;
            TargetPointer staThread = GetSTAThread(bucket);

            TargetPointer rcwPtr = bucketPtr;
            while (rcwPtr != TargetPointer.Null)
            {
                Data.RCW rcw = _target.ProcessedData.GetOrAdd<Data.RCW>(rcwPtr);
                yield return new RCWCleanupInfo(rcwPtr, ctxCookie, staThread, isFreeThreaded);
                rcwPtr = rcw.NextRCW;
            }

            bucketPtr = bucket.NextCleanupBucket;
        }
    }

    private TargetPointer GetSTAThread(Data.RCW rcw)
    {
        TargetPointer ctxEntryPtr = rcw.CtxEntry & ~(ulong)1;
        if (ctxEntryPtr == TargetPointer.Null)
            return TargetPointer.Null;

        Data.CtxEntry ctxEntry = _target.ProcessedData.GetOrAdd<Data.CtxEntry>(ctxEntryPtr);
        return ctxEntry.STAThread;
    }
}
