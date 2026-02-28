// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class InteropSyncBlockInfo : IData<InteropSyncBlockInfo>
{
    static InteropSyncBlockInfo IData<InteropSyncBlockInfo>.Create(Target target, TargetPointer address)
        => new InteropSyncBlockInfo(target, address);

    public InteropSyncBlockInfo(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.InteropSyncBlockInfo);

        // m_pRCW uses bit0 as a lock bit (see InteropSyncBlockInfo::DacGetRawRCW in syncblk.h).
        // Mask off the lock bit to get the real pointer; value of 0 (or 1 sentinel) means no RCW.
        if (type.Fields.TryGetValue(nameof(RCW), out Target.FieldInfo rcwField))
        {
            ulong rawRcw = target.ReadPointer(address + (ulong)rcwField.Offset).Value;
            ulong maskedRcw = rawRcw & ~(ulong)1;
            RCW = maskedRcw != 0 ? new TargetPointer(maskedRcw) : TargetPointer.Null;
        }
        else
        {
            RCW = TargetPointer.Null;
        }

        // m_pCCW uses 0x1 as a sentinel meaning "was set but now null" (see InteropSyncBlockInfo::GetCCW).
        if (type.Fields.TryGetValue(nameof(CCW), out Target.FieldInfo ccwField))
        {
            ulong rawCcw = target.ReadPointer(address + (ulong)ccwField.Offset).Value;
            CCW = rawCcw == 1 ? TargetPointer.Null : new TargetPointer(rawCcw);
        }
        else
        {
            CCW = TargetPointer.Null;
        }
    }

    public TargetPointer RCW { get; init; }
    public TargetPointer CCW { get; init; }
}
