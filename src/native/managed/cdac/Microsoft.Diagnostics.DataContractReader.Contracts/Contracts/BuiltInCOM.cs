// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct BuiltInCOM_1 : IBuiltInCOM
{
    private readonly Target _target;

    private enum Flags
    {
        IsHandleWeak = 0x4,
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

    public IEnumerable<(TargetPointer MethodTable, TargetPointer Unknown)> GetRCWInterfaces(TargetPointer rcw)
    {
        Data.RCW rcwData = _target.ProcessedData.GetOrAdd<Data.RCW>(rcw);
        uint cacheSize = _target.ReadGlobal<uint>(Constants.Globals.RCWInterfaceCacheSize);
        Target.TypeInfo entryTypeInfo = _target.GetTypeInfo(DataType.InterfaceEntry);
        uint entrySize = entryTypeInfo.Size!.Value;

        for (uint i = 0; i < cacheSize; i++)
        {
            TargetPointer entryAddress = rcwData.InterfaceEntries + i * entrySize;
            Data.InterfaceEntry entry = _target.ProcessedData.GetOrAdd<Data.InterfaceEntry>(entryAddress);
            if (entry.Unknown != TargetPointer.Null)
            {
                yield return (entry.MethodTable, entry.Unknown);
            }
        }
    }
}
