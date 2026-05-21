// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Type information needed by ArgIterator for calling convention analysis.
// Ported from crossgen2's TypeHandle struct in ArgIterator.cs.

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.CallingConventionHelpers;

/// <summary>
/// Pre-computed type information needed by <see cref="ArgIterator"/> for
/// calling convention analysis. This is a value type to avoid allocations
/// during argument iteration.
/// </summary>
/// <remarks>
/// Mirrors crossgen2's <c>TypeHandle</c> struct in ArgIterator.cs, but uses
/// data from the cDAC's <see cref="IRuntimeTypeSystem"/> rather than
/// crossgen2's <c>TypeDesc</c>.
/// </remarks>
internal readonly struct ArgTypeInfo
{
    public CorElementType CorElementType { get; init; }
    public int Size { get; init; }
    public bool IsValueType { get; init; }
    public bool RequiresAlign8 { get; init; }
    public bool IsHomogeneousAggregate { get; init; }
    public int HomogeneousAggregateElementSize { get; init; }

    /// <summary>
    /// The TypeHandle from the target runtime, used for value type field enumeration
    /// and SystemV struct classification.
    /// </summary>
    public TypeHandle RuntimeTypeHandle { get; init; }

    public bool IsNull => CorElementType == default && Size == 0;

    /// <summary>
    /// Gets the element size for a given CorElementType, matching crossgen2's
    /// <c>TypeHandle.GetElemSize</c>. Returns the type's actual size for value
    /// types, or pointer size for reference types.
    /// </summary>
    public static int GetElemSize(CorElementType t, ArgTypeInfo thValueType, int pointerSize)
    {
        if ((int)t <= 0x1d)
        {
            int elemSize = s_elemSizes[(int)t];
            if (elemSize == -1)
                return thValueType.Size;
            if (elemSize == -2)
                return pointerSize;
            return elemSize;
        }
        return 0;
    }

    private static readonly int[] s_elemSizes =
    [
        0,  // ELEMENT_TYPE_END          0x0
        0,  // ELEMENT_TYPE_VOID         0x1
        1,  // ELEMENT_TYPE_BOOLEAN      0x2
        2,  // ELEMENT_TYPE_CHAR         0x3
        1,  // ELEMENT_TYPE_I1           0x4
        1,  // ELEMENT_TYPE_U1           0x5
        2,  // ELEMENT_TYPE_I2           0x6
        2,  // ELEMENT_TYPE_U2           0x7
        4,  // ELEMENT_TYPE_I4           0x8
        4,  // ELEMENT_TYPE_U4           0x9
        8,  // ELEMENT_TYPE_I8           0xa
        8,  // ELEMENT_TYPE_U8           0xb
        4,  // ELEMENT_TYPE_R4           0xc
        8,  // ELEMENT_TYPE_R8           0xd
        -2, // ELEMENT_TYPE_STRING       0xe
        -2, // ELEMENT_TYPE_PTR          0xf
        -2, // ELEMENT_TYPE_BYREF        0x10
        -1, // ELEMENT_TYPE_VALUETYPE    0x11
        -2, // ELEMENT_TYPE_CLASS        0x12
        0,  // ELEMENT_TYPE_VAR          0x13
        -2, // ELEMENT_TYPE_ARRAY        0x14
        0,  // ELEMENT_TYPE_GENERICINST  0x15
        0,  // ELEMENT_TYPE_TYPEDBYREF   0x16
        0,  // UNUSED                    0x17
        -2, // ELEMENT_TYPE_I            0x18
        -2, // ELEMENT_TYPE_U            0x19
        0,  // UNUSED                    0x1a
        -2, // ELEMENT_TYPE_FPTR         0x1b
        -2, // ELEMENT_TYPE_OBJECT       0x1c
        -2, // ELEMENT_TYPE_SZARRAY      0x1d
    ];

    /// <summary>
    /// Creates an <see cref="ArgTypeInfo"/> from a target TypeHandle using the
    /// runtime type system contract.
    /// </summary>
    public static ArgTypeInfo FromTypeHandle(Target target, TypeHandle th)
    {
        IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
        CorElementType corType = rts.GetSignatureCorElementType(th);

        bool isValueType = corType is CorElementType.ValueType;
        int size = isValueType
            ? rts.GetNumInstanceFieldBytes(th)
            : target.PointerSize;

        bool requiresAlign8 = false;
        bool isHfa = false;
        int hfaElemSize = 0;

        if (isValueType)
        {
            requiresAlign8 = rts.RequiresAlign8(th);
            isHfa = rts.IsHFA(th);
            if (isHfa)
            {
                hfaElemSize = ComputeHfaElementSize(target, rts, th, requiresAlign8);
            }
        }

        return new ArgTypeInfo
        {
            CorElementType = corType,
            Size = size,
            IsValueType = isValueType,
            RequiresAlign8 = requiresAlign8,
            IsHomogeneousAggregate = isHfa,
            HomogeneousAggregateElementSize = hfaElemSize,
            RuntimeTypeHandle = th,
        };
    }

    /// <summary>
    /// Computes the element size of a Homogeneous Floating-point Aggregate (HFA),
    /// matching crossgen2's <c>DefType.GetHomogeneousAggregateElementSize</c>.
    /// </summary>
    /// <remarks>
    /// On ARM, the element size is fully determined by the alignment requirement:
    /// HFAs of doubles have 8-byte alignment; HFAs of floats use 4-byte alignment.
    ///
    /// On ARM64, we walk the first field of the value type, recursing through nested
    /// value types until we reach a primitive (R4/R8) or a Vector intrinsic
    /// (Vector64`1, Vector128`1, or System.Numerics.Vector`1). This mirrors the
    /// runtime's <c>MethodTable::GetHFAType</c> in src/coreclr/vm/class.cpp.
    /// </remarks>
    private static int ComputeHfaElementSize(Target target, IRuntimeTypeSystem rts, TypeHandle th, bool requiresAlign8)
    {
        RuntimeInfoArchitecture arch = target.Contracts.RuntimeInfo.GetTargetArchitecture();
        if (arch == RuntimeInfoArchitecture.Arm)
        {
            return requiresAlign8 ? 8 : 4;
        }
        if (arch != RuntimeInfoArchitecture.Arm64)
        {
            // FEATURE_HFA is only enabled on ARM/ARM64; IsHFA should never be true elsewhere.
            return 0;
        }

        // ARM64: walk the first field, descending into nested value types. All HFA fields
        // must be of the same primitive/vector type, so the first field determines the
        // element size for the entire aggregate.
        TypeHandle current = th;
        // Bound the loop to defend against unexpected metadata cycles.
        for (int depth = 0; depth < 16; depth++)
        {
            int vectorSize = rts.GetVectorSize(current);
            if (vectorSize != 0)
                return vectorSize;

            TargetPointer firstField = rts.GetFieldDescList(current);
            if (firstField == TargetPointer.Null)
                return 0;
            CorElementType fieldType = rts.GetFieldDescType(firstField);
            switch (fieldType)
            {
                case CorElementType.R4:
                    return 4;
                case CorElementType.R8:
                    return 8;
                case CorElementType.ValueType:
                    TypeHandle nested = LookupApproxFieldTypeHandle(target, rts, firstField);
                    if (nested.IsNull || !nested.IsMethodTable())
                        return 0;
                    current = nested;
                    continue;
                default:
                    // IsHFA should only be set on types that resolve to a valid HFA element;
                    // anything else here indicates a metadata mismatch we can't classify.
                    return 0;
            }
        }
        return 0;
    }

    /// <summary>
    /// Resolves a field's declared type without triggering type loading. Mirrors native
    /// <c>FieldDesc::LookupApproxFieldTypeHandle</c> (DAC variant): walks the field's
    /// metadata signature and returns the resulting <see cref="TypeHandle"/>, or a null
    /// handle when the type isn't already loaded.
    /// </summary>
    private static TypeHandle LookupApproxFieldTypeHandle(Target target, IRuntimeTypeSystem rts, TargetPointer fieldDescPointer)
    {
        if (fieldDescPointer == TargetPointer.Null)
            return default;

        uint token = rts.GetFieldDescMemberDef(fieldDescPointer);
        EntityHandle entityHandle = MetadataTokens.EntityHandle((int)token);
        if (entityHandle.IsNil || entityHandle.Kind != HandleKind.FieldDefinition)
            return default;

        TargetPointer enclosingMT = rts.GetMTOfEnclosingClass(fieldDescPointer);
        TypeHandle ctx = rts.GetTypeHandle(enclosingMT);
        if (!ctx.IsMethodTable())
            return default;

        TargetPointer modulePtr = rts.GetModule(ctx);
        if (modulePtr == TargetPointer.Null)
            return default;

        ModuleHandle moduleHandle = target.Contracts.Loader.GetModuleHandleFromModulePtr(modulePtr);
        MetadataReader? mdReader = target.Contracts.EcmaMetadata.GetMetadata(moduleHandle);
        if (mdReader is null)
            return default;

        FieldDefinition fieldDef = mdReader.GetFieldDefinition((FieldDefinitionHandle)entityHandle);
        try
        {
            return target.Contracts.Signature.DecodeFieldSignature(fieldDef.Signature, moduleHandle, ctx);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Creates an <see cref="ArgTypeInfo"/> for a primitive type that doesn't need
    /// type handle resolution.
    /// </summary>
    public static ArgTypeInfo ForPrimitive(CorElementType corType, int pointerSize)
    {
        return new ArgTypeInfo
        {
            CorElementType = corType,
            Size = GetElemSize(corType, default, pointerSize),
            IsValueType = false,
            RequiresAlign8 = false,
            IsHomogeneousAggregate = false,
            HomogeneousAggregateElementSize = 0,
            RuntimeTypeHandle = default,
        };
    }
}
