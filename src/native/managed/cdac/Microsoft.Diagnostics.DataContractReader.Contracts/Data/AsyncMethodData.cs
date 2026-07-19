// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.AsyncMethodData))]
internal sealed partial class AsyncMethodData : IData<AsyncMethodData>
{
    [Field] public uint Flags { get; }
    [Field] public Signature Signature { get; }
}
