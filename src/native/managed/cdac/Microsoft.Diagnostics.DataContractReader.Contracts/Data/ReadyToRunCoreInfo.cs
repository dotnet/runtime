// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ReadyToRunCoreInfo))]
internal sealed partial class ReadyToRunCoreInfo : IData<ReadyToRunCoreInfo>
{
    [Field(Pointer = true)]
    public ReadyToRunCoreHeader Header { get; }
}
