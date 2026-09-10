// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ProtectValueClassFrame))]
internal sealed partial class ProtectValueClassFrame : IData<ProtectValueClassFrame>
{
    [Field] public partial TargetPointer ValueClassInfoList { get; }
}
