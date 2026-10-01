// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.GCCoverageInfo))]
internal sealed partial class GCCoverageInfo : IData<GCCoverageInfo>
{
    [FieldAddress]
    public partial TargetPointer SavedCode { get; }
}
