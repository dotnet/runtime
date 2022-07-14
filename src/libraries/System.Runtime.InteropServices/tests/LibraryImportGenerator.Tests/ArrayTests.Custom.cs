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
    }
}
