// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType("System.Threading.Lock")]
internal sealed partial class Lock : IData<Lock>
{
    [Field("_state")]
    public uint State { get; }

    [Field("_owningThreadId")]
    public int OwningThreadId { get; }

    [Field("_recursionCount")]
    public uint RecursionCount { get; }
}
