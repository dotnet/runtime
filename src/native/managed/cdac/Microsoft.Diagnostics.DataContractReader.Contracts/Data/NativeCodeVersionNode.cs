// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.NativeCodeVersionNode))]
internal sealed partial class NativeCodeVersionNode : IData<NativeCodeVersionNode>
{
    [Field] public partial TargetPointer Next { get; }
    [Field] public partial TargetPointer MethodDesc { get; }

    [Field] public partial TargetCodePointer NativeCode { get; }
    [Field] public partial uint Flags { get; }
    [Field] public partial TargetNUInt ILVersionId { get; }

    [Field] public partial TargetPointer? GCCoverageInfo { get; }
    [Field] public partial uint OptimizationTier { get; }
}
