// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Generated;

namespace Microsoft.Diagnostics.DataContractReader.Data.Managed;

/// <summary>Wraps a <c>System.Collections.Generic.List&lt;T&gt;</c> instance.</summary>
[CdacType(ManagedFullName = "System.Collections.Generic.List`1")]
internal sealed partial class List : IData<List>
{
    /// <summary>Pointer to the backing <c>T[]</c> array.</summary>
    [Field("_items")]
    public TargetPointer Items { get; }

    /// <summary>Logical element count.</summary>
    [Field("_size")]
    public int Size { get; }
}
