// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ComMethodTable))]
internal sealed partial class ComMethodTable : IData<ComMethodTable>
{
    [Field] public TargetNUInt Flags { get; }
    [Field] public TargetPointer MethodTable { get; }
}
