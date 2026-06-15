// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.CodeHeap))]
internal sealed partial class CodeHeap : IData<CodeHeap>
{
    [Field] public byte HeapType { get; }
}
