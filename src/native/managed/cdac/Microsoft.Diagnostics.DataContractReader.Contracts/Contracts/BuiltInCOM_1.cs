// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct BuiltInCOM_1 : IBuiltInCOM
{
    private readonly Target _target;
    // Mirrors enum SimpleComCallWrapperFlags in src/coreclr/vm/comcallablewrapper.h
    private enum SimpleComCallWrapperFlags : uint
    {
        IsAggregated    = 0x1,
        IsExtendsCom    = 0x2,
        IsHandleWeak    = 0x4,
    }

    // Mirrors enum Masks in src/coreclr/vm/comcallablewrapper.h
    private enum ComMethodTableFlags : ulong
    {
        LayoutComplete = 0x10,
    }

    // Mirrors the anonymous enum in class ComCallWrapper in src/coreclr/vm/comcallablewrapper.h
    private enum ComWrapperFlags : uint
    {
        Slot_Basic = 0,
    }

    [Flags]
    private enum ComRefCount : long
    {
        RefCountMask = 0x7FFFFFFF,
        CleanupSentinel = 0x80000000,
    }
    // Mirrors RCW::RCWFlags bits in src/coreclr/vm/runtimecallablewrapper.h.
    [Flags]
    private enum RCWFlags : uint
    {
        URTAggregated          = 0x010u, // bit 4: m_fURTAggregated
        URTContained           = 0x020u, // bit 5: m_fURTContained
        MarshalingTypeMask     = 0x180u, // bits 7-8: m_MarshalingType
        MarshalingTypeFreeThreaded = 0x100u, // MarshalingType_FreeThreaded (2) in bits 7-8
    }

    // Sentinel value written to IUnkEntry::m_pUnknown when an RCW is disconnected from its COM object.
    // Mirrors the value 0xBADF00D used in IUnkEntry::IsDisconnected in src/coreclr/vm/comcache.h.
    private const ulong DisconnectedSentinel = 0xBADF00Du;

    internal BuiltInCOM_1(Target target)
    {
        _target = target;
    }

    // See ClrDataAccess::DACGetCCWFromAddress in src/coreclr/debug/daccess/request.cpp.
    // Handles two cases:
    //   1. Address is a COM IP into a ComCallWrapper          → apply ThisMask to get the CCW
    //   2. Address is a COM IP into a SimpleComCallWrapper    → navigate via vtable + wrapper
    // Returns TargetPointer.Null if interfacePointer is not a recognised COM interface pointer.
    public TargetPointer GetCCWFromInterfacePointer(TargetPointer interfacePointer)
    {
        int pointerSize = _target.PointerSize;

        // Try to interpret interfacePointer as a COM interface pointer (IP).
        // Read the vtable pointer stored at the address, then read AddRef (slot 1) from that vtable.
        if (!_target.TryReadPointer(interfacePointer, out TargetPointer vtable) || vtable == TargetPointer.Null
            || !_target.TryReadCodePointer(vtable + (ulong)pointerSize, out TargetCodePointer addRefValue))
        {
            return TargetPointer.Null;
        }

        TargetPointer tearOffAddRef      = _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.TearOffAddRef));
        TargetPointer tearOffSimple      = _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.TearOffAddRefSimple));
        TargetPointer tearOffSimpleInner = _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.TearOffAddRefSimpleInner));

        TargetPointer ccw;
        if (addRefValue == tearOffAddRef)
        {
            // Standard CCW IP: apply ThisMask to get the aligned ComCallWrapper pointer.
            ulong thisMask = _target.ReadGlobal<ulong>(Constants.Globals.CCWThisMask);
            ccw = new TargetPointer(interfacePointer & thisMask);
        }
        else if (addRefValue == tearOffSimple || addRefValue == tearOffSimpleInner)
        {
            // SimpleComCallWrapper IP: use GetStdInterfaceKind to find the SCCW base, then get MainWrapper.
            // GetStdInterfaceKind reads from just before the vtable array:
            TargetPointer descBase = vtable - (ulong)pointerSize;
            int interfaceKind = _target.Read<int>(descBase);

            Target.TypeInfo sccwTypeInfo = _target.GetTypeInfo(DataType.SimpleComCallWrapper);
            ulong vtablePtrOffset = (ulong)sccwTypeInfo.Fields[nameof(Data.SimpleComCallWrapper.VTablePtr)].Offset;
            TargetPointer sccwAddr = interfacePointer - (ulong)(interfaceKind * pointerSize) - vtablePtrOffset;
            Data.SimpleComCallWrapper sccw = _target.ProcessedData.GetOrAdd<Data.SimpleComCallWrapper>(sccwAddr);
            ccw = sccw.MainWrapper;
        }
        else
        {
            // AddRef function does not match any known CCW tear-off: not a recognised COM IP.
            return TargetPointer.Null;
        }
        return ccw;
    }

    public IEnumerable<COMInterfacePointerData> GetCCWInterfaces(TargetPointer ccw)
    {
        ccw = GetStartWrapper(ccw);

        ulong comMethodTableSize = _target.GetTypeInfo(DataType.ComMethodTable).Size!.Value;
        int pointerSize = _target.PointerSize;
        // LinkedWrapperTerminator = (PTR_ComCallWrapper)-1: all pointer-sized bits set
        TargetPointer linkedWrapperTerminator = pointerSize == 8 ? TargetPointer.Max64Bit : TargetPointer.Max32Bit;
        bool isFirst = true;
        TargetPointer current = ccw;
        while (current != TargetPointer.Null)
        {
            Data.ComCallWrapper wrapper = _target.ProcessedData.GetOrAdd<Data.ComCallWrapper>(current);

            for (int i = 0; i < wrapper.IPtrs.Length; i++)
            {
                TargetPointer slotValue = wrapper.IPtrs[i];
                if (slotValue == TargetPointer.Null)
                    continue;

                // ComMethodTable is located immediately before the vtable in memory
                TargetPointer comMethodTableAddr = new TargetPointer(slotValue - comMethodTableSize);
                Data.ComMethodTable comMethodTable = _target.ProcessedData.GetOrAdd<Data.ComMethodTable>(comMethodTableAddr);

                // Skip interfaces whose vtable layout is not yet complete
                if ((comMethodTable.Flags.Value & (ulong)ComMethodTableFlags.LayoutComplete) == 0)
                    continue;

                // slotAddr is the address of m_rgpIPtr[i] in the CCW struct (= InterfacePointerAddress)
                TargetPointer slotAddr = wrapper.IPtr + (ulong)(i * pointerSize);

                // Slot_Basic (index 0) of the first wrapper = IUnknown/IDispatch, no associated MethodTable
                TargetPointer methodTable = (isFirst && i == (int)ComWrapperFlags.Slot_Basic)
                    ? TargetPointer.Null
                    : comMethodTable.MethodTable;

                yield return new COMInterfacePointerData
                {
                    InterfacePointerAddress = slotAddr,
                    MethodTable = methodTable,
                };
            }

            isFirst = false;

            // Advance to the next wrapper in the chain
            // LinkedWrapperTerminator = all-bits-set sentinel means end of list
            current = wrapper.Next == linkedWrapperTerminator ? TargetPointer.Null : wrapper.Next;
        }
    }

    public TargetPointer GetObjectHandle(TargetPointer ccw)
    {
        Data.ComCallWrapper wrapper = _target.ProcessedData.GetOrAdd<Data.ComCallWrapper>(ccw);
        return wrapper.Handle;
    }

    // Returns the address of the SimpleComCallWrapper associated with the given ComCallWrapper.
    private TargetPointer GetSimpleComCallWrapper(TargetPointer ccw)
    {
        Data.ComCallWrapper wrapper = _target.ProcessedData.GetOrAdd<Data.ComCallWrapper>(ccw);
        return wrapper.SimpleWrapper;
    }

    public SimpleComCallWrapperData GetSimpleComCallWrapperData(TargetPointer ccw)
    {
        TargetPointer sccw = GetSimpleComCallWrapper(ccw);
        Data.SimpleComCallWrapper data = _target.ProcessedData.GetOrAdd<Data.SimpleComCallWrapper>(sccw);
        return new SimpleComCallWrapperData
        {
            RefCount = (uint)(data.RefCount & (long)ComRefCount.RefCountMask),
            IsNeutered = (data.RefCount & (long)ComRefCount.CleanupSentinel) != 0,
            IsAggregated = (data.Flags & (uint)SimpleComCallWrapperFlags.IsAggregated) != 0,
            IsExtendsCOMObject = (data.Flags & (uint)SimpleComCallWrapperFlags.IsExtendsCom) != 0,
            IsHandleWeak = (data.Flags & (uint)SimpleComCallWrapperFlags.IsHandleWeak) != 0,
            OuterIUnknown = data.OuterIUnknown,
        };
    }

    // Navigates to the start ComCallWrapper in a linked chain.
    // If ccw is already the start wrapper (or the only wrapper), returns ccw unchanged.
    // Mirrors ComCallWrapper::GetStartWrapper / IsLinked in the runtime.
    public TargetPointer GetStartWrapper(TargetPointer ccw)
    {
        Data.ComCallWrapper wrapper = _target.ProcessedData.GetOrAdd<Data.ComCallWrapper>(ccw);
        if (wrapper.Next != TargetPointer.Null)
        {
            Data.SimpleComCallWrapper sccw = _target.ProcessedData.GetOrAdd<Data.SimpleComCallWrapper>(wrapper.SimpleWrapper);
            ccw = sccw.MainWrapper;
        }
        return ccw;
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
            bool isFreeThreaded = ((RCWFlags)bucket.Flags & RCWFlags.MarshalingTypeMask) == RCWFlags.MarshalingTypeFreeThreaded;
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

    public IEnumerable<(TargetPointer MethodTable, TargetPointer Unknown)> GetRCWInterfaces(TargetPointer rcw)
    {
        Data.RCW rcwData = _target.ProcessedData.GetOrAdd<Data.RCW>(rcw);
        foreach (Data.InterfaceEntry entry in rcwData.InterfaceEntries)
        {
            if (entry.Unknown != TargetPointer.Null)
            {
                yield return (entry.MethodTable, entry.Unknown);
            }
        }
    }

    public TargetPointer GetRCWContext(TargetPointer rcw)
    {
        Data.RCW rcwData = _target.ProcessedData.GetOrAdd<Data.RCW>(rcw);

        return rcwData.CtxCookie;
    }

    public RCWData GetRCWData(TargetPointer rcw)
    {
        Data.RCW rcwData = _target.ProcessedData.GetOrAdd<Data.RCW>(rcw);

        TargetPointer managedObject = TargetPointer.Null;
        if (rcwData.SyncBlockIndex != 0)
        {
            ISyncBlock syncBlock = _target.Contracts.SyncBlock;
            managedObject = syncBlock.GetSyncBlockObject(rcwData.SyncBlockIndex);
        }

        return new RCWData(
            IdentityPointer: rcwData.IdentityPointer,
            UnknownPointer: rcwData.UnknownPointer,
            ManagedObject: managedObject,
            VTablePtr: rcwData.VTablePtr,
            CreatorThread: rcwData.CreatorThread,
            CtxCookie: rcwData.CtxCookie,
            RefCount: rcwData.RefCount,
            IsAggregated: ((RCWFlags)rcwData.Flags).HasFlag(RCWFlags.URTAggregated),
            IsContained: ((RCWFlags)rcwData.Flags).HasFlag(RCWFlags.URTContained),
            IsFreeThreaded: ((RCWFlags)rcwData.Flags & RCWFlags.MarshalingTypeMask) == RCWFlags.MarshalingTypeFreeThreaded,
            IsDisconnected: IsRCWDisconnected(rcwData));
    }

    // Mirrors IUnkEntry::IsDisconnected in src/coreclr/vm/comcache.h.
    private bool IsRCWDisconnected(Data.RCW rcw)
    {
        if (rcw.UnknownPointer == DisconnectedSentinel)
            return true;

        TargetPointer ctxEntryPtr = rcw.CtxEntry & ~(ulong)1;
        if (ctxEntryPtr == TargetPointer.Null)
            return false;

        Data.CtxEntry ctxEntry = _target.ProcessedData.GetOrAdd<Data.CtxEntry>(ctxEntryPtr);
        return rcw.CtxCookie != ctxEntry.CtxCookie;
    }
}
