// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using ILCompiler;
using Internal.TypeSystem;
using static Internal.JitInterface.StructFloatFieldInfoFlags;

namespace Internal.JitInterface
{
    internal static class RISCV64PassStructInRegister
    {
        private const int
            ENREGISTERED_PARAMTYPE_MAXSIZE = 16,
            TARGET_POINTER_SIZE = 8;

        private static bool HandleInlineArray(int elementTypeIndex, int nElements, Span<StructFloatFieldInfoFlags> types, ref int typeIndex)
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

        private static bool FlattenFieldTypes(TypeDesc td, Span<StructFloatFieldInfoFlags> types, ref int typeIndex)
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
                    if (!FlattenFieldTypes(nested, types, ref typeIndex))
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
                if (!HandleInlineArray(elementTypeIndex, nElements, types, ref typeIndex))
                    return false;
            }
            return true;
        }

        public static uint GetRISCV64PassStructInRegisterFlags(TypeDesc td)
        {
            if (td.GetElementSize().AsInt > ENREGISTERED_PARAMTYPE_MAXSIZE)
                return (uint)STRUCT_NO_FLOAT_FIELD;

            Span<StructFloatFieldInfoFlags> types = stackalloc StructFloatFieldInfoFlags[] {
                STRUCT_NO_FLOAT_FIELD, STRUCT_NO_FLOAT_FIELD
            };
            int nFields = 0;
            if (!FlattenFieldTypes(td, types, ref nFields) || nFields == 0)
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
    }
}
