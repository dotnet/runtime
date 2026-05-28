// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.CodeHeapListNode))]
internal sealed partial class CodeHeapListNode : IData<CodeHeapListNode>
{
    [Field] public TargetPointer Next { get; }
    [Field] public TargetPointer StartAddress { get; }
    [Field] public TargetPointer EndAddress { get; }
    [Field] public TargetPointer MapBase { get; }
    [Field] public TargetPointer HeaderMap { get; }
    [Field] public TargetPointer Heap { get; }
}
