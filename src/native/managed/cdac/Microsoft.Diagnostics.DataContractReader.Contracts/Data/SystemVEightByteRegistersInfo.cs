// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.SystemVEightByteRegistersInfo))]
internal sealed partial class SystemVEightByteRegistersInfo : IData<SystemVEightByteRegistersInfo>
{
    [Field] public byte NumEightBytes { get; }

    // Slots beyond NumEightBytes are undefined.
    public byte[] EightByteClassifications { get; private set; } = new byte[2];
    public byte[] EightByteSizes { get; private set; } = new byte[2];

    partial void OnInit(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.SystemVEightByteRegistersInfo);
        target.ReadBuffer(address + (ulong)type.Fields[nameof(EightByteClassifications)].Offset, EightByteClassifications);
        target.ReadBuffer(address + (ulong)type.Fields[nameof(EightByteSizes)].Offset, EightByteSizes);
    }
}
