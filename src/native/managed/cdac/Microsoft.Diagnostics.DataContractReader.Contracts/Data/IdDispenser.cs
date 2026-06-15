// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.IdDispenser))]
internal sealed partial class IdDispenser : IData<IdDispenser>
{
    [Field] public TargetPointer IdToThread { get; }
    [Field] public uint HighestId { get; }
}
