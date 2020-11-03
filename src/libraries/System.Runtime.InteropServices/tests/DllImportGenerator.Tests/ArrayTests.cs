using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Xunit;

namespace DllImportGenerator.IntegrationTests
{
    partial class NativeExportsNE
    {
        public partial class Arrays
        {
            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "sum_int_array")]
            public static partial int Sum(int[] values, int numValues);

            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "sum_int_array_ref")]
            public static partial int SumInArray(in int[] values, int numValues);

            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "duplicate_int_array")]
            public static partial void Duplicate([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref int[] values, int numValues);

            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "create_range_array")]
            [return:MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
            public static partial int[] CreateRange(int start, int end, out int numValues);

            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "sum_string_lengths")]
            public static partial int SumStringLengths([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)] string[] strArray);

            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "reverse_strings")]
            public static partial void ReverseStrings([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] ref string[] strArray, out int numElements);

            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "get_long_bytes")]
            [return:MarshalAs(UnmanagedType.LPArray, SizeConst = sizeof(long))]
            public static partial byte[] GetLongBytes(long l);

            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "append_int_to_array")]
            public static partial void Append([MarshalAs(UnmanagedType.LPArray, SizeConst = 1, SizeParamIndex = 1)] ref int[] values, int numOriginalValues, int newValue);
        }
    }

    public class ArrayTests
    {
        [Fact]
        public void IntArrayMarshalledToNativeAsExpected()
        {
            var array = new [] { 1, 5, 79, 165, 32, 3 };
            Assert.Equal(array.Sum(), NativeExportsNE.Arrays.Sum(array, array.Length));
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
        public void ArraysReturnedFromNative()
        {
            int start = 5;

            int end = 20;

            Assert.Equal(Enumerable.Range(start, end - start), NativeExportsNE.Arrays.CreateRange(start, end, out _));
        }

        [Fact]
        public void NullArrayReturnedFromNative()
        {
            Assert.Null(NativeExportsNE.Arrays.CreateRange(1, 0, out _));
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
            NativeExportsNE.Arrays.ReverseStrings(ref strings, out _);
            
            Assert.Equal((IEnumerable<string>)expectedStrings, strings);
        }

        [Fact]
        public void ByRefNullArrayWithElementMarshalling()
        {
            string[] strings = null;
            NativeExportsNE.Arrays.ReverseStrings(ref strings, out _);
            
            Assert.Null(strings);
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
