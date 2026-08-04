// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.StubLinkStubManager))]
internal sealed partial class StubLinkStubManager : IData<StubLinkStubManager>
{
    [FieldAddress]
    public partial TargetPointer RangeList { get; }
}
