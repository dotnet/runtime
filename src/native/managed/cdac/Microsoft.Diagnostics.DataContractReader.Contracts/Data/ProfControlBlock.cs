// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ProfControlBlock))]
internal sealed partial class ProfControlBlock : IData<ProfControlBlock>
{
    [Field] public partial ulong GlobalEventMask { get; }
    [Field] public partial bool RejitOnAttachEnabled { get; }
    [Field] public partial TargetPointer MainProfilerProfInterface { get; }
    [Field] public partial int NotificationProfilerCount { get; }
}
