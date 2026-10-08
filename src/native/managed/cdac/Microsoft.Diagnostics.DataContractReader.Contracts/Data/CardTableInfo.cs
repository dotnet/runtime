// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.CardTableInfo))]
internal sealed partial class CardTableInfo : IData<CardTableInfo>
{
    [Field] public partial uint Recount { get; }
    [Field] public partial TargetNUInt Size { get; }
    [Field] public partial TargetPointer NextCardTable { get; }
}
