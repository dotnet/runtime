// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class InterpreterFrame : IData<InterpreterFrame>
{
    static InterpreterFrame IData<InterpreterFrame>.Create(Target target, TargetPointer address)
        => new InterpreterFrame(target, address);

    public InterpreterFrame(Target target, TargetPointer address)
    {
        Address = address;
        Target.TypeInfo type = target.GetTypeInfo(DataType.InterpreterFrame);
        TopInterpMethodContextFrame = target.ReadPointerField(address, type, nameof(TopInterpMethodContextFrame));
        IsFaulting = target.ReadField<byte>(address, type, nameof(IsFaulting)) != 0;
    }

    public TargetPointer Address { get; init; }
    public TargetPointer TopInterpMethodContextFrame { get; init; }
    public bool IsFaulting { get; init; }
}
