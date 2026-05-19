// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data.Managed;

/// <summary>
/// Wraps a single <c>ConditionalWeakTable&lt;,&gt;+Entry</c> value-type slot embedded
/// inline in the <c>_entries</c> array. The slot has no object header.
/// </summary>
[CdacType(ManagedFullName = "System.Runtime.CompilerServices.ConditionalWeakTable`2+Entry", IsValueType = true)]
internal sealed partial class ConditionalWeakTableEntry : IData<ConditionalWeakTableEntry>
{
    [Field("HashCode")]
    public int HashCode { get; }

    [Field("Next")]
    public int Next { get; }

    [FieldAddress("depHnd")]
    public TargetPointer DepHndAddress { get; }
}
