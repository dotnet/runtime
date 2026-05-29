// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.CGrowableSymbolStream))]
internal sealed partial class CGrowableSymbolStream : IData<CGrowableSymbolStream>
{
    [Field] public TargetPointer Buffer { get; }
    [Field] public uint Size { get; }
}
