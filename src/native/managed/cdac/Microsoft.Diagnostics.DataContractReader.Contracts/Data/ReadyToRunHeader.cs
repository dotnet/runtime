// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ReadyToRunHeader : IData<ReadyToRunHeader>
{
    static ReadyToRunHeader IData<ReadyToRunHeader>.Create(Target target, TargetPointer address)
        => new ReadyToRunHeader(target, address);

    public ReadyToRunHeader(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ReadyToRunHeader);

        MajorVersion = target.ReadField<ushort>(address, type, nameof(MajorVersion));
        MinorVersion = target.ReadField<ushort>(address, type, nameof(MinorVersion));
    }

    public ushort MajorVersion { get; }
    public ushort MinorVersion { get; }
}
