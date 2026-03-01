// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

    private enum ComWrapperFlags : uint
    {
        Slot_Basic = 0,
    }

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

    // Mirrors ClrDataAccess::DACGetCCWFromAddress in src/coreclr/debug/daccess/request.cpp.
    // Handles three cases:
    //   1. Address is a COM IP into a ComCallWrapper     → apply ThisMask to get the CCW
    //   2. Address is a COM IP into a SimpleComCallWrapper → navigate via vtable + wrapper
    //   3. Address is a direct ComCallWrapper pointer     → use directly
    // After resolving, if the wrapper is linked (not the start), navigate to the start
    // wrapper via SimpleComCallWrapper.MainWrapper.
    private TargetPointer GetCCWFromAddress(TargetPointer address)
    {
        int pointerSize = _target.PointerSize;
        TargetPointer ccw = address;

        // Try to interpret address as a COM interface pointer (IP).
        // Read the vtable pointer stored at the address, then read AddRef (slot 1) from that vtable.
        if (_target.TryReadPointer(address, out TargetPointer vtable) && vtable != TargetPointer.Null
            && _target.TryReadCodePointer(vtable + (ulong)pointerSize, out TargetCodePointer addRefSlot))
        {
            TargetPointer addRefValue = CodePointerUtils.AddressFromCodePointer(addRefSlot, _target);

            TargetPointer tearOffAddRef      = _target.ReadGlobalPointer(Constants.Globals.TearOffAddRef);
            TargetPointer tearOffSimple      = _target.ReadGlobalPointer(Constants.Globals.TearOffAddRefSimple);
            TargetPointer tearOffSimpleInner = _target.ReadGlobalPointer(Constants.Globals.TearOffAddRefSimpleInner);

            ulong thisMask = _target.ReadGlobal<ulong>(Constants.Globals.CCWThisMask);

            if (addRefValue == tearOffAddRef)
            {
                // Standard CCW IP: apply ThisMask to get the aligned ComCallWrapper pointer.
                ccw = new TargetPointer(address & thisMask);
            }
            else if (addRefValue == tearOffSimple || addRefValue == tearOffSimpleInner)
            {
                // SimpleComCallWrapper IP: use GetStdInterfaceKind to find the SCCW base, then get MainWrapper.
                // GetStdInterfaceKind reads from just before the vtable array:
                TargetPointer descBase = vtable - (ulong)pointerSize;
                int interfaceKind = _target.Read<int>(descBase);

                Target.TypeInfo sccwTypeInfo = _target.GetTypeInfo(DataType.SimpleComCallWrapper);
                ulong vtablePtrOffset = (ulong)sccwTypeInfo.Fields[nameof(Data.SimpleComCallWrapper.VTablePtr)].Offset;
                TargetPointer sccwAddr = address - (ulong)(interfaceKind * pointerSize) - vtablePtrOffset;
                Data.SimpleComCallWrapper sccw = _target.ProcessedData.GetOrAdd<Data.SimpleComCallWrapper>(sccwAddr);
                ccw = sccw.MainWrapper;
            }
        }

        Data.ComCallWrapper wrapper = _target.ProcessedData.GetOrAdd<Data.ComCallWrapper>(ccw);
        if (wrapper.Next != TargetPointer.Null)
        {
            Data.SimpleComCallWrapper sccw = _target.ProcessedData.GetOrAdd<Data.SimpleComCallWrapper>(wrapper.SimpleWrapper);
            ccw = sccw.MainWrapper;
        }

        return ccw;
    }

    public IEnumerable<COMInterfacePointerData> GetCCWInterfaces(TargetPointer ccw)
    {
        ulong comMethodTableSize = _target.GetTypeInfo(DataType.ComMethodTable).Size!.Value;
        int pointerSize = _target.PointerSize;
        // LinkedWrapperTerminator = (PTR_ComCallWrapper)-1: all pointer-sized bits set
        TargetPointer linkedWrapperTerminator = pointerSize == 8 ? TargetPointer.Max64Bit : TargetPointer.Max32Bit;

        TargetPointer startCCW = GetCCWFromAddress(ccw);

        bool isFirst = true;
        TargetPointer current = startCCW;
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
                if ((comMethodTable.Flags & (ulong)ComMethodTableFlags.LayoutComplete) == 0)
                    continue;

                // slotAddr is the address of m_rgpIPtr[i] in the CCW struct (= InterfacePointer)
                TargetPointer slotAddr = wrapper.IPtr + (ulong)(i * pointerSize);

                // Slot_Basic (index 0) of the first wrapper = IUnknown/IDispatch, no associated MethodTable
                TargetPointer methodTable = (isFirst && i == ComWrapperFlags.Slot_Basic)
                    ? TargetPointer.Null
                    : comMethodTable.MethodTable;

                yield return new COMInterfacePointerData
                {
                    InterfacePointer = slotAddr,
                    MethodTable = methodTable,
                };
            }

            isFirst = false;

            // Advance to the next wrapper in the chain
            // LinkedWrapperTerminator = all-bits-set sentinel means end of list
            current = wrapper.Next == linkedWrapperTerminator ? TargetPointer.Null : wrapper.Next;
        }
    }
}
