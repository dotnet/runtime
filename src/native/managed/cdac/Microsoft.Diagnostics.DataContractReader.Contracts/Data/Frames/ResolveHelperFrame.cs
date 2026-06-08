// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ResolveHelperFrame))]
internal partial class ResolveHelperFrame : IData<ResolveHelperFrame>
{
    [Field] public TargetPointer TransitionBlockPtr { get; }
}
