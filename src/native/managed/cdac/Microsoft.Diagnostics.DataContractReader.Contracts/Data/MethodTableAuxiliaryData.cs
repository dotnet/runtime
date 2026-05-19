// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.MethodTableAuxiliaryData))]
internal sealed partial class MethodTableAuxiliaryData : IData<MethodTableAuxiliaryData>
{
    [Field] public TargetPointer LoaderModule { get; }
    [Field] public short OffsetToNonVirtualSlots { get; }
    [Field] public uint Flags { get; }
}
