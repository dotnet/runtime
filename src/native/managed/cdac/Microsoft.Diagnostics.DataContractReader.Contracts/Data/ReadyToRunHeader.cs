// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ReadyToRunHeader))]
internal sealed partial class ReadyToRunHeader : IData<ReadyToRunHeader>
{
    [Field] public partial ushort MajorVersion { get; }
    [Field] public partial ushort MinorVersion { get; }
}
