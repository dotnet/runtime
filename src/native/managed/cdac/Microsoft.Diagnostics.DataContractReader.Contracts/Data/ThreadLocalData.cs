// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ThreadLocalData : IData<ThreadLocalData>
{
    static ThreadLocalData IData<ThreadLocalData>.Create(Target target, TargetPointer address) => new ThreadLocalData(target, address);
    public ThreadLocalData(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ThreadLocalData);
        CollectibleTlsArrayData = target.ReadPointerField(address, type, nameof(CollectibleTlsArrayData));
        NonCollectibleTlsArrayData = target.ReadPointerField(address, type, nameof(NonCollectibleTlsArrayData));
        CollectibleTlsDataCount = target.ReadField<int>(address, type, nameof(CollectibleTlsDataCount));
        NonCollectibleTlsDataCount = target.ReadField<int>(address, type, nameof(NonCollectibleTlsDataCount));
        InFlightData = target.ReadPointerField(address, type, nameof(InFlightData));
    }
    public TargetPointer CollectibleTlsArrayData { get; init; }
    public TargetPointer NonCollectibleTlsArrayData { get; init; }
    public int CollectibleTlsDataCount { get; init; }
    public int NonCollectibleTlsDataCount { get; init; }
    public TargetPointer InFlightData { get; init; }
}
