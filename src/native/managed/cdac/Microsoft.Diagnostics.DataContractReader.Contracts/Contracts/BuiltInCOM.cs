// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct BuiltInCOM_1 : IBuiltInCOM
{
    private readonly Target _target;

    private enum Flags
    {
        IsHandleWeak = 0x4,
    }

    // Mirrors enum Masks in src/coreclr/vm/comcallablewrapper.h
    // [cDAC] [BuiltInCOM]: Contract depends on this value
    private enum ComMethodTableFlags : ulong
    {
        LayoutComplete = 0x10,
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
        return (simpleWrapper.Flags & (uint)Flags.IsHandleWeak) != 0;
    }

    public IEnumerable<COMInterfacePointerData> GetCCWInterfaces(TargetPointer ccw)
    {
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
                // slotValue is the vtable pointer stored in m_rgpIPtr[i]
                TargetPointer slotValue = wrapper.IPtrs[i];
                if (slotValue == TargetPointer.Null)
                    continue;

                // ComMethodTable is located immediately before the vtable in memory
                TargetPointer comMethodTableAddr = new TargetPointer(slotValue.Value - comMethodTableSize);
                Data.ComMethodTable comMethodTable = _target.ProcessedData.GetOrAdd<Data.ComMethodTable>(comMethodTableAddr);

                // Skip interfaces whose vtable layout is not yet complete
                if ((comMethodTable.Flags.Value & (ulong)ComMethodTableFlags.LayoutComplete) == 0)
                    continue;

                // slotAddr is the address of m_rgpIPtr[i] in the CCW struct (= InterfacePointer)
                TargetPointer slotAddr = wrapper.IPtr + (ulong)(i * pointerSize);

                // Slot_Basic (index 0) of the first wrapper = IUnknown/IDispatch, no associated MethodTable
                TargetPointer methodTable = (isFirst && i == 0)
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
