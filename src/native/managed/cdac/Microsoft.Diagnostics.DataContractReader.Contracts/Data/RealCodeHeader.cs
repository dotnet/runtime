// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.RealCodeHeader))]
internal sealed partial class RealCodeHeader : IData<RealCodeHeader>
{
    [Field] public TargetPointer MethodDesc { get; }
    [Field] public TargetPointer DebugInfo { get; }
    [Field] public TargetPointer EHInfo { get; }
    [Field] public TargetPointer GCInfo { get; }
    [Field] public uint NumUnwindInfos { get; }

    [FieldAddress]
    public TargetPointer UnwindInfos { get; }
}
