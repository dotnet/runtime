// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.StubDispatchFrame))]
internal partial class StubDispatchFrame : IData<StubDispatchFrame>
{
    [Field] public TargetPointer MethodDescPtr { get; }
    [Field] public TargetPointer RepresentativeMTPtr { get; }
    [Field] public uint RepresentativeSlot { get; }
    [Field] public TargetPointer Indirection { get; }
}
