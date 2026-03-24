// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class TableSegment : IData<TableSegment>
{
    static TableSegment IData<TableSegment>.Create(Target target, TargetPointer address) => new TableSegment(target, address);
    public TableSegment(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.TableSegment);
        NextSegment = target.ReadPointer(address + (ulong)type.Fields[nameof(NextSegment)].Offset);
        uint handleBlocksPerSegment = target.ReadGlobal<uint>(Constants.Globals.HandleBlocksPerSegment);
        uint handleMaxInternalTypes = target.ReadGlobal<uint>(Constants.Globals.HandleMaxInternalTypes);

        TargetPointer rgTailPtr = address + (ulong)type.Fields[nameof(RgTail)].Offset;
        RgTail = new byte[handleMaxInternalTypes];
        target.ReadBuffer(rgTailPtr, RgTail);

        TargetPointer rgAllocationPtr = address + (ulong)type.Fields[nameof(RgAllocation)].Offset;
        RgAllocation = new byte[handleBlocksPerSegment];
        target.ReadBuffer(rgAllocationPtr, RgAllocation);

        // let's not read the entire array because it is large and not always fully mapped.
        RgValue = address + (ulong)type.Fields[nameof(RgValue)].Offset;

        TargetPointer rgUserDataPtr = address + (ulong)type.Fields[nameof(RgUserData)].Offset;
        RgUserData = new byte[handleBlocksPerSegment];
        target.ReadBuffer(rgUserDataPtr, RgUserData);
    }

    public TargetPointer NextSegment { get; init; }
    public byte[] RgTail { get; init; }
    public byte[] RgAllocation { get; init; }
    public TargetPointer RgValue { get; init; }
    public byte[] RgUserData { get; init; }
}
