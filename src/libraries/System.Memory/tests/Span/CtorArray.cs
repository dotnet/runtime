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
    public static partial class SpanTests
    {
        [Fact]
        public static void CtorArray1()
        {
            int[] a = { 91, 92, -93, 94 };
            Span<int> span;

            span = new Span<int>(a);
            span.Validate(91, 92, -93, 94);

            span = new Span<int>(a, 0, a.Length);
            span.Validate(91, 92, -93, 94);
        }

        [Fact]
        public static void CtorArray2()
        {
            long[] a = { 91, -92, 93, 94, -95 };
            Span<long> span;

            span = new Span<long>(a);
            span.Validate(91, -92, 93, 94, -95);

            span = new Span<long>(a, 0, a.Length);
            span.Validate(91, -92, 93, 94, -95);
        }

        [Fact]
        public static void CtorArray3()
        {
            object o1 = new object();
            object o2 = new object();
            object[] a = { o1, o2 };
            Span<object> span;

            span = new Span<object>(a);
            span.ValidateReferenceType(o1, o2);

            span = new Span<object>(a, 0, a.Length);
            span.ValidateReferenceType(o1, o2);
        }

        [Fact]
        public static void CtorArrayZeroLength()
        {
            int[] empty = Array.Empty<int>();
            Span<int> span;

            span = new Span<int>(empty);
            span.ValidateNonNullEmpty();

            span = new Span<int>(empty, 0, empty.Length);
            span.ValidateNonNullEmpty();
        }

        [Fact]
        public static void CtorArrayNullArray()
        {
            Assert.Throws<ArgumentNullException>(() => new Span<int>(null).DontBox());
            Assert.Throws<ArgumentNullException>(() => new Span<int>(null, 0, 0).DontBox());
        }

        [Fact]
        public static void CtorArrayWrongArrayType()
        {
            // Cannot pass variant array, if array type is not a valuetype.
            string[] a = { "Hello" };
            Assert.Throws<ArrayTypeMismatchException>(() => new Span<object>(a).DontBox());
            Assert.Throws<ArrayTypeMismatchException>(() => new Span<object>(a, 0, a.Length).DontBox());
        }

        [Fact]
        public static void CtorArrayWrongValueType()
        {
            // Can pass variant array, if array type is a valuetype.

            uint[] a = { 42u, 0xffffffffu };
            int[] aAsIntArray = (int[])(object)a;
            Span<int> span;

            span = new Span<int>(aAsIntArray);
            span.Validate(42, -1);

            span = new Span<int>(aAsIntArray, 0, aAsIntArray.Length);
            span.Validate(42, -1);
        }
    }
}

