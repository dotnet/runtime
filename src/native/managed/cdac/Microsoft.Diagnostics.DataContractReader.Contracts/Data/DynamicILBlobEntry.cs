// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.DynamicILBlobTable))]
internal sealed partial class DynamicILBlobEntry : IData<DynamicILBlobEntry>
{
    [Field] public partial uint EntryMethodToken { get; }
    [Field] public partial TargetPointer EntryIL { get; }
}
