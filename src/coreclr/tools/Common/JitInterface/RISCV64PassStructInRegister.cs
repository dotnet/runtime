// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using ILCompiler;
using Internal.TypeSystem;
using static Internal.JitInterface.StructFloatFieldInfoFlags;
using static Internal.JitInterface.FpStruct;

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
            bool isFloating, uint size, uint offset)
        {
            Debug.Assert(index < 2);
            Debug.Assert(!isFloating || size == sizeof(float) || size == sizeof(double));

            int sizeShift =
                (size == 1) ? 0 :
                (size == 2) ? 1 :
                (size == 4) ? 2 :
                (size == 8) ? 3 :
                -1;
            Debug.Assert(sizeShift != -1);

            const int typeSize = (int)PosFloat2nd - (int)PosFloat1st;
            Debug.Assert((Float2nd | SizeShift2nd) == (FpStruct)((uint)(Float1st | SizeShift1st) << typeSize),
                "1st flags need to be 2nd flags shifted by typeSize");

            int type = (Convert.ToInt32(isFloating) << (int)PosFloat1st) | (sizeShift << (int)PosSizeShift1st);
            info.flags |= (FpStruct)(type << (typeSize * index));
            (index == 0 ? ref info.offset1st : ref info.offset2nd) = offset;
        }

        private static bool HandleInlineArray(int elementTypeIndex, int nElements, ref FpStructInRegistersInfo info, ref int typeIndex)
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

                // duplicate the array element info
                const int typeSize = (int)PosFloat2nd - (int)PosFloat1st;
                info.flags = (FpStruct)((int)info.flags << typeSize) | info.flags;
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
                    return false; // overlapping fields

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
                        return false;

                    SetFpStructInRegistersInfoField(ref info, typeIndex++,
                        (category is TypeFlags.Single or TypeFlags.Double),
                        (uint)field.FieldType.GetElementSize().AsInt,
                        offset + (uint)field.Offset.AsInt);
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
                if (!HandleInlineArray(elementTypeIndex, nElements, ref info, ref typeIndex))
                    return false;
            }
            return true;
        }

        private static FpStructInRegistersInfo GetRiscV64PassFpStructInRegistersInfoImpl(TypeDesc td)
        {
            FpStructInRegistersInfo info = new FpStructInRegistersInfo{};
            int nFields = 0;
            if (!FlattenFields(td, 0, ref info, ref nFields))
            {
                return new FpStructInRegistersInfo{};
            }

            if ((info.flags & (Float1st | Float2nd)) == 0)
            {
                return new FpStructInRegistersInfo{};
            }
            Debug.Assert(nFields == 1 || nFields == 2);

            if ((info.flags & (Float1st | Float2nd)) == (Float1st | Float2nd))
            {
                Debug.Assert(nFields == 2);
                info.flags ^= (Float1st | Float2nd | BothFloat); // replace (1st|2nd)Float with BothFloat
            }
            else if (nFields == 1)
            {
                Debug.Assert((info.flags & Float1st) != 0);
                Debug.Assert((info.flags & (Float2nd | SizeShift2nd)) == 0);
                Debug.Assert(info.offset2nd == 0);
                info.flags ^= (Float1st | OnlyOne); // replace Float1st with OnlyOne
            }
            Debug.Assert(nFields == 1 + Convert.ToInt32((info.flags & OnlyOne) == 0));
            FpStruct floatFlags = info.flags & (OnlyOne | BothFloat | Float1st | Float2nd);
            Debug.Assert(floatFlags != 0);
            Debug.Assert(((uint)floatFlags & ((uint)floatFlags - 1)) == 0,
                "there can be only one of (OnlyOne | BothFloat | Float1st | Float2nd)");
            if (nFields == 2)
            {
                uint end1st = info.offset1st + info.GetSize1st();
                uint end2nd = info.offset2nd + info.GetSize2nd();
                Debug.Assert(end1st <= info.offset2nd || end2nd <= info.offset1st, "fields must not overlap");
            }
            Debug.Assert(info.offset1st + info.GetSize1st() <= td.GetElementSize().AsInt);
            Debug.Assert(info.offset2nd + info.GetSize2nd() <= td.GetElementSize().AsInt);

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
