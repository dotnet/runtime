// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.RCW))]
internal sealed partial class RCW : IData<RCW>
{
    [Field] public partial TargetPointer NextCleanupBucket { get; }
    [Field] public partial TargetPointer NextRCW { get; }
    [Field] public partial uint Flags { get; }
    [Field] public partial TargetPointer CtxCookie { get; }
    [Field] public partial TargetPointer CtxEntry { get; }
    [Field] public partial TargetPointer IdentityPointer { get; }
    [Field] public partial uint SyncBlockIndex { get; }
    [Field] public partial TargetPointer VTablePtr { get; }
    [Field] public partial TargetPointer CreatorThread { get; }
    [Field] public partial uint RefCount { get; }
    [Field] public partial TargetPointer UnknownPointer { get; }
    [DataDescriptorDependency(nameof(InterfaceEntries), "pointer")]
    public IReadOnlyList<Data.InterfaceEntry> InterfaceEntries { get; private set; } = [];

    [MemberNotNull(nameof(InterfaceEntries))]
    partial void OnInit(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.RCW);
        TargetPointer interfaceEntriesAddr = address + (ulong)type.Fields[nameof(InterfaceEntries)].Offset;

        uint cacheSize = target.ReadGlobal<uint>(Constants.Globals.RCWInterfaceCacheSize);
        uint entrySize = InterfaceEntry.GetSize(target);

        List<Data.InterfaceEntry> entries = new((int)cacheSize);
        for (uint i = 0; i < cacheSize; i++)
        {
            TargetPointer entryAddress = interfaceEntriesAddr + i * entrySize;
            entries.Add(target.ProcessedData.GetOrAdd<Data.InterfaceEntry>(entryAddress));
        }

        InterfaceEntries = entries;
    }
}
