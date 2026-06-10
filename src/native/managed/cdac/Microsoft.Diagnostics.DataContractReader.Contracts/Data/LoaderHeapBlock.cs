// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.LoaderHeapBlock))]
internal sealed partial class LoaderHeapBlock : IData<LoaderHeapBlock>
{
    [Field] public TargetPointer Next { get; }
    [Field] public TargetPointer VirtualAddress { get; }
    [Field] public TargetNUInt VirtualSize { get; }
}
