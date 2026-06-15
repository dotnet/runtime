// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.OomHistory))]
internal sealed partial class OomHistory : IData<OomHistory>
{
    [Field] public int Reason { get; }
    [Field] public TargetNUInt AllocSize { get; }
    [Field] public TargetPointer Reserved { get; }
    [Field] public TargetPointer Allocated { get; }
    [Field] public TargetNUInt GcIndex { get; }
    [Field] public int Fgm { get; }
    [Field] public TargetNUInt Size { get; }
    [Field] public TargetNUInt AvailablePagefileMb { get; }
    [Field] public uint LohP { get; }
}
