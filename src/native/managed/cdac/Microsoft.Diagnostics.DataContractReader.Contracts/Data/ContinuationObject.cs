// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ContinuationObject))]
internal sealed partial class ContinuationObject : IData<ContinuationObject>
{
    [Field] public TargetPointer Next { get; }
    [Field] public TargetPointer ResumeInfo { get; }
    [Field] public int State { get; }
}
