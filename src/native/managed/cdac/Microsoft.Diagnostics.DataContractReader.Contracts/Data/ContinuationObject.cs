// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ContinuationObject))]
internal sealed partial class ContinuationObject : IData<ContinuationObject>
{
    [Field] public partial TargetPointer Next { get; }
    [Field] public partial TargetPointer ResumeInfo { get; }
    [Field] public partial int State { get; }
}
