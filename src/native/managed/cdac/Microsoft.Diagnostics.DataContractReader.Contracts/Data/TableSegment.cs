// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.TableSegment))]
internal sealed partial class TableSegment : IData<TableSegment>
{
    [Field] public partial TargetPointer NextSegment { get; }

    [FieldAddress]
    public partial TargetPointer RgValue { get; }
    [CustomInit(nameof(InitRgTail))] public partial byte[] RgTail { get; }
    [CustomInit(nameof(InitRgAllocation))] public partial byte[] RgAllocation { get; }
    [CustomInit(nameof(InitRgUserData))] public partial byte[] RgUserData { get; }

    [DataDescriptorDependency(nameof(RgTail), "uint8[]")]
    private partial byte[] InitRgTail(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.TableSegment);
        uint handleMaxInternalTypes = target.ReadGlobal<uint>(Constants.Globals.HandleMaxInternalTypes);
        TargetPointer rgTailPtr = address + (ulong)type.Fields[nameof(RgTail)].Offset;
        byte[] rgTail = new byte[handleMaxInternalTypes];
        target.ReadBuffer(rgTailPtr, rgTail);
        return rgTail;
    }

    [DataDescriptorDependency(nameof(RgAllocation), "uint8[]")]
    private partial byte[] InitRgAllocation(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.TableSegment);
        uint handleBlocksPerSegment = target.ReadGlobal<uint>(Constants.Globals.HandleBlocksPerSegment);
        TargetPointer rgAllocationPtr = address + (ulong)type.Fields[nameof(RgAllocation)].Offset;
        byte[] rgAllocation = new byte[handleBlocksPerSegment];
        target.ReadBuffer(rgAllocationPtr, rgAllocation);
        return rgAllocation;
    }

    [DataDescriptorDependency(nameof(RgUserData), "uint8[]")]
    private partial byte[] InitRgUserData(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.TableSegment);
        uint handleBlocksPerSegment = target.ReadGlobal<uint>(Constants.Globals.HandleBlocksPerSegment);
        TargetPointer rgUserDataPtr = address + (ulong)type.Fields[nameof(RgUserData)].Offset;
        byte[] rgUserData = new byte[handleBlocksPerSegment];
        target.ReadBuffer(rgUserDataPtr, rgUserData);
        return rgUserData;
    }
}
