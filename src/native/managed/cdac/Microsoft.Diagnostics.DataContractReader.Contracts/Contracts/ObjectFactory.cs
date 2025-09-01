// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public sealed class ObjectFactory : IContractFactory<IObject>
{
    IObject IContractFactory<IObject>.CreateContract(Target target, int version)
    {
        ulong methodTableOffset = (ulong)target.GetTypeInfo(DataType.Object).Fields["m_pMethTab"].Offset;
        byte objectToMethodTableUnmask = target.ReadGlobal<byte>(Constants.Globals.ObjectToMethodTableUnmask);
        TargetPointer stringMethodTable = target.ReadPointer(
            target.ReadGlobalPointer(Constants.Globals.StringMethodTable));
        TargetPointer syncTableEntries = target.ReadPointer(
            target.ReadGlobalPointer(Constants.Globals.SyncTableEntries));
        return version switch
        {
            1 => new Object_1(target, methodTableOffset, objectToMethodTableUnmask, stringMethodTable, syncTableEntries),
            _ => default(Object),
        };
    }

}
