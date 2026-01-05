// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal class HijackArgsAMD64 : IData<HijackArgsAMD64>
{
    static HijackArgsAMD64 IData<HijackArgsAMD64>.Create(Target target, TargetPointer address)
        => new HijackArgsAMD64(target, address);

    public HijackArgsAMD64(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.HijackArgs);
        CalleeSavedRegisters = address + (ulong)type.Fields[nameof(CalleeSavedRegisters)].Offset;

        // On Windows, Rsp is present
        if (type.Fields.ContainsKey(nameof(Rsp)))
        {
            Rsp = target.ReadPointer(address + (ulong)type.Fields[nameof(Rsp)].Offset);
        }
    }

    public TargetPointer CalleeSavedRegisters { get; }
    public TargetPointer? Rsp { get; }
}
