// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ThreadLocalData))]
internal sealed partial class ThreadLocalData : IData<ThreadLocalData>
{
    [Field] public TargetPointer CollectibleTlsArrayData { get; }
    [Field] public TargetPointer NonCollectibleTlsArrayData { get; }
    [Field] public int CollectibleTlsDataCount { get; }
    [Field] public int NonCollectibleTlsDataCount { get; }
    [Field] public TargetPointer InFlightData { get; }
}
