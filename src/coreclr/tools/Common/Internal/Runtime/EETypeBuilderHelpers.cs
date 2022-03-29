// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using Internal.TypeSystem;

namespace Internal.Runtime
{
    internal static class EETypeBuilderHelpers
    {
        private static EETypeElementType ComputeEETypeElementType(TypeDesc type)
        {
            // Enums are represented as their underlying type
            type = type.UnderlyingType;

            if (type.IsWellKnownType(WellKnownType.Array))
            {
                // SystemArray is a special EETypeElementType that doesn't exist in TypeFlags
                return EETypeElementType.SystemArray;
            }
            else
            {
                // The rest of TypeFlags should be directly castable to EETypeElementType.
                // Spot check the enums match.
                Debug.Assert((int)TypeFlags.Void == (int)EETypeElementType.Void);
                Debug.Assert((int)TypeFlags.IntPtr == (int)EETypeElementType.IntPtr);
                Debug.Assert((int)TypeFlags.Single == (int)EETypeElementType.Single);
                Debug.Assert((int)TypeFlags.UInt32 == (int)EETypeElementType.UInt32);
                Debug.Assert((int)TypeFlags.Pointer == (int)EETypeElementType.Pointer);
                Debug.Assert((int)TypeFlags.Array == (int)EETypeElementType.Array);

                EETypeElementType elementType = (EETypeElementType)type.Category;

                // Would be surprising to get these here though.
                Debug.Assert(elementType != EETypeElementType.SystemArray);
                Debug.Assert(elementType <= EETypeElementType.Pointer);

                return elementType;

            }
        }

        public static ushort ComputeFlags(TypeDesc type)
        {
            ushort flags = type.IsParameterizedType ?
                (ushort)EETypeKind.ParameterizedEEType : (ushort)EETypeKind.CanonicalEEType;

            // The top 5 bits of flags are used to convey enum underlying type, primitive type, or mark the type as being System.Array
            EETypeElementType elementType = ComputeEETypeElementType(type);
            flags |= (ushort)((ushort)elementType << (ushort)EETypeFlags.ElementTypeShift);

            if (type.IsGenericDefinition)
            {
                flags |= (ushort)EETypeKind.GenericTypeDefEEType;

                // Generic type definition EETypes don't set the other flags.
                return flags;
            }

            if (type.HasFinalizer)
            {
                flags |= (ushort)EETypeFlags.HasFinalizerFlag;
            }

            if (type.IsDefType
                && !type.IsCanonicalSubtype(CanonicalFormKind.Universal)
                && ((DefType)type).ContainsGCPointers)
            {
                flags |= (ushort)EETypeFlags.HasPointersFlag;
            }
            else if (type.IsArray && !type.IsCanonicalSubtype(CanonicalFormKind.Universal))
            {
                var arrayElementType = ((ArrayType)type).ElementType;
                if ((arrayElementType.IsValueType && ((DefType)arrayElementType).ContainsGCPointers) || arrayElementType.IsGCPointer)
                {
                    flags |= (ushort)EETypeFlags.HasPointersFlag;
                }
            }

            if (type.HasInstantiation)
            {
                flags |= (ushort)EETypeFlags.IsGenericFlag;

                if (type.HasVariance)
                {
                    flags |= (ushort)EETypeFlags.GenericVarianceFlag;
                }
            }

            return flags;
        }

        // These masks and paddings have been chosen so that the ValueTypePadding field can always fit in a byte of data
        // if the alignment is 8 bytes or less. If the alignment is higher then there may be a need for more bits to hold
        // the rest of the padding data.
        // If paddings of greater than 7 bytes are necessary, then the high bits of the field represent that padding
        private const uint ValueTypePaddingLowMask = 0x7;
        private const uint ValueTypePaddingHighMask = 0xFFFFFF00;
        private const uint ValueTypePaddingMax = 0x07FFFFFF;
        private const int ValueTypePaddingHighShift = 8;
        private const uint ValueTypePaddingAlignmentMask = 0xF8;
        private const int ValueTypePaddingAlignmentShift = 3;

        /// <summary>
        /// Compute the encoded value type padding and alignment that are stored as optional fields on an
        /// <c>MethodTable</c>. This padding as added to naturally align value types when laid out as fields
        /// of objects on the GCHeap. The amount of padding is recorded to allow unboxing to locals /
        /// arrays of value types which don't need it.
        /// </summary>
        internal static uint ComputeValueTypeFieldPaddingFieldValue(uint padding, uint alignment, int targetPointerSize)
        {
            // For the default case, return 0
            if ((padding == 0) && (alignment == targetPointerSize))
                return 0;

            uint alignmentLog2 = 0;
            Debug.Assert(alignment != 0);

            while ((alignment & 1) == 0)
            {
                alignmentLog2++;
                alignment = alignment >> 1;
            }
            Debug.Assert(alignment == 1);

            Debug.Assert(ValueTypePaddingMax >= padding);

            // Our alignment values here are adjusted by one to allow for a default of 0 (which represents pointer alignment)
            alignmentLog2++;

            uint paddingLowBits = padding & ValueTypePaddingLowMask;
            uint paddingHighBits = ((padding & ~ValueTypePaddingLowMask) >> ValueTypePaddingAlignmentShift) << ValueTypePaddingHighShift;
            uint alignmentLog2Bits = alignmentLog2 << ValueTypePaddingAlignmentShift;
            Debug.Assert((alignmentLog2Bits & ~ValueTypePaddingAlignmentMask) == 0);
            return paddingLowBits | paddingHighBits | alignmentLog2Bits;
        }
    }
}
