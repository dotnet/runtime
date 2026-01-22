// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal class StubDispatchFrame : IData<StubDispatchFrame>
{
    static StubDispatchFrame IData<StubDispatchFrame>.Create(Target target, TargetPointer address)
        => new StubDispatchFrame(target, address);

    public StubDispatchFrame(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.StubDispatchFrame);
        MethodDescPtr = target.ReadPointer(address + (ulong)type.Fields[nameof(MethodDescPtr)].Offset);
        RepresentativeMTPtr = target.ReadPointer(address + (ulong)type.Fields[nameof(RepresentativeMTPtr)].Offset);
        RepresentativeSlot = target.Read<uint>(address + (ulong)type.Fields[nameof(RepresentativeSlot)].Offset);
        Address = address;
    }

    public TargetPointer Address { get; }
    public TargetPointer MethodDescPtr { get; }
    public TargetPointer RepresentativeMTPtr { get; }
    public uint RepresentativeSlot { get; }
}
