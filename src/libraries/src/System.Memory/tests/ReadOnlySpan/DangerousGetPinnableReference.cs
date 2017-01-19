// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void DangerousGetPinnableReferenceArray()
        {
            int[] a = { 91, 92, 93, 94, 95 };
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(a, 1, 3);
            ref int pinnableReference = ref span.DangerousGetPinnableReference();
            Assert.True(Unsafe.AreSame<int>(ref a[1], ref pinnableReference));
        }

        [Fact]
        public static void DangerousGetPinnableReferenceArrayPastEnd()
        {
            // The only real difference between DangerousGetPinnableReference() and "ref span[0]" is that
            // DangerousGetPinnableReference() of a zero-length won't throw an IndexOutOfRange.

            int[] a = { 91, 92, 93, 94, 95 };
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(a, a.Length, 0);
            ref int pinnableReference = ref span.DangerousGetPinnableReference();
            ref int expected = ref Unsafe.Add<int>(ref a[a.Length - 1], 1);
            Assert.True(Unsafe.AreSame<int>(ref expected, ref pinnableReference));
        }

        [Fact]
        public static void DangerousGetPinnableReferencePointer()
        {
            unsafe
            {
                int i = 42;
                ReadOnlySpan<int> span = new ReadOnlySpan<int>(&i, 1);
                ref int pinnableReference = ref span.DangerousGetPinnableReference();
                Assert.True(Unsafe.AreSame<int>(ref i, ref pinnableReference));
            }
        }

        [Fact]
        public static void DangerousGetPinnableReferencePointerDangerousCreate1()
        {
            TestClass testClass = new TestClass();
            ReadOnlySpan<char> span = ReadOnlySpan<char>.DangerousCreate(testClass, ref testClass.C1, 3);

            ref char pinnableReference = ref span.DangerousGetPinnableReference();
            Assert.True(Unsafe.AreSame<char>(ref testClass.C1, ref pinnableReference));
        }

        [Fact]
        public static void DangerousGetPinnableReferenceEmpty()
        {
            unsafe
            {
                ReadOnlySpan<int> span = ReadOnlySpan<int>.Empty;
                ref int pinnableReference = ref span.DangerousGetPinnableReference();
                Assert.True(Unsafe.AreSame<int>(ref Unsafe.AsRef<int>(null), ref pinnableReference));
            }
        }
    }
}
