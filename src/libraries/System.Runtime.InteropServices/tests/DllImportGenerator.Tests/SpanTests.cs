using SharedTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.GeneratedMarshalling;
using System.Text;

using Xunit;

namespace DllImportGenerator.IntegrationTests
{
    partial class NativeExportsNE
    {
        public partial class Span
        {
            [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "sum_int_array")]
            public static partial int Sum([MarshalUsing(typeof(SpanMarshaller<int>))] Span<int> values, int numValues);

            [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "sum_int_array")]
            public static partial int SumNeverNull([MarshalUsing(typeof(NeverNullSpanMarshaller<int>))] Span<int> values, int numValues);

            [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "sum_int_array")]
            public static partial int SumNeverNull([MarshalUsing(typeof(NeverNullReadOnlySpanMarshaller<int>))] ReadOnlySpan<int> values, int numValues);

            [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "sum_int_array_ref")]
            public static partial int SumInArray([MarshalUsing(typeof(SpanMarshaller<int>))] in Span<int> values, int numValues);

            [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "duplicate_int_array")]
            public static partial void Duplicate([MarshalUsing(typeof(SpanMarshaller<int>), CountElementName = "numValues")] ref Span<int> values, int numValues);

            [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "duplicate_int_array")]
            public static partial void DuplicateRaw([MarshalUsing(typeof(DirectSpanMarshaller<int>), CountElementName = "numValues")] ref Span<int> values, int numValues);

            [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "create_range_array")]
            [return: MarshalUsing(typeof(SpanMarshaller<int>), CountElementName = "numValues")]
            public static partial Span<int> CreateRange(int start, int end, out int numValues);

            [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "create_range_array_out")]
            public static partial void CreateRange_Out(int start, int end, out int numValues, [MarshalUsing(typeof(SpanMarshaller<int>), CountElementName = "numValues")] out Span<int> res);

            [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "get_long_bytes")]
            [return: MarshalUsing(typeof(SpanMarshaller<byte>), ConstantElementCount = sizeof(long))]
            public static partial Span<byte> GetLongBytes(long l);

            [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "and_all_members")]
            [return: MarshalAs(UnmanagedType.U1)]
            public static partial bool AndAllMembers([MarshalUsing(typeof(SpanMarshaller<BoolStruct>))] Span<BoolStruct> pArray, int length);
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
        public void NeverNullSpanMarshallerMarshalsDefaultAsNonNull()
        {
            Assert.Equal(0, NativeExportsNE.Span.SumNeverNull(Span<int>.Empty, 0));
        }

        [Fact]
        public void NeverNullReadOnlySpanMarshallerMarshalsDefaultAsNonNull()
        {
            Assert.Equal(0, NativeExportsNE.Span.SumNeverNull(ReadOnlySpan<int>.Empty, 0));
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
        public unsafe void DirectSpanMarshaller()
        {
            var list = new int[] { 1, 5, 79, 165, 32, 3 };
            Span<int> newSpan = list;
            NativeExportsNE.Span.DuplicateRaw(ref newSpan, list.Length);
            Assert.Equal((IEnumerable<int>)list, newSpan.ToArray());
            Marshal.FreeCoTaskMem((IntPtr)Unsafe.AsPointer(ref newSpan.GetPinnableReference()));
        }

        [Fact]
        public void BlittableElementSpanReturnedFromNative()
        {
            int start = 5;
            int end = 20;

            IEnumerable<int> expected = Enumerable.Range(start, end - start);
            Assert.Equal(expected, NativeExportsNE.Collections.CreateRange(start, end, out _));

            Span<int> res;
            NativeExportsNE.Span.CreateRange_Out(start, end, out _, out res);
            Assert.Equal(expected, res.ToArray());
        }

        [Fact]
        public void NullBlittableElementSpanReturnedFromNative()
        {
            Assert.Null(NativeExportsNE.Collections.CreateRange(1, 0, out _));

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
