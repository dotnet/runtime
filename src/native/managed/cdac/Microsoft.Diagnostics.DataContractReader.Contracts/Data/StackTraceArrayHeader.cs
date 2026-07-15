// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class StackTraceArrayHeader : IData<StackTraceArrayHeader>
{
    static StackTraceArrayHeader IData<StackTraceArrayHeader>.Create(Target target, TargetPointer address)
        => new StackTraceArrayHeader(target, address);

    public StackTraceArrayHeader(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.StackTraceArrayHeader);

        Size = target.ReadField<uint>(address, type, nameof(Size));
    }

    public uint Size { get; init; }
}
