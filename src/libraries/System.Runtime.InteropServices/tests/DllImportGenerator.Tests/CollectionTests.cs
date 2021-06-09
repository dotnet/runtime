using SharedTypes;
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
        public partial class Collections
        {
            [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "sum_int_array")]
            public static partial int Sum([MarshalUsing(typeof(ListMarshaller<int>))] List<int> values, int numValues);

            [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "sum_int_array")]
            public static partial int Sum(ref int values, int numValues);

            [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "sum_int_array_ref")]
            public static partial int SumInArray([MarshalUsing(typeof(ListMarshaller<int>))] in List<int> values, int numValues);

            [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "duplicate_int_array")]
            public static partial void Duplicate([MarshalUsing(typeof(ListMarshaller<int>), CountElementName = "numValues")] ref List<int> values, int numValues);

            [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "create_range_array")]
            [return:MarshalUsing(typeof(ListMarshaller<int>), CountElementName = "numValues")]
            public static partial List<int> CreateRange(int start, int end, out int numValues);

            [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "create_range_array_out")]
            public static partial void CreateRange_Out(int start, int end, out int numValues, [MarshalUsing(typeof(ListMarshaller<int>), CountElementName = "numValues")] out List<int> res);

            [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "get_long_bytes")]
            [return:MarshalUsing(typeof(ListMarshaller<byte>), ConstantElementCount = sizeof(long))]
            public static partial List<byte> GetLongBytes(long l);

            [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "and_all_members")]
            [return:MarshalAs(UnmanagedType.U1)]
            public static partial bool AndAllMembers([MarshalUsing(typeof(ListMarshaller<BoolStruct>))] List<BoolStruct> pArray, int length);
        }
    }

    public class CollectionTests
    {
        [Fact]
        public void BlittableElementColllectionMarshalledToNativeAsExpected()
        {
            var list = new List<int> { 1, 5, 79, 165, 32, 3 };
            Assert.Equal(list.Sum(), NativeExportsNE.Collections.Sum(list, list.Count));
        }

        [Fact]
        public void NullBlittableElementColllectionMarshalledToNativeAsExpected()
        {
            Assert.Equal(-1, NativeExportsNE.Collections.Sum(null, 0));
        }

        [Fact]
        public void BlittableElementColllectionInParameter()
        {
            var list = new List<int> { 1, 5, 79, 165, 32, 3 };
            Assert.Equal(list.Sum(), NativeExportsNE.Collections.SumInArray(list, list.Count));
        }

        [Fact]
        public void BlittableElementCollectionRefParameter()
        {
            var list = new List<int> { 1, 5, 79, 165, 32, 3 };
            var newList = list;
            NativeExportsNE.Collections.Duplicate(ref newList, list.Count);
            Assert.Equal((IEnumerable<int>)list, newList);
        }

        [Fact]
        public void BlittableElementCollectionReturnedFromNative()
        {
            int start = 5;
            int end = 20;

            IEnumerable<int> expected = Enumerable.Range(start, end - start);
            Assert.Equal(expected, NativeExportsNE.Collections.CreateRange(start, end, out _));

            List<int> res;
            NativeExportsNE.Collections.CreateRange_Out(start, end, out _, out res);
            Assert.Equal(expected, res);
        }

        [Fact]
        public void NullBlittableElementCollectionReturnedFromNative()
        {
            Assert.Null(NativeExportsNE.Collections.CreateRange(1, 0, out _));

            List<int> res;
            NativeExportsNE.Collections.CreateRange_Out(1, 0, out _, out res);
            Assert.Null(res);
        }

        private static List<string> GetStringList()
        {
            return new()
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
        public void ConstantSizeCollection()
        {
            var longVal = 0x12345678ABCDEF10L;

            Assert.Equal(longVal, MemoryMarshal.Read<long>(CollectionsMarshal.AsSpan(NativeExportsNE.Collections.GetLongBytes(longVal))));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CollectionWithSimpleNonBlittableTypeMarshalling(bool result)
        {
            var boolValues = new List<BoolStruct>
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

            Assert.Equal(result, NativeExportsNE.Collections.AndAllMembers(boolValues, boolValues.Count));
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
