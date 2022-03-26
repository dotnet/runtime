﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SharedTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Xunit;

namespace LibraryImportGenerator.IntegrationTests
{
    partial class NativeExportsNE
    {
        public partial class Arrays
        {
            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "sum_int_array")]
            public static partial int Sum(int[] values, int numValues);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "sum_int_array")]
            public static partial int Sum(ref int values, int numValues);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "sum_int_array_ref")]
            public static partial int SumInArray(in int[] values, int numValues);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "duplicate_int_array")]
            public static partial void Duplicate([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref int[] values, int numValues);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "create_range_array")]
            [return:MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
            public static partial int[] CreateRange(int start, int end, out int numValues);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "create_range_array_out")]
            public static partial void CreateRange_Out(int start, int end, out int numValues, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] out int[] res);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "sum_char_array", StringMarshalling = StringMarshalling.Utf16)]
            public static partial int SumChars(char[] chars, int numElements);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "fill_char_array", StringMarshalling = StringMarshalling.Utf16)]
            public static partial void FillChars([Out] char[] chars, int length, ushort start);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "reverse_char_array", StringMarshalling = StringMarshalling.Utf16)]
            public static partial void ReverseChars([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref char[] chars, int numElements);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "sum_string_lengths")]
            public static partial int SumStringLengths([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)] string[] strArray);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "reverse_strings_replace")]
            public static partial void ReverseStrings_Ref([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] ref string[] strArray, out int numElements);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "reverse_strings_return")]
            [return: MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)]
            public static partial string[] ReverseStrings_Return([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)] string[] strArray, out int numElements);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "reverse_strings_out")]
            public static partial void ReverseStrings_Out([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)] string[] strArray, out int numElements, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] out string[] res);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "get_long_bytes")]
            [return:MarshalAs(UnmanagedType.LPArray, SizeConst = sizeof(long))]
            public static partial byte[] GetLongBytes(long l);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "append_int_to_array")]
            public static partial void Append([MarshalAs(UnmanagedType.LPArray, SizeConst = 1, SizeParamIndex = 1)] ref int[] values, int numOriginalValues, int newValue);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "fill_range_array")]
            [return: MarshalAs(UnmanagedType.U1)]
            public static partial bool FillRangeArray([Out] IntStructWrapper[] array, int length, int start);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "double_values")]
            public static partial void DoubleValues([In, Out] IntStructWrapper[] array, int length);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "and_all_members")]
            [return:MarshalAs(UnmanagedType.U1)]
            public static partial bool AndAllMembers(BoolStruct[] pArray, int length);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "transpose_matrix")]
            [return: MarshalUsing(CountElementName = "numColumns")]
            [return: MarshalUsing(CountElementName = "numRows", ElementIndirectionDepth = 1)]
            public static partial int[][] TransposeMatrix(int[][] matrix, int[] numRows, int numColumns);
        }
    }

    public class ArrayTests
    {
        [Fact]
        public void IntArrayMarshalledToNativeAsExpected()
        {
            var array = new[] { 1, 5, 79, 165, 32, 3 };
            Assert.Equal(array.Sum(), NativeExportsNE.Arrays.Sum(array, array.Length));
        }

        [Fact]
        public void IntArrayRefToFirstElementMarshalledToNativeAsExpected()
        {
            var array = new[] { 1, 5, 79, 165, 32, 3 };
            Assert.Equal(array.Sum(), NativeExportsNE.Arrays.Sum(ref array[0], array.Length));
        }

        [Fact]
        public void NullIntArrayMarshalledToNativeAsExpected()
        {
            Assert.Equal(-1, NativeExportsNE.Arrays.Sum(null, 0));
        }

        [Fact]
        public void ZeroLengthArrayMarshalledAsNonNull()
        {
            var array = new int[0];
            Assert.Equal(0, NativeExportsNE.Arrays.Sum(array, array.Length));
        }

        [Fact]
        public void IntArrayInParameter()
        {
            var array = new[] { 1, 5, 79, 165, 32, 3 };
            Assert.Equal(array.Sum(), NativeExportsNE.Arrays.SumInArray(array, array.Length));
        }

        [Fact]
        public void IntArrayRefParameter()
        {
            var array = new [] { 1, 5, 79, 165, 32, 3 };
            var newArray = array;
            NativeExportsNE.Arrays.Duplicate(ref newArray, array.Length);
            Assert.Equal((IEnumerable<int>)array, newArray);
        }

        [Fact]
        public void CharArrayMarshalledToNativeAsExpected()
        {
            char[] array = CharacterTests.CharacterMappings().Select(o => (char)o[0]).ToArray();
            Assert.Equal(array.Sum(c => c), NativeExportsNE.Arrays.SumChars(array, array.Length));
        }

        [Fact]
        public void CharArrayRefParameter()
        {
            char[] array = CharacterTests.CharacterMappings().Select(o => (char)o[0]).ToArray();
            var newArray = array;
            NativeExportsNE.Arrays.ReverseChars(ref newArray, array.Length);
            Assert.Equal(array.Reverse(), newArray);
        }

        [Fact]
        public void ArraysReturnedFromNative()
        {
            int start = 5;
            int end = 20;

            IEnumerable<int> expected = Enumerable.Range(start, end - start);
            Assert.Equal(expected, NativeExportsNE.Arrays.CreateRange(start, end, out _));

            int[] res;
            NativeExportsNE.Arrays.CreateRange_Out(start, end, out _, out res);
            Assert.Equal(expected, res);
        }

        [Fact]
        public void NullArrayReturnedFromNative()
        {
            Assert.Null(NativeExportsNE.Arrays.CreateRange(1, 0, out _));

            int[] res;
            NativeExportsNE.Arrays.CreateRange_Out(1, 0, out _, out res);
            Assert.Null(res);
        }

        private static string[] GetStringArray()
        {
            return new []
            {
                "ABCdef 123$%^",
                "🍜 !! 🍜 !!",
                "🌲 木 🔥 火 🌾 土 🛡 金 🌊 水" ,
                "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed vitae posuere mauris, sed ultrices leo. Suspendisse potenti. Mauris enim enim, blandit tincidunt consequat in, varius sit amet neque. Morbi eget porttitor ex. Duis mattis aliquet ante quis imperdiet. Duis sit.",
                string.Empty,
                null
            };
        }

        [Fact]
        public void ByValueArrayWithElementMarshalling()
        {
            var strings = GetStringArray();
            Assert.Equal(strings.Sum(str => str?.Length ?? 0), NativeExportsNE.Arrays.SumStringLengths(strings));
        }

        [Fact]
        public void ByValueNullArrayWithElementMarshalling()
        {
            Assert.Equal(0, NativeExportsNE.Arrays.SumStringLengths(null));
        }

        [Fact]
        public void ByRefArrayWithElementMarshalling()
        {
            var strings = GetStringArray();
            var expectedStrings = strings.Select(s => ReverseChars(s)).ToArray();
            NativeExportsNE.Arrays.ReverseStrings_Ref(ref strings, out _);

            Assert.Equal((IEnumerable<string>)expectedStrings, strings);
        }

        [Fact]
        public void ReturnArrayWithElementMarshalling()
        {
            var strings = GetStringArray();
            var expectedStrings = strings.Select(s => ReverseChars(s)).ToArray();
            Assert.Equal(expectedStrings, NativeExportsNE.Arrays.ReverseStrings_Return(strings, out _));

            string[] res;
            NativeExportsNE.Arrays.ReverseStrings_Out(strings, out _, out res);
            Assert.Equal(expectedStrings, res);
        }

        [Fact]
        public void ByRefNullArrayWithElementMarshalling()
        {
            string[] strings = null;
            NativeExportsNE.Arrays.ReverseStrings_Ref(ref strings, out _);

            Assert.Null(strings);
        }

        [Fact]
        public void ReturnNullArrayWithElementMarshalling()
        {
            string[] strings = null;
            Assert.Null(NativeExportsNE.Arrays.ReverseStrings_Return(strings, out _));

            string[] res;
            NativeExportsNE.Arrays.ReverseStrings_Out(strings, out _, out res);
            Assert.Null(res);
        }

        [Fact]
        public void ConstantSizeArray()
        {
            var longVal = 0x12345678ABCDEF10L;

            Assert.Equal(longVal, MemoryMarshal.Read<long>(NativeExportsNE.Arrays.GetLongBytes(longVal)));
        }

        [Fact]
        public void DynamicSizedArrayWithConstantComponent()
        {
            var array = new [] { 1, 5, 79, 165, 32, 3 };
            int newValue = 42;
            var newArray = array;
            NativeExportsNE.Arrays.Append(ref newArray, array.Length, newValue);
            Assert.Equal(array.Concat(new [] { newValue }), newArray);
        }

        [Fact]
        public void ArrayByValueOutParameter()
        {
            {
                var testArray = new IntStructWrapper[10];
                int start = 5;

                NativeExportsNE.Arrays.FillRangeArray(testArray, testArray.Length, start);
                Assert.Equal(Enumerable.Range(start, testArray.Length), testArray.Select(wrapper => wrapper.Value));

                // Any items not populated by the invoke target should be initialized to default
                testArray = new IntStructWrapper[10];
                int lengthToFill = testArray.Length / 2;
                NativeExportsNE.Arrays.FillRangeArray(testArray, lengthToFill, start);
                Assert.Equal(Enumerable.Range(start, lengthToFill), testArray[..lengthToFill].Select(wrapper => wrapper.Value));
                Assert.All(testArray[lengthToFill..], wrapper => Assert.Equal(0, wrapper.Value));
            }
            {
                var testArray = new char[10];
                ushort start = 65;

                NativeExportsNE.Arrays.FillChars(testArray, testArray.Length, start);
                Assert.Equal(Enumerable.Range(start, testArray.Length), testArray.Select(c => (int)c));

                // Any items not populated by the invoke target should be initialized to default
                testArray = new char[10];
                int lengthToFill = testArray.Length / 2;
                NativeExportsNE.Arrays.FillChars(testArray, lengthToFill, start);
                Assert.Equal(Enumerable.Range(start, lengthToFill), testArray[..lengthToFill].Select(c => (int)c));
                Assert.All(testArray[lengthToFill..], c => Assert.Equal(0, c));
            }
        }

        [Fact]
        public void ArrayByValueInOutParameter()
        {
            var testValues = Enumerable.Range(42, 15).Select(i => new IntStructWrapper { Value = i });

            var testArray = testValues.ToArray();

            NativeExportsNE.Arrays.DoubleValues(testArray, testArray.Length);

            Assert.Equal(testValues.Select(wrapper => wrapper.Value * 2), testArray.Select(wrapper => wrapper.Value));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ArrayWithSimpleNonBlittableTypeMarshalling(bool result)
        {
            var boolValues = new[]
            {
                new BoolStruct
                {
                    b1 = true,
                    b2 = true,
                    b3 = true,
                },
                new BoolStruct
                {
                    b1 = true,
                    b2 = true,
                    b3 = true,
                },
                new BoolStruct
                {
                    b1 = true,
                    b2 = true,
                    b3 = result,
                },
            };

            Assert.Equal(result, NativeExportsNE.Arrays.AndAllMembers(boolValues, boolValues.Length));
        }

        [Fact]
        public void ArraysOfArrays()
        {
            var random = new Random(42);
            int numRows = random.Next(1, 5);
            int numColumns = random.Next(1, 5);
            int[][] matrix = new int[numRows][];
            for (int i = 0; i < numRows; i++)
            {
                matrix[i] = new int[numColumns];
                for (int j = 0; j < numColumns; j++)
                {
                    matrix[i][j] = random.Next();
                }
            }

            int[] numRowsArray = new int[numColumns];
            numRowsArray.AsSpan().Fill(numRows);

            int[][] transposed = NativeExportsNE.Arrays.TransposeMatrix(matrix, numRowsArray, numColumns);

            for (int i = 0; i < numRows; i++)
            {
                for (int j = 0; j < numColumns; j++)
                {
                    Assert.Equal(matrix[i][j], transposed[j][i]);
                }
            }
        }

        private static string ReverseChars(string value)
        {
            if (value == null)
                return null;

            var chars = value.ToCharArray();
            Array.Reverse(chars);
            return new string(chars);
        }
    }
}
