// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.TableSegment))]
internal sealed partial class TableSegment : IData<TableSegment>
{
    [Field] public TargetPointer NextSegment { get; }

    [FieldAddress]
    public TargetPointer RgValue { get; }

    public byte[] RgTail { get; private set; }
    public byte[] RgAllocation { get; private set; }
    public byte[] RgUserData { get; private set; }

    [MemberNotNull(nameof(RgTail), nameof(RgAllocation), nameof(RgUserData))]
    partial void OnInit(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.TableSegment);
        uint handleBlocksPerSegment = target.ReadGlobal<uint>(Constants.Globals.HandleBlocksPerSegment);
        uint handleMaxInternalTypes = target.ReadGlobal<uint>(Constants.Globals.HandleMaxInternalTypes);

        TargetPointer rgTailPtr = address + (ulong)type.Fields[nameof(RgTail)].Offset;
        RgTail = new byte[handleMaxInternalTypes];
        target.ReadBuffer(rgTailPtr, RgTail);

        TargetPointer rgAllocationPtr = address + (ulong)type.Fields[nameof(RgAllocation)].Offset;
        RgAllocation = new byte[handleBlocksPerSegment];
        target.ReadBuffer(rgAllocationPtr, RgAllocation);

        TargetPointer rgUserDataPtr = address + (ulong)type.Fields[nameof(RgUserData)].Offset;
        RgUserData = new byte[handleBlocksPerSegment];
        target.ReadBuffer(rgUserDataPtr, RgUserData);
    }
}
