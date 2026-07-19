// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.HostCodeHeap))]
internal sealed partial class HostCodeHeap : IData<HostCodeHeap>
{
    [Field] public TargetPointer BaseAddress { get; }
    [Field] public TargetPointer CurrentAddress { get; }
}
