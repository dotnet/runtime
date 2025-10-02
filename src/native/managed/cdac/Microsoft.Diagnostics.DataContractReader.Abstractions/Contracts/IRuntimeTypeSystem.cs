// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

// an opaque handle to a type handle.  See IMetadata.GetMethodTableData
public readonly struct TypeHandle
{
    // TODO-Layering: These members should be accessible only to contract implementations.
    public TypeHandle(TargetPointer address)
    {
        Address = address;
    }

    public TargetPointer Address { get; }

    public bool IsNull => Address == 0;
}

public enum CorElementType
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
    CModInternal = 0x22,
    Sentinel = 0x41,
}

public readonly struct MethodDescHandle
{
    // TODO-Layering: These members should be accessible only to contract implementations.
    public MethodDescHandle(TargetPointer address)
    {
        Address = address;
    }

    public TargetPointer Address { get; }
}

public enum ArrayFunctionType
{
    Get = 0,
    Set = 1,
    Address = 2,
    Constructor = 3
}

public interface IRuntimeTypeSystem : IContract
{
    static string IContract.Name => nameof(RuntimeTypeSystem);

    #region TypeHandle inspection APIs
    TypeHandle GetTypeHandle(TargetPointer address) => throw new NotImplementedException();
    TargetPointer GetModule(TypeHandle typeHandle) => throw new NotImplementedException();
    TargetPointer GetLoaderModule(TypeHandle typeHandle) => throw new NotImplementedException();

    // A canonical method table is either the MethodTable itself, or in the case of a generic instantiation, it is the
    // MethodTable of the prototypical instance.
    TargetPointer GetCanonicalMethodTable(TypeHandle typeHandle) => throw new NotImplementedException();
    TargetPointer GetParentMethodTable(TypeHandle typeHandle) => throw new NotImplementedException();

    TargetPointer GetMethodDescForSlot(TypeHandle methodTable, ushort slot) => throw new NotImplementedException();
    IEnumerable<TargetPointer> GetIntroducedMethodDescs(TypeHandle methodTable) => throw new NotImplementedException();
    TargetCodePointer GetSlot(TypeHandle typeHandle, uint slot) => throw new NotImplementedException();

    uint GetBaseSize(TypeHandle typeHandle) => throw new NotImplementedException();
    // The component size is only available for strings and arrays.  It is the size of the element type of the array, or the size of an ECMA 335 character (2 bytes)
    uint GetComponentSize(TypeHandle typeHandle) => throw new NotImplementedException();

    // True if the MethodTable is the sentinel value associated with unallocated space in the managed heap
    bool IsFreeObjectMethodTable(TypeHandle typeHandle) => throw new NotImplementedException();
    bool IsString(TypeHandle typeHandle) => throw new NotImplementedException();
    // True if the MethodTable represents a type that contains managed references
    bool ContainsGCPointers(TypeHandle typeHandle) => throw new NotImplementedException();
    bool IsDynamicStatics(TypeHandle typeHandle) => throw new NotImplementedException();
    ushort GetNumInterfaces(TypeHandle typeHandle) => throw new NotImplementedException();

    // Returns an ECMA-335 TypeDef table token for this type, or for its generic type definition if it is a generic instantiation
    uint GetTypeDefToken(TypeHandle typeHandle) => throw new NotImplementedException();
    ushort GetNumVtableSlots(TypeHandle typeHandle) => throw new NotImplementedException();
    ushort GetNumMethods(TypeHandle typeHandle) => throw new NotImplementedException();
    // Returns the ECMA 335 TypeDef table Flags value (a bitmask of TypeAttributes) for this type,
    // or for its generic type definition if it is a generic instantiation
    uint GetTypeDefTypeAttributes(TypeHandle typeHandle) => throw new NotImplementedException();
    ushort GetNumInstanceFields(TypeHandle typeHandle) => throw new NotImplementedException();
    ushort GetNumStaticFields(TypeHandle typeHandle) => throw new NotImplementedException();
    ushort GetNumThreadStaticFields(TypeHandle typeHandle) => throw new NotImplementedException();
    TargetPointer GetFieldDescList(TypeHandle typeHandle) => throw new NotImplementedException();
    TargetPointer GetGCStaticsBasePointer(TypeHandle typeHandle) => throw new NotImplementedException();
    TargetPointer GetNonGCStaticsBasePointer(TypeHandle typeHandle) => throw new NotImplementedException();
    TargetPointer GetGCThreadStaticsBasePointer(TypeHandle typeHandle, TargetPointer threadPtr) => throw new NotImplementedException();
    TargetPointer GetNonGCThreadStaticsBasePointer(TypeHandle typeHandle, TargetPointer threadPtr) => throw new NotImplementedException();


    ReadOnlySpan<TypeHandle> GetInstantiation(TypeHandle typeHandle) => throw new NotImplementedException();
    public bool IsClassInited(TypeHandle typeHandle) => throw new NotImplementedException();
    public bool IsInitError(TypeHandle typeHandle) => throw new NotImplementedException();
    bool IsGenericTypeDefinition(TypeHandle typeHandle) => throw new NotImplementedException();
    bool IsCollectible(TypeHandle typeHandle) => throw new NotImplementedException();

    bool HasTypeParam(TypeHandle typeHandle) => throw new NotImplementedException();

    // Element type of the type. NOTE: this drops the CorElementType.GenericInst, and CorElementType.String is returned as CorElementType.Class.
    // If this returns CorElementType.ValueType it may be a normal valuetype or a "NATIVE" valuetype used to represent an interop view on a structure
    // HasTypeParam will return true for cases where this is the interop view
    CorElementType GetSignatureCorElementType(TypeHandle typeHandle) => throw new NotImplementedException();

    // return true if the TypeHandle represents an array, and set the rank to either 0 (if the type is not an array), or the rank number if it is.
    bool IsArray(TypeHandle typeHandle, out uint rank) => throw new NotImplementedException();
    TypeHandle GetTypeParam(TypeHandle typeHandle) => throw new NotImplementedException();
    TypeHandle GetConstructedType(TypeHandle typeHandle, CorElementType corElementType, int rank, ImmutableArray<TypeHandle> typeArguments) => throw new NotImplementedException();
    TypeHandle GetPrimitiveType(CorElementType typeCode) => throw new NotImplementedException();
    TypeHandle GetTypeByNameAndModule(string name, string nameSpace, ModuleHandle moduleHandle) => throw new NotImplementedException();
    void GetNameSpaceAndNameFromBinder(ushort index, out string nameSpace, out string name) => throw new NotImplementedException();
    bool IsGenericVariable(TypeHandle typeHandle, out TargetPointer module, out uint token) => throw new NotImplementedException();
    bool IsFunctionPointer(TypeHandle typeHandle, out ReadOnlySpan<TypeHandle> retAndArgTypes, out byte callConv) => throw new NotImplementedException();
    bool IsPointer(TypeHandle typeHandle) => throw new NotImplementedException();
    // Returns null if the TypeHandle is not a class/struct/generic variable
    #endregion TypeHandle inspection APIs

    #region MethodDesc inspection APIs
    MethodDescHandle GetMethodDescHandle(TargetPointer targetPointer) => throw new NotImplementedException();
    TargetPointer GetMethodTable(MethodDescHandle methodDesc) => throw new NotImplementedException();

    // Return true for an uninstantiated generic method
    bool IsGenericMethodDefinition(MethodDescHandle methodDesc) => throw new NotImplementedException();
    ReadOnlySpan<TypeHandle> GetGenericMethodInstantiation(MethodDescHandle methodDesc) => throw new NotImplementedException();

    // Return mdtMethodDef (0x06000000) if the method doesn't have a token, otherwise return the token of the method
    uint GetMethodToken(MethodDescHandle methodDesc) => throw new NotImplementedException();

    // Return true if a MethodDesc represents an array method
    // An array method is also a StoredSigMethodDesc
    bool IsArrayMethod(MethodDescHandle methodDesc, out ArrayFunctionType functionType) => throw new NotImplementedException();

    // Return true if a MethodDesc represents a method without metadata, either an IL Stub dynamically
    // generated by the runtime, or a MethodDesc that describes a method represented by the System.Reflection.Emit.DynamicMethod class
    // Or something else similar.
    // A no metadata method is also a StoredSigMethodDesc
    bool IsNoMetadataMethod(MethodDescHandle methodDesc, out string methodName) => throw new NotImplementedException();
    // A StoredSigMethodDesc is a MethodDesc for which the signature isn't found in metadata.
    bool IsStoredSigMethodDesc(MethodDescHandle methodDesc, out ReadOnlySpan<byte> signature) => throw new NotImplementedException();

    // Return true for a MethodDesc that describes a method represented by the System.Reflection.Emit.DynamicMethod class
    // A DynamicMethod is also a StoredSigMethodDesc, and a NoMetadataMethod
    bool IsDynamicMethod(MethodDescHandle methodDesc) => throw new NotImplementedException();

    // Return true if a MethodDesc represents an IL Stub dynamically generated by the runtime
    // A IL Stub method is also a StoredSigMethodDesc, and a NoMetadataMethod
    bool IsILStub(MethodDescHandle methodDesc) => throw new NotImplementedException();

    // Return true if a MethodDesc represents an IL stub with a special MethodDesc context arg
    bool HasMDContextArg(MethodDescHandle methodDesc) => throw new NotImplementedException();

    bool IsCollectibleMethod(MethodDescHandle methodDesc) => throw new NotImplementedException();
    bool IsVersionable(MethodDescHandle methodDesc) => throw new NotImplementedException();

    TargetPointer GetMethodDescVersioningState(MethodDescHandle methodDesc) => throw new NotImplementedException();

    TargetCodePointer GetNativeCode(MethodDescHandle methodDesc) => throw new NotImplementedException();
    TargetCodePointer GetMethodEntryPointIfExists(MethodDescHandle methodDesc) => throw new NotImplementedException();

    ushort GetSlotNumber(MethodDescHandle methodDesc) => throw new NotImplementedException();

    bool HasNativeCodeSlot(MethodDescHandle methodDesc) => throw new NotImplementedException();

    TargetPointer GetAddressOfNativeCodeSlot(MethodDescHandle methodDesc) => throw new NotImplementedException();

    TargetPointer GetGCStressCodeCopy(MethodDescHandle methodDesc) => throw new NotImplementedException();
    #endregion MethodDesc inspection APIs
    #region FieldDesc inspection APIs
    TargetPointer GetMTOfEnclosingClass(TargetPointer fieldDescPointer) => throw new NotImplementedException();
    uint GetFieldDescMemberDef(TargetPointer fieldDescPointer) => throw new NotImplementedException();
    bool IsFieldDescThreadStatic(TargetPointer fieldDescPointer) => throw new NotImplementedException();
    bool IsFieldDescStatic(TargetPointer fieldDescPointer) => throw new NotImplementedException();
    CorElementType GetFieldDescType(TargetPointer fieldDescPointer) => throw new NotImplementedException();
    uint GetFieldDescOffset(TargetPointer fieldDescPointer, FieldDefinition fieldDef) => throw new NotImplementedException();
    #endregion FieldDesc inspection APIs
}

public struct RuntimeTypeSystem : IRuntimeTypeSystem
{
    // Everything throws NotImplementedException
}
