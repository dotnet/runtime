// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.InterpByteCodeStart))]
internal sealed partial class InterpByteCodeStart : IData<InterpByteCodeStart>
{
    [Field] public TargetPointer Method { get; }
}
