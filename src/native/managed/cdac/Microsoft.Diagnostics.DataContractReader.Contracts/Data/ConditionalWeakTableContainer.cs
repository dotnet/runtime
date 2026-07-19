// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType("System.Runtime.CompilerServices.ConditionalWeakTable`2+Container")]
internal sealed partial class ConditionalWeakTableContainer : IData<ConditionalWeakTableContainer>
{
    /// <summary>Pointer to the <c>int[]</c> hash buckets array.</summary>
    [Field("_buckets")]
    public TargetPointer Buckets { get; }

    /// <summary>Pointer to the <c>Entry[]</c> entries array.</summary>
    [Field("_entries")]
    public TargetPointer Entries { get; }
}
