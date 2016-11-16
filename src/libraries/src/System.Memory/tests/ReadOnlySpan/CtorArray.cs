// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.SpanTests
{
    //
    // Tests for Span<T>.ctor(T[])
    //
    // These tests will also exercise the matching codepaths in Span<T>.ctor(T[], int) and .ctor(T[], int, int). This makes it easier to ensure
    // that these parallel tests stay consistent, and avoid excess repetition in the files devoted to those specific overloads.
    //
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void CtorArray1()
        {
            int[] a = { 91, 92, -93, 94 };
            ReadOnlySpan<int> span;

            span = new ReadOnlySpan<int>(a);
            span.Validate<int>(91, 92, -93, 94);

            span = new ReadOnlySpan<int>(a, 0);
            span.Validate<int>(91, 92, -93, 94);

            span = new ReadOnlySpan<int>(a, 0, a.Length);
            span.Validate<int>(91, 92, -93, 94);
        }

        [Fact]
        public static void CtorArray2()
        {
            long[] a = { 91, -92, 93, 94, -95 };
            ReadOnlySpan<long> span;

            span = new ReadOnlySpan<long>(a);
            span.Validate<long>(91, -92, 93, 94, -95);

            span = new ReadOnlySpan<long>(a, 0);
            span.Validate<long>(91, -92, 93, 94, -95);

            span = new ReadOnlySpan<long>(a, 0, a.Length);
            span.Validate<long>(91, -92, 93, 94, -95);
        }

        [Fact]
        public static void CtorArray3()
        {
            object o1 = new object();
            object o2 = new object();
            object[] a = { o1, o2 };
            ReadOnlySpan<object> span;

            span = new ReadOnlySpan<object>(a);
            span.Validate<object>(o1, o2);

            span = new ReadOnlySpan<object>(a, 0);
            span.Validate<object>(o1, o2);

            span = new ReadOnlySpan<object>(a, 0, a.Length);
            span.Validate<object>(o1, o2);
        }

        [Fact]
        public static void CtorArrayZeroLength()
        {
            int[] empty = Array.Empty<int>();
            ReadOnlySpan<int> span;

            span = new ReadOnlySpan<int>(empty);
            span.Validate<int>();

            span = new ReadOnlySpan<int>(empty, 0);
            span.Validate<int>();

            span = new ReadOnlySpan<int>(empty, 0, empty.Length);
            span.Validate<int>();
        }

        [Fact]
        public static void CtorArrayNullArray()
        {
            Assert.Throws<ArgumentNullException>(() => new ReadOnlySpan<int>((int[])null).DontBox());
            Assert.Throws<ArgumentNullException>(() => new ReadOnlySpan<int>((int[])null, 0).DontBox());
            Assert.Throws<ArgumentNullException>(() => new ReadOnlySpan<int>((int[])null, 0, 0).DontBox());
        }

        [Fact]
        public static void CtorArrayWrongArrayType()
        {
            // Cannot pass variant array, if array type is not a valuetype.
            string[] a = { "Hello" };
            Assert.Throws<ArrayTypeMismatchException>(() => new ReadOnlySpan<object>(a).DontBox());
            Assert.Throws<ArrayTypeMismatchException>(() => new ReadOnlySpan<object>(a, 0).DontBox());
            Assert.Throws<ArrayTypeMismatchException>(() => new ReadOnlySpan<object>(a, 0, a.Length).DontBox());
        }

        [Fact]
        public static void CtorArrayWrongValueType()
        {
            // Can pass variant array, if array type is a valuetype.

            uint[] a = { 42u, 0xffffffffu };
            int[] aAsIntArray = (int[])(object)a;
            ReadOnlySpan<int> span;

            span = new ReadOnlySpan<int>(aAsIntArray);
            span.Validate<int>(42, -1);

            span = new ReadOnlySpan<int>(aAsIntArray, 0);
            span.Validate<int>(42, -1);

            span = new ReadOnlySpan<int>(aAsIntArray, 0, aAsIntArray.Length);
            span.Validate<int>(42, -1);
        }
    }
}

