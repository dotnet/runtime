// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ReadyToRunInfo : IData<ReadyToRunInfo>
{
    static ReadyToRunInfo IData<ReadyToRunInfo>.Create(Target target, TargetPointer address)
        => new ReadyToRunInfo(target, address);

    private readonly Target _target;

    public ReadyToRunInfo(Target target, TargetPointer address)
    {
        _target = target;
        Target.TypeInfo type = target.GetTypeInfo(DataType.ReadyToRunInfo);

        CompositeInfo = target.ReadPointer(address + (ulong)type.Fields[nameof(CompositeInfo)].Offset);

        NumRuntimeFunctions = target.Read<uint>(address + (ulong)type.Fields[nameof(NumRuntimeFunctions)].Offset);
        RuntimeFunctions = target.ReadPointer(address + (ulong)type.Fields[nameof(RuntimeFunctions)].Offset);

        DelayLoadMethodCallThunks = target.ReadPointer(address + (ulong)type.Fields[nameof(DelayLoadMethodCallThunks)].Offset);

        // Map is from the composite info pointer (set to itself for non-multi-assembly composite images)
        EntryPointToMethodDescMap = CompositeInfo + (ulong)type.Fields[nameof(EntryPointToMethodDescMap)].Offset;
     }

    internal TargetPointer CompositeInfo { get; }

    public uint NumRuntimeFunctions { get; }
    public TargetPointer RuntimeFunctions { get; }

    public TargetPointer DelayLoadMethodCallThunks { get; }
    public TargetPointer EntryPointToMethodDescMap { get; }
}
