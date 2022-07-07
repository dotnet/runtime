// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SharedTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

using Xunit;

namespace LibraryImportGenerator.IntegrationTests
{
    partial class NativeExportsNE
    {
        public partial class Arrays
        {
            // TODO: All these tests can be removed once we switch the array marshaller in runtime libraries
            // to V2 of custom type marshalling shapes
            public partial class Custom
            {
                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "sum_int_array")]
                public static partial int Sum([MarshalUsing(typeof(CustomArrayMarshaller<,>))] int[] values, int numValues);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "sum_int_array_ref")]
                public static partial int SumInArray([MarshalUsing(typeof(CustomArrayMarshaller<,>))] in int[] values, int numValues);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "duplicate_int_array")]
                public static partial void Duplicate([MarshalUsing(typeof(CustomArrayMarshaller<,>), CountElementName = "numValues")] ref int[] values, int numValues);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "create_range_array")]
                [return: MarshalUsing(typeof(CustomArrayMarshaller<,>), CountElementName = "numValues")]
                public static partial int[] CreateRange(int start, int end, out int numValues);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "create_range_array_out")]
                public static partial void CreateRange_Out(int start, int end, out int numValues, [MarshalUsing(typeof(CustomArrayMarshaller<,>), CountElementName = "numValues")] out int[] res);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "sum_char_array", StringMarshalling = StringMarshalling.Utf16)]
                public static partial int SumChars([MarshalUsing(typeof(CustomArrayMarshaller<,>))] char[] chars, int numElements);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "reverse_char_array", StringMarshalling = StringMarshalling.Utf16)]
                public static partial void ReverseChars([MarshalUsing(typeof(CustomArrayMarshaller<,>), CountElementName = "numElements")] ref char[] chars, int numElements);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "get_long_bytes")]
                [return: MarshalUsing(typeof(CustomArrayMarshaller<,>), ConstantElementCount = sizeof(long))]
                public static partial byte[] GetLongBytes(long l);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "and_bool_struct_array")]
                [return: MarshalAs(UnmanagedType.U1)]
                public static partial bool AndAllMembers([MarshalUsing(typeof(CustomArrayMarshaller<,>))] BoolStruct[] pArray, int length);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "and_bool_struct_array_in")]
                [return: MarshalAs(UnmanagedType.U1)]
                public static partial bool AndAllMembersIn([MarshalUsing(typeof(CustomArrayMarshaller<,>))] in BoolStruct[] pArray, int length);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "negate_bool_struct_array_ref")]
                public static partial void NegateBools(
                    [MarshalUsing(typeof(CustomArrayMarshaller<,>), CountElementName = "numValues")] ref BoolStruct[] boolStruct,
                    int numValues);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "negate_bool_struct_array_out")]
                public static partial void NegateBools(
                    [MarshalUsing(typeof(CustomArrayMarshaller<,>))] BoolStruct[] boolStruct,
                    int numValues,
                    [MarshalUsing(typeof(CustomArrayMarshaller<,>), CountElementName = "numValues")] out BoolStruct[] pBoolStructOut);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "negate_bool_struct_array_return")]
                [return: MarshalUsing(typeof(CustomArrayMarshaller<,>), CountElementName = "numValues")]
                public static partial BoolStruct[] NegateBools(
                    [MarshalUsing(typeof(CustomArrayMarshaller<,>))] BoolStruct[] boolStruct,
                    int numValues);
            }
        }
    }

    public class ArrayTests_Custom
    {
        private int[] GetIntArray() => new[] { 1, 5, 79, 165, 32, 3 };

        [Fact]
        public void IntArray_ByValue()
        {
            int[] array = GetIntArray();
            Assert.Equal(array.Sum(), NativeExportsNE.Arrays.Custom.Sum(array, array.Length));
        }

        [Fact]
        public void NullIntArray_ByValue()
        {
            int[] array = null;
            Assert.Equal(-1, NativeExportsNE.Arrays.Custom.Sum(array, 0));
        }

        [Fact]
        public void ZeroLengthArray_MarshalledAsNonNull()
        {
            var array = new int[0];
            Assert.Equal(0, NativeExportsNE.Arrays.Custom.Sum(array, array.Length));
        }

        [Fact]
        public void IntArray_In()
        {
            int[] array = GetIntArray();
            Assert.Equal(array.Sum(), NativeExportsNE.Arrays.Custom.SumInArray(array, array.Length));
        }

        [Fact]
        public void IntArray_Ref()
        {
            int[] array = GetIntArray();
            var newArray = array;
            NativeExportsNE.Arrays.Custom.Duplicate(ref newArray, array.Length);
            Assert.Equal((IEnumerable<int>)array, newArray);
        }

        [Fact]
        public void CharArray_ByValue()
        {
            char[] array = CharacterTests.CharacterMappings().Select(o => (char)o[0]).ToArray();
            Assert.Equal(array.Sum(c => c), NativeExportsNE.Arrays.Custom.SumChars(array, array.Length));
        }

        [Fact]
        public void CharArray_Ref()
        {
            char[] array = CharacterTests.CharacterMappings().Select(o => (char)o[0]).ToArray();
            var newArray = array;
            NativeExportsNE.Arrays.Custom.ReverseChars(ref newArray, array.Length);
            Assert.Equal(array.Reverse(), newArray);
        }

        [Fact]
        public void IntArray_Return()
        {
            int start = 5;
            int end = 20;

            IEnumerable<int> expected = Enumerable.Range(start, end - start);
            Assert.Equal(expected, NativeExportsNE.Arrays.Custom.CreateRange(start, end, out _));

            int[] res;
            NativeExportsNE.Arrays.Custom.CreateRange_Out(start, end, out _, out res);
            Assert.Equal(expected, res);
        }

        [Fact]
        public void NullArray_Return()
        {
            Assert.Null(NativeExportsNE.Arrays.Custom.CreateRange(1, 0, out _));

            int[] res;
            NativeExportsNE.Arrays.Custom.CreateRange_Out(1, 0, out _, out res);
            Assert.Null(res);
        }

        [Fact]
        public void ConstantSizeArray()
        {
            var longVal = 0x12345678ABCDEF10L;

            Assert.Equal(longVal, MemoryMarshal.Read<long>(NativeExportsNE.Arrays.Custom.GetLongBytes(longVal)));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void NonBlittableElementArray_ByValue(bool result)
        {
            BoolStruct[] array = GetBoolStructsToAnd(result);
            Assert.Equal(result, NativeExportsNE.Arrays.Custom.AndAllMembers(array, array.Length));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void NonBlittableElementArray_In(bool result)
        {
            BoolStruct[] array = GetBoolStructsToAnd(result);
            Assert.Equal(result, NativeExportsNE.Arrays.Custom.AndAllMembersIn(array, array.Length));
        }

        [Fact]
        public void NonBlittableElementArray_Ref()
        {
            BoolStruct[] array = GetBoolStructsToNegate();
            BoolStruct[] expected = GetNegatedBoolStructs(array);

            NativeExportsNE.Arrays.Custom.NegateBools(ref array, array.Length);
            Assert.Equal(expected, array);
        }

        [Fact]
        public void NonBlittableElementArray_Out()
        {
            BoolStruct[] array = GetBoolStructsToNegate();
            BoolStruct[] expected = GetNegatedBoolStructs(array);

            BoolStruct[] result;
            NativeExportsNE.Arrays.Custom.NegateBools(array, array.Length, out result);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void NonBlittableElementArray_Return()
        {
            BoolStruct[] array = GetBoolStructsToNegate();
            BoolStruct[] expected = GetNegatedBoolStructs(array);

            BoolStruct[] result = NativeExportsNE.Arrays.Custom.NegateBools(array, array.Length);
            Assert.Equal(expected, result);
        }

        private static BoolStruct[] GetBoolStructsToAnd(bool result) => new BoolStruct[]
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

        private static BoolStruct[] GetBoolStructsToNegate() => new BoolStruct[]
            {
                new BoolStruct
                {
                    b1 = true,
                    b2 = false,
                    b3 = true
                },
                new BoolStruct
                {
                    b1 = false,
                    b2 = true,
                    b3 = false
                },
                new BoolStruct
                {
                    b1 = true,
                    b2 = true,
                    b3 = true
                },
                new BoolStruct
                {
                    b1 = false,
                    b2 = false,
                    b3 = false
                }
            };

        private static BoolStruct[] GetNegatedBoolStructs(BoolStruct[] toNegate)
            => toNegate.Select(b => new BoolStruct() { b1 = !b.b1, b2 = !b.b2, b3 = !b.b3 }).ToArray();

    }
}
