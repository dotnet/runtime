// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.NativeCodeVersionNode))]
internal sealed partial class NativeCodeVersionNode : IData<NativeCodeVersionNode>
{
    [Field] public TargetPointer Next { get; }
    [Field] public TargetPointer MethodDesc { get; }

    [Field] public TargetCodePointer NativeCode { get; }
    [Field] public uint Flags { get; }
    [Field] public TargetNUInt ILVersionId { get; }

    [Field] public TargetPointer? GCCoverageInfo { get; }
    [Field] public uint OptimizationTier { get; }
}
