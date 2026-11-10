// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.CodeHeapListNode))]
internal sealed partial class CodeHeapListNode : IData<CodeHeapListNode>
{
    [Field] public partial TargetPointer Next { get; }
    [Field] public partial TargetPointer StartAddress { get; }
    [Field] public partial TargetPointer EndAddress { get; }
    [Field] public partial TargetPointer MapBase { get; }
    [Field] public partial TargetPointer HeaderMap { get; }
    [Field] public partial TargetPointer Heap { get; }

    // 64-bit only: jump thunk to the personality routine. Used as the module base
    // when matching a dynamic function table's minimum address.
    [Field] public partial TargetPointer? CLRPersonalityRoutine { get; }
}
