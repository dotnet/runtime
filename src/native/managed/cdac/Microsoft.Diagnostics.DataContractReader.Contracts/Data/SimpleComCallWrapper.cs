// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.SimpleComCallWrapper))]
internal sealed partial class SimpleComCallWrapper : IData<SimpleComCallWrapper>
{
    [Field] public partial TargetPointer OuterIUnknown { get; }
    [Field] public partial long RefCount { get; }
    [Field] public partial uint Flags { get; }
    [Field] public partial TargetPointer MainWrapper { get; }

    [FieldAddress]
    public partial TargetPointer VTablePtr { get; }
}
