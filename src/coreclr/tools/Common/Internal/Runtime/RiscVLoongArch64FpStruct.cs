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
    // StructFloatFieldInfoFlags: used on LoongArch64 and RISC-V architecture as a legacy representation of
    // FpStructInRegistersInfo, returned by FpStructInRegistersInfo.ToOldFlags()
    //
    // `STRUCT_NO_FLOAT_FIELD` means structs are not passed using the float register(s).
    //
    // Otherwise, and only for structs with no more than two fields and a total struct size no larger
    // than two pointers:
    //
    // The lowest four bits denote the floating-point info:
    //   bit 0: `1` means there is only one float or double field within the struct.
    //   bit 1: `1` means only the first field is floating-point type.
    //   bit 2: `1` means only the second field is floating-point type.
    //   bit 3: `1` means the two fields are both floating-point type.
    // The bits[5:4] denoting whether the field size is 8-bytes:
    //   bit 4: `1` means the first field's size is 8.
    //   bit 5: `1` means the second field's size is 8.
    //
    // Note that bit 0 and 3 cannot both be set.
    [Flags]
    public enum StructFloatFieldInfoFlags
    {
        STRUCT_NO_FLOAT_FIELD         = 0x0,
        STRUCT_FLOAT_FIELD_ONLY_ONE   = 0x1,
        STRUCT_FLOAT_FIELD_ONLY_TWO   = 0x8,
        STRUCT_FLOAT_FIELD_FIRST      = 0x2,
        STRUCT_FLOAT_FIELD_SECOND     = 0x4,
        STRUCT_FIRST_FIELD_SIZE_IS8   = 0x10,
        STRUCT_SECOND_FIELD_SIZE_IS8  = 0x20,
    };


    // Bitfields for FpStructInRegistersInfo.flags
    [Flags]
    public enum FpStruct
    {
        // Positions of flags and bitfields
        PosOnlyOne      = 0,
        PosBothFloat    = 1,
        PosFloatInt     = 2,
        PosIntFloat     = 3,
        PosSizeShift1st = 4, // 2 bits
        PosSizeShift2nd = 6, // 2 bits

        UseIntCallConv = 0, // struct is passed according to integer calling convention

        // The flags and bitfields
        OnlyOne          =    1 << PosOnlyOne,      // has only one field, which is floating-point
        BothFloat        =    1 << PosBothFloat,    // has two fields, both are floating-point
        FloatInt         =    1 << PosFloatInt,     // has two fields, 1st is floating and 2nd is integer
        IntFloat         =    1 << PosIntFloat,     // has two fields, 2nd is floating and 1st is integer
        SizeShift1stMask = 0b11 << PosSizeShift1st, // log2(size) of 1st field
        SizeShift2ndMask = 0b11 << PosSizeShift2nd, // log2(size) of 2nd field
        // Note: flags OnlyOne, BothFloat, FloatInt, and IntFloat are mutually exclusive
    }

    // On RISC-V and LoongArch a struct with up to two non-empty fields, at least one of them floating-point,
    // can be passed in registers according to hardware FP calling convention. FpStructInRegistersInfo represents
    // passing information for such parameters.
    public struct FpStructInRegistersInfo
    {
        public FpStruct flags;
        public uint offset1st;
        public uint offset2nd;

        public uint SizeShift1st() { return (uint)((int)flags >> (int)FpStruct.PosSizeShift1st) & 0b11; }

        public uint SizeShift2nd() { return (uint)((int)flags >> (int)FpStruct.PosSizeShift2nd) & 0b11; }

        public uint Size1st() { return 1u << (int)SizeShift1st(); }
        public uint Size2nd() { return 1u << (int)SizeShift2nd(); }

        public StructFloatFieldInfoFlags ToOldFlags()
        {
            return
                ((flags & FpStruct.OnlyOne) != 0 ? StructFloatFieldInfoFlags.STRUCT_FLOAT_FIELD_ONLY_ONE : 0) |
                ((flags & FpStruct.BothFloat) != 0 ? StructFloatFieldInfoFlags.STRUCT_FLOAT_FIELD_ONLY_TWO : 0) |
                ((flags & FpStruct.FloatInt) != 0 ? StructFloatFieldInfoFlags.STRUCT_FLOAT_FIELD_FIRST : 0) |
                ((flags & FpStruct.IntFloat) != 0 ? StructFloatFieldInfoFlags.STRUCT_FLOAT_FIELD_SECOND : 0) |
                ((SizeShift1st() == 3) ? StructFloatFieldInfoFlags.STRUCT_FIRST_FIELD_SIZE_IS8 : 0) |
                ((SizeShift2nd() == 3) ? StructFloatFieldInfoFlags.STRUCT_SECOND_FIELD_SIZE_IS8 : 0);
        }
    }

    internal static class RiscVLoongArch64FpStruct
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

        public static FpStructInRegistersInfo GetFpStructInRegistersInfo(TypeDesc td, TargetArchitecture arch)
        {
            Debug.Assert(arch is TargetArchitecture.RiscV64 or TargetArchitecture.LoongArch64);

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
