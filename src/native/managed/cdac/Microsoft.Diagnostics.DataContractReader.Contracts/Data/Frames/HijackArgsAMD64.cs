// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.HijackArgs))]
internal partial class HijackArgsAMD64 : IData<HijackArgsAMD64>
{
    [FieldAddress]
    public TargetPointer CalleeSavedRegisters { get; }

    // On Windows, Rsp is present
    [Field] public TargetPointer? Rsp { get; }
}
