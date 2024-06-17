// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

// an opaque handle to a method table.  See IMetadata.GetMethodTableData
internal readonly struct MethodTableHandle
{
    internal MethodTableHandle(TargetPointer address)
    {
        Address = address;
    }

    internal TargetPointer Address { get; }
}

internal interface IMetadata : IContract
{
    static string IContract.Name => nameof(Metadata);
    static IContract IContract.Create(Target target, int version)
    {
        TargetPointer targetPointer = target.ReadGlobalPointer(Constants.Globals.FreeObjectMethodTable);
        TargetPointer freeObjectMethodTable = target.ReadPointer(targetPointer);
        return version switch
        {
            1 => new Metadata_1(target, freeObjectMethodTable),
            _ => default(Metadata),
        };
    }

    public virtual MethodTableHandle GetMethodTableData(TargetPointer targetPointer) => throw new NotImplementedException();

    public virtual TargetPointer GetClass(MethodTableHandle methodTable) => throw new NotImplementedException();

    public virtual uint GetBaseSize(MethodTableHandle methodTable) => throw new NotImplementedException();
    public virtual uint GetComponentSize(MethodTableHandle methodTable) => throw new NotImplementedException();

    public virtual bool IsFreeObjectMethodTable(MethodTableHandle methodTable) => throw new NotImplementedException();
    public virtual bool IsString(MethodTableHandle methodTable) => throw new NotImplementedException();
    public virtual bool ContainsPointers(MethodTableHandle methodTable) => throw new NotImplementedException();
    public virtual bool IsDynamicStatics(MethodTableHandle methodTable) => throw new NotImplementedException();
    public virtual uint GetTypeDefToken(MethodTableHandle methodTable) => throw new NotImplementedException();
    public virtual ushort GetNumMethods(MethodTableHandle methodTable) => throw new NotImplementedException();

    public virtual ushort GetNumVtableSlots(MethodTableHandle methodTable) => throw new NotImplementedException();

    public virtual uint GetTypeDefTypeAttributes(MethodTableHandle methodTable) => throw new NotImplementedException();
}

internal struct Metadata : IMetadata
{
    // Everything throws NotImplementedException
}
