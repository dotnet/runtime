// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SharedTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

using Xunit;

namespace LibraryImportGenerator.IntegrationTests
{
    partial class NativeExportsNE
    {
        public partial class Span
        {
            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "sum_int_array")]
            public static partial int Sum(Span<int> values, int numValues);
            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "sum_int_array")]
            public static partial int Sum(ReadOnlySpan<int> values, int numValues);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "sum_int_array_ref")]
            public static partial int SumInArray(in Span<int> values, int numValues);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "duplicate_int_array")]
            public static partial void Duplicate([MarshalUsing(CountElementName = "numValues")] ref Span<int> values, int numValues);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "create_range_array")]
            [return: MarshalUsing(CountElementName = "numValues")]
            public static partial Span<int> CreateRange(int start, int end, out int numValues);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "create_range_array_out")]
            public static partial void CreateRange_Out(int start, int end, out int numValues, [MarshalUsing(CountElementName = "numValues")] out Span<int> res);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "get_long_bytes")]
            [return: MarshalUsing(ConstantElementCount = sizeof(long))]
            public static partial Span<byte> GetLongBytes(long l);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "and_bool_struct_array")]
            [return: MarshalAs(UnmanagedType.U1)]
            public static partial bool AndAllMembers(Span<BoolStruct> pArray, int length);
        }
    }

    public class SpanTests
    {
        [Fact]
        public void BlittableElementSpanMarshalledToNativeAsExpected()
        {
            var list = new int[] { 1, 5, 79, 165, 32, 3 };
            Assert.Equal(list.Sum(), NativeExportsNE.Span.Sum(list, list.Length));
        }

        [Fact]
        public void DefaultBlittableElementSpanMarshalledToNativeAsExpected()
        {
            Assert.Equal(-1, NativeExportsNE.Span.Sum(default, 0));
        }

        [Fact]
        public void ZeroLengthSpanMarshalsAsNonNull()
        {
            Span<int> list = new int[0];
            Assert.Equal(0, NativeExportsNE.Span.Sum(list, list.Length));
        }

        [Fact]
        public void ZeroLengthReadOnlySpanMarshalsAsNonNull()
        {
            ReadOnlySpan<int> list = new int[0];
            Assert.Equal(0, NativeExportsNE.Span.Sum(list, list.Length));
        }

        [Fact]
        public void BlittableElementSpanInParameter()
        {
            var list = new int[] { 1, 5, 79, 165, 32, 3 };
            Assert.Equal(list.Sum(), NativeExportsNE.Span.SumInArray(list, list.Length));
        }

        [Fact]
        public void BlittableElementSpanRefParameter()
        {
            var list = new int[] { 1, 5, 79, 165, 32, 3 };
            Span<int> newSpan = list;
            NativeExportsNE.Span.Duplicate(ref newSpan, list.Length);
            Assert.Equal((IEnumerable<int>)list, newSpan.ToArray());
        }

        [Fact]
        public void BlittableElementSpanReturnedFromNative()
        {
            int start = 5;
            int end = 20;

            IEnumerable<int> expected = Enumerable.Range(start, end - start);
            Assert.Equal(expected, NativeExportsNE.Collections.Stateless.CreateRange(start, end, out _));

            Span<int> res;
            NativeExportsNE.Span.CreateRange_Out(start, end, out _, out res);
            Assert.Equal(expected, res.ToArray());
        }

        [Fact]
        public void NullBlittableElementSpanReturnedFromNative()
        {
            Assert.Null(NativeExportsNE.Collections.Stateless.CreateRange(1, 0, out _));

            Span<int> res;
            NativeExportsNE.Span.CreateRange_Out(1, 0, out _, out res);
            Assert.True(res.IsEmpty);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SpanWithSimpleNonBlittableTypeMarshalling(bool result)
        {
            var boolValues = new BoolStruct[]
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

            Assert.Equal(result, NativeExportsNE.Span.AndAllMembers(boolValues, boolValues.Length));
        }
    }
}
