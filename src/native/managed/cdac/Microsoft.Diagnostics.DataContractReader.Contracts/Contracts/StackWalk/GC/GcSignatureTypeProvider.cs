// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Diagnostics.DataContractReader.SignatureHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

/// <summary>
/// Classification of a signature type for GC scanning purposes.
/// </summary>
internal enum GcTypeKind
{
    /// <summary>Not a GC reference (primitives, pointers).</summary>
    None,
    /// <summary>Object reference (class, string, array).</summary>
    Ref,
    /// <summary>Interior pointer (byref).</summary>
    Interior,
    /// <summary>Value type that may contain embedded GC references.</summary>
    Other,
}

/// <summary>
/// Generic context used to resolve <c>ELEMENT_TYPE_VAR</c> and <c>ELEMENT_TYPE_MVAR</c>
/// while decoding a method signature for GC scanning. <see cref="ClassContext"/> is the
/// owning type's <see cref="TypeHandle"/> (used for VAR), and <see cref="MethodContext"/>
/// is the owning method's <see cref="MethodDescHandle"/> (used for MVAR).
/// </summary>
internal readonly record struct GcSignatureContext(TypeHandle ClassContext, MethodDescHandle MethodContext);

/// <summary>
/// Classifies signature types for GC scanning purposes.
/// Implements <see cref="IRuntimeSignatureTypeProvider{TType, TGenericContext}"/> which
/// is a superset of SRM's <see cref="ISignatureTypeProvider{TType, TGenericContext}"/>,
/// adding support for <c>ELEMENT_TYPE_INTERNAL</c>.
/// </summary>
/// <remarks>
/// The provider is scoped to a single module: <c>GetTypeFromDefinition</c> and
/// <c>GetTypeFromReference</c> resolve TypeDef/TypeRef tokens via the module's
/// lookup tables so enums (and other runtime-normalized value types) are classified
/// using the actual <see cref="CorElementType"/>, matching native
/// <c>SigPointer::PeekElemTypeNormalized</c>.
/// </remarks>
internal sealed class GcSignatureTypeProvider
    : IRuntimeSignatureTypeProvider<GcTypeKind, GcSignatureContext>
{
    private readonly Target _target;
    private readonly ModuleHandle _moduleHandle;

    public GcSignatureTypeProvider(Target target, ModuleHandle moduleHandle)
    {
        _target = target;
        _moduleHandle = moduleHandle;
    }

    public GcTypeKind GetPrimitiveType(PrimitiveTypeCode typeCode)
        => typeCode switch
        {
            PrimitiveTypeCode.String or PrimitiveTypeCode.Object => GcTypeKind.Ref,
            PrimitiveTypeCode.TypedReference => GcTypeKind.Other,
            _ => GcTypeKind.None,
        };

    public GcTypeKind GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
        => ClassifyTokenLookup(_target.Contracts.Loader.GetLookupTables(_moduleHandle).TypeDefToMethodTable, MetadataTokens.GetToken(handle), rawTypeKind);

    public GcTypeKind GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
        => ClassifyTokenLookup(_target.Contracts.Loader.GetLookupTables(_moduleHandle).TypeRefToMethodTable, MetadataTokens.GetToken(handle), rawTypeKind);

    public GcTypeKind GetTypeFromSpecification(MetadataReader reader, GcSignatureContext genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
        => rawTypeKind == (byte)SignatureTypeKind.ValueType ? GcTypeKind.Other : GcTypeKind.Ref;

    public GcTypeKind GetSZArrayType(GcTypeKind elementType) => GcTypeKind.Ref;
    public GcTypeKind GetArrayType(GcTypeKind elementType, ArrayShape shape) => GcTypeKind.Ref;
    public GcTypeKind GetByReferenceType(GcTypeKind elementType) => GcTypeKind.Interior;
    public GcTypeKind GetPointerType(GcTypeKind elementType) => GcTypeKind.None;

    public GcTypeKind GetGenericInstantiation(GcTypeKind genericType, ImmutableArray<GcTypeKind> typeArguments)
        => genericType;

    public GcTypeKind GetGenericMethodParameter(GcSignatureContext genericContext, int index)
    {
        try
        {
            ReadOnlySpan<TypeHandle> instantiation = _target.Contracts.RuntimeTypeSystem.GetGenericMethodInstantiation(genericContext.MethodContext);
            if ((uint)index >= (uint)instantiation.Length)
                return GcTypeKind.Ref;
            return ClassifyTypeHandle(instantiation[index]);
        }
        catch
        {
            return GcTypeKind.Ref;
        }
    }

    public GcTypeKind GetGenericTypeParameter(GcSignatureContext genericContext, int index)
    {
        try
        {
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            TypeHandle classCtx = genericContext.ClassContext;

            if (rts.IsArray(classCtx, out _))
            {
                // Match native SigTypeContext::InitTypeContext (typectxt.cpp): arrays use
                // the element type as their class instantiation. RuntimeTypeSystem.GetInstantiation
                // returns an empty span for arrays, so consult GetTypeParam directly (the
                // managed equivalent of MethodTable::GetArrayInstantiation).
                Debug.Assert(index == 0, "Array class context has a 1-element instantiation; index > 0 indicates a malformed signature.");
                if (index != 0)
                    return GcTypeKind.Ref;
                return ClassifyTypeHandle(rts.GetTypeParam(classCtx));
            }

            ReadOnlySpan<TypeHandle> instantiation = rts.GetInstantiation(classCtx);
            if ((uint)index >= (uint)instantiation.Length)
                return GcTypeKind.Ref;
            return ClassifyTypeHandle(instantiation[index]);
        }
        catch
        {
            return GcTypeKind.Ref;
        }
    }

    public GcTypeKind GetFunctionPointerType(MethodSignature<GcTypeKind> signature) => GcTypeKind.None;
    public GcTypeKind GetModifiedType(GcTypeKind modifier, GcTypeKind unmodifiedType, bool isRequired) => unmodifiedType;
    public GcTypeKind GetInternalModifiedType(TargetPointer typeHandlePointer, GcTypeKind unmodifiedType, bool isRequired) => unmodifiedType;
    public GcTypeKind GetPinnedType(GcTypeKind elementType) => elementType;

    public GcTypeKind GetInternalType(TargetPointer typeHandlePointer)
    {
        if (typeHandlePointer == TargetPointer.Null)
            return GcTypeKind.None;

        try
        {
            return ClassifyTypeHandle(_target.Contracts.RuntimeTypeSystem.GetTypeHandle(typeHandlePointer));
        }
        catch
        {
            return GcTypeKind.Ref;
        }
    }

    /// <summary>
    /// Resolve a TypeDef/TypeRef token via the module's lookup tables and classify the
    /// resulting <see cref="TypeHandle"/>. Falls back to a <paramref name="rawTypeKind"/>-based
    /// classification when the type has not been loaded.
    /// </summary>
    private GcTypeKind ClassifyTokenLookup(TargetPointer lookupTable, int token, byte rawTypeKind)
    {
        try
        {
            TargetPointer typeHandlePtr = _target.Contracts.Loader.GetModuleLookupMapElement(lookupTable, (uint)token, out _);
            if (typeHandlePtr == TargetPointer.Null)
                return rawTypeKind == (byte)SignatureTypeKind.ValueType ? GcTypeKind.Other : GcTypeKind.Ref;

            return ClassifyTypeHandle(_target.Contracts.RuntimeTypeSystem.GetTypeHandle(typeHandlePtr));
        }
        catch
        {
            return rawTypeKind == (byte)SignatureTypeKind.ValueType ? GcTypeKind.Other : GcTypeKind.Ref;
        }
    }

    /// <summary>
    /// Classify a resolved <see cref="TypeHandle"/>. Mirrors native
    /// <c>SigPointer::PeekElemTypeNormalized</c> + <c>gElementTypeInfo[etype].m_gc</c>:
    /// enums collapse to their underlying primitive (<see cref="GcTypeKind.None"/>) so
    /// they are skipped during stack scanning, matching native behavior.
    /// </summary>
    private GcTypeKind ClassifyTypeHandle(TypeHandle typeHandle)
    {
        if (typeHandle.Address == TargetPointer.Null)
            return GcTypeKind.Ref;

        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        CorElementType corType = rts.GetSignatureCorElementType(typeHandle);

        switch (corType)
        {
            case CorElementType.Void:
            case CorElementType.Boolean:
            case CorElementType.Char:
            case CorElementType.I1:
            case CorElementType.U1:
            case CorElementType.I2:
            case CorElementType.U2:
            case CorElementType.I4:
            case CorElementType.U4:
            case CorElementType.I8:
            case CorElementType.U8:
            case CorElementType.R4:
            case CorElementType.R8:
            case CorElementType.I:
            case CorElementType.U:
            case CorElementType.FnPtr:
            case CorElementType.Ptr:
                return GcTypeKind.None;

            case CorElementType.Byref:
                return GcTypeKind.Interior;

            case CorElementType.ValueType:
                // Native PeekElemTypeNormalized resolves enums to their underlying primitive
                // CorElementType, which classifies as TYPE_GC_NONE in gElementTypeInfo.
                return rts.IsEnum(typeHandle) ? GcTypeKind.None : GcTypeKind.Other;

            default:
                return GcTypeKind.Ref;
        }
    }
}
