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

        TargetPointer tearOffAddRef      = _target.ReadGlobalPointer(Constants.Globals.TearOffAddRef);
        TargetPointer tearOffSimple      = _target.ReadGlobalPointer(Constants.Globals.TearOffAddRefSimple);
        TargetPointer tearOffSimpleInner = _target.ReadGlobalPointer(Constants.Globals.TearOffAddRefSimpleInner);

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
        // Navigate to the start of the linked chain, mirroring ComCallWrapper::GetStartWrapper in the runtime.
        Data.ComCallWrapper firstCheck = _target.ProcessedData.GetOrAdd<Data.ComCallWrapper>(ccw);
        if (firstCheck.Next != TargetPointer.Null)
        {
            Data.SimpleComCallWrapper sccwFirst = _target.ProcessedData.GetOrAdd<Data.SimpleComCallWrapper>(firstCheck.SimpleWrapper);
            ccw = sccwFirst.MainWrapper;
        }

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
