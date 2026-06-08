// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.UnorderedArrayBase))]
internal sealed partial class UnorderedArrayBase : IData<UnorderedArrayBase>
{
    [Field] public uint Count { get; }
    [Field] public TargetPointer Table { get; }
}
