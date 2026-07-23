// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.StubDispatchFrame))]
internal partial class StubDispatchFrame : IData<StubDispatchFrame>
{
    [Field] public partial TargetPointer MethodDescPtr { get; }
    [Field] public partial TargetPointer RepresentativeMTPtr { get; }
    [Field] public partial uint RepresentativeSlot { get; }
    [Field] public partial TargetPointer Indirection { get; }
}
