// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class Bucket : IData<Bucket>
{
    static Bucket IData<Bucket>.Create(Target target, TargetPointer address)
        => new Bucket(target, address);

    public Bucket(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.Bucket);
        ulong keysStart = address + (ulong)type.Fields[nameof(Keys)].Offset;
        ulong valuesStart = address + (ulong)type.Fields[nameof(Values)].Offset;

        uint numSlots = target.ReadGlobal<uint>(Constants.Globals.HashMapSlotsPerBucket);
        Keys = new TargetPointer[numSlots];
        Values = new TargetPointer[numSlots];
        for (int i = 0; i < numSlots; i++)
        {
            Keys[i] = target.ReadPointer(keysStart + (ulong)(i * target.PointerSize));
            Values[i] = target.ReadPointer(valuesStart + (ulong)(i * target.PointerSize));
        }
    }

    public TargetPointer[] Keys { get; }
    public TargetPointer[] Values { get; }
}
