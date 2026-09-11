// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.OomHistory))]
internal sealed partial class OomHistory : IData<OomHistory>
{
    [Field] public partial int Reason { get; }
    [Field] public partial TargetNUInt AllocSize { get; }
    [Field] public partial TargetPointer Reserved { get; }
    [Field] public partial TargetPointer Allocated { get; }
    [Field] public partial TargetNUInt GcIndex { get; }
    [Field] public partial int Fgm { get; }
    [Field] public partial TargetNUInt Size { get; }
    [Field] public partial TargetNUInt AvailablePagefileMb { get; }
    [Field] public partial uint LohP { get; }
}
