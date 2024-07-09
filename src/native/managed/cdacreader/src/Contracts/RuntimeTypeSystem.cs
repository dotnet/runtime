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

internal interface IRuntimeTypeSystem : IContract
{
    static string IContract.Name => nameof(RuntimeTypeSystem);
    static IContract IContract.Create(Target target, int version)
    {
        TargetPointer targetPointer = target.ReadGlobalPointer(Constants.Globals.FreeObjectMethodTable);
        TargetPointer freeObjectMethodTable = target.ReadPointer(targetPointer);
        return version switch
        {
            1 => new RuntimeTypeSystem_1(target, freeObjectMethodTable),
            _ => default(RuntimeTypeSystem),
        };
    }

    #region MethodTable inspection APIs
    public virtual MethodTableHandle GetMethodTableHandle(TargetPointer targetPointer) => throw new NotImplementedException();

    public virtual TargetPointer GetModule(MethodTableHandle methodTable) => throw new NotImplementedException();
    // A canonical method table is either the MethodTable itself, or in the case of a generic instantiation, it is the
    // MethodTable of the prototypical instance.
    public virtual TargetPointer GetCanonicalMethodTable(MethodTableHandle methodTable) => throw new NotImplementedException();
    public virtual TargetPointer GetParentMethodTable(MethodTableHandle methodTable) => throw new NotImplementedException();

    public virtual uint GetBaseSize(MethodTableHandle methodTable) => throw new NotImplementedException();
    // The component size is only available for strings and arrays.  It is the size of the element type of the array, or the size of an ECMA 335 character (2 bytes)
    public virtual uint GetComponentSize(MethodTableHandle methodTable) => throw new NotImplementedException();

    // True if the MethodTable is the sentinel value associated with unallocated space in the managed heap
    public virtual bool IsFreeObjectMethodTable(MethodTableHandle methodTable) => throw new NotImplementedException();
    public virtual bool IsString(MethodTableHandle methodTable) => throw new NotImplementedException();
    // True if the MethodTable represents a type that contains managed references
    public virtual bool ContainsGCPointers(MethodTableHandle methodTable) => throw new NotImplementedException();
    public virtual bool IsDynamicStatics(MethodTableHandle methodTable) => throw new NotImplementedException();
    public virtual ushort GetNumMethods(MethodTableHandle methodTable) => throw new NotImplementedException();
    public virtual ushort GetNumInterfaces(MethodTableHandle methodTable) => throw new NotImplementedException();

    // Returns an ECMA-335 TypeDef table token for this type, or for its generic type definition if it is a generic instantiation
    public virtual uint GetTypeDefToken(MethodTableHandle methodTable) => throw new NotImplementedException();
    // Returns the ECMA 335 TypeDef table Flags value (a bitmask of TypeAttributes) for this type,
    // or for its generic type definition if it is a generic instantiation
    public virtual uint GetTypeDefTypeAttributes(MethodTableHandle methodTable) => throw new NotImplementedException();
    #endregion MethodTable inspection APIs
}

internal struct RuntimeTypeSystem : IRuntimeTypeSystem
{
    // Everything throws NotImplementedException
}
