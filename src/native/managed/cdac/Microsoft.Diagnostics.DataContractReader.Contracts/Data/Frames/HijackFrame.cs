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
        ReturnAddress = target.ReadPointerField(address, type, nameof(ReturnAddress));
        HijackArgsPtr = target.ReadPointerField(address, type, nameof(HijackArgsPtr));
        Address = address;
    }

    public TargetPointer Address { get; }
    public TargetPointer ReturnAddress { get; }
    public TargetPointer HijackArgsPtr { get; }
}
