// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.DNMDTable))]
internal sealed partial class DNMDTable : IData<DNMDTable>
{
    [FieldAddress] public partial TargetPointer Data { get; }
    [Field] public partial uint RowCount { get; }
    [Field] public partial byte RowSize { get; }
    [Field] public partial byte Sorted { get; }
    [Field] public partial byte AddingNewRow { get; }
    [Field] public partial byte TableId { get; }
    [Field] public partial TargetPointer Context { get; }
}
