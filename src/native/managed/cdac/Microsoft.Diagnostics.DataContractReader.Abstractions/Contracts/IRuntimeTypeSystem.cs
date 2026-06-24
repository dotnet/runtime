// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

/// <summary>
/// An opaque handle to a runtime type. May represent a loaded type (backed by a
/// target-process MethodTable or TypeDesc address) or a synthetic type fabricated
/// by the reader for unloaded constructed types.
/// </summary>
public interface ITypeHandle : IEquatable<ITypeHandle>
{
    TargetPointer Address { get; }
    bool IsNull { get; }
    bool IsSynthetic { get; }
    static ITypeHandle Null { get; } = NullTypeHandle.Instance;
}

/// <summary>
/// Singleton ITypeHandle representing the absence of a type.
/// </summary>
public sealed class NullTypeHandle : ITypeHandle
{
    public static readonly NullTypeHandle Instance = new();
    private NullTypeHandle() { }
    public TargetPointer Address => TargetPointer.Null;
    public bool IsNull => true;
    public bool IsSynthetic => false;
    public bool Equals(ITypeHandle? other) => other is not null && other.IsNull && !other.IsSynthetic;
    public override bool Equals(object? obj) => obj is ITypeHandle th && Equals(th);
    public override int GetHashCode() => 0;
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

public readonly record struct TypedByRefInfo(TargetPointer Data, TargetPointer ITypeHandle);

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


public interface IRuntimeTypeSystem : IContract
{
    static string IContract.Name => nameof(RuntimeTypeSystem);

    #region ITypeHandle inspection APIs
    ITypeHandle GetTypeHandle(TargetPointer address) => throw new NotImplementedException();
    TargetPointer GetModule(ITypeHandle typeHandle) => throw new NotImplementedException();
    TargetPointer GetLoaderModule(ITypeHandle typeHandle) => throw new NotImplementedException();

    // A canonical method table is either the MethodTable itself, or in the case of a generic instantiation, it is the
    // MethodTable of the prototypical instance.
    TargetPointer GetCanonicalMethodTable(ITypeHandle typeHandle) => throw new NotImplementedException();
    // Returns the EEClass pointer for this MethodTable. For non-canonical MTs, follows the tagged pointer
    // to the canonical MT and returns its EEClass.
    TargetPointer GetClassPointer(ITypeHandle typeHandle) => throw new NotImplementedException();
    // True if this MethodTable is the canonical MethodTable (i.e., EEClassOrCanonMT points directly to the EEClass)
    bool IsCanonicalMethodTable(ITypeHandle typeHandle) => throw new NotImplementedException();
    TargetPointer GetParentMethodTable(ITypeHandle typeHandle) => throw new NotImplementedException();

    TargetPointer GetMethodDescForSlot(ITypeHandle methodTable, ushort slot) => throw new NotImplementedException();
    IEnumerable<TargetPointer> GetIntroducedMethodDescs(ITypeHandle methodTable) => throw new NotImplementedException();
    TargetCodePointer GetSlot(ITypeHandle typeHandle, uint slot) => throw new NotImplementedException();

    uint GetBaseSize(ITypeHandle typeHandle) => throw new NotImplementedException();
    uint GetNumInstanceFieldBytes(ITypeHandle typeHandle) => throw new NotImplementedException();
    // The component size is only available for strings and arrays.  It is the size of the element type of the array, or the size of an ECMA 335 character (2 bytes)
    uint GetComponentSize(ITypeHandle typeHandle) => throw new NotImplementedException();

    // True if the MethodTable is the sentinel value associated with unallocated space in the managed heap
    bool IsFreeObjectMethodTable(ITypeHandle typeHandle) => throw new NotImplementedException();
    // True if the MethodTable is the System.Object MethodTable (g_pObjectClass)
    bool IsObject(ITypeHandle typeHandle) => throw new NotImplementedException();
    bool IsString(ITypeHandle typeHandle) => throw new NotImplementedException();
    // True if the CorElementType represents a GC-collectable object reference.
    bool IsCorElementTypeObjRef(CorElementType elementType) => throw new NotImplementedException();
    // Returns the address of one of the runtime's well-known singleton MethodTables,
    // or TargetPointer.Null if the runtime has not yet initialized that global.
    TargetPointer GetWellKnownMethodTable(WellKnownMethodTable kind) => throw new NotImplementedException();
    // True if the MethodTable represents a type that contains managed references
    bool ContainsGCPointers(ITypeHandle typeHandle) => throw new NotImplementedException();
    // True if MethodTable represents a byreflike value (Span<T>, ReadOnlySpan<T>, etc.).
    bool IsByRefLike(ITypeHandle typeHandle) => throw new NotImplementedException();
    // True if the type requires 8-byte alignment on platforms that don't 8-byte align by default (FEATURE_64BIT_ALIGNMENT)
    bool RequiresAlign8(ITypeHandle typeHandle) => throw new NotImplementedException();
    // True if the MethodTable represents a continuation subtype that has no metadata of its own
    bool IsContinuationWithoutMetadata(ITypeHandle typeHandle) => throw new NotImplementedException();
    /// <summary>
    /// Enumerates GC pointer runs from the CGCDesc stored before the method table.
    /// Returns (offset, size) pairs normalized to actual byte lengths.
    /// See RuntimeTypeSystem.md for the full GCDesc format documentation.
    /// </summary>
    IEnumerable<(uint Offset, uint Size)> GetGCDescSeries(ITypeHandle typeHandle, uint numComponents = 0) => throw new NotImplementedException();
    bool IsDynamicStatics(ITypeHandle typeHandle) => throw new NotImplementedException();
    ushort GetNumInterfaces(ITypeHandle typeHandle) => throw new NotImplementedException();

    // Returns an ECMA-335 TypeDef table token for this type, or for its generic type definition if it is a generic instantiation
    uint GetTypeDefToken(ITypeHandle typeHandle) => throw new NotImplementedException();
    ushort GetNumVtableSlots(ITypeHandle typeHandle) => throw new NotImplementedException();
    ushort GetNumMethods(ITypeHandle typeHandle) => throw new NotImplementedException();
    // Returns the ECMA 335 TypeDef table Flags value (a bitmask of TypeAttributes) for this type,
    // or for its generic type definition if it is a generic instantiation
    uint GetTypeDefTypeAttributes(ITypeHandle typeHandle) => throw new NotImplementedException();
    ushort GetNumInstanceFields(ITypeHandle typeHandle) => throw new NotImplementedException();
    ushort GetNumStaticFields(ITypeHandle typeHandle) => throw new NotImplementedException();
    ushort GetNumThreadStaticFields(ITypeHandle typeHandle) => throw new NotImplementedException();
    IEnumerable<TargetPointer> GetFieldDescList(ITypeHandle typeHandle) => throw new NotImplementedException();
    // True if the MethodTable represents a type tracked as an Objective-C reference type with a finalizer
    bool IsTrackedReferenceWithFinalizer(ITypeHandle typeHandle) => throw new NotImplementedException();
    TargetPointer GetGCStaticsBasePointer(ITypeHandle typeHandle) => throw new NotImplementedException();
    TargetPointer GetNonGCStaticsBasePointer(ITypeHandle typeHandle) => throw new NotImplementedException();
    TargetPointer GetGCThreadStaticsBasePointer(ITypeHandle typeHandle, TargetPointer threadPtr) => throw new NotImplementedException();
    TargetPointer GetNonGCThreadStaticsBasePointer(ITypeHandle typeHandle, TargetPointer threadPtr) => throw new NotImplementedException();


    ITypeHandle[] GetInstantiation(ITypeHandle typeHandle) => throw new NotImplementedException();
    public bool IsClassInited(ITypeHandle typeHandle) => throw new NotImplementedException();
    public bool IsInitError(ITypeHandle typeHandle) => throw new NotImplementedException();
    bool IsGenericTypeDefinition(ITypeHandle typeHandle) => throw new NotImplementedException();
    bool ContainsGenericVariables(ITypeHandle typeHandle) => throw new NotImplementedException();
    bool IsCollectible(ITypeHandle typeHandle) => throw new NotImplementedException();

    bool HasTypeParam(ITypeHandle typeHandle) => throw new NotImplementedException();

    // Element type of the type. NOTE: this drops the CorElementType.GenericInst, and CorElementType.String is returned as CorElementType.Class.
    // If this returns CorElementType.ValueType it may be a normal valuetype or a "NATIVE" valuetype used to represent an interop view on a structure
    // HasTypeParam will return true for cases where this is the interop view
    CorElementType GetSignatureCorElementType(ITypeHandle typeHandle) => throw new NotImplementedException();
    bool IsValueType(ITypeHandle typeHandle) => throw new NotImplementedException();

    // Internal element type of the type. Unlike GetSignatureCorElementType, this returns the underlying primitive
    // type for enums (e.g. I4 for an enum with int underlying type) and for PrimitiveValueType categories.
    // For arrays, reference types, and TypeDescs, behaves identically to GetSignatureCorElementType.
    CorElementType GetInternalCorElementType(ITypeHandle typeHandle) => throw new NotImplementedException();

    // return true if the ITypeHandle represents an enum type.
    bool IsEnum(ITypeHandle typeHandle) => throw new NotImplementedException();

    // return true if the ITypeHandle represents a delegate type (i.e., its parent is System.MulticastDelegate)
    bool IsDelegate(ITypeHandle typeHandle) => throw new NotImplementedException();

    // return true if the ITypeHandle represents an array, and set the rank to either 0 (if the type is not an array), or the rank number if it is.
    bool IsArray(ITypeHandle typeHandle, out uint rank) => throw new NotImplementedException();
    ITypeHandle GetTypeParam(ITypeHandle typeHandle) => throw new NotImplementedException();
    ITypeHandle GetConstructedType(ITypeHandle typeHandle, CorElementType corElementType, int rank, ImmutableArray<ITypeHandle> typeArguments, SignatureCallingConvention callConv = SignatureCallingConvention.Default) => throw new NotImplementedException();
    ITypeHandle GetPrimitiveType(CorElementType typeCode) => throw new NotImplementedException();
    bool IsGenericVariable(ITypeHandle typeHandle, out TargetPointer module, out uint token) => throw new NotImplementedException();
    bool IsFunctionPointer(ITypeHandle typeHandle, out ITypeHandle[] retAndArgTypes, out SignatureCallingConvention callConv) => throw new NotImplementedException();
    bool IsPointer(ITypeHandle typeHandle) => throw new NotImplementedException();
    bool IsTypeDesc(ITypeHandle typeHandle) => throw new NotImplementedException();
    bool IsSynthetic(ITypeHandle typeHandle) => typeHandle.IsSynthetic;
    TypedByRefInfo GetTypedByRefInfo(TargetPointer typedByRef) => throw new NotImplementedException();
    // Returns null if the ITypeHandle is not a class/struct/generic variable
    #endregion ITypeHandle inspection APIs

    #region MethodDesc inspection APIs
    MethodDescHandle GetMethodDescHandle(TargetPointer targetPointer) => throw new NotImplementedException();
    TargetPointer GetMethodTable(MethodDescHandle methodDesc) => throw new NotImplementedException();

    // Return true for an uninstantiated generic method
    bool IsGenericMethodDefinition(MethodDescHandle methodDesc) => throw new NotImplementedException();
    ITypeHandle[] GetGenericMethodInstantiation(MethodDescHandle methodDesc) => throw new NotImplementedException();

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
    // A StoredSigMethodDesc is a MethodDesc for which the signature isn't found in metadata.
    bool IsStoredSigMethodDesc(MethodDescHandle methodDesc, out ReadOnlySpan<byte> signature) => throw new NotImplementedException();

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
    ITypeHandle GetFieldDescApproxTypeHandle(TargetPointer fieldDescPointer) => throw new NotImplementedException();
    TargetPointer GetFieldDescByName(ITypeHandle typeHandle, string fieldName) => throw new NotImplementedException();
    TargetPointer GetFieldDescStaticAddress(TargetPointer fieldDescPointer, bool unboxValueTypes = true) => throw new NotImplementedException();
    TargetPointer GetFieldDescThreadStaticAddress(TargetPointer fieldDescPointer, TargetPointer thread, bool unboxValueTypes = true) => throw new NotImplementedException();
    #endregion FieldDesc inspection APIs
}

public struct RuntimeTypeSystem : IRuntimeTypeSystem
{
    // Everything throws NotImplementedException
}
