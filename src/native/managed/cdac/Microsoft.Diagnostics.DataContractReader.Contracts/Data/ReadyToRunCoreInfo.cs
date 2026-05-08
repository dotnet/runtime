// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ReadyToRunCoreInfo : IData<ReadyToRunCoreInfo>
{
    static ReadyToRunCoreInfo IData<ReadyToRunCoreInfo>.Create(Target target, TargetPointer address)
        => new ReadyToRunCoreInfo(target, address);

    public ReadyToRunCoreInfo(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ReadyToRunCoreInfo);
        TargetPointer headerAddress = target.ReadPointerField(address, type, nameof(Header));
        Header = target.ProcessedData.GetOrAdd<ReadyToRunCoreHeader>(headerAddress);
    }

    public ReadyToRunCoreHeader Header { get; }
}
