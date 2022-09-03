// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SharedTypes;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static SharedTypes.BoolStructMarshaller;

namespace NativeExports
{
    public static unsafe class Arrays
    {
        [UnmanagedCallersOnly(EntryPoint = "sum_int_array")]
        public static int Sum(int* values, int numValues)
        {
            if (values == null)
            {
                return -1;
            }

            int sum = 0;
            for (int i = 0; i < numValues; i++)
            {
                sum += values[i];
            }
            return sum;
        }

        [UnmanagedCallersOnly(EntryPoint = "sum_int_array_ref")]
        public static int SumInArray(int** values, int numValues)
        {
            if (*values == null)
            {
                return -1;
            }

            int sum = 0;
            for (int i = 0; i < numValues; i++)
            {
                sum += (*values)[i];
            }
            return sum;
        }

        [UnmanagedCallersOnly(EntryPoint = "duplicate_int_array")]
        public static void Duplicate(int** values, int numValues)
        {
            int* newArray = (int*)Marshal.AllocCoTaskMem(sizeof(int) * numValues);
            new Span<int>(*values, numValues).CopyTo(new Span<int>(newArray, numValues));
            Marshal.FreeCoTaskMem((IntPtr)(*values));
            *values = newArray;
        }

        [UnmanagedCallersOnly(EntryPoint = "create_range_array")]
        public static int* CreateRange(int start, int end, int* numValues)
        {
            return CreateRangeImpl(start, end, numValues);
        }

        [UnmanagedCallersOnly(EntryPoint = "create_range_array_out")]
        public static void CreateRangeAsOut(int start, int end, int* numValues, int** res)
        {
            *res = CreateRangeImpl(start, end, numValues);
        }

        [UnmanagedCallersOnly(EntryPoint = "sum_int_ptr_array")]
        public static int SumPointers(int** values, int numValues)
        {
            if (values == null)
                return -1;

            int sum = 0;
            for (int i = 0; i < numValues; i++)
            {
                sum += *values[i];
            }
            return sum;
        }

        [UnmanagedCallersOnly(EntryPoint = "sum_int_ptr_array_ref")]
        public static int SumInPointers(int*** values, int numValues)
        {
            if (*values == null)
                return -1;

            int sum = 0;
            for (int i = 0; i < numValues; i++)
            {
                sum += *(*values)[i];
            }
            return sum;
        }

        [UnmanagedCallersOnly(EntryPoint = "duplicate_int_ptr_array")]
        public static void DuplicatePointers(int*** values, int numValues)
        {
            int sizeInBytes = sizeof(int*) * numValues;
            int** newArray = (int**)Marshal.AllocCoTaskMem(sizeInBytes);
            new Span<byte>(*values, sizeInBytes).CopyTo(new Span<byte>(newArray, sizeInBytes));
            Marshal.FreeCoTaskMem((IntPtr)(*values));
            *values = newArray;
        }

        [UnmanagedCallersOnly(EntryPoint = "return_duplicate_int_ptr_array")]
        public static int** ReturnDuplicatePointers(int** values, int numValues)
        {
            int sizeInBytes = sizeof(int*) * numValues;
            int** newArray = (int**)Marshal.AllocCoTaskMem(sizeInBytes);
            new Span<byte>(values, sizeInBytes).CopyTo(new Span<byte>(newArray, sizeInBytes));
            return newArray;
        }

        [UnmanagedCallersOnly(EntryPoint = "fill_range_array")]
        [DNNE.C99DeclCode("struct int_struct_wrapper;")]
        public static byte FillRange([DNNE.C99Type("struct int_struct_wrapper*")] IntStructWrapperNative* numValues, int length, int start)
        {
            if (numValues == null)
            {
                return 0;
            }

            for (int i = 0; i < length; i++, start++)
            {
                numValues[i] = new IntStructWrapperNative { value = start };
            }

            return 1;
        }

        [UnmanagedCallersOnly(EntryPoint = "double_values")]
        [DNNE.C99DeclCode("struct int_struct_wrapper { int value; };")]
        public static void DoubleValues([DNNE.C99Type("struct int_struct_wrapper*")] IntStructWrapperNative* numValues, int length)
        {
            for (int i = 0; i < length; i++)
            {
                numValues[i].value *= 2;
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "sum_char_array")]
        public static int SumChars(ushort* values, int numValues)
        {
            if (values == null)
            {
                return -1;
            }

            int sum = 0;
            for (int i = 0; i < numValues; i++)
            {
                sum += values[i];
            }
            return sum;
        }

        [UnmanagedCallersOnly(EntryPoint = "fill_char_array")]
        public static void FillChars(ushort* values, int length, ushort start)
        {
            if (values == null)
            {
                return;
            }

            ushort val = start;
            for (int i = 0; i < length; i++)
            {
                values[i] = val++;
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "reverse_char_array")]
        public static void ReverseChars(ushort** values, int numValues)
        {
            if (*values == null)
            {
                return;
            }

            var span = new Span<ushort>(*values, numValues);
            span.Reverse();
        }

        [UnmanagedCallersOnly(EntryPoint = "sum_string_lengths")]
        public static int SumStringLengths(ushort** strArray)
        {
            if (strArray == null)
            {
                return 0;
            }
            int length = 0;
            for (int i = 0; (nint)strArray[i] != 0; i++)
            {
                length += new string((char*)strArray[i]).Length;
            }
            return length;
        }

        [UnmanagedCallersOnly(EntryPoint = "reverse_strings_return")]
        public static ushort** ReverseStrings(ushort** strArray, int* numValues)
        {
            return ReverseStringsImpl(strArray, numValues);
        }

        [UnmanagedCallersOnly(EntryPoint = "reverse_strings_out")]
        public static void ReverseStringsAsOut(ushort** strArray, int* numValues, ushort*** res)
        {
            *res = ReverseStringsImpl(strArray, numValues);
        }

        [UnmanagedCallersOnly(EntryPoint = "reverse_strings_replace")]
        public static void ReverseStringsReplace(ushort*** strArray, int* numValues)
        {
            ushort** res = ReverseStringsImpl(*strArray, numValues);
            Marshal.FreeCoTaskMem((IntPtr)(*strArray));
            *strArray = res;
        }

        [UnmanagedCallersOnly(EntryPoint = "get_long_bytes")]
        public static byte* GetLongBytes(long l)
        {
            const int NumBytesInLong = sizeof(long);

            byte* bytes = (byte*)Marshal.AllocCoTaskMem(NumBytesInLong);
            MemoryMarshal.Write(new Span<byte>(bytes, NumBytesInLong), ref l);
            return bytes;
        }

        [UnmanagedCallersOnly(EntryPoint = "append_int_to_array")]
        public static void Append(int** values, int numOriginalValues, int newValue)
        {
            int* newArray = (int*)Marshal.AllocCoTaskMem(sizeof(int) * (numOriginalValues + 1));
            new Span<int>(*values, numOriginalValues).CopyTo(new Span<int>(newArray, numOriginalValues));
            newArray[numOriginalValues] = newValue;
            *values = newArray;
        }

        [UnmanagedCallersOnly(EntryPoint = "and_bool_struct_array")]
        [DNNE.C99DeclCode("struct bool_struct;")]
        public static byte AndBoolStructs([DNNE.C99Type("struct bool_struct*")] BoolStructMarshaller.BoolStructNative* pArray, int length)
        {
            bool result = true;
            for (int i = 0; i < length; i++)
            {
                BoolStruct managed = BoolStructMarshaller.ConvertToManaged(pArray[i]);
                result &= managed.b1 && managed.b2 && managed.b3;
            }
            return (byte)(result ? 1 : 0);
        }

        [UnmanagedCallersOnly(EntryPoint = "and_bool_struct_array_in")]
        [DNNE.C99DeclCode("struct bool_struct;")]
        public static byte AndBoolStructsIn([DNNE.C99Type("struct bool_struct**")] BoolStructMarshaller.BoolStructNative** pArray, int length)
        {
            bool result = true;
            for (int i = 0; i < length; i++)
            {
                BoolStruct managed = BoolStructMarshaller.ConvertToManaged((*pArray)[i]);
                result &= managed.b1 && managed.b2 && managed.b3;
            }
            return (byte)(result ? 1 : 0);
        }

        [UnmanagedCallersOnly(EntryPoint = "negate_bool_struct_array")]
        [DNNE.C99DeclCode("struct bool_struct;")]
        public static void NegateBoolStructs(
            [DNNE.C99Type("struct bool_struct*")] BoolStructMarshaller.BoolStructNative* array,
            int length)
        {
            for (int i = 0; i < length; i++)
            {
                BoolStructMarshaller.BoolStructNative boolStruct = array[i];
                array[i].b1 = (byte)(boolStruct.b1 != 0 ? 0 : 1);
                array[i].b2 = (byte)(boolStruct.b2 != 0 ? 0 : 1);
                array[i].b3 = (byte)(boolStruct.b3 != 0 ? 0 : 1);
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "negate_bool_struct_array_ref")]
        [DNNE.C99DeclCode("struct bool_struct;")]
        public static void NegateBoolStructsRef(
            [DNNE.C99Type("struct bool_struct**")] BoolStructMarshaller.BoolStructNative** array,
            int length)
        {
            for (int i = 0; i < length; i++)
            {
                BoolStructMarshaller.BoolStructNative boolStruct = (*array)[i];
                (*array)[i].b1 = (byte)(boolStruct.b1 != 0 ? 0 : 1);
                (*array)[i].b2 = (byte)(boolStruct.b2 != 0 ? 0 : 1);
                (*array)[i].b3 = (byte)(boolStruct.b3 != 0 ? 0 : 1);
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "negate_bool_struct_array_out")]
        [DNNE.C99DeclCode("struct bool_struct;")]
        public static void NegateBoolStructsOut(
            [DNNE.C99Type("struct bool_struct*")] BoolStructMarshaller.BoolStructNative* array,
            int length,
            [DNNE.C99Type("struct bool_struct**")] BoolStructMarshaller.BoolStructNative** outArray)
        {
            *outArray = NegateBoolStructsImpl(array, length);
        }

        [UnmanagedCallersOnly(EntryPoint = "negate_bool_struct_array_return")]
        [DNNE.C99DeclCode("struct bool_struct;")]
        [return: DNNE.C99Type("struct bool_struct*")]
        public static BoolStructMarshaller.BoolStructNative* NegateBoolStructsReturn(
            [DNNE.C99Type("struct bool_struct*")] BoolStructMarshaller.BoolStructNative* array,
            int length)
        {
            return NegateBoolStructsImpl(array, length);
        }

        [UnmanagedCallersOnly(EntryPoint = "transpose_matrix")]
        public static int** TransposeMatrix(int** matrix, int* numRows, int numColumns)
        {
            int** newRows = (int**)Marshal.AllocCoTaskMem(numColumns * sizeof(int*));
            for (int i = 0; i < numColumns; i++)
            {
                newRows[i] = (int*)Marshal.AllocCoTaskMem(numRows[i] * sizeof(int));
                for (int j = 0; j < numRows[i]; j++)
                {
                    newRows[i][j] = matrix[j][i];
                }
            }

            return newRows;
        }

        private static BoolStructMarshaller.BoolStructNative* NegateBoolStructsImpl(BoolStructMarshaller.BoolStructNative* array, int numValues)
        {
            if (array == null)
                return null;

            BoolStructMarshaller.BoolStructNative* retVal = (BoolStructMarshaller.BoolStructNative*)Marshal.AllocCoTaskMem(sizeof(BoolStructMarshaller.BoolStructNative) * numValues);
            for (int i = 0; i < numValues; i++)
            {
                BoolStructMarshaller.BoolStructNative boolStruct = array[i];
                retVal[i] = new BoolStructMarshaller.BoolStructNative
                {
                    b1 = (byte)(boolStruct.b1 != 0 ? 0 : 1),
                    b2 = (byte)(boolStruct.b2 != 0 ? 0 : 1),
                    b3 = (byte)(boolStruct.b3 != 0 ? 0 : 1)
                };
            }

            return retVal;
        }

        private static int* CreateRangeImpl(int start, int end, int* numValues)
        {
            if (start >= end)
            {
                *numValues = 0;
                return null;
            }

            *numValues = end - start;

            int* retVal = (int*)Marshal.AllocCoTaskMem(sizeof(int) * (*numValues));
            for (int i = start; i < end; i++)
            {
                retVal[i - start] = i;
            }

            return retVal;
        }

        private static ushort** ReverseStringsImpl(ushort** strArray, int* numValues)
        {
            if (strArray == null)
            {
                *numValues = 0;
                return null;
            }

            List<IntPtr> newStrings = new List<IntPtr>();
            for (int i = 0; (nint)strArray[i] != 0; i++)
            {
                newStrings.Add((IntPtr)Strings.Reverse(strArray[i]));
            }
            newStrings.Add(IntPtr.Zero);

            ushort** res = (ushort**)Marshal.AllocCoTaskMem(sizeof(ushort*) * newStrings.Count);
            CollectionsMarshal.AsSpan(newStrings).CopyTo(new Span<IntPtr>((IntPtr*)(res), newStrings.Count));
            *numValues = newStrings.Count;
            return res;
        }
    }
}
