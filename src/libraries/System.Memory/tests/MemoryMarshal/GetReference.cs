// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static System.TestHelpers;

namespace System.SpanTests
{
    public static partial class MemoryMarshalTests
    {
        [Fact]
        public static void SpanGetReferenceArray()
        {
            int[] a = { 91, 92, 93, 94, 95 };
            Span<int> span = new Span<int>(a, 1, 3);
            ref int pinnableReference = ref MemoryMarshal.GetReference(span);
            Assert.True(Unsafe.AreSame(ref a[1], ref pinnableReference));
        }

        [Fact]
        public static void SpanGetReferenceArrayPastEnd()
        {
            // The only real difference between GetReference() and "ref span[0]" is that
            // GetReference() of a zero-length won't throw an IndexOutOfRange.

            int[] a = { 91, 92, 93, 94, 95 };
            Span<int> span = new Span<int>(a, a.Length, 0);
            ref int pinnableReference = ref MemoryMarshal.GetReference(span);
            ref int expected = ref Unsafe.Add<int>(ref a[a.Length - 1], 1);
            Assert.True(Unsafe.AreSame(ref expected, ref pinnableReference));
        }

        [Fact]
        public static void SpanGetReferencePointer()
        {
            unsafe
            {
                int i = 42;
                Span<int> span = new Span<int>(&i, 1);
                ref int pinnableReference = ref MemoryMarshal.GetReference(span);
                Assert.True(Unsafe.AreSame(ref i, ref pinnableReference));
            }
        }

        [Fact]
        public static void SpanGetReferencePointerDangerousCreate1()
        {
            TestClass testClass = new TestClass();
            Span<char> span = Span<char>.DangerousCreate(testClass, ref testClass.C1, 3);

            ref char pinnableReference = ref MemoryMarshal.GetReference(span);
            Assert.True(Unsafe.AreSame(ref testClass.C1, ref pinnableReference));
        }

        [Fact]
        public static void SpanGetReferenceEmpty()
        {
            unsafe
            {
                Span<int> span = Span<int>.Empty;
                ref int pinnableReference = ref MemoryMarshal.GetReference(span);
                Assert.True(Unsafe.AreSame(ref Unsafe.AsRef<int>(null), ref pinnableReference));
            }
        }

        [Fact]
        public static void ReadOnlySpanGetReferenceArray()
        {
            int[] a = { 91, 92, 93, 94, 95 };
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(a, 1, 3);
            ref int pinnableReference = ref Unsafe.AsRef(in MemoryMarshal.GetReference(span));
            Assert.True(Unsafe.AreSame(ref a[1], ref pinnableReference));
        }

        [Fact]
        public static void ReadOnlySpanGetReferenceArrayPastEnd()
        {
            // The only real difference between GetReference() and "ref span[0]" is that
            // GetReference() of a zero-length won't throw an IndexOutOfRange.

            int[] a = { 91, 92, 93, 94, 95 };
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(a, a.Length, 0);
            ref int pinnableReference = ref Unsafe.AsRef(in MemoryMarshal.GetReference(span));
            ref int expected = ref Unsafe.Add<int>(ref a[a.Length - 1], 1);
            Assert.True(Unsafe.AreSame(ref expected, ref pinnableReference));
        }

        [Fact]
        public static void ReadOnlySpanGetReferencePointer()
        {
            unsafe
            {
                int i = 42;
                ReadOnlySpan<int> span = new ReadOnlySpan<int>(&i, 1);
                ref int pinnableReference = ref Unsafe.AsRef(in MemoryMarshal.GetReference(span));
                Assert.True(Unsafe.AreSame(ref i, ref pinnableReference));
            }
        }

        [Fact]
        public static void ReadOnlySpanGetReferencePointerDangerousCreate1()
        {
            TestClass testClass = new TestClass();
            ReadOnlySpan<char> span = ReadOnlySpan<char>.DangerousCreate(testClass, ref testClass.C1, 3);

            ref char pinnableReference = ref Unsafe.AsRef(in MemoryMarshal.GetReference(span));
            Assert.True(Unsafe.AreSame(ref testClass.C1, ref pinnableReference));
        }

        [Fact]
        public static void ReadOnlySpanGetReferenceEmpty()
        {
            unsafe
            {
                ReadOnlySpan<int> span = ReadOnlySpan<int>.Empty;
                ref int pinnableReference = ref Unsafe.AsRef(in MemoryMarshal.GetReference(span));
                Assert.True(Unsafe.AreSame(ref Unsafe.AsRef<int>(null), ref pinnableReference));
            }
        }
    }
}
