// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType("System.Threading.Lock")]
internal sealed partial class Lock : IData<Lock>
{
    [Field("_state")]
    public partial uint State { get; }

    [Field("_owningThreadId")]
    public partial int OwningThreadId { get; }

    [Field("_recursionCount")]
    public partial uint RecursionCount { get; }
}
