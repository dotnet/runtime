// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType("System.Collections.Generic.List`1")]
internal sealed partial class List : IData<List>
{
    /// <summary>Pointer to the backing <c>T[]</c> array.</summary>
    [Field("_items")]
    public TargetPointer Items { get; }

    /// <summary>Logical element count.</summary>
    [Field("_size")]
    public int Size { get; }
}
