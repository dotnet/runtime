// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Type information needed by ArgIterator for calling convention analysis.
// Ported from crossgen2's TypeHandle struct in ArgIterator.cs.

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.CallingConvention;

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
            ? (int)rts.GetBaseSize(th) - 2 * target.PointerSize // InstanceFieldSize = BaseSize - ObjHeader - MethodTable ptr
            : target.PointerSize;

        bool requiresAlign8 = false;
        bool isHfa = false;
        int hfaElemSize = 0;

        if (isValueType)
        {
            // TODO: Implement RequiresAlign8 via IRuntimeTypeSystem
            // TODO: Implement IsHomogeneousAggregate via IRuntimeTypeSystem
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
