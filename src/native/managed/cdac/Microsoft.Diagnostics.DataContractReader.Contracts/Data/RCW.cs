// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.RCW))]
internal sealed partial class RCW : IData<RCW>
{
    [Field] public TargetPointer NextCleanupBucket { get; }
    [Field] public TargetPointer NextRCW { get; }
    [Field] public uint Flags { get; }
    [Field] public TargetPointer CtxCookie { get; }
    [Field] public TargetPointer CtxEntry { get; }
    [Field] public TargetPointer IdentityPointer { get; }
    [Field] public uint SyncBlockIndex { get; }
    [Field] public TargetPointer VTablePtr { get; }
    [Field] public TargetPointer CreatorThread { get; }
    [Field] public uint RefCount { get; }
    [Field] public TargetPointer UnknownPointer { get; }

    public IReadOnlyList<Data.InterfaceEntry> InterfaceEntries { get; private set; } = [];

    [MemberNotNull(nameof(InterfaceEntries))]
    partial void OnInit(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.RCW);
        TargetPointer interfaceEntriesAddr = address + (ulong)type.Fields[nameof(InterfaceEntries)].Offset;

        uint cacheSize = target.ReadGlobal<uint>(Constants.Globals.RCWInterfaceCacheSize);
        Target.TypeInfo entryTypeInfo = target.GetTypeInfo(DataType.InterfaceEntry);
        uint entrySize = entryTypeInfo.Size!.Value;

        List<Data.InterfaceEntry> entries = new((int)cacheSize);
        for (uint i = 0; i < cacheSize; i++)
        {
            TargetPointer entryAddress = interfaceEntriesAddr + i * entrySize;
            entries.Add(target.ProcessedData.GetOrAdd<Data.InterfaceEntry>(entryAddress));
        }

        InterfaceEntries = entries;
    }
}
