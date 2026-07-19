// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ReadyToRunInfo))]
internal sealed partial class ReadyToRunInfo : IData<ReadyToRunInfo>
{
    [Field] public TargetPointer CompositeInfo { get; }
    [Field] public TargetPointer ReadyToRunHeader { get; }
    [Field] public uint NumRuntimeFunctions { get; }
    [Field] public uint NumHotColdMap { get; }
    [Field] public TargetPointer DelayLoadMethodCallThunks { get; }
    [Field] public TargetPointer DebugInfoSection { get; }
    [Field] public TargetPointer ExceptionInfoSection { get; }
    [Field] public TargetPointer LoadedImageBase { get; }
    [Field] public TargetPointer Composite { get; }
    [Field] public uint NumImportSections { get; }

    public TargetPointer RuntimeFunctions { get; private set; }
    public TargetPointer HotColdMap { get; private set; }
    public TargetPointer ImportSections { get; private set; }
    public TargetPointer EntryPointToMethodDescMap { get; private set; }

    partial void OnInit(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ReadyToRunInfo);

        RuntimeFunctions = NumRuntimeFunctions > 0
            ? target.ReadPointerField(address, type, nameof(RuntimeFunctions))
            : TargetPointer.Null;

        Debug.Assert(NumHotColdMap % 2 == 0, "Hot/cold map should have an even number of entries (pairs of hot/cold runtime function indexes)");
        HotColdMap = NumHotColdMap > 0
            ? target.ReadPointerField(address, type, nameof(HotColdMap))
            : TargetPointer.Null;

        ImportSections = NumImportSections > 0
            ? target.ReadPointer(address + (ulong)type.Fields[nameof(ImportSections)].Offset)
            : TargetPointer.Null;

        // Map is from the composite info pointer (set to itself for non-multi-assembly composite images)
        EntryPointToMethodDescMap = CompositeInfo + (ulong)type.Fields[nameof(EntryPointToMethodDescMap)].Offset;
    }
}
