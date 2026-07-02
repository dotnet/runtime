// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.Signature))]
internal sealed partial class Signature : IData<Signature>
{
    [Field] public TargetPointer SignaturePointer { get; }
    [Field] public uint SignatureLength { get; }
}
