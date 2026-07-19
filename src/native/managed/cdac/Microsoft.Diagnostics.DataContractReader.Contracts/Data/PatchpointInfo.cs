// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.PatchpointInfo))]
internal sealed partial class PatchpointInfo : IData<PatchpointInfo>
{
    [Field] public uint LocalCount { get; }
}
