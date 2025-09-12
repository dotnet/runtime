// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class UnwindInfo : IData<UnwindInfo>
{
    static UnwindInfo IData<UnwindInfo>.Create(Target target, TargetPointer address)
        => new UnwindInfo(target, address);

    public UnwindInfo(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.UnwindInfo);

        if (type.Fields.ContainsKey(nameof(FunctionLength)))
        {
            // The unwind info contains the function length on some platforms (x86)
            FunctionLength = target.Read<uint>(address + (ulong)type.Fields[nameof(FunctionLength)].Offset);
        }
        else
        {
            // Otherwise, it starts with a bitfield header
            Header = target.Read<uint>(address);
        }
        if (type.Fields.ContainsKey(nameof(CountOfUnwindCodes)))
        {
            // The unwind info contains a header and unwind codes on some platforms (x64, Arm64)
            CountOfUnwindCodes = target.Read<byte>(address + (ulong)type.Fields[nameof(CountOfUnwindCodes)].Offset);
            UnwindCodeOffset = (uint?)type.Fields[nameof(UnwindCodeOffset)].Offset;
        }
    }

    public uint? FunctionLength { get; }
    public uint? Header { get; }
    public byte? CountOfUnwindCodes { get; }
    public uint? UnwindCodeOffset { get; }
}
