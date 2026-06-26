// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.EnCSyncBlockInfo))]
internal sealed partial class EnCSyncBlockInfo : IData<EnCSyncBlockInfo>
{
    [Field] public TargetPointer List { get; }
}
