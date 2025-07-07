// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ReadyToRunInfo : IData<ReadyToRunInfo>
{
    static ReadyToRunInfo IData<ReadyToRunInfo>.Create(Target target, TargetPointer address)
        => new ReadyToRunInfo(target, address);

    public ReadyToRunInfo(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ReadyToRunInfo);

        CompositeInfo = target.ReadPointer(address + (ulong)type.Fields[nameof(CompositeInfo)].Offset);

        ReadyToRunHeader = target.ReadPointer(address + (ulong)type.Fields[nameof(ReadyToRunHeader)].Offset);

        NumRuntimeFunctions = target.Read<uint>(address + (ulong)type.Fields[nameof(NumRuntimeFunctions)].Offset);
        RuntimeFunctions = NumRuntimeFunctions > 0
            ? target.ReadPointer(address + (ulong)type.Fields[nameof(RuntimeFunctions)].Offset)
            : TargetPointer.Null;

        NumHotColdMap = target.Read<uint>(address + (ulong)type.Fields[nameof(NumHotColdMap)].Offset);
        Debug.Assert(NumHotColdMap % 2 == 0, "Hot/cold map should have an even number of entries (pairs of hot/cold runtime function indexes)");
        HotColdMap = NumHotColdMap > 0
            ? target.ReadPointer(address + (ulong)type.Fields[nameof(HotColdMap)].Offset)
            : TargetPointer.Null;

        DelayLoadMethodCallThunks = target.ReadPointer(address + (ulong)type.Fields[nameof(DelayLoadMethodCallThunks)].Offset);

        // Map is from the composite info pointer (set to itself for non-multi-assembly composite images)
        EntryPointToMethodDescMap = CompositeInfo + (ulong)type.Fields[nameof(EntryPointToMethodDescMap)].Offset;
    }

    internal TargetPointer CompositeInfo { get; }

    public TargetPointer ReadyToRunHeader { get; }

    public uint NumRuntimeFunctions { get; }
    public TargetPointer RuntimeFunctions { get; }

    public uint NumHotColdMap { get; }
    public TargetPointer HotColdMap { get; }

    public TargetPointer DelayLoadMethodCallThunks { get; }
    public TargetPointer EntryPointToMethodDescMap { get; }
}
