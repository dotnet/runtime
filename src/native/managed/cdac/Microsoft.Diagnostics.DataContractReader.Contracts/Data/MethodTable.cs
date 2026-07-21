// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.MethodTable))]
internal sealed partial class MethodTable : IData<MethodTable>
{
    [Field] public uint MTFlags { get; }
    [Field] public uint BaseSize { get; }
    [Field] public uint MTFlags2 { get; }
    [Field] public TargetPointer EEClassOrCanonMT { get; }
    [Field] public TargetPointer Module { get; }
    [Field] public TargetPointer ParentMethodTable { get; }
    [Field] public TargetPointer PerInstInfo { get; }
    [Field] public ushort NumInterfaces { get; }
    [Field] public ushort NumVirtuals { get; }
    [Field] public TargetPointer AuxiliaryData { get; }
}
