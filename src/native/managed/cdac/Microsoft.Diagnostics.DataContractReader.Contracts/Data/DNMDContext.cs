// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.DNMDContext))]
internal sealed partial class DNMDContext : IData<DNMDContext>
{
    [Field] public partial uint Magic { get; }
    [Field] public partial uint Flags { get; }
    [Field] public partial TargetPointer Version { get; }
    [FieldAddress] public partial TargetPointer StringsHeap { get; }
    [FieldAddress] public partial TargetPointer GuidHeap { get; }
    [FieldAddress] public partial TargetPointer BlobHeap { get; }
    [FieldAddress] public partial TargetPointer UserStringHeap { get; }
    [Field] public partial TargetPointer Tables { get; }
}
