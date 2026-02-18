// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public sealed class GCFactory : IContractFactory<IGC>
{
    IGC IContractFactory<IGC>.CreateContract(Target target, int version)
    {
        uint handlesPerBlock = target.ReadGlobal<uint>(Constants.Globals.HandlesPerBlock);
        byte blockInvalid = target.ReadGlobal<byte>(Constants.Globals.BlockInvalid);
        TargetPointer debugDestroyedHandleValue = target.ReadGlobalPointer(Constants.Globals.DebugDestroyedHandleValue);
        return version switch
        {
            1 => new GC_1(target, handlesPerBlock, blockInvalid, debugDestroyedHandleValue),
            _ => default(GC),
        };
    }
}
