// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ThreadLocalData))]
internal sealed partial class ThreadLocalData : IData<ThreadLocalData>
{
    [Field] public partial TargetPointer CollectibleTlsArrayData { get; }
    [Field] public partial TargetPointer NonCollectibleTlsArrayData { get; }
    [Field] public partial int CollectibleTlsDataCount { get; }
    [Field] public partial int NonCollectibleTlsDataCount { get; }
    [Field] public partial TargetPointer InFlightData { get; }
}
