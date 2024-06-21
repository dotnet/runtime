// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using ILCompiler;
using Internal.TypeSystem;
using static Internal.JitInterface.FpStruct;
using static Internal.JitInterface.StructFloatFieldInfoFlags;

namespace Internal.JitInterface
{
    internal static class RISCV64PassStructInRegister
    {
        private const int TARGET_POINTER_SIZE = 8;

        private static bool HandleInlineArrayOld(int elementTypeIndex, int nElements, Span<StructFloatFieldInfoFlags> types, ref int typeIndex)
        {
            int nFlattenedFieldsPerElement = typeIndex - elementTypeIndex;
            if (nFlattenedFieldsPerElement == 0)
                return true;

            Debug.Assert(nFlattenedFieldsPerElement == 1 || nFlattenedFieldsPerElement == 2);

            if (nElements > 2)
                return false;

            if (nElements == 2)
            {
                if (typeIndex + nFlattenedFieldsPerElement > 2)
                    return false;

                Debug.Assert(elementTypeIndex == 0);
                Debug.Assert(typeIndex == 1);
                types[typeIndex++] = types[elementTypeIndex]; // duplicate the array element type
            }
            return true;
        }

        private static bool FlattenFieldTypesOld(TypeDesc td, Span<StructFloatFieldInfoFlags> types, ref int typeIndex)
        {
            IEnumerable<FieldDesc> fields = td.GetFields();
            int nFields = 0;
            int elementTypeIndex = typeIndex;
            FieldDesc prevField = null;
            foreach (FieldDesc field in fields)
            {
                if (field.IsStatic)
                    continue;
                nFields++;

                if (prevField != null && prevField.Offset.AsInt + prevField.FieldType.GetElementSize().AsInt > field.Offset.AsInt)
                    return false; // overlapping fields

                prevField = field;

                TypeFlags category = field.FieldType.Category;
                if (category == TypeFlags.ValueType)
                {
                    TypeDesc nested = field.FieldType;
                    if (!FlattenFieldTypesOld(nested, types, ref typeIndex))
                        return false;
                }
                else if (field.FieldType.GetElementSize().AsInt <= TARGET_POINTER_SIZE)
                {
                    if (typeIndex >= 2)
                        return false;

                    StructFloatFieldInfoFlags type =
                        (category is TypeFlags.Single or TypeFlags.Double ? STRUCT_FLOAT_FIELD_FIRST : (StructFloatFieldInfoFlags)0) |
                        (field.FieldType.GetElementSize().AsInt == TARGET_POINTER_SIZE ? STRUCT_FIRST_FIELD_SIZE_IS8 : (StructFloatFieldInfoFlags)0);
                    types[typeIndex++] = type;
                }
                else
                {
                    return false;
                }
            }

            if ((td as MetadataType).HasImpliedRepeatedFields())
            {
                Debug.Assert(nFields == 1);
                int nElements = td.GetElementSize().AsInt / prevField.FieldType.GetElementSize().AsInt;

                // Only InlineArrays can have an element type of an empty struct, fixed-size buffers take only primitives
                if ((typeIndex - elementTypeIndex) == 0 && (td as MetadataType).IsInlineArray)
                {
                    Debug.Assert(nElements > 0, "InlineArray length must be > 0");
                    return false; // struct containing an array of empty structs is passed by integer calling convention
                }

                if (!HandleInlineArrayOld(elementTypeIndex, nElements, types, ref typeIndex))
                    return false;
            }
            return true;
        }

        private static uint GetRISCV64PassStructInRegisterFlags(TypeDesc td)
        {
            Span<StructFloatFieldInfoFlags> types = stackalloc StructFloatFieldInfoFlags[] {
                STRUCT_NO_FLOAT_FIELD, STRUCT_NO_FLOAT_FIELD
            };
            int nFields = 0;
            if (!FlattenFieldTypesOld(td, types, ref nFields) || nFields == 0)
                return (uint)STRUCT_NO_FLOAT_FIELD;

            Debug.Assert(nFields == 1 || nFields == 2);

            Debug.Assert((uint)(STRUCT_FLOAT_FIELD_SECOND | STRUCT_SECOND_FIELD_SIZE_IS8)
                == (uint)(STRUCT_FLOAT_FIELD_FIRST | STRUCT_FIRST_FIELD_SIZE_IS8) << 1,
                "SECOND flags need to be FIRST shifted by 1");
            StructFloatFieldInfoFlags flags = types[0] | (StructFloatFieldInfoFlags)((uint)types[1] << 1);

            const StructFloatFieldInfoFlags bothFloat = STRUCT_FLOAT_FIELD_FIRST | STRUCT_FLOAT_FIELD_SECOND;
            if ((flags & bothFloat) == 0)
                return (uint)STRUCT_NO_FLOAT_FIELD;

            if ((flags & bothFloat) == bothFloat)
            {
                Debug.Assert(nFields == 2);
                flags ^= (bothFloat | STRUCT_FLOAT_FIELD_ONLY_TWO); // replace bothFloat with ONLY_TWO
            }
            else if (nFields == 1)
            {
                Debug.Assert((flags & STRUCT_FLOAT_FIELD_FIRST) != 0);
                flags ^= (STRUCT_FLOAT_FIELD_FIRST | STRUCT_FLOAT_FIELD_ONLY_ONE); // replace FIRST with ONLY_ONE
            }
            return (uint)flags;
        }


        private static void SetFpStructInRegistersInfoField(ref FpStructInRegistersInfo info, int index,
            bool isFloating, FpStruct_IntKind intKind, uint size, uint offset)
        {
            Debug.Assert(index < 2);
            if (isFloating)
            {
                Debug.Assert(size == sizeof(float) || size == sizeof(double));
                Debug.Assert(intKind == FpStruct_IntKind.Signed);
                Debug.Assert((int)FpStruct_IntKind.Signed == 0,
                    "IntKind for floating fields should not clobber IntKind for int fields");
            }

            Debug.Assert(size >= 1 && size <= 8);
            Debug.Assert((size & (size - 1)) == 0, "size needs to be a power of 2");
            const int sizeShiftLUT = (0 << (1*2)) | (1 << (2*2)) | (2 << (4*2)) | (3 << (8*2));
            int sizeShift = (sizeShiftLUT >> ((int)size * 2)) & 0b11;

            // Use FloatInt and IntFloat as marker flags for 1st and 2nd field respectively being floating.
            // Fix to real flags (with OnlyOne and BothFloat) after flattening is complete.
            Debug.Assert((int)PosIntFloat == (int)PosFloatInt + 1, "FloatInt and IntFloat need to be adjacent");
            Debug.Assert((int)PosSizeShift2nd == (int)PosSizeShift1st + 2, "SizeShift1st and 2nd need to be adjacent");
            int floatFlag = Convert.ToInt32(isFloating) << ((int)PosFloatInt + index);
            int sizeShiftMask = sizeShift << ((int)PosSizeShift1st + 2 * index);

            info.flags |= (FpStruct)(floatFlag | sizeShiftMask | ((int)intKind << (int)PosIntFieldKind));
            (index == 0 ? ref info.offset1st : ref info.offset2nd) = offset;
        }

        private static bool HandleInlineArray(int elementTypeIndex, int nElements, ref FpStructInRegistersInfo info, ref int typeIndex)
        {
            int nFlattenedFieldsPerElement = typeIndex - elementTypeIndex;
            if (nFlattenedFieldsPerElement == 0)
            {
                Debug.Assert(nElements == 1, "HasImpliedRepeatedFields must have returned a false positive");
                return true; // ignoring empty struct
            }

            Debug.Assert(nFlattenedFieldsPerElement == 1 || nFlattenedFieldsPerElement == 2);

            if (nElements > 2)
                return false; // array has too many elements

            if (nElements == 2)
            {
                if (typeIndex + nFlattenedFieldsPerElement > 2)
                    return false; // array has too many fields per element

                Debug.Assert(elementTypeIndex == 0);
                Debug.Assert(typeIndex == 1);

                // duplicate the array element info
                const int typeSize = (int)PosIntFloat - (int)PosFloatInt;
                info.flags |= (FpStruct)((int)info.flags << typeSize);
                info.offset2nd = info.offset1st + info.GetSize1st();
            }
            return true;
        }

        private static bool FlattenFields(TypeDesc td, uint offset, ref FpStructInRegistersInfo info, ref int typeIndex)
        {
            IEnumerable<FieldDesc> fields = td.GetFields();
            int nFields = 0;
            int elementTypeIndex = typeIndex;
            FieldDesc prevField = null;
            foreach (FieldDesc field in fields)
            {
                if (field.IsStatic)
                    continue;
                nFields++;

                if (prevField != null && prevField.Offset.AsInt + prevField.FieldType.GetElementSize().AsInt > field.Offset.AsInt)
                    return false; // fields overlap, treat as union

                prevField = field;

                TypeFlags category = field.FieldType.Category;
                if (category == TypeFlags.ValueType)
                {
                    TypeDesc nested = field.FieldType;
                    if (!FlattenFields(nested, offset + (uint)field.Offset.AsInt, ref info, ref typeIndex))
                        return false;
                }
                else if (field.FieldType.GetElementSize().AsInt <= TARGET_POINTER_SIZE)
                {
                    if (typeIndex >= 2)
                        return false; // too many fields

                    bool isFloating = category is TypeFlags.Single or TypeFlags.Double;
                    bool isSignedInt = category is
                        TypeFlags.SByte or
                        TypeFlags.Int16 or
                        TypeFlags.Int32 or
                        TypeFlags.Int64 or
                        TypeFlags.IntPtr;
                    bool isGcRef = category is
                        TypeFlags.Class or
                        TypeFlags.Interface or
                        TypeFlags.Array or
                        TypeFlags.SzArray;

                    FpStruct_IntKind intKind =
                        isGcRef ? FpStruct_IntKind.GcRef :
                        (category is TypeFlags.ByRef) ? FpStruct_IntKind.GcByRef :
                        (isSignedInt || isFloating) ? FpStruct_IntKind.Signed : FpStruct_IntKind.Unsigned;

                    SetFpStructInRegistersInfoField(ref info, typeIndex++,
                        isFloating, intKind, (uint)field.FieldType.GetElementSize().AsInt, offset + (uint)field.Offset.AsInt);
                }
                else
                {
                    return false; // field is too big
                }
            }

            if ((td as MetadataType).HasImpliedRepeatedFields())
            {
                Debug.Assert(nFields == 1);
                int nElements = td.GetElementSize().AsInt / prevField.FieldType.GetElementSize().AsInt;

                // Only InlineArrays can have an element type of an empty struct, fixed-size buffers take only primitives
                if ((typeIndex - elementTypeIndex) == 0 && (td as MetadataType).IsInlineArray)
                {
                    Debug.Assert(nElements > 0, "InlineArray length must be > 0");
                    return false; // struct containing an array of empty structs is passed by integer calling convention
                }

                if (!HandleInlineArray(elementTypeIndex, nElements, ref info, ref typeIndex))
                    return false;
            }
            return true;
        }

        private static bool IsAligned(uint val, uint alignment) => 0 == (val & (alignment - 1));

        private static FpStructInRegistersInfo GetRiscV64PassFpStructInRegistersInfoImpl(TypeDesc td)
        {
            FpStructInRegistersInfo info = new FpStructInRegistersInfo{};
            int nFields = 0;
            if (!FlattenFields(td, 0, ref info, ref nFields))
                return new FpStructInRegistersInfo{};

            if ((info.flags & (FloatInt | IntFloat)) == 0)
                return new FpStructInRegistersInfo{}; // struct has no floating fields

            Debug.Assert(nFields == 1 || nFields == 2);

            if ((info.flags & (FloatInt | IntFloat)) == (FloatInt | IntFloat))
            {
                Debug.Assert(nFields == 2);
                info.flags ^= (FloatInt | IntFloat | BothFloat); // replace (FloatInt | IntFloat) with BothFloat
            }
            else if (nFields == 1)
            {
                Debug.Assert((info.flags & FloatInt) != 0);
                Debug.Assert((info.flags & (IntFloat | SizeShift2ndMask)) == 0);
                Debug.Assert(info.offset2nd == 0);
                info.flags ^= (FloatInt | OnlyOne); // replace FloatInt with OnlyOne
            }
            Debug.Assert(nFields == ((info.flags & OnlyOne) != 0 ? 1 : 2));
            FpStruct floatFlags = info.flags & (OnlyOne | BothFloat | FloatInt | IntFloat);
            Debug.Assert(floatFlags != 0);
            Debug.Assert(((uint)floatFlags & ((uint)floatFlags - 1)) == 0,
                "there can be only one of (OnlyOne | BothFloat | FloatInt | IntFloat)");
            if (nFields == 2)
            {
                uint end1st = info.offset1st + info.GetSize1st();
                uint end2nd = info.offset2nd + info.GetSize2nd();
                Debug.Assert(end1st <= info.offset2nd || end2nd <= info.offset1st, "fields must not overlap");
            }
            Debug.Assert(info.offset1st + info.GetSize1st() <= td.GetElementSize().AsInt);
            Debug.Assert(info.offset2nd + info.GetSize2nd() <= td.GetElementSize().AsInt);
            if (info.GetIntFieldKind() != FpStruct_IntKind.Signed)
            {
                Debug.Assert((info.flags & (FloatInt | IntFloat)) != 0);
                if (info.GetIntFieldKind() >= FpStruct_IntKind.GcRef)
                {
                    Debug.Assert((info.flags & IntFloat) != 0
                        ? (info.IsSize1st8() && IsAligned(info.offset1st, TARGET_POINTER_SIZE))
                        : (info.IsSize2nd8() && IsAligned(info.offset2nd, TARGET_POINTER_SIZE)));
                }
            }
            if ((info.flags & (OnlyOne | BothFloat)) != 0)
                Debug.Assert(info.GetIntFieldKind() == FpStruct_IntKind.Signed);

            return info;
        }

        public static FpStructInRegistersInfo GetRiscV64PassFpStructInRegistersInfo(TypeDesc td)
        {
            FpStructInRegistersInfo info = GetRiscV64PassFpStructInRegistersInfoImpl(td);
            uint flags = GetRISCV64PassStructInRegisterFlags(td);

            Debug.Assert(flags == (uint)info.ToOldFlags());

            return info;
        }
    }
}
