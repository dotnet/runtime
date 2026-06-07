// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.MemoryRange))]
internal sealed partial class MemoryRange : IData<MemoryRange>
{
    [Field] public TargetPointer StartAddress { get; }
    [Field] public TargetNUInt Size { get; }
}
