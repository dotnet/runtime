// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal interface IObject : IContract
{
    static string IContract.Name { get; } = nameof(Object);
    static IContract IContract.Create(Target target, int version)
    {
        ulong methodTableOffset = (ulong)target.GetTypeInfo(DataType.Object).Fields["m_pMethTab"].Offset;
        byte objectToMethodTableUnmask = target.ReadGlobal<byte>(Constants.Globals.ObjectToMethodTableUnmask);
        TargetPointer stringMethodTable = target.ReadPointer(
            target.ReadGlobalPointer(Constants.Globals.StringMethodTable));
        return version switch
        {
            1 => new Object_1(target, methodTableOffset, objectToMethodTableUnmask, stringMethodTable),
            _ => default(Object),
        };
    }

    public virtual TargetPointer GetMethodTableAddress(TargetPointer address) => throw new NotImplementedException();

    public virtual string GetStringValue(TargetPointer address) => throw new NotImplementedException();
    public virtual TargetPointer GetArrayData(TargetPointer address, out uint count, out TargetPointer boundsStart, out TargetPointer lowerBounds) => throw new NotImplementedException();
}

internal readonly struct Object : IObject
{
    // Everything throws NotImplementedException
}
