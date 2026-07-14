// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.EEClassOptionalFields))]
internal sealed partial class EEClassOptionalFields : IData<EEClassOptionalFields>
{
    [Field] public SystemVEightByteRegistersInfo? EightByteRegistersInfo { get; }
}
