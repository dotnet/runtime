// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class AsyncMethodData : IData<AsyncMethodData>
{
    static AsyncMethodData IData<AsyncMethodData>.Create(Target target, TargetPointer address) => new AsyncMethodData(target, address);
    public AsyncMethodData(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.AsyncMethodData);

        Flags = target.ReadField<uint>(address, type, nameof(Flags));
    }

    public uint Flags { get; init; }
}
