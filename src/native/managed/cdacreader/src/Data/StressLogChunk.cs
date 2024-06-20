// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class StressLogChunk : IData<StressLogChunk>
{
    static StressLogChunk IData<StressLogChunk>.Create(Target target, TargetPointer address)
        => new StressLogChunk(target, address);

    public StressLogChunk(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.StressLogChunk);

        Next = target.ReadPointer(address + (ulong)type.Fields[nameof(Next)].Offset);
        Prev = target.ReadPointer(address + (ulong)type.Fields[nameof(Prev)].Offset);
        Buf = target.ReadPointer(address + (ulong)type.Fields[nameof(Buf)].Offset);
        BufSize = target.ReadGlobal<uint>(Constants.Globals.StressLogChunkSize);
        Sig1 = target.Read<uint>(address + (ulong)type.Fields[nameof(Sig1)].Offset);
        Sig2 = target.Read<uint>(address + (ulong)type.Fields[nameof(Sig2)].Offset);
    }

    public TargetPointer Next { get; init; }
    public TargetPointer Prev { get; init; }
    public TargetPointer Buf { get; init; }
    public uint BufSize { get; init; }
    public uint Sig1 { get; init; }
    public uint Sig2 { get; init; }
}
