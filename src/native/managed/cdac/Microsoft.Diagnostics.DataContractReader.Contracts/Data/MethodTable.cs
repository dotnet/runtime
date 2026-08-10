// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.MethodTable))]
internal sealed partial class MethodTable : IData<MethodTable>
{
    [Field] public partial uint MTFlags { get; }
    [Field] public partial uint BaseSize { get; }
    [Field] public partial uint MTFlags2 { get; }
    [Field] public partial TargetPointer EEClassOrCanonMT { get; }
    [Field] public partial TargetPointer Module { get; }
    [Field] public partial TargetPointer ParentMethodTable { get; }
    [Field] public partial TargetPointer PerInstInfo { get; }
    [Field] public partial ushort NumInterfaces { get; }
    [Field] public partial ushort NumVirtuals { get; }
    [Field] public partial TargetPointer AuxiliaryData { get; }
}
