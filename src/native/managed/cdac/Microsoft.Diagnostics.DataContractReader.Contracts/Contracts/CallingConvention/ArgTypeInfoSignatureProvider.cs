// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Diagnostics.DataContractReader.SignatureHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.CallingConventionHelpers;

/// <summary>
/// Generic context used to resolve <c>ELEMENT_TYPE_VAR</c> and <c>ELEMENT_TYPE_MVAR</c>
/// while decoding a method signature into <see cref="ArgTypeInfo"/> values.
/// <see cref="ClassContext"/> is the owning type's <see cref="TypeHandle"/> (used for VAR),
/// and <see cref="MethodContext"/> is the owning method's <see cref="MethodDescHandle"/>
/// (used for MVAR).
/// </summary>
internal readonly record struct ArgTypeInfoSignatureContext(TypeHandle ClassContext, MethodDescHandle MethodContext);

/// <summary>
/// Decodes signature elements directly into <see cref="ArgTypeInfo"/> so that
/// <see cref="ArgIterator"/> can drive argument iteration without an intermediate
/// classification stage.
/// Implements <see cref="IRuntimeSignatureTypeProvider{TType, TGenericContext}"/>, which
/// is a superset of SRM's <see cref="ISignatureTypeProvider{TType, TGenericContext}"/>
/// adding support for <c>ELEMENT_TYPE_INTERNAL</c>.
/// </summary>
/// <remarks>
/// The provider is scoped to a single module: <c>GetTypeFromDefinition</c> and
/// <c>GetTypeFromReference</c> resolve TypeDef/TypeRef tokens via the module's lookup
/// tables so enums (and other runtime-normalized value types) are classified using their
/// actual <see cref="CorElementType"/>, matching native
/// <c>SigPointer::PeekElemTypeNormalized</c>. For value-type elements the resolved
/// <see cref="TypeHandle"/> is surfaced in <see cref="ArgTypeInfo.RuntimeTypeHandle"/> so
/// <c>ArgIterator</c> sees the correct size / HFA / alignment in a single signature walk
/// (mirroring native <c>MetaSig::GetByValType</c>).
/// </remarks>
internal sealed class ArgTypeInfoSignatureProvider
    : IRuntimeSignatureTypeProvider<ArgTypeInfo, ArgTypeInfoSignatureContext>
{
    private readonly Target _target;
    private readonly ModuleHandle _moduleHandle;
    private ArgTypeInfo? _cachedTypedReferenceInfo;

    public ArgTypeInfoSignatureProvider(Target target, ModuleHandle moduleHandle)
    {
        _target = target;
        _moduleHandle = moduleHandle;
    }

    public ArgTypeInfo GetPrimitiveType(PrimitiveTypeCode typeCode)
        => typeCode switch
        {
            PrimitiveTypeCode.String or PrimitiveTypeCode.Object
                => ArgTypeInfo.ForPrimitive(CorElementType.Class, _target.PointerSize),
            // TypedReference has no class token in the signature blob -- the runtime
            // identifies its layout via the well-known g_TypedReferenceMT global.
            // Mirroring native callingconvention.h:1351-1355, we substitute the
            // TypedReference MethodTable here so the rest of ArgIterator (and the
            // SystemV struct classifier) see it as an ordinary 16-byte value type.
            PrimitiveTypeCode.TypedReference => GetTypedReferenceInfo(),
            _ => ArgTypeInfo.ForPrimitive(PrimitiveToCorElementType(typeCode), _target.PointerSize),
        };

    private ArgTypeInfo GetTypedReferenceInfo()
    {
        if (_cachedTypedReferenceInfo is { } cached)
            return cached;

        ArgTypeInfo info;
        try
        {
            TargetPointer mtPtr = _target.ReadPointer(
                _target.ReadGlobalPointer(Constants.Globals.TypedReferenceMethodTable));
            if (mtPtr == TargetPointer.Null)
            {
                info = UnresolvedValueType();
            }
            else
            {
                TypeHandle th = _target.Contracts.RuntimeTypeSystem.GetTypeHandle(mtPtr);
                info = ArgTypeInfo.FromTypeHandle(_target, th);
            }
        }
        catch
        {
            // Older runtime images without the TypedReferenceMethodTable global, or
            // any failure resolving the type, falls back to a conservative placeholder.
            info = UnresolvedValueType();
        }

        _cachedTypedReferenceInfo = info;
        return info;
    }

    public ArgTypeInfo GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
        => FromTokenLookup(_target.Contracts.Loader.GetLookupTables(_moduleHandle).TypeDefToMethodTable, MetadataTokens.GetToken(handle), rawTypeKind);

    public ArgTypeInfo GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
        => FromTokenLookup(_target.Contracts.Loader.GetLookupTables(_moduleHandle).TypeRefToMethodTable, MetadataTokens.GetToken(handle), rawTypeKind);

    public ArgTypeInfo GetTypeFromSpecification(MetadataReader reader, ArgTypeInfoSignatureContext genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
        // TODO: Resolve the TypeSpec to a concrete (already-loaded) TypeHandle so that
        // generic value-type instantiations get correct size / HFA classification. Native
        // does this via SigPointer::GetTypeHandleThrowing + the instantiated-type
        // hashtable; cDAC needs an equivalent lookup-only RTS API. Until then, fall back
        // to a conservative pointer-sized placeholder for value types.
        => rawTypeKind == (byte)SignatureTypeKind.ValueType
            ? UnresolvedValueType()
            : ArgTypeInfo.ForPrimitive(CorElementType.Class, _target.PointerSize);

    public ArgTypeInfo GetSZArrayType(ArgTypeInfo elementType) => ArgTypeInfo.ForPrimitive(CorElementType.SzArray, _target.PointerSize);
    public ArgTypeInfo GetArrayType(ArgTypeInfo elementType, ArrayShape shape) => ArgTypeInfo.ForPrimitive(CorElementType.Array, _target.PointerSize);
    public ArgTypeInfo GetByReferenceType(ArgTypeInfo elementType) => ArgTypeInfo.ForPrimitive(CorElementType.Byref, _target.PointerSize);
    public ArgTypeInfo GetPointerType(ArgTypeInfo elementType) => ArgTypeInfo.ForPrimitive(CorElementType.Ptr, _target.PointerSize);

    public ArgTypeInfo GetGenericInstantiation(ArgTypeInfo genericType, ImmutableArray<ArgTypeInfo> typeArguments)
    {
        // TODO: lookup the instantiated MethodTable so generic value-type args get correct
        // size / HFA. For reference-type generic instantiations the open-generic's
        // ArgTypeInfo (pointer-sized Class) is already correct; for value-type
        // instantiations the open generic's size is not meaningful, so we downgrade to a
        // conservative pointer-sized placeholder.
        if (genericType.CorElementType == CorElementType.ValueType)
            return UnresolvedValueType();
        return genericType;
    }

    public ArgTypeInfo GetGenericMethodParameter(ArgTypeInfoSignatureContext genericContext, int index)
    {
        try
        {
            ReadOnlySpan<TypeHandle> instantiation = _target.Contracts.RuntimeTypeSystem.GetGenericMethodInstantiation(genericContext.MethodContext);
            if ((uint)index >= (uint)instantiation.Length)
                return ArgTypeInfo.ForPrimitive(CorElementType.Class, _target.PointerSize);
            return BuildFromTypeHandle(instantiation[index]);
        }
        catch
        {
            return ArgTypeInfo.ForPrimitive(CorElementType.Class, _target.PointerSize);
        }
    }

    public ArgTypeInfo GetGenericTypeParameter(ArgTypeInfoSignatureContext genericContext, int index)
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
                    return ArgTypeInfo.ForPrimitive(CorElementType.Class, _target.PointerSize);
                return BuildFromTypeHandle(rts.GetTypeParam(classCtx));
            }

            ReadOnlySpan<TypeHandle> instantiation = rts.GetInstantiation(classCtx);
            if ((uint)index >= (uint)instantiation.Length)
                return ArgTypeInfo.ForPrimitive(CorElementType.Class, _target.PointerSize);
            return BuildFromTypeHandle(instantiation[index]);
        }
        catch
        {
            return ArgTypeInfo.ForPrimitive(CorElementType.Class, _target.PointerSize);
        }
    }

    public ArgTypeInfo GetFunctionPointerType(MethodSignature<ArgTypeInfo> signature)
        => ArgTypeInfo.ForPrimitive(CorElementType.FnPtr, _target.PointerSize);
    public ArgTypeInfo GetModifiedType(ArgTypeInfo modifier, ArgTypeInfo unmodifiedType, bool isRequired) => unmodifiedType;
    public ArgTypeInfo GetInternalModifiedType(TargetPointer typeHandlePointer, ArgTypeInfo unmodifiedType, bool isRequired) => unmodifiedType;
    public ArgTypeInfo GetPinnedType(ArgTypeInfo elementType) => elementType;

    public ArgTypeInfo GetInternalType(TargetPointer typeHandlePointer)
    {
        if (typeHandlePointer == TargetPointer.Null)
            return ArgTypeInfo.ForPrimitive(CorElementType.I, _target.PointerSize);

        try
        {
            return BuildFromTypeHandle(_target.Contracts.RuntimeTypeSystem.GetTypeHandle(typeHandlePointer));
        }
        catch
        {
            return ArgTypeInfo.ForPrimitive(CorElementType.Class, _target.PointerSize);
        }
    }

    /// <summary>
    /// Resolve a TypeDef/TypeRef token via the module's lookup tables and build an
    /// <see cref="ArgTypeInfo"/> from the resulting <see cref="TypeHandle"/>. Falls back
    /// to a <paramref name="rawTypeKind"/>-driven conservative placeholder when the type
    /// has not been loaded.
    /// </summary>
    private ArgTypeInfo FromTokenLookup(TargetPointer lookupTable, int token, byte rawTypeKind)
    {
        try
        {
            TargetPointer typeHandlePtr = _target.Contracts.Loader.GetModuleLookupMapElement(lookupTable, (uint)token, out _);
            if (typeHandlePtr == TargetPointer.Null)
                return FallbackForRawTypeKind(rawTypeKind);

            return BuildFromTypeHandle(_target.Contracts.RuntimeTypeSystem.GetTypeHandle(typeHandlePtr));
        }
        catch
        {
            return FallbackForRawTypeKind(rawTypeKind);
        }
    }

    private ArgTypeInfo FallbackForRawTypeKind(byte rawTypeKind)
        => rawTypeKind == (byte)SignatureTypeKind.ValueType
            ? UnresolvedValueType()
            : ArgTypeInfo.ForPrimitive(CorElementType.Class, _target.PointerSize);

    /// <summary>
    /// Build an <see cref="ArgTypeInfo"/> from a resolved <see cref="TypeHandle"/>. Mirrors
    /// native <c>SigPointer::PeekElemTypeNormalized</c> + <c>MetaSig::GetByValType</c>:
    /// enums collapse to their underlying primitive (via <c>GetSignatureCorElementType</c>)
    /// so they classify as a non-GC scalar; value types surface the resolved
    /// <see cref="TypeHandle"/> with full size / HFA / alignment for ArgIterator.
    /// </summary>
    private ArgTypeInfo BuildFromTypeHandle(TypeHandle typeHandle)
    {
        if (typeHandle.Address == TargetPointer.Null)
            return ArgTypeInfo.ForPrimitive(CorElementType.Class, _target.PointerSize);

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
                return ArgTypeInfo.ForPrimitive(corType, _target.PointerSize);

            case CorElementType.Byref:
                return ArgTypeInfo.ForPrimitive(CorElementType.Byref, _target.PointerSize);

            case CorElementType.ValueType:
                // GetSignatureCorElementType already collapses enums to their underlying
                // primitive; anything still typed as ValueType is a real struct.
                return ArgTypeInfo.FromTypeHandle(_target, typeHandle);

            default:
                return ArgTypeInfo.ForPrimitive(CorElementType.Class, _target.PointerSize);
        }
    }

    /// <summary>
    /// Conservative value-type placeholder used when a TypeSpec / TypedReference /
    /// unloaded TypeDef/TypeRef can't be resolved to a concrete <see cref="TypeHandle"/>.
    /// ArgIterator sees a pointer-sized value type with no HFA classification.
    /// </summary>
    private ArgTypeInfo UnresolvedValueType()
        => new ArgTypeInfo
        {
            CorElementType = CorElementType.ValueType,
            Size = _target.PointerSize,
            IsValueType = true,
        };

    private static CorElementType PrimitiveToCorElementType(PrimitiveTypeCode typeCode) => typeCode switch
    {
        PrimitiveTypeCode.Void => CorElementType.Void,
        PrimitiveTypeCode.Boolean => CorElementType.Boolean,
        PrimitiveTypeCode.Char => CorElementType.Char,
        PrimitiveTypeCode.SByte => CorElementType.I1,
        PrimitiveTypeCode.Byte => CorElementType.U1,
        PrimitiveTypeCode.Int16 => CorElementType.I2,
        PrimitiveTypeCode.UInt16 => CorElementType.U2,
        PrimitiveTypeCode.Int32 => CorElementType.I4,
        PrimitiveTypeCode.UInt32 => CorElementType.U4,
        PrimitiveTypeCode.Int64 => CorElementType.I8,
        PrimitiveTypeCode.UInt64 => CorElementType.U8,
        PrimitiveTypeCode.Single => CorElementType.R4,
        PrimitiveTypeCode.Double => CorElementType.R8,
        PrimitiveTypeCode.IntPtr => CorElementType.I,
        PrimitiveTypeCode.UIntPtr => CorElementType.U,
        _ => CorElementType.Void,
    };
}
