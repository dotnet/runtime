// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.RangeSectionMap))]
internal sealed partial class RangeSectionMap : IData<RangeSectionMap>
{
    /// <summary>Pointer to first element.</summary>
    [FieldAddress]
    public TargetPointer TopLevelData { get; }
}
