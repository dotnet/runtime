// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.DebuggerPatchTable))]
internal sealed partial class DebuggerPatchTable : IData<DebuggerPatchTable>
{
    [Field] public partial TargetPointer Entries { get; }
    [Field] public partial uint Count { get; }
}
