// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

/// <summary>Wraps a <c>System.Runtime.CompilerServices.ConditionalWeakTable&lt;TKey, TValue&gt;</c> instance.</summary>
[CdacType(ManagedFullName = "System.Runtime.CompilerServices.ConditionalWeakTable`2")]
internal sealed partial class ConditionalWeakTable : IData<ConditionalWeakTable>
{
    /// <summary>Pointer to the active <c>Container</c> object.</summary>
    [Field("_container")]
    public TargetPointer Container { get; }
}
