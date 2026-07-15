// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.InstrumentedILOffsetMapping))]
internal sealed partial class InstrumentedILOffsetMapping : IData<InstrumentedILOffsetMapping>
{
    [Field] public uint Count { get; }
    [Field] public TargetPointer Map { get; }
}
