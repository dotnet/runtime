// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ReadyToRunSection))]
internal sealed partial class ReadyToRunSection : IData<ReadyToRunSection>
{
    [Field] public uint Type { get; }
    [Field] public ImageDataDirectory Section { get; }
}
