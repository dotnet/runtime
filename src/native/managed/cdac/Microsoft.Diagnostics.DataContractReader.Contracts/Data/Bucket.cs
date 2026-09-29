// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.Bucket))]
internal sealed partial class Bucket : IData<Bucket>
{
    [CustomInit(nameof(InitKeys))] public partial TargetPointer[] Keys { get; }
    [CustomInit(nameof(InitValues))] public partial TargetPointer[] Values { get; }

    [DataDescriptorDependency(nameof(Keys), "pointer")]
    private partial TargetPointer[] InitKeys(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.Bucket);
        ulong keysStart = address + (ulong)type.Fields[nameof(Keys)].Offset;
        uint numSlots = target.ReadGlobal<uint>(Constants.Globals.HashMapSlotsPerBucket);
        TargetPointer[] keys = new TargetPointer[numSlots];
        for (int i = 0; i < numSlots; i++)
        {
            keys[i] = target.ReadPointer(keysStart + (ulong)(i * target.PointerSize));
        }

        return keys;
    }

    [DataDescriptorDependency(nameof(Values), "pointer")]
    private partial TargetPointer[] InitValues(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.Bucket);
        ulong valuesStart = address + (ulong)type.Fields[nameof(Values)].Offset;
        uint numSlots = target.ReadGlobal<uint>(Constants.Globals.HashMapSlotsPerBucket);
        TargetPointer[] values = new TargetPointer[numSlots];
        for (int i = 0; i < numSlots; i++)
        {
            values[i] = target.ReadPointer(valuesStart + (ulong)(i * target.PointerSize));
        }

        return values;
    }
}
