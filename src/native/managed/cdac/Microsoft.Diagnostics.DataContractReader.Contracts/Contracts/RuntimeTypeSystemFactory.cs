// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public sealed class RuntimeTypeSystemFactory : IContractFactory<IRuntimeTypeSystem>
{
    IRuntimeTypeSystem IContractFactory<IRuntimeTypeSystem>.CreateContract(Target target, int version)
    {
        TargetPointer targetPointer = target.ReadGlobalPointer(Constants.Globals.FreeObjectMethodTable);
        TargetPointer freeObjectMethodTable = target.ReadPointer(targetPointer);
        ulong methodDescAlignment = target.ReadGlobal<ulong>(Constants.Globals.MethodDescAlignment);
        return version switch
        {
            1 => new RuntimeTypeSystem_1(target, new RuntimeTypeSystemHelpers.TypeValidation(target), new RuntimeTypeSystemHelpers.MethodValidation(target, methodDescAlignment), freeObjectMethodTable, methodDescAlignment),
            _ => default(RuntimeTypeSystem),
        };
    }
}
