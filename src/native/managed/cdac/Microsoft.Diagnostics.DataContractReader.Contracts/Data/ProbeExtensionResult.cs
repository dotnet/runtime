// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ProbeExtensionResult))]
internal sealed partial class ProbeExtensionResult : IData<ProbeExtensionResult>
{
    [Field] public int Type { get; }
}
