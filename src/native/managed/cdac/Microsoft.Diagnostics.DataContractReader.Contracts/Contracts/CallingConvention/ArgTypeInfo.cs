// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Type information needed by ArgIterator for calling convention analysis.
// Ported from crossgen2's TypeHandle struct in ArgIterator.cs.

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.CallingConventionHelpers;

internal readonly struct ArgTypeInfo
{
    public CorElementType CorElementType { get; init; }
    public int Size { get; init; }
    public bool IsValueType { get; init; }
    public bool RequiresAlign8 { get; init; }
    public bool IsHomogeneousAggregate { get; init; }
    public int HomogeneousAggregateElementSize { get; init; }

    public TypeHandle RuntimeTypeHandle { get; init; }

    public bool IsNull => CorElementType == default && Size == 0;

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

    public static ArgTypeInfo FromTypeHandle(Target target, TypeHandle th)
    {
        IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
        CorElementType corType = rts.GetSignatureCorElementType(th);

        switch (corType)
        {
            case CorElementType.ValueType:
                return FromValueType(target, rts, th, corType);

            // Primitives have static sizes -- consult s_elemSizes rather than reading
            // the MethodTable. CorElementType for primitives is already normalized by
            // GetSignatureCorElementType (e.g. enums collapse to their underlying type).
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
                return ForPrimitive(corType, target.PointerSize, th);

            case CorElementType.Byref:
                return ForPrimitive(CorElementType.Byref, target.PointerSize, th);

            default:
                // Reference types (Class, String, Object, Array, SzArray, etc.) are
                // projected to CorElementType.Class so downstream classification
                // (X86ArgIterator, SystemVStructClassifier) sees them as a single
                // category. The TypeHandle is preserved for generic-instantiation
                // matching.
                return ForPrimitive(CorElementType.Class, target.PointerSize, th);
        }
    }

    private static ArgTypeInfo FromValueType(Target target, IRuntimeTypeSystem rts, TypeHandle th, CorElementType corType)
    {
        int size = rts.GetNumInstanceFieldBytes(th);
        bool requiresAlign8 = rts.RequiresAlign8(th);
        bool isHfa = rts.IsHFA(th);
        int hfaElemSize = isHfa ? ComputeHfaElementSize(target, rts, th, requiresAlign8) : 0;

        return new ArgTypeInfo
        {
            CorElementType = corType,
            Size = size,
            IsValueType = true,
            RequiresAlign8 = requiresAlign8,
            IsHomogeneousAggregate = isHfa,
            HomogeneousAggregateElementSize = hfaElemSize,
            RuntimeTypeHandle = th,
        };
    }

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
                    TypeHandle nested = rts.LookupApproxFieldTypeHandle(firstField);
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

    public static ArgTypeInfo ForPrimitive(CorElementType corType, int pointerSize)
        => ForPrimitive(corType, pointerSize, default);

    public static ArgTypeInfo ForPrimitive(CorElementType corType, int pointerSize, TypeHandle runtimeTypeHandle)
    {
        return new ArgTypeInfo
        {
            CorElementType = corType,
            Size = GetElemSize(corType, default, pointerSize),
            IsValueType = false,
            RequiresAlign8 = false,
            IsHomogeneousAggregate = false,
            HomogeneousAggregateElementSize = 0,
            RuntimeTypeHandle = runtimeTypeHandle,
        };
    }
}
