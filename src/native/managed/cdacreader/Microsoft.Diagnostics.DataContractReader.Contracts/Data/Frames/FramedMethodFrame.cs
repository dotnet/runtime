// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal class FramedMethodFrame : IData<FramedMethodFrame>
{
    static FramedMethodFrame IData<FramedMethodFrame>.Create(Target target, TargetPointer address)
        => new FramedMethodFrame(target, address);

    public FramedMethodFrame(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.FramedMethodFrame);
        TransitionBlockPtr = target.ReadPointer(address + (ulong)type.Fields[nameof(TransitionBlockPtr)].Offset);
        Address = address;
    }

    public TargetPointer Address { get; }
    public TargetPointer TransitionBlockPtr { get; }
}
