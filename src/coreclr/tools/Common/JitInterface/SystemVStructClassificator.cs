// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using ILCompiler;
using Internal.TypeSystem;
using static Internal.JitInterface.SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR;
using static Internal.JitInterface.SystemVClassificationType;

namespace Internal.JitInterface
{
    internal static class SystemVStructClassificator
    {
        private struct SystemVStructRegisterPassingHelper
        {
            internal SystemVStructRegisterPassingHelper(int totalStructSize)
            {
                StructSize = totalStructSize;
                EightByteCount = 0;
                InEmbeddedStruct = false;
                CurrentUniqueOffsetField = 0;
                LargestFieldOffset = -1;

                EightByteClassifications = new SystemVClassificationType[CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS];
                EightByteSizes = new int[CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS];
                EightByteOffsets = new int[CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS];

                FieldClassifications = new SystemVClassificationType[SYSTEMV_MAX_NUM_FIELDS_IN_REGISTER_PASSED_STRUCT];
                FieldSizes = new int[SYSTEMV_MAX_NUM_FIELDS_IN_REGISTER_PASSED_STRUCT];
                FieldOffsets = new int[SYSTEMV_MAX_NUM_FIELDS_IN_REGISTER_PASSED_STRUCT];

                for (int i = 0; i < CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS; i++)
                {
                    EightByteClassifications[i] = SystemVClassificationTypeNoClass;
                    EightByteSizes[i] = 0;
                    EightByteOffsets[i] = 0;
                }

                // Initialize the work arrays
                for (int i = 0; i < SYSTEMV_MAX_NUM_FIELDS_IN_REGISTER_PASSED_STRUCT; i++)
                {
                    FieldClassifications[i] = SystemVClassificationTypeNoClass;
                    FieldSizes[i] = 0;
                    FieldOffsets[i] = 0;
                }
            }

            // Input state.
            public int                         StructSize;

            // These fields are the output; these are what is computed by the classification algorithm.
            public int                         EightByteCount;
            public SystemVClassificationType[] EightByteClassifications;
            public int[]                       EightByteSizes;
            public int[]                       EightByteOffsets;

            // Helper members to track state.
            public bool                        InEmbeddedStruct;
            public int                         CurrentUniqueOffsetField; // A virtual field that could encompass many overlapping fields.
            public int                         LargestFieldOffset;
            public SystemVClassificationType[] FieldClassifications;
            public int[]                       FieldSizes;
            public int[]                       FieldOffsets;
        };

        private static class FieldEnumerator
        {
            internal static IEnumerable<FieldDesc> GetInstanceFields(TypeDesc typeDesc, bool isFixedBuffer, int numIntroducedFields)
            {
                foreach (FieldDesc field in typeDesc.GetFields())
                {
                    if (field.IsStatic)
                        continue;

                    if (isFixedBuffer)
                    {
                        for (int i = 0; i < numIntroducedFields; i++)
                        {
                            yield return field;
                        }
                        break;
                    }
                    else
                    {
                        yield return field;
                    }
                }
            }
        }

        public static void GetSystemVAmd64PassStructInRegisterDescriptor(TypeDesc typeDesc, out SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR structPassInRegDescPtr)
        {
            structPassInRegDescPtr = default;
            structPassInRegDescPtr.passedInRegisters = false;

            int typeSize = typeDesc.GetElementSize().AsInt;
            if (typeDesc.IsValueType && (typeSize <= CLR_SYSTEMV_MAX_STRUCT_BYTES_TO_PASS_IN_REGISTERS))
            {
                if (TypeDef2SystemVClassification(typeDesc) != SystemVClassificationTypeStruct)
                {
                    return;
                }

                SystemVStructRegisterPassingHelper helper = new SystemVStructRegisterPassingHelper(typeSize);
                bool canPassInRegisters = ClassifyEightBytes(typeDesc, ref helper, 0);
                if (canPassInRegisters)
                {
                    structPassInRegDescPtr.passedInRegisters = canPassInRegisters;
                    structPassInRegDescPtr.eightByteCount = (byte)helper.EightByteCount;
                    Debug.Assert(structPassInRegDescPtr.eightByteCount <= CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS);

                    structPassInRegDescPtr.eightByteClassifications0 = helper.EightByteClassifications[0];
                    structPassInRegDescPtr.eightByteSizes0 = (byte)helper.EightByteSizes[0];
                    structPassInRegDescPtr.eightByteOffsets0 = (byte)helper.EightByteOffsets[0];

                    structPassInRegDescPtr.eightByteClassifications1 = helper.EightByteClassifications[1];
                    structPassInRegDescPtr.eightByteSizes1 = (byte)helper.EightByteSizes[1];
                    structPassInRegDescPtr.eightByteOffsets1 = (byte)helper.EightByteOffsets[1];
                }
            }
        }

        private static SystemVClassificationType TypeDef2SystemVClassification(TypeDesc typeDesc)
        {
            switch (typeDesc.Category)
            {
                case TypeFlags.Boolean:
                case TypeFlags.Char:
                case TypeFlags.SByte:
                case TypeFlags.Byte:
                case TypeFlags.Int16:
                case TypeFlags.UInt16:
                case TypeFlags.Int32:
                case TypeFlags.UInt32:
                case TypeFlags.Int64:
                case TypeFlags.UInt64:
                case TypeFlags.IntPtr:
                case TypeFlags.UIntPtr:
                case TypeFlags.Enum:
                case TypeFlags.Pointer:
                case TypeFlags.FunctionPointer:
                    return SystemVClassificationTypeInteger;
                case TypeFlags.Single:
                case TypeFlags.Double:
                    return SystemVClassificationTypeSSE;
                case TypeFlags.ValueType:
                case TypeFlags.Nullable:
                    return SystemVClassificationTypeStruct;
                case TypeFlags.Class:
                case TypeFlags.Interface:
                case TypeFlags.Array:
                case TypeFlags.SzArray:
                    return SystemVClassificationTypeIntegerReference;
                case TypeFlags.ByRef:
                    return SystemVClassificationTypeIntegerByRef;
                case TypeFlags.GenericParameter:
                case TypeFlags.SignatureTypeVariable:
                case TypeFlags.SignatureMethodVariable:
                    Debug.Fail($"Type {typeDesc} with unexpected category {typeDesc.Category}");
                    return SystemVClassificationTypeUnknown;
                default:
                    return SystemVClassificationTypeUnknown;
            }
        }

        // If we have a field classification already, but there is a union, we must merge the classification type of the field. Returns the
        // new, merged classification type.
        private static SystemVClassificationType ReClassifyField(SystemVClassificationType originalClassification, SystemVClassificationType newFieldClassification)
        {
            Debug.Assert((newFieldClassification == SystemVClassificationTypeInteger) ||
                            (newFieldClassification == SystemVClassificationTypeIntegerReference) ||
                            (newFieldClassification == SystemVClassificationTypeIntegerByRef) ||
                            (newFieldClassification == SystemVClassificationTypeSSE));

            switch (newFieldClassification)
            {
            case SystemVClassificationTypeInteger:
                // Integer overrides everything; the resulting classification is Integer. Can't merge Integer and IntegerReference.
                Debug.Assert((originalClassification == SystemVClassificationTypeInteger) ||
                                (originalClassification == SystemVClassificationTypeSSE));

                return SystemVClassificationTypeInteger;

            case SystemVClassificationTypeSSE:
                // If the old and new classifications are both SSE, then the merge is SSE, otherwise it will be integer. Can't merge SSE and IntegerReference.
                Debug.Assert((originalClassification == SystemVClassificationTypeInteger) ||
                                (originalClassification == SystemVClassificationTypeSSE));

                if (originalClassification == SystemVClassificationTypeSSE)
                {
                    return SystemVClassificationTypeSSE;
                }
                else
                {
                    return SystemVClassificationTypeInteger;
                }

            case SystemVClassificationTypeIntegerReference:
                // IntegerReference can only merge with IntegerReference.
                Debug.Assert(originalClassification == SystemVClassificationTypeIntegerReference);
                return SystemVClassificationTypeIntegerReference;

            case SystemVClassificationTypeIntegerByRef:
                // IntegerByReference can only merge with IntegerByReference.
                Debug.Assert(originalClassification == SystemVClassificationTypeIntegerByRef);
                return SystemVClassificationTypeIntegerByRef;

            default:
                Debug.Assert(false); // Unexpected type.
                return SystemVClassificationTypeUnknown;
            }
        }

        /// <summary>
        /// Returns 'true' if the struct is passed in registers, 'false' otherwise.
        /// </summary>
        private static bool ClassifyEightBytes(TypeDesc typeDesc,
                                               ref SystemVStructRegisterPassingHelper helper,
                                               int startOffsetOfStruct)
        {
            FieldDesc firstField = null;
            int numIntroducedFields = 0;
            foreach (FieldDesc field in typeDesc.GetFields())
            {
                if (!field.IsStatic)
                {
                    firstField ??= field;
                    numIntroducedFields++;
                }
            }

            if (numIntroducedFields == 0)
            {
                return false;
            }

            // The SIMD and Int128 Intrinsic types are meant to be handled specially and should not be passed as struct registers
            if (typeDesc.IsIntrinsic)
            {
                InstantiatedType instantiatedType = typeDesc as InstantiatedType;
                if (instantiatedType != null)
                {
                    if (VectorFieldLayoutAlgorithm.IsVectorType(instantiatedType) ||
                        VectorOfTFieldLayoutAlgorithm.IsVectorOfTType(instantiatedType) ||
                        Int128FieldLayoutAlgorithm.IsIntegerType(instantiatedType))
                    {
                        return false;
                    }
                }
            }

            MetadataType mdType = typeDesc as MetadataType;
            Debug.Assert(mdType != null);

            TypeDesc firstFieldElementType = firstField.FieldType;
            int firstFieldSize = firstFieldElementType.GetElementSize().AsInt;

            // A fixed buffer type is always a value type that has exactly one value type field at offset 0
            // and who's size is an exact multiple of the size of the field.
            // It is possible that we catch a false positive with this check, but that chance is extremely slim
            // and the user can always change their structure to something more descriptive of what they want
            // instead of adding additional padding at the end of a one-field structure.
            // We do this check here to save looking up the FixedBufferAttribute when loading the field
            // from metadata.
            bool isFixedBuffer = numIntroducedFields == 1
                                    && firstFieldElementType.IsValueType
                                    && firstField.Offset.AsInt == 0
                                    && mdType.HasLayout()
                                    && ((typeDesc.GetElementSize().AsInt % firstFieldSize) == 0);

            if (isFixedBuffer)
            {
                numIntroducedFields = typeDesc.GetElementSize().AsInt / firstFieldSize;
            }

            int fieldIndex = 0;
            foreach (FieldDesc field in FieldEnumerator.GetInstanceFields(typeDesc, isFixedBuffer, numIntroducedFields))
            {
                Debug.Assert(fieldIndex < numIntroducedFields);

                int fieldOffset = isFixedBuffer ? fieldIndex * firstFieldSize : field.Offset.AsInt;
                int normalizedFieldOffset = fieldOffset + startOffsetOfStruct;

                int fieldSize = field.FieldType.GetElementSize().AsInt;

                // The field can't span past the end of the struct.
                if ((normalizedFieldOffset + fieldSize) > helper.StructSize)
                {
                    Debug.Assert(false, "Invalid struct size. The size of fields and overall size don't agree");
                    return false;
                }

                SystemVClassificationType fieldClassificationType = TypeDef2SystemVClassification(field.FieldType);
                if (fieldClassificationType == SystemVClassificationTypeStruct)
                {
                    bool inEmbeddedStructPrev = helper.InEmbeddedStruct;
                    helper.InEmbeddedStruct = true;

                    bool structRet = false;
                    structRet = ClassifyEightBytes(field.FieldType, ref helper, normalizedFieldOffset);

                    helper.InEmbeddedStruct = inEmbeddedStructPrev;

                    if (!structRet)
                    {
                        // If the nested struct says not to enregister, there's no need to continue analyzing at this level. Just return do not enregister.
                        return false;
                    }

                    continue;
                }

                if ((normalizedFieldOffset % fieldSize) != 0)
                {
                    // The spec requires that struct values on the stack from register passed fields expects
                    // those fields to be at their natural alignment.
                    return false;
                }

                if (normalizedFieldOffset <= helper.LargestFieldOffset)
                {
                    // Find the field corresponding to this offset and update the size if needed.
                    // If the offset matches a previously encountered offset, update the classification and field size.
                    int i;
                    for (i = helper.CurrentUniqueOffsetField - 1; i >= 0; i--)
                    {
                        if (helper.FieldOffsets[i] == normalizedFieldOffset)
                        {
                            if (fieldSize > helper.FieldSizes[i])
                            {
                                helper.FieldSizes[i] = fieldSize;
                            }

                            helper.FieldClassifications[i] = ReClassifyField(helper.FieldClassifications[i], fieldClassificationType);

                            break;
                        }
                    }

                    if (i >= 0)
                    {
                        // The proper size of the union set of fields has been set above; continue to the next field.
                        continue;
                    }
                }
                else
                {
                    helper.LargestFieldOffset = (int)normalizedFieldOffset;
                }

                // Set the data for a new field.

                // The new field classification must not have been initialized yet.
                Debug.Assert(helper.FieldClassifications[helper.CurrentUniqueOffsetField] == SystemVClassificationTypeNoClass);

                // There are only a few field classifications that are allowed.
                Debug.Assert((fieldClassificationType == SystemVClassificationTypeInteger) ||
                             (fieldClassificationType == SystemVClassificationTypeIntegerReference) ||
                             (fieldClassificationType == SystemVClassificationTypeIntegerByRef) ||
                             (fieldClassificationType == SystemVClassificationTypeSSE));

                helper.FieldClassifications[helper.CurrentUniqueOffsetField] = fieldClassificationType;
                helper.FieldSizes[helper.CurrentUniqueOffsetField] = fieldSize;
                helper.FieldOffsets[helper.CurrentUniqueOffsetField] = normalizedFieldOffset;

                Debug.Assert(helper.CurrentUniqueOffsetField < SYSTEMV_MAX_NUM_FIELDS_IN_REGISTER_PASSED_STRUCT);
                helper.CurrentUniqueOffsetField++;

                fieldIndex++;
            }

            AssignClassifiedEightByteTypes(ref helper);

            return true;
        }

        // Assigns the classification types to the array with eightbyte types.
        private static void AssignClassifiedEightByteTypes(ref SystemVStructRegisterPassingHelper helper)
        {
            const int CLR_SYSTEMV_MAX_BYTES_TO_PASS_IN_REGISTERS = CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS * SYSTEMV_EIGHT_BYTE_SIZE_IN_BYTES;
            Debug.Assert(CLR_SYSTEMV_MAX_BYTES_TO_PASS_IN_REGISTERS == SYSTEMV_MAX_NUM_FIELDS_IN_REGISTER_PASSED_STRUCT);

            if (!helper.InEmbeddedStruct)
            {
                int largestFieldOffset = helper.LargestFieldOffset;
                Debug.Assert(largestFieldOffset != -1);

                // We're at the top level of the recursion, and we're done looking at the fields.
                // Now sort the fields by offset and set the output data.

                int[] sortedFieldOrder = new int[CLR_SYSTEMV_MAX_BYTES_TO_PASS_IN_REGISTERS];
                for (int i = 0; i < CLR_SYSTEMV_MAX_BYTES_TO_PASS_IN_REGISTERS; i++)
                {
                    sortedFieldOrder[i] = -1;
                }

                int numFields = helper.CurrentUniqueOffsetField;
                for (int i = 0; i < numFields; i++)
                {
                    Debug.Assert(helper.FieldOffsets[i] < CLR_SYSTEMV_MAX_BYTES_TO_PASS_IN_REGISTERS);
                    Debug.Assert(sortedFieldOrder[helper.FieldOffsets[i]] == -1); // we haven't seen this field offset yet.
                    sortedFieldOrder[helper.FieldOffsets[i]] = i;
                }

                // Calculate the eightbytes and their types.

                int lastFieldOrdinal = sortedFieldOrder[largestFieldOffset];
                int offsetAfterLastFieldByte = largestFieldOffset + helper.FieldSizes[lastFieldOrdinal];
                SystemVClassificationType lastFieldClassification = helper.FieldClassifications[lastFieldOrdinal];

                int usedEightBytes = 0;
                int accumulatedSizeForEightBytes = 0;
                bool foundFieldInEightByte = false;
                for (int offset = 0; offset < helper.StructSize; offset++)
                {
                    SystemVClassificationType fieldClassificationType;
                    int fieldSize = 0;

                    int ordinal = sortedFieldOrder[offset];
                    if (ordinal == -1)
                    {
                        if (offset < accumulatedSizeForEightBytes)
                        {
                            // We're within a field and there is not an overlapping field that starts here.
                            // There's no work we need to do, so go to the next loop iteration.
                            continue;
                        }

                        // If there is no field that starts as this offset and we are not within another field,
                        // treat its contents as padding.
                        // Any padding that follows the last field receives the same classification as the
                        // last field; padding between fields receives the NO_CLASS classification as per
                        // the SysV ABI spec.
                        fieldSize = 1;
                        fieldClassificationType = offset < offsetAfterLastFieldByte ? SystemVClassificationTypeNoClass : lastFieldClassification;
                    }
                    else
                    {
                        foundFieldInEightByte = true;
                        fieldSize = helper.FieldSizes[ordinal];
                        Debug.Assert(fieldSize > 0);

                        fieldClassificationType = helper.FieldClassifications[ordinal];
                        Debug.Assert(fieldClassificationType != SystemVClassificationTypeMemory && fieldClassificationType != SystemVClassificationTypeUnknown);
                    }

                    int fieldStartEightByte = offset / SYSTEMV_EIGHT_BYTE_SIZE_IN_BYTES;
                    int fieldEndEightByte = (offset + fieldSize - 1) / SYSTEMV_EIGHT_BYTE_SIZE_IN_BYTES;

                    Debug.Assert(fieldEndEightByte < CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS);

                    usedEightBytes = Math.Max(usedEightBytes, fieldEndEightByte + 1);

                    for (int currentFieldEightByte = fieldStartEightByte; currentFieldEightByte <= fieldEndEightByte; currentFieldEightByte++)
                    {
                        if (helper.EightByteClassifications[currentFieldEightByte] == fieldClassificationType)
                        {
                            // Do nothing. The eight-byte already has this classification.
                        }
                        else if (helper.EightByteClassifications[currentFieldEightByte] == SystemVClassificationTypeNoClass)
                        {
                            helper.EightByteClassifications[currentFieldEightByte] = fieldClassificationType;
                        }
                        else if ((helper.EightByteClassifications[currentFieldEightByte] == SystemVClassificationTypeInteger) ||
                            (fieldClassificationType == SystemVClassificationTypeInteger))
                        {
                            Debug.Assert((fieldClassificationType != SystemVClassificationTypeIntegerReference) &&
                                            (fieldClassificationType != SystemVClassificationTypeIntegerByRef));

                            helper.EightByteClassifications[currentFieldEightByte] = SystemVClassificationTypeInteger;
                        }
                        else if ((helper.EightByteClassifications[currentFieldEightByte] == SystemVClassificationTypeIntegerReference) ||
                            (fieldClassificationType == SystemVClassificationTypeIntegerReference))
                        {
                            helper.EightByteClassifications[currentFieldEightByte] = SystemVClassificationTypeIntegerReference;
                        }
                        else if ((helper.EightByteClassifications[currentFieldEightByte] == SystemVClassificationTypeIntegerByRef) ||
                            (fieldClassificationType == SystemVClassificationTypeIntegerByRef))
                        {
                            helper.EightByteClassifications[currentFieldEightByte] = SystemVClassificationTypeIntegerByRef;
                        }
                        else
                        {
                            helper.EightByteClassifications[currentFieldEightByte] = SystemVClassificationTypeSSE;
                        }
                    }

                    if ((offset + 1) % SYSTEMV_EIGHT_BYTE_SIZE_IN_BYTES == 0) // If we just finished checking the last byte of an eightbyte
                    {
                        if (!foundFieldInEightByte)
                        {
                            // If we didn't find a field in an eight-byte (i.e. there are no explicit offsets that start a field in this eightbyte)
                            // then the classification of this eightbyte might be NoClass. We can't hand a classification of NoClass to the JIT
                            // so set the class to Integer (as though the struct has a char[8] padding) if the class is NoClass.
                            if (helper.EightByteClassifications[offset / SYSTEMV_EIGHT_BYTE_SIZE_IN_BYTES] == SystemVClassificationTypeNoClass)
                            {
                                helper.EightByteClassifications[offset / SYSTEMV_EIGHT_BYTE_SIZE_IN_BYTES] = SystemVClassificationTypeInteger;
                            }
                        }

                        foundFieldInEightByte = false;
                    }

                    accumulatedSizeForEightBytes = Math.Max(accumulatedSizeForEightBytes, offset + fieldSize);
                }

                for (int currentEightByte = 0; currentEightByte < usedEightBytes; currentEightByte++)
                {
                    int eightByteSize = accumulatedSizeForEightBytes < (SYSTEMV_EIGHT_BYTE_SIZE_IN_BYTES * (currentEightByte + 1))
                        ? accumulatedSizeForEightBytes % SYSTEMV_EIGHT_BYTE_SIZE_IN_BYTES
                        :   SYSTEMV_EIGHT_BYTE_SIZE_IN_BYTES;

                    // Save data for this eightbyte.
                    helper.EightByteSizes[currentEightByte] = eightByteSize;
                    helper.EightByteOffsets[currentEightByte] = currentEightByte * SYSTEMV_EIGHT_BYTE_SIZE_IN_BYTES;
                }

                helper.EightByteCount = usedEightBytes;

                Debug.Assert(helper.EightByteCount <= CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS);

#if DEBUG
                for (int i = 0; i < helper.EightByteCount; i++)
                {
                    Debug.Assert(helper.EightByteClassifications[i] != SystemVClassificationTypeNoClass);
                }
#endif // DEBUG
            }
        }
    }
}
