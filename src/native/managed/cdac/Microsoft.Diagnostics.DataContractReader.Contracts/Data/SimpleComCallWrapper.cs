// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.SimpleComCallWrapper))]
internal sealed partial class SimpleComCallWrapper : IData<SimpleComCallWrapper>
{
    [Field] public TargetPointer OuterIUnknown { get; }
    [Field] public long RefCount { get; }
    [Field] public uint Flags { get; }
    [Field] public TargetPointer MainWrapper { get; }

    [FieldAddress]
    public TargetPointer VTablePtr { get; }
}
