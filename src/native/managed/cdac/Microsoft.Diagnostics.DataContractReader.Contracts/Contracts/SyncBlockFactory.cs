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

        return version switch
        {
            1 => new SyncBlock_1(target, syncTableEntries),
            _ => default(SyncBlock),
        };
    }
}
