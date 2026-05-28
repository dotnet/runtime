// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.LoaderHeap))]
internal sealed partial class LoaderHeap : IData<LoaderHeap>
{
    [Field] public TargetPointer FirstBlock { get; }
}
