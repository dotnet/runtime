// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ThreadStore))]
internal sealed partial class ThreadStore : IData<ThreadStore>
{
    [Field] public partial int ThreadCount { get; }
    [Field] public partial TargetPointer FirstThreadLink { get; }
    [Field] public partial int UnstartedCount { get; }
    [Field] public partial int BackgroundCount { get; }
    [Field] public partial int PendingCount { get; }
    [Field] public partial int DeadCount { get; }
}
