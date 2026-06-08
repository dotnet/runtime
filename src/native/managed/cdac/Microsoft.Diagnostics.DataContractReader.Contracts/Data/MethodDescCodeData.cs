// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.MethodDescCodeData))]
internal sealed partial class MethodDescCodeData : IData<MethodDescCodeData>
{
    [Field] public TargetCodePointer TemporaryEntryPoint { get; }
    [Field] public TargetPointer VersioningState { get; }
    [Field] public uint OptimizationTier { get; }
}
