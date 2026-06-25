// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.LoaderCodeHeap))]
internal sealed partial class LoaderCodeHeap : IData<LoaderCodeHeap>
{
    /// <summary>Address of the embedded ExplicitControlLoaderHeap within this LoaderCodeHeap object.</summary>
    [FieldAddress]
    public TargetPointer LoaderHeap { get; }
}
