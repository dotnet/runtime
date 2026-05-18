// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class RCW : IData<RCW>
{
    static RCW IData<RCW>.Create(Target target, TargetPointer address) => new RCW(target, address);
    public RCW(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.RCW);

        NextCleanupBucket = target.ReadPointerField(address, type, nameof(NextCleanupBucket));
        NextRCW = target.ReadPointerField(address, type, nameof(NextRCW));
        Flags = target.ReadField<uint>(address, type, nameof(Flags));
        CtxCookie = target.ReadPointerField(address, type, nameof(CtxCookie));
        CtxEntry = target.ReadPointerField(address, type, nameof(CtxEntry));
        IdentityPointer = target.ReadPointerField(address, type, nameof(IdentityPointer));
        SyncBlockIndex = target.ReadField<uint>(address, type, nameof(SyncBlockIndex));
        VTablePtr = target.ReadPointerField(address, type, nameof(VTablePtr));
        CreatorThread = target.ReadPointerField(address, type, nameof(CreatorThread));
        RefCount = target.ReadField<uint>(address, type, nameof(RefCount));
        UnknownPointer = target.ReadPointerField(address, type, nameof(UnknownPointer));
        TargetPointer interfaceEntriesAddr = address + (ulong)type.Fields[nameof(InterfaceEntries)].Offset;

        uint cacheSize = target.ReadGlobal<uint>(Constants.Globals.RCWInterfaceCacheSize);
        Target.TypeInfo entryTypeInfo = target.GetTypeInfo(DataType.InterfaceEntry);
        uint entrySize = entryTypeInfo.Size!.Value;

        for (uint i = 0; i < cacheSize; i++)
        {
            TargetPointer entryAddress = interfaceEntriesAddr + i * entrySize;
            InterfaceEntries.Add(target.ProcessedData.GetOrAdd<Data.InterfaceEntry>(entryAddress));
        }
    }

    public TargetPointer NextCleanupBucket { get; init; }
    public TargetPointer NextRCW { get; init; }
    public uint Flags { get; init; }
    public TargetPointer CtxCookie { get; init; }
    public TargetPointer CtxEntry { get; init; }
    public TargetPointer IdentityPointer { get; init; }
    public uint SyncBlockIndex { get; init; }
    public TargetPointer VTablePtr { get; init; }
    public TargetPointer CreatorThread { get; init; }
    public uint RefCount { get; init; }
    public TargetPointer UnknownPointer { get; init; }
    public List<Data.InterfaceEntry> InterfaceEntries { get; } = new List<Data.InterfaceEntry>();
}
