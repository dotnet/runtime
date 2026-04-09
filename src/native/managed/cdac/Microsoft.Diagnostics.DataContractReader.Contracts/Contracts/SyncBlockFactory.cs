// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public sealed class SyncBlockFactory : IContractFactory<ISyncBlock>
{
    ISyncBlock IContractFactory<ISyncBlock>.CreateContract(Target target, int version)
    {
        TargetPointer syncTableEntries = target.ReadPointer(
            target.ReadGlobalPointer(Constants.Globals.SyncTableEntries));
        ulong syncBlockLinkOffset = (ulong)target.GetTypeInfo(DataType.SyncBlock).Fields[nameof(Data.SyncBlock.LinkNext)].Offset;
        return version switch
        {
            1 => new SyncBlock_1(target, syncTableEntries, syncBlockLinkOffset),
            _ => default(SyncBlock),
        };
    }

}
