// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ReadyToRunInfo))]
internal sealed partial class ReadyToRunInfo : IData<ReadyToRunInfo>
{
    [Field] public partial TargetPointer CompositeInfo { get; }
    [Field] public partial TargetPointer ReadyToRunHeader { get; }
    [Field] public partial uint NumRuntimeFunctions { get; }
    [Field] public partial uint NumHotColdMap { get; }
    [Field] public partial TargetPointer DelayLoadMethodCallThunks { get; }
    [Field] public partial TargetPointer DebugInfoSection { get; }
    [Field] public partial TargetPointer ExceptionInfoSection { get; }
    [Field] public partial TargetPointer LoadedImageBase { get; }
    [Field] public partial TargetPointer Composite { get; }
    [Field] public partial uint NumImportSections { get; }

    // WASM-only: base virtual IP for this module's R2R function table (m_minVirtualIP).
    [Field] public partial TargetPointer? MinVirtualIP { get; }

    [DataDescriptorDependency(nameof(NumRuntimeFunctions), "uint32")]
    [DataDescriptorDependency(nameof(RuntimeFunctions), "pointer")]
    public TargetPointer RuntimeFunctions { get; private set; }

    [DataDescriptorDependency(nameof(NumHotColdMap), "uint32")]
    [DataDescriptorDependency(nameof(HotColdMap), "pointer")]
    public TargetPointer HotColdMap { get; private set; }

    [DataDescriptorDependency(nameof(NumImportSections), "uint32")]
    [DataDescriptorDependency(nameof(ImportSections), "pointer")]
    public TargetPointer ImportSections { get; private set; }

    [DataDescriptorDependency(nameof(CompositeInfo), "pointer")]
    [DataDescriptorDependency(nameof(EntryPointToMethodDescMap), "HashMap")]
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
