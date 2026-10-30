// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.SystemVEightByteRegistersInfo))]
internal sealed partial class SystemVEightByteRegistersInfo : IData<SystemVEightByteRegistersInfo>
{
    // Slots beyond NumEightBytes are undefined.
    [Field] public partial byte NumEightBytes { get; }
    [Field] public partial byte EightByteClassification0 { get; }
    [Field] public partial byte EightByteClassification1 { get; }
    [Field] public partial byte EightByteSize0 { get; }
    [Field] public partial byte EightByteSize1 { get; }
}
