// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ThreadStore))]
internal sealed partial class ThreadStore : IData<ThreadStore>
{
    [Field] public int ThreadCount { get; }
    [Field] public TargetPointer FirstThreadLink { get; }
    [Field] public int UnstartedCount { get; }
    [Field] public int BackgroundCount { get; }
    [Field] public int PendingCount { get; }
    [Field] public int DeadCount { get; }
}
