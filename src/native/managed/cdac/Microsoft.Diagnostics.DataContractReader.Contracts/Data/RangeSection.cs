// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.RangeSection))]
internal sealed partial class RangeSection : IData<RangeSection>
{
    [Field] public TargetPointer RangeBegin { get; }
    [Field] public TargetPointer RangeEndOpen { get; }
    [Field] public TargetPointer NextForDelete { get; }
    [Field] public TargetPointer JitManager { get; }
    [Field] public TargetPointer HeapList { get; }
    [Field] public int Flags { get; }
    [Field] public TargetPointer R2RModule { get; }
    [Field] public TargetPointer RangeList { get; }
}
