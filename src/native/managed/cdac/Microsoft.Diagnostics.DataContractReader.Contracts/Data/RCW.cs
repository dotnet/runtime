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

        NextCleanupBucket = target.ReadPointer(address + (ulong)type.Fields[nameof(NextCleanupBucket)].Offset);
        NextRCW = target.ReadPointer(address + (ulong)type.Fields[nameof(NextRCW)].Offset);
        Flags = target.Read<uint>(address + (ulong)type.Fields[nameof(Flags)].Offset);
        CtxCookie = target.ReadPointer(address + (ulong)type.Fields[nameof(CtxCookie)].Offset);
        CtxEntry = target.ReadPointer(address + (ulong)type.Fields[nameof(CtxEntry)].Offset);
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
    public List<Data.InterfaceEntry> InterfaceEntries { get; } = new List<Data.InterfaceEntry>();
}
