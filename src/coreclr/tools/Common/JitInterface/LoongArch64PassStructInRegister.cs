// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Diagnostics;
using ILCompiler;
using Internal.TypeSystem;

namespace Internal.JitInterface
{

    internal static class LoongArch64PassStructInRegister
    {
        public static uint GetLoongArch64PassStructInRegisterFlags(TypeDesc typeDesc)
        {
            FieldDesc firstField = null;
            uint floatFieldFlags = (uint)StructFloatFieldInfoFlags.STRUCT_NO_FLOAT_FIELD;
            int numIntroducedFields = 0;
            foreach (FieldDesc field in typeDesc.GetFields())
            {
                if (!field.IsStatic)
                {
                    firstField ??= field;
                    numIntroducedFields++;
                }
            }

            if ((numIntroducedFields == 0) || (numIntroducedFields > 2) || (typeDesc.GetElementSize().AsInt > 16))
            {
                return (uint)StructFloatFieldInfoFlags.STRUCT_NO_FLOAT_FIELD;
            }

            //// The SIMD Intrinsic types are meant to be handled specially and should not be passed as struct registers
            if (typeDesc.IsIntrinsic)
            {
                throw new NotImplementedException("For LoongArch64, SIMD would be implemented later");
            }

            MetadataType mdType = typeDesc as MetadataType;
            Debug.Assert(mdType != null);

            TypeDesc firstFieldElementType = firstField.FieldType;
            int firstFieldSize = firstFieldElementType.GetElementSize().AsInt;

            bool hasImpliedRepeatedFields = mdType.HasImpliedRepeatedFields();

            if (hasImpliedRepeatedFields)
            {
                numIntroducedFields = typeDesc.GetElementSize().AsInt / firstFieldSize;
                if (numIntroducedFields > 2)
                {
                    return (uint)StructFloatFieldInfoFlags.STRUCT_NO_FLOAT_FIELD;
                }
            }

            int fieldIndex = 0;
            foreach (FieldDesc field in typeDesc.GetFields())
            {
                if (field.IsStatic)
                {
                    continue;
                }

                Debug.Assert(fieldIndex < numIntroducedFields);

                switch (field.FieldType.Category)
                {
                    case TypeFlags.Double:
                    {
                        if (numIntroducedFields == 1)
                        {
                            floatFieldFlags = (uint)StructFloatFieldInfoFlags.STRUCT_FLOAT_FIELD_ONLY_ONE;
                        }
                        else if (fieldIndex == 0)
                        {
                            floatFieldFlags = (uint)StructFloatFieldInfoFlags.STRUCT_FIRST_FIELD_DOUBLE;
                        }
                        else if ((floatFieldFlags & (uint)StructFloatFieldInfoFlags.STRUCT_FLOAT_FIELD_FIRST) != 0)
                        {
                            floatFieldFlags ^= (uint)StructFloatFieldInfoFlags.STRUCT_MERGE_FIRST_SECOND_8;
                        }
                        else
                        {
                            floatFieldFlags |= (uint)StructFloatFieldInfoFlags.STRUCT_SECOND_FIELD_DOUBLE;
                        }
                    }
                    break;

                    case  TypeFlags.Single:
                    {
                        if (numIntroducedFields == 1)
                        {
                            floatFieldFlags = (uint)StructFloatFieldInfoFlags.STRUCT_FLOAT_FIELD_ONLY_ONE;
                        }
                        else if (fieldIndex == 0)
                        {
                            floatFieldFlags = (uint)StructFloatFieldInfoFlags.STRUCT_FLOAT_FIELD_FIRST;
                        }
                        else if ((floatFieldFlags & (uint)StructFloatFieldInfoFlags.STRUCT_FLOAT_FIELD_FIRST) != 0)
                        {
                            floatFieldFlags ^= (uint)StructFloatFieldInfoFlags.STRUCT_MERGE_FIRST_SECOND;
                        }
                        else
                        {
                            floatFieldFlags |= (uint)StructFloatFieldInfoFlags.STRUCT_FLOAT_FIELD_SECOND;
                        }
                    }
                    break;

                    case TypeFlags.ValueType:
                    //case TypeFlags.Class:
                    //case TypeFlags.Array:
                    //case TypeFlags.SzArray:
                    {
                        uint floatFieldFlags2 = GetLoongArch64PassStructInRegisterFlags(field.FieldType);
                        if (numIntroducedFields == 1)
                        {
                            floatFieldFlags = floatFieldFlags2;
                        }
                        else if (field.FieldType.GetElementSize().AsInt > 8)
                        {
                            return (uint)StructFloatFieldInfoFlags.STRUCT_NO_FLOAT_FIELD;
                        }
                        else if (fieldIndex == 0)
                        {
                            if ((floatFieldFlags2 & (uint)StructFloatFieldInfoFlags.STRUCT_FLOAT_FIELD_ONLY_ONE) != 0)
                            {
                                floatFieldFlags = (uint)StructFloatFieldInfoFlags.STRUCT_FLOAT_FIELD_FIRST;
                            }
                            if (field.FieldType.GetElementSize().AsInt == 8)
                            {
                                floatFieldFlags |= (uint)StructFloatFieldInfoFlags.STRUCT_FIRST_FIELD_SIZE_IS8;
                            }
                        }
                        else
                        {
                            Debug.Assert(fieldIndex == 1);
                            if ((floatFieldFlags2 & (uint)StructFloatFieldInfoFlags.STRUCT_FLOAT_FIELD_ONLY_ONE) != 0)
                            {
                                floatFieldFlags |= (uint)StructFloatFieldInfoFlags.STRUCT_MERGE_FIRST_SECOND;
                            }
                            if (field.FieldType.GetElementSize().AsInt == 8)
                            {
                                floatFieldFlags |= (uint)StructFloatFieldInfoFlags.STRUCT_SECOND_FIELD_SIZE_IS8;
                            }

                            floatFieldFlags2 = floatFieldFlags & ((uint)StructFloatFieldInfoFlags.STRUCT_FLOAT_FIELD_FIRST | (uint)StructFloatFieldInfoFlags.STRUCT_FLOAT_FIELD_SECOND);
                            if (floatFieldFlags2 == 0)
                            {
                                floatFieldFlags = (uint)StructFloatFieldInfoFlags.STRUCT_NO_FLOAT_FIELD;
                            }
                            else if (floatFieldFlags2 == ((uint)StructFloatFieldInfoFlags.STRUCT_FLOAT_FIELD_FIRST | (uint)StructFloatFieldInfoFlags.STRUCT_FLOAT_FIELD_SECOND))
                            {
                                floatFieldFlags ^= ((uint)StructFloatFieldInfoFlags.STRUCT_FLOAT_FIELD_ONLY_TWO | (uint)StructFloatFieldInfoFlags.STRUCT_FLOAT_FIELD_FIRST | (uint)StructFloatFieldInfoFlags.STRUCT_FLOAT_FIELD_SECOND);
                            }
                        }
                    }
                    break;

                    default:
                    {
                        if ((numIntroducedFields == 2) && (field.FieldType.Category == TypeFlags.Class))
                        {
                            return (uint)StructFloatFieldInfoFlags.STRUCT_NO_FLOAT_FIELD;
                        }

                        if (field.FieldType.GetElementSize().AsInt == 8)
                        {
                            if (numIntroducedFields > 1)
                            {
                                if (fieldIndex == 0)
                                {
                                    floatFieldFlags = (uint)StructFloatFieldInfoFlags.STRUCT_FIRST_FIELD_SIZE_IS8;
                                }
                                else if ((floatFieldFlags & (uint)StructFloatFieldInfoFlags.STRUCT_FLOAT_FIELD_FIRST) != 0)
                                {
                                    floatFieldFlags |= (uint)StructFloatFieldInfoFlags.STRUCT_SECOND_FIELD_SIZE_IS8;
                                }
                                else
                                {
                                    floatFieldFlags = (uint)StructFloatFieldInfoFlags.STRUCT_NO_FLOAT_FIELD;
                                }
                            }
                        }
                        else if (fieldIndex == 1)
                        {
                            floatFieldFlags = (floatFieldFlags & (uint)StructFloatFieldInfoFlags.STRUCT_FLOAT_FIELD_FIRST) > 0 ? floatFieldFlags : (uint)StructFloatFieldInfoFlags.STRUCT_NO_FLOAT_FIELD;
                        }
                        break;
                    }
                }

                fieldIndex++;
            }

            return floatFieldFlags;
        }
    }
}
