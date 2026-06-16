// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.StressLogModuleDesc))]
internal sealed partial class StressLogModuleDesc : IData<StressLogModuleDesc>
{
    [Field] public TargetPointer BaseAddress { get; }
    [Field] public TargetNUInt Size { get; }
}
