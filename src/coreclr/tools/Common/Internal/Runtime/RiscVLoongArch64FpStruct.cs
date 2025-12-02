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

        private static bool HandleInlineArray(int elementTypeIndex, int nElements,
            ref FpStructInRegistersInfo info, ref int typeIndex, ref uint occupiedBytesMap)
        {
            int nFlattenedFieldsPerElement = typeIndex - elementTypeIndex;
            if (nFlattenedFieldsPerElement == 0)
            {
                Debug.Assert(nElements == 1, "HasImpliedRepeatedFields must have returned a false, it can't be an array");
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

                Debug.Assert(info.Size1st() == info.Size2nd());
                uint startOffset = info.offset2nd;
                uint endOffset = startOffset + info.Size2nd();

                uint fieldOccupation = (~0u << (int)startOffset) ^ (~0u << (int)endOffset);
                if ((occupiedBytesMap & fieldOccupation) != 0)
                    return false; // duplicated array element overlaps with other fields

                occupiedBytesMap |= fieldOccupation;
            }
            return true;
        }

        private static bool FlattenFields(TypeDesc td, uint offset, ref FpStructInRegistersInfo info, ref int typeIndex)
        {
            IEnumerable<FieldDesc> fields = td.GetFields();
            int nFields = 0;
            int elementTypeIndex = typeIndex;
            FieldDesc lastField = null;
            uint occupiedBytesMap = 0;
            foreach (FieldDesc field in fields)
            {
                if (field.IsStatic)
                    continue;
                nFields++;

                uint startOffset = offset + (uint)field.Offset.AsInt;
                uint endOffset = startOffset + (uint)field.FieldType.GetElementSize().AsInt;

                uint fieldOccupation = (~0u << (int)startOffset) ^ (~0u << (int)endOffset);
                if ((occupiedBytesMap & fieldOccupation) != 0)
                    return false; // fields overlap, treat as union

                occupiedBytesMap |= fieldOccupation;

                lastField = field;

                TypeFlags category = field.FieldType.Category;
                if (category == TypeFlags.ValueType)
                {
                    TypeDesc nested = field.FieldType;
                    if (!FlattenFields(nested, startOffset, ref info, ref typeIndex))
                        return false;
                }
                else if (field.FieldType.GetElementSize().AsInt <= TARGET_POINTER_SIZE)
                {
                    if (typeIndex >= 2)
                        return false; // too many fields

                    bool isFloating = category is TypeFlags.Single or TypeFlags.Double;
                    SetFpStructInRegistersInfoField(ref info, typeIndex++,
                        isFloating, (uint)field.FieldType.GetElementSize().AsInt, startOffset);
                }
                else
                {
                    return false; // field is too big
                }
            }

            if ((td as MetadataType).HasImpliedRepeatedFields())
            {
                Debug.Assert(nFields == 1);
                int nElements = td.GetElementSize().AsInt / lastField.FieldType.GetElementSize().AsInt;

                // Only InlineArrays can have element type of empty struct, fixed-size buffers take only primitives
                if ((typeIndex - elementTypeIndex) == 0 && (td as MetadataType).IsInlineArray)
                {
                    Debug.Assert(nElements > 0, "InlineArray length must be > 0");
                    return false; // struct containing an array of empty structs is passed by integer calling convention
                }

                if (!HandleInlineArray(elementTypeIndex, nElements, ref info, ref typeIndex, ref occupiedBytesMap))
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
            if (nFields == 2 && info.offset1st > info.offset2nd)
            {
                // swap fields to match memory order
                info.flags = (FpStruct)(
                    ((uint)(info.flags & FloatInt) << (PosIntFloat - PosFloatInt)) |
                    ((uint)(info.flags & IntFloat) >> (PosIntFloat - PosFloatInt)) |
                    ((uint)(info.flags & SizeShift1stMask) << (PosSizeShift2nd - PosSizeShift1st)) |
                    ((uint)(info.flags & SizeShift2ndMask) >> (PosSizeShift2nd - PosSizeShift1st))
                );
                (info.offset2nd, info.offset1st) = (info.offset1st, info.offset2nd);
            }
            Debug.Assert((info.flags & (OnlyOne | BothFloat)) == 0);
            Debug.Assert((info.flags & FloatInt) == 0 || info.Size1st() == sizeof(float) || info.Size1st() == sizeof(double));
            Debug.Assert((info.flags & IntFloat) == 0 || info.Size2nd() == sizeof(float) || info.Size2nd() == sizeof(double));

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
