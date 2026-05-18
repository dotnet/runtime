// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct EnC_1 : IEnC
{
    private readonly Target _target;
    private readonly ulong _defaultEnCFunctionVersion;

    public EnC_1(Target target)
    {
        _target = target;
        _defaultEnCFunctionVersion = target.ReadGlobal<ulong>(Constants.Globals.CorDBDefaultEnCFunctionVersion);
    }

    TargetNUInt IEnC.GetLatestEnCVersion(TargetPointer module, uint methodDef)
    {
        Data.EnCData? entry = FindFirstByToken(module, methodDef);
        return entry is null
            ? new TargetNUInt(_defaultEnCFunctionVersion)
            : entry.EnCVersion;
    }

    TargetNUInt IEnC.GetEnCVersion(TargetPointer module, uint methodDef, TargetCodePointer nativeCodeAddress)
    {
        if (nativeCodeAddress.Value == 0)
        {
            return new TargetNUInt(_defaultEnCFunctionVersion);
        }

        // EnCData entries store native code start addresses as TADDR (already stripped of any thumb bit).
        TargetPointer addr = new TargetPointer(nativeCodeAddress.Value);

        Data.Module moduleData = _target.ProcessedData.GetOrAdd<Data.Module>(module);
        TargetPointer cur = moduleData.EnCDataList;
        while (cur != TargetPointer.Null)
        {
            Data.EnCData entry = _target.ProcessedData.GetOrAdd<Data.EnCData>(cur);
            if (entry.Token == methodDef && entry.AddrOfCode == addr)
            {
                return entry.EnCVersion;
            }
            cur = entry.Next;
        }

        return new TargetNUInt(_defaultEnCFunctionVersion);
    }

    private Data.EnCData? FindFirstByToken(TargetPointer module, uint methodDef)
    {
        Data.Module moduleData = _target.ProcessedData.GetOrAdd<Data.Module>(module);
        TargetPointer cur = moduleData.EnCDataList;
        while (cur != TargetPointer.Null)
        {
            Data.EnCData entry = _target.ProcessedData.GetOrAdd<Data.EnCData>(cur);
            if (entry.Token == methodDef)
            {
                return entry;
            }
            cur = entry.Next;
        }

        return null;
    }
}
