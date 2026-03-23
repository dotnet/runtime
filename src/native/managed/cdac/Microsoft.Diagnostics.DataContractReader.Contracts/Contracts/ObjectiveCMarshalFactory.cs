// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public sealed class ObjectiveCMarshalFactory : IContractFactory<IObjectiveCMarshal>
{
    IObjectiveCMarshal IContractFactory<IObjectiveCMarshal>.CreateContract(Target target, int version)
    {
        TargetPointer syncTableEntries = target.ReadPointer(
            target.ReadGlobalPointer(Constants.Globals.SyncTableEntries));
        uint syncBlockIsHashOrSyncBlockIndex = target.ReadGlobal<uint>(Constants.Globals.SyncBlockIsHashOrSyncBlockIndex);
        uint syncBlockIsHashCode = target.ReadGlobal<uint>(Constants.Globals.SyncBlockIsHashCode);
        uint syncBlockIndexMask = target.ReadGlobal<uint>(Constants.Globals.SyncBlockIndexMask);
        return version switch
        {
            1 => new ObjectiveCMarshal_1(target, syncTableEntries, syncBlockIsHashOrSyncBlockIndex, syncBlockIsHashCode, syncBlockIndexMask),
            _ => default(ObjectiveCMarshal),
        };
    }
}
