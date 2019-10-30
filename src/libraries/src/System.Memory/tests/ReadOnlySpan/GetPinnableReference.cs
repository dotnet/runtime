// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void GetPinnableReferenceArray()
        {
            int[] a = { 91, 92, 93, 94, 95 };
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(a, 1, 3);
            ref int pinnableReference = ref Unsafe.AsRef(in span.GetPinnableReference());
            Assert.True(Unsafe.AreSame(ref a[1], ref pinnableReference));
        }

        [Fact]
        public static unsafe void UsingSpanInFixed()
        {
            byte[] a = { 91, 92, 93, 94, 95 };
            ReadOnlySpan<byte> span = a;
            fixed (byte* ptr = span)
            {
                for (int i = 0; i < span.Length; i++)
                {
                    Assert.Equal(a[i], ptr[i]);
                }
            }
        }

        [Fact]
        public static unsafe void UsingEmptySpanInFixed()
        {
            ReadOnlySpan<int> span = ReadOnlySpan<int>.Empty;
            fixed (int* ptr = span)
            {
                Assert.True(ptr == null);
            }

            ReadOnlySpan<int> spanFromEmptyArray = Array.Empty<int>();
            fixed (int* ptr = spanFromEmptyArray)
            {
                Assert.True(ptr == null);
            }
        }

        [Fact]
        public static unsafe void GetPinnableReferenceArrayPastEnd()
        {
            // The only real difference between GetPinnableReference() and "ref span[0]" is that
            // GetPinnableReference() of a zero-length won't throw an IndexOutOfRange but instead return a null ref.

            int[] a = { 91, 92, 93, 94, 95 };
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(a, a.Length, 0);
            ref int pinnableReference = ref Unsafe.AsRef(in span.GetPinnableReference());
            ref int expected = ref Unsafe.AsRef<int>(null);
            Assert.True(Unsafe.AreSame(ref expected, ref pinnableReference));
        }

        [Fact]
        public static unsafe void GetPinnableReferencePointer()
        {
            int i = 42;
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(&i, 1);
            ref int pinnableReference = ref Unsafe.AsRef(in span.GetPinnableReference());
            Assert.True(Unsafe.AreSame(ref i, ref pinnableReference));
        }

        [Fact]
        public static unsafe void GetPinnableReferenceEmpty()
        {
            ReadOnlySpan<int> span = ReadOnlySpan<int>.Empty;
            ref int pinnableReference = ref Unsafe.AsRef(in span.GetPinnableReference());
            Assert.True(Unsafe.AreSame(ref Unsafe.AsRef<int>(null), ref pinnableReference));

            ReadOnlySpan<int> spanFromEmptyArray = Array.Empty<int>();
            pinnableReference = ref Unsafe.AsRef(in spanFromEmptyArray.GetPinnableReference());
            Assert.True(Unsafe.AreSame(ref Unsafe.AsRef<int>(null), ref pinnableReference));
        }
    }
}
