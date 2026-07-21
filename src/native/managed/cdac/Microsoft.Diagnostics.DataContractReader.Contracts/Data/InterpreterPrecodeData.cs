// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.InterpreterPrecodeData))]
internal sealed partial class InterpreterPrecodeData : IData<InterpreterPrecodeData>
{
    [Field] public TargetPointer ByteCodeAddr { get; }
    [Field] public byte Type { get; }
}
