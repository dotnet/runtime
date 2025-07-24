// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal class ResumableFrame : IData<ResumableFrame>
{
    static ResumableFrame IData<ResumableFrame>.Create(Target target, TargetPointer address)
        => new ResumableFrame(target, address);

    public ResumableFrame(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ResumableFrame);
        TargetContextPtr = target.ReadPointer(address + (ulong)type.Fields[nameof(TargetContextPtr)].Offset);
        Address = address;
    }

    public TargetPointer Address { get; }
    public TargetPointer TargetContextPtr { get; }
}
