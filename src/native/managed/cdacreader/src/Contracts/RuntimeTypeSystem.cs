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

internal readonly struct TypeHandle
{
    private readonly MethodTableHandle? _mtHandle;

    internal TypeHandle(MethodTableHandle mtHandle)
    {
        _mtHandle = mtHandle;
    }

    public static implicit operator TypeHandle(MethodTableHandle mtHandle) => new TypeHandle(mtHandle);

    public MethodTableHandle AsMethodTable => _mtHandle!.Value;
    public bool IsMethodTable => _mtHandle.HasValue;
    public bool IsNull => !_mtHandle.HasValue;
}

internal enum CorElementType
{
    Void = 1,
    Boolean = 2,
    Char = 3,
    I1 = 4,
    U1 = 5,
    I2 = 6,
    U2 = 7,
    I4 = 8,
    U4 = 9,
    I8 = 0xa,
    U8 = 0xb,
    R4 = 0xc,
    R8 = 0xd,
    String = 0xe,
    Ptr = 0xf,
    Byref = 0x10,
    ValueType = 0x11,
    Class = 0x12,
    Var = 0x13,
    Array = 0x14,
    GenericInst = 0x15,
    TypedByRef = 0x16,
    I = 0x18,
    U = 0x19,
    FnPtr = 0x1b,
    Object = 0x1c,
    SzArray = 0x1d,
    MVar = 0x1e,
    CModReqd = 0x1f,
    CModOpt = 0x20,
    Internal = 0x21,
    Sentinel = 0x41,
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

    public virtual uint GetInstantiation(MethodTableHandle methodTable, out TargetPointer instantiation) => throw new NotImplementedException();
    public virtual bool IsGenericTypeDefinition(MethodTableHandle methodTable) => throw new NotImplementedException();
    #endregion MethodTable inspection APIs

    #region TypeHandle inspection APIs
    public virtual bool HasTypeParam(TypeHandle typeHandle) => throw new NotImplementedException();
    public virtual CorElementType GetSignatureCorElementType(TypeHandle typeHandle) => throw new NotImplementedException();

    // return true if the TypeHandle represents an array, and set the rank to either 0 (if the type is not an array), or the rank number if it is.
    public virtual bool IsArray(TypeHandle typeHandle, out uint rank) => throw new NotImplementedException();
    public virtual TypeHandle GetTypeParam(TypeHandle typeHandle) => throw new NotImplementedException();
    public virtual bool IsGenericVariable(TypeHandle typeHandle, out TargetPointer module, out uint token) => throw new NotImplementedException();
    public virtual bool IsFunctionPointer(TypeHandle typeHandle, out ReadOnlySpan<TypeHandle> retAndArgTypes, out byte callConv) => throw new NotImplementedException();
    // Returns null if the TypeHandle is not a class/struct/generic variable

    // Default implementation is implemented in terms of other apis already on RuntimeTypeSystem
    public virtual TargetPointer GetModule(TypeHandle typeHandle)
    {
        if (typeHandle.IsMethodTable)
            return GetModule(typeHandle.AsMethodTable);
        else
        {
            if (HasTypeParam(typeHandle))
            {
                return GetModule(GetTypeParam(typeHandle));
            }
            else if (IsGenericVariable(typeHandle, out TargetPointer genericParamModule, out _))
            {
                return genericParamModule;
            }
            else
            {
                System.Diagnostics.Debug.Assert(IsFunctionPointer(typeHandle, out _, out _));
                return TargetPointer.Null;
            }
        }
    }

    #endregion TypeHandle inspection APIs
}

internal struct RuntimeTypeSystem : IRuntimeTypeSystem
{
    // Everything throws NotImplementedException
}
