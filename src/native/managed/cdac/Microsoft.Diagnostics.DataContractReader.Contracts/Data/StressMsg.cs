// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.StressMsg))]
internal sealed partial class StressMsg : IData<StressMsg>
{
    [FieldAddress]
    public TargetPointer Header { get; }

    [FieldAddress]
    public TargetPointer Args { get; }
}
