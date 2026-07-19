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

public readonly record struct TypedByRefInfo(TargetPointer Data, TargetPointer TypeHandle);

public enum ArrayFunctionType
{
    Get = 0,
    Set = 1,
    Address = 2,
    Constructor = 3
}

public enum OptimizationTier : uint
{
    OptimizationTierUnknown,
    OptimizationTier0,
    OptimizationTier1,
    OptimizationTier1OSR,
    OptimizationTierOptimized,
    OptimizationTier0Instrumented,
    OptimizationTier1Instrumented,
}

public enum GenericContextLoc
{
    None,
    InstArgMethodDesc,
    InstArgMethodTable,
    ThisPtr,
}

public enum WellKnownMethodTable
{
    Object,
    String,
    Array,
    Exception,
    Free,
    Canon,
}

// cDAC-owned representation of a SystemV AMD64 eightbyte register classification. The values mirror
// the ABI classification (see the runtime's Internal.JitInterface.SystemVClassificationType), but the
// enum is owned by the cDAC contract surface so consumers don't depend on runtime-internal types.
public enum SystemVAmd64Classification : byte
{
    Unknown = 0,
    Struct = 1,
    NoClass = 2,
    Memory = 3,
    Integer = 4,
    IntegerReference = 5,
    IntegerByRef = 6,
    SSE = 7,
}

// A single SystemV AMD64 eightbyte register-passing slot for a value type: how that eightbyte is
// classified and how many of its bytes are occupied (1-8; the final eightbyte may be partial).
public readonly record struct SystemVAmd64EightByte(
    SystemVAmd64Classification Classification,
    byte Size);

// SystemV AMD64 eightbyte register-passing classification for a value type, read from the type's
// EEClass optional fields (populated only on UNIX_AMD64_ABI builds). A value type is passed in at
// most two eightbytes (CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS): First is always
// present, Second is present only when the value spans a second eightbyte.
public readonly record struct SystemVAmd64EightByteClassification(
    SystemVAmd64EightByte First,
    SystemVAmd64EightByte? Second);


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
    // True if this MethodTable is the canonical MethodTable (i.e., EEClassOrCanonMT points directly to the EEClass)
    bool IsCanonicalMethodTable(TypeHandle typeHandle) => throw new NotImplementedException();
    TargetPointer GetParentMethodTable(TypeHandle typeHandle) => throw new NotImplementedException();

    TargetPointer GetMethodDescForSlot(TypeHandle methodTable, ushort slot) => throw new NotImplementedException();
    IEnumerable<TargetPointer> GetIntroducedMethodDescs(TypeHandle methodTable) => throw new NotImplementedException();
    TargetCodePointer GetSlot(TypeHandle typeHandle, uint slot) => throw new NotImplementedException();

    uint GetBaseSize(TypeHandle typeHandle) => throw new NotImplementedException();
    uint GetNumInstanceFieldBytes(TypeHandle typeHandle) => throw new NotImplementedException();
    // The component size is only available for strings and arrays.  It is the size of the element type of the array, or the size of an ECMA 335 character (2 bytes)
    uint GetComponentSize(TypeHandle typeHandle) => throw new NotImplementedException();

    // True if the MethodTable is the sentinel value associated with unallocated space in the managed heap
    bool IsFreeObjectMethodTable(TypeHandle typeHandle) => throw new NotImplementedException();
    // True if the MethodTable is the System.Object MethodTable (g_pObjectClass)
    bool IsObject(TypeHandle typeHandle) => throw new NotImplementedException();
    bool IsString(TypeHandle typeHandle) => throw new NotImplementedException();
    // True if the CorElementType represents a GC-collectable object reference.
    bool IsCorElementTypeObjRef(CorElementType elementType) => throw new NotImplementedException();
    // Returns the address of one of the runtime's well-known singleton MethodTables,
    // or TargetPointer.Null if the runtime has not yet initialized that global.
    TargetPointer GetWellKnownMethodTable(WellKnownMethodTable kind) => throw new NotImplementedException();
    // True if the MethodTable represents a type that contains managed references
    bool ContainsGCPointers(TypeHandle typeHandle) => throw new NotImplementedException();
    // True if MethodTable represents a byreflike value (Span<T>, ReadOnlySpan<T>, etc.).
    bool IsByRefLike(TypeHandle typeHandle) => throw new NotImplementedException();
    // If the type is an HFA (or HVA on ARM64), returns true and sets elementSize
    // to 4, 8, or 16. Returns false otherwise (including on targets that don't
    // define FEATURE_HFA). Mirrors MethodTable::GetHFAType in
    // src/coreclr/vm/class.cpp.
    bool TryGetHFAElementSize(TypeHandle typeHandle, out int elementSize) => throw new NotImplementedException();
    // True if the type requires 8-byte alignment on platforms that don't 8-byte align by default (FEATURE_64BIT_ALIGNMENT)
    bool RequiresAlign8(TypeHandle typeHandle) => throw new NotImplementedException();
    // Returns the cached SystemV AMD64 eightbyte register-passing classification for a value type
    // (used to decide how a struct is passed in registers), or false if the type has no such
    // classification (not applicable, or the runtime was not built with UNIX_AMD64_ABI). Mirrors
    // the EEClass::GetSystemVAmd64EightByteInfo runtime data used by the JIT.
    bool TryGetSystemVAmd64EightByteClassification(TypeHandle typeHandle, out SystemVAmd64EightByteClassification classification) => throw new NotImplementedException();
    // True if the MethodTable represents a continuation subtype that has no metadata of its own
    bool IsContinuationWithoutMetadata(TypeHandle typeHandle) => throw new NotImplementedException();
    /// <summary>
    /// Enumerates GC pointer runs from the CGCDesc stored before the method table.
    /// Returns (offset, size) pairs normalized to actual byte lengths.
    /// See RuntimeTypeSystem.md for the full GCDesc format documentation.
    /// </summary>
    IEnumerable<(uint Offset, uint Size)> GetGCDescSeries(TypeHandle typeHandle, uint numComponents = 0) => throw new NotImplementedException();
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
    IEnumerable<TargetPointer> GetFieldDescList(TypeHandle typeHandle) => throw new NotImplementedException();
    // True if the MethodTable represents a type tracked as an Objective-C reference type with a finalizer
    bool IsTrackedReferenceWithFinalizer(TypeHandle typeHandle) => throw new NotImplementedException();
    TargetPointer GetGCStaticsBasePointer(TypeHandle typeHandle) => throw new NotImplementedException();
    TargetPointer GetNonGCStaticsBasePointer(TypeHandle typeHandle) => throw new NotImplementedException();
    TargetPointer GetGCThreadStaticsBasePointer(TypeHandle typeHandle, TargetPointer threadPtr) => throw new NotImplementedException();
    TargetPointer GetNonGCThreadStaticsBasePointer(TypeHandle typeHandle, TargetPointer threadPtr) => throw new NotImplementedException();


    ReadOnlySpan<TypeHandle> GetInstantiation(TypeHandle typeHandle) => throw new NotImplementedException();
    public bool IsClassInited(TypeHandle typeHandle) => throw new NotImplementedException();
    public bool IsInitError(TypeHandle typeHandle) => throw new NotImplementedException();
    bool IsGenericTypeDefinition(TypeHandle typeHandle) => throw new NotImplementedException();
    bool ContainsGenericVariables(TypeHandle typeHandle) => throw new NotImplementedException();
    bool IsCollectible(TypeHandle typeHandle) => throw new NotImplementedException();

    bool HasTypeParam(TypeHandle typeHandle) => throw new NotImplementedException();

    // Element type of the type. NOTE: this drops the CorElementType.GenericInst, and CorElementType.String is returned as CorElementType.Class.
    // If this returns CorElementType.ValueType it may be a normal valuetype or a "NATIVE" valuetype used to represent an interop view on a structure
    // HasTypeParam will return true for cases where this is the interop view
    CorElementType GetSignatureCorElementType(TypeHandle typeHandle) => throw new NotImplementedException();
    bool IsValueType(TypeHandle typeHandle) => throw new NotImplementedException();

    // Internal element type of the type. Unlike GetSignatureCorElementType, this returns the underlying primitive
    // type for enums (e.g. I4 for an enum with int underlying type) and for PrimitiveValueType categories.
    // For arrays, reference types, and TypeDescs, behaves identically to GetSignatureCorElementType.
    CorElementType GetInternalCorElementType(TypeHandle typeHandle) => throw new NotImplementedException();

    // return true if the TypeHandle represents an enum type.
    bool IsEnum(TypeHandle typeHandle) => throw new NotImplementedException();

    // return true if the TypeHandle represents a delegate type (i.e., its parent is System.MulticastDelegate)
    bool IsDelegate(TypeHandle typeHandle) => throw new NotImplementedException();

    // return true if the TypeHandle represents an array, and set the rank to either 0 (if the type is not an array), or the rank number if it is.
    bool IsArray(TypeHandle typeHandle, out uint rank) => throw new NotImplementedException();
    TypeHandle GetTypeParam(TypeHandle typeHandle) => throw new NotImplementedException();
    TypeHandle GetConstructedType(TypeHandle typeHandle, CorElementType corElementType, int rank, ImmutableArray<TypeHandle> typeArguments, SignatureCallingConvention callConv = SignatureCallingConvention.Default) => throw new NotImplementedException();
    TypeHandle GetPrimitiveType(CorElementType typeCode) => throw new NotImplementedException();
    bool IsGenericVariable(TypeHandle typeHandle, out TargetPointer module, out uint token) => throw new NotImplementedException();
    bool IsFunctionPointer(TypeHandle typeHandle, out ReadOnlySpan<TypeHandle> retAndArgTypes, out SignatureCallingConvention callConv) => throw new NotImplementedException();
    bool IsPointer(TypeHandle typeHandle) => throw new NotImplementedException();
    bool IsTypeDesc(TypeHandle typeHandle) => throw new NotImplementedException();
    TypedByRefInfo GetTypedByRefInfo(TargetPointer typedByRef) => throw new NotImplementedException();
    // Returns null if the TypeHandle is not a class/struct/generic variable
    #endregion TypeHandle inspection APIs

    #region MethodDesc inspection APIs
    MethodDescHandle GetMethodDescHandle(TargetPointer targetPointer) => throw new NotImplementedException();
    TargetPointer GetMethodTable(MethodDescHandle methodDesc) => throw new NotImplementedException();

    // Return true for an uninstantiated generic method
    bool IsGenericMethodDefinition(MethodDescHandle methodDesc) => throw new NotImplementedException();
    ReadOnlySpan<TypeHandle> GetGenericMethodInstantiation(MethodDescHandle methodDesc) => throw new NotImplementedException();

    GenericContextLoc GetGenericContextLoc(MethodDescHandle methodDescHandle) => throw new NotImplementedException();

    // Return true if the method uses the async calling convention (CORINFO_CALLCONV_ASYNCCALL).
    // This corresponds to native MethodDesc::IsAsyncMethod().
    bool IsAsyncMethod(MethodDescHandle methodDesc) => throw new NotImplementedException();

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

    // Gets the raw signature bytes for a MethodDesc by checking stored signature, async variant signature, then metadata.
    // Returns false if no signature could be resolved.
    bool TryGetMethodSignature(MethodDescHandle methodDesc, out ReadOnlySpan<byte> signature) => throw new NotImplementedException();

    // Return true for a MethodDesc that describes a method represented by the System.Reflection.Emit.DynamicMethod class
    // A DynamicMethod is also a StoredSigMethodDesc, and a NoMetadataMethod
    bool IsDynamicMethod(MethodDescHandle methodDesc) => throw new NotImplementedException();

    // Returns true if a MethodDesc represents an IL-backed method
    bool IsIL(MethodDescHandle methodDesc) => throw new NotImplementedException();

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

    OptimizationTier GetMethodDescOptimizationTier(MethodDescHandle methodDescHandle) => throw new NotImplementedException();
    bool IsEligibleForTieredCompilation(MethodDescHandle methodDescHandle) => throw new NotImplementedException();

    bool IsAsyncThunkMethod(MethodDescHandle methodDesc) => throw new NotImplementedException();

    bool IsWrapperStub(MethodDescHandle methodDesc) => throw new NotImplementedException();
    bool IsUnboxingStub(MethodDescHandle methodDesc) => throw new NotImplementedException();
    #endregion MethodDesc inspection APIs
    #region FieldDesc inspection APIs
    TargetPointer GetMTOfEnclosingClass(TargetPointer fieldDescPointer) => throw new NotImplementedException();
    uint GetFieldDescMemberDef(TargetPointer fieldDescPointer) => throw new NotImplementedException();
    bool IsFieldDescThreadStatic(TargetPointer fieldDescPointer) => throw new NotImplementedException();
    bool IsFieldDescStatic(TargetPointer fieldDescPointer) => throw new NotImplementedException();
    bool IsFieldDescRVA(TargetPointer fieldDescPointer) => throw new NotImplementedException();
    CorElementType GetFieldDescType(TargetPointer fieldDescPointer) => throw new NotImplementedException();
    uint GetFieldDescOffset(TargetPointer fieldDescPointer, FieldDefinition? fieldDef) => throw new NotImplementedException();
    TypeHandle GetFieldDescApproxTypeHandle(TargetPointer fieldDescPointer) => throw new NotImplementedException();
    bool TryGetFieldDescNext(TargetPointer fieldDescPointer, out TargetPointer nextFieldDesc) => throw new NotImplementedException();
    TargetPointer GetFieldDescByName(TypeHandle typeHandle, string fieldName) => throw new NotImplementedException();
    TargetPointer GetFieldDescStaticAddress(TargetPointer fieldDescPointer, bool unboxValueTypes = true) => throw new NotImplementedException();
    TargetPointer GetFieldDescThreadStaticAddress(TargetPointer fieldDescPointer, TargetPointer thread, bool unboxValueTypes = true) => throw new NotImplementedException();
    #endregion FieldDesc inspection APIs
}

public struct RuntimeTypeSystem : IRuntimeTypeSystem
{
    // Everything throws NotImplementedException
}
