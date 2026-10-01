// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.MethodDescCodeData))]
internal sealed partial class MethodDescCodeData : IData<MethodDescCodeData>
{
    [Field] public partial TargetCodePointer TemporaryEntryPoint { get; }
    [Field] public partial TargetPointer VersioningState { get; }
    [Field] public partial uint OptimizationTier { get; }
}
