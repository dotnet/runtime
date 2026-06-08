// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ResumableFrame))]
internal partial class ResumableFrame : IData<ResumableFrame>
{
    [Field] public TargetPointer TargetContextPtr { get; }
}
