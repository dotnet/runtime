// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class RuntimeFunction : IData<RuntimeFunction>
{
    static RuntimeFunction IData<RuntimeFunction>.Create(Target target, TargetPointer address)
        => new RuntimeFunction(target, address);

    public RuntimeFunction(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.RuntimeFunction);

        BeginAddress = target.Read<uint>(address + (ulong)type.Fields[nameof(BeginAddress)].Offset);
     }

    public uint BeginAddress { get; }
}
