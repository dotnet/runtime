// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using ILCompiler;
using Internal.TypeSystem;
using static Internal.JitInterface.FpStruct;

namespace Internal.JitInterface
{
    internal static class LoongArch64PassStructInRegister
    {
        private const int
            ENREGISTERED_PARAMTYPE_MAXSIZE = 16,
            TARGET_POINTER_SIZE = 8;

        private static void SetFpStructInRegistersInfoField(ref FpStructInRegistersInfo info, int index,
            bool isFloating, uint size, uint offset)
        {
            Debug.Assert(index < 2);
            if (isFloating)
                Debug.Assert(size == sizeof(float) || size == sizeof(double));

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

            info.flags |= (FpStruct)(floatFlag | sizeShiftMask);
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

                // Duplicate the array element info
                Debug.Assert((int)FpStruct.IntFloat == ((int)FpStruct.FloatInt << 1),
                    "FloatInt and IntFloat need to be adjacent");
                Debug.Assert((int)FpStruct.SizeShift2ndMask == ((int)FpStruct.SizeShift1stMask << 2),
                    "SizeShift1st and 2nd need to be adjacent");
                // Take the 1st field info and shift up to the 2nd field's positions
                int floatFlag = (int)(info.flags & FpStruct.FloatInt) << 1;
                int sizeShiftMask = (int)(info.flags & FpStruct.SizeShift1stMask) << 2;
                info.flags |= (FpStruct)(floatFlag | sizeShiftMask); // merge with 1st field
                info.offset2nd = info.offset1st + info.Size1st(); // bump up the field offset
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
                    SetFpStructInRegistersInfoField(ref info, typeIndex++,
                        isFloating, (uint)field.FieldType.GetElementSize().AsInt, offset + (uint)field.Offset.AsInt);
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
                if (!HandleInlineArray(elementTypeIndex, nElements, ref info, ref typeIndex))
                    return false;
            }
            return true;
        }

        private static bool IsAligned(uint val, uint alignment) => 0 == (val & (alignment - 1));

        public static FpStructInRegistersInfo GetLoongArch64PassFpStructInRegistersInfo(TypeDesc td)
        {
            if (td.GetElementSize().AsInt > ENREGISTERED_PARAMTYPE_MAXSIZE)
                return new FpStructInRegistersInfo{};

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
                uint end1st = info.offset1st + info.Size1st();
                uint end2nd = info.offset2nd + info.Size2nd();
                Debug.Assert(end1st <= info.offset2nd || end2nd <= info.offset1st, "fields must not overlap");
            }
            Debug.Assert(info.offset1st + info.Size1st() <= td.GetElementSize().AsInt);
            Debug.Assert(info.offset2nd + info.Size2nd() <= td.GetElementSize().AsInt);

            return info;
        }
    }
}
