// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.RealCodeHeader))]
internal sealed partial class RealCodeHeader : IData<RealCodeHeader>
{
    [Field] public partial TargetPointer MethodDesc { get; }
    [Field] public partial TargetPointer DebugInfo { get; }
    [Field] public partial TargetPointer EHInfo { get; }
    [Field] public partial TargetPointer GCInfo { get; }
    [Field] public partial uint NumUnwindInfos { get; }

    [FieldAddress]
    public partial TargetPointer UnwindInfos { get; }
}
