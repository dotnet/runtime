// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.SyncBlockCache))]
internal sealed partial class SyncBlockCache : IData<SyncBlockCache>
{
    [Field] public partial uint FreeSyncTableIndex { get; }
    [Field] public partial TargetPointer CleanupBlockList { get; }
}
