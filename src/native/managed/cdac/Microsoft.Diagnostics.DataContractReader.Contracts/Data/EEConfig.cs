// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.EEConfig))]
internal sealed partial class EEConfig : IData<EEConfig>
{
    [Field] public uint ModifiableAssemblies { get; }
}
