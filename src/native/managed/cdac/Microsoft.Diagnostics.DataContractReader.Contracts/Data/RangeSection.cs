// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class RangeSection : IData<RangeSection>
{
    static RangeSection IData<RangeSection>.Create(Target target, TargetPointer address)
        => new RangeSection(target, address);

    public RangeSection(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.RangeSection);
        RangeBegin = target.ReadPointerField(address, type, nameof(RangeBegin));
        RangeEndOpen = target.ReadPointerField(address, type, nameof(RangeEndOpen));
        NextForDelete = target.ReadPointerField(address, type, nameof(NextForDelete));
        JitManager = target.ReadPointerField(address, type, nameof(JitManager));
        Flags = target.ReadField<int>(address, type, nameof(Flags));
        HeapList = target.ReadPointerField(address, type, nameof(HeapList));
        R2RModule = target.ReadPointerField(address, type, nameof(R2RModule));
    }

    public TargetPointer RangeBegin { get; init; }
    public TargetPointer RangeEndOpen { get; init; }
    public TargetPointer NextForDelete { get; init; }
    public TargetPointer JitManager { get; init; }
    public TargetPointer HeapList { get; init; }
    public int Flags { get; init; }
    public TargetPointer R2RModule { get; init; }
}
