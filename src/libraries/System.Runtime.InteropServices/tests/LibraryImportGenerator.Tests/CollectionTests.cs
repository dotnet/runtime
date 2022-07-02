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
        public partial class Collections
        {
            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "sum_int_array")]
            public static partial int Sum([MarshalUsing(typeof(ListMarshaller<,>))] List<int> values, int numValues);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "double_values")]
            public static partial int DoubleValues([MarshalUsing(typeof(ListMarshallerWithPinning<,>))] List<BlittableIntWrapper> values, int length);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "sum_int_array_ref")]
            public static partial int SumInArray([MarshalUsing(typeof(ListMarshaller<,>))] in List<int> values, int numValues);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "duplicate_int_array")]
            public static partial void Duplicate([MarshalUsing(typeof(ListMarshaller<,>), CountElementName = "numValues")] ref List<int> values, int numValues);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "create_range_array")]
            [return: MarshalUsing(typeof(ListMarshaller<,>), CountElementName = "numValues")]
            public static partial List<int> CreateRange(int start, int end, out int numValues);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "create_range_array_out")]
            public static partial void CreateRange_Out(int start, int end, out int numValues, [MarshalUsing(typeof(ListMarshaller<,>), CountElementName = "numValues")] out List<int> res);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "get_long_bytes")]
            [return: MarshalUsing(typeof(ListMarshaller<,>), ConstantElementCount = sizeof(long))]
            public static partial List<byte> GetLongBytes(long l);
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
        public void BlittableElementColllectionMarshalledToNativeWithPinningAsExpected()
        {
            var data = new List<int> { 1, 5, 79, 165, 32, 3 };
            var list = data.Select(i => new BlittableIntWrapper { i = i }).ToList();
            NativeExportsNE.Collections.DoubleValues(list, list.Count);
            Assert.Equal(data.Select(i => i * 2), list.Select(wrapper => wrapper.i));
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
    }
}
