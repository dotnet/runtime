// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal class SoftwareExceptionFrame : IData<SoftwareExceptionFrame>
{
    static SoftwareExceptionFrame IData<SoftwareExceptionFrame>.Create(Target target, TargetPointer address)
        => new SoftwareExceptionFrame(target, address);

    public SoftwareExceptionFrame(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.SoftwareExceptionFrame);
        Address = address;
        TargetContext = address + (ulong)type.Fields[nameof(TargetContext)].Offset;
        ReturnAddress = target.ReadPointer(address + (ulong)type.Fields[nameof(ReturnAddress)].Offset);
    }

    public TargetPointer Address { get; }
    public TargetPointer TargetContext { get; }
    public TargetPointer ReturnAddress { get; }
}
