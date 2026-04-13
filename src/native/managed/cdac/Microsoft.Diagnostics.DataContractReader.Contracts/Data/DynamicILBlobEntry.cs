// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class DynamicILBlobEntry : IData<DynamicILBlobEntry>
{
    static DynamicILBlobEntry IData<DynamicILBlobEntry>.Create(Target target, TargetPointer address)
        => new DynamicILBlobEntry(target, address);

    public DynamicILBlobEntry(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.DynamicILBlobTable);
        EntryMethodToken = target.ReadField<uint>(address, type, nameof(EntryMethodToken));
        EntryIL = target.ReadPointerField(address, type, nameof(EntryIL));
    }

    public DynamicILBlobEntry(uint entryMethodToken, TargetPointer entryIL)
    {
        EntryMethodToken = entryMethodToken;
        EntryIL = entryIL;
    }

    public uint EntryMethodToken { get; }
    public TargetPointer EntryIL { get; }
}
