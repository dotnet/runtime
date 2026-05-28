// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class StackTraceElement : IData<StackTraceElement>
{
    static StackTraceElement IData<StackTraceElement>.Create(Target target, TargetPointer address)
        => new StackTraceElement(target, address);

    public StackTraceElement(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.StackTraceElement);

        Ip = target.ReadPointerField(address, type, nameof(Ip));
        MethodDesc = target.ReadPointerField(address, type, nameof(MethodDesc));
        Flags = target.ReadField<int>(address, type, nameof(Flags));
    }

    public TargetPointer Ip { get; init; }
    public TargetPointer MethodDesc { get; init; }
    public int Flags { get; init; }
}
