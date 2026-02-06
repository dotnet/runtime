// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal class HijackFrame : IData<HijackFrame>
{
    static HijackFrame IData<HijackFrame>.Create(Target target, TargetPointer address)
        => new HijackFrame(target, address);

    public HijackFrame(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.HijackFrame);
        ReturnAddress = target.ReadPointer(address + (ulong)type.Fields[nameof(ReturnAddress)].Offset);
        HijackArgsPtr = target.ReadPointer(address + (ulong)type.Fields[nameof(HijackArgsPtr)].Offset);
        Address = address;
    }

    public TargetPointer Address { get; }
    public TargetPointer ReturnAddress { get; }
    public TargetPointer HijackArgsPtr { get; }
}
