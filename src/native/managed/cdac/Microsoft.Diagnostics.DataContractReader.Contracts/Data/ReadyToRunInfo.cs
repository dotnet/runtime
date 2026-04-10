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

        CompositeInfo = target.ReadPointerField(address, type, nameof(CompositeInfo));

        ReadyToRunHeader = target.ReadPointerField(address, type, nameof(ReadyToRunHeader));

        NumRuntimeFunctions = target.ReadField<uint>(address, type, nameof(NumRuntimeFunctions));
        RuntimeFunctions = NumRuntimeFunctions > 0
            ? target.ReadPointerField(address, type, nameof(RuntimeFunctions))
            : TargetPointer.Null;

        NumHotColdMap = target.ReadField<uint>(address, type, nameof(NumHotColdMap));
        Debug.Assert(NumHotColdMap % 2 == 0, "Hot/cold map should have an even number of entries (pairs of hot/cold runtime function indexes)");
        HotColdMap = NumHotColdMap > 0
            ? target.ReadPointerField(address, type, nameof(HotColdMap))
            : TargetPointer.Null;

        DelayLoadMethodCallThunks = target.ReadPointerField(address, type, nameof(DelayLoadMethodCallThunks));
        DebugInfoSection = target.ReadPointerField(address, type, nameof(DebugInfoSection));
        ExceptionInfoSection = target.ReadPointerField(address, type, nameof(ExceptionInfoSection));

        // Map is from the composite info pointer (set to itself for non-multi-assembly composite images)
        EntryPointToMethodDescMap = CompositeInfo + (ulong)type.Fields[nameof(EntryPointToMethodDescMap)].Offset;
        LoadedImageBase = target.ReadPointerField(address, type, nameof(LoadedImageBase));
        Composite = target.ReadPointerField(address, type, nameof(Composite));
    }

    internal TargetPointer CompositeInfo { get; }

    public TargetPointer ReadyToRunHeader { get; }

    public uint NumRuntimeFunctions { get; }
    public TargetPointer RuntimeFunctions { get; }

    public uint NumHotColdMap { get; }
    public TargetPointer HotColdMap { get; }

    public TargetPointer DelayLoadMethodCallThunks { get; }
    public TargetPointer DebugInfoSection { get; }
    public TargetPointer ExceptionInfoSection { get; }
    public TargetPointer EntryPointToMethodDescMap { get; }
    public TargetPointer LoadedImageBase { get; }
    public TargetPointer Composite { get; }
}
