// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.RangeSection))]
internal sealed partial class RangeSection : IData<RangeSection>
{
    [Field] public partial TargetPointer RangeBegin { get; }
    [Field] public partial TargetPointer RangeEndOpen { get; }
    [Field] public partial TargetPointer NextForDelete { get; }
    [Field] public partial TargetPointer JitManager { get; }
    [Field] public partial TargetPointer HeapList { get; }
    [Field] public partial int Flags { get; }
    [Field] public partial TargetPointer R2RModule { get; }
    [Field] public partial TargetPointer RangeList { get; }
}
