// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using static System.TestHelpers;

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
            span.Validate(91, 92, -93, 94);

            span = new ReadOnlySpan<int>(a, 0, a.Length);
            span.Validate(91, 92, -93, 94);
        }

        [Fact]
        public static void CtorArray2()
        {
            long[] a = { 91, -92, 93, 94, -95 };
            ReadOnlySpan<long> span;

            span = new ReadOnlySpan<long>(a);
            span.Validate(91, -92, 93, 94, -95);

            span = new ReadOnlySpan<long>(a, 0, a.Length);
            span.Validate(91, -92, 93, 94, -95);
        }

        [Fact]
        public static void CtorArray3()
        {
            object o1 = new object();
            object o2 = new object();
            object[] a = { o1, o2 };
            ReadOnlySpan<object> span;

            span = new ReadOnlySpan<object>(a);
            span.ValidateReferenceType(o1, o2);

            span = new ReadOnlySpan<object>(a, 0, a.Length);
            span.ValidateReferenceType(o1, o2);
        }

        [Fact]
        public static void CtorArrayZeroLength()
        {
            int[] empty = Array.Empty<int>();
            ReadOnlySpan<int> span;

            span = new ReadOnlySpan<int>(empty);
            span.ValidateNonNullEmpty();

            span = new ReadOnlySpan<int>(empty, 0, empty.Length);
            span.ValidateNonNullEmpty();
        }

        [Fact]
        public static void CtorArrayNullArray()
        {
            Assert.Throws<ArgumentNullException>(() => new ReadOnlySpan<int>(null).DontBox());
            Assert.Throws<ArgumentNullException>(() => new ReadOnlySpan<int>(null, 0, 0).DontBox());
        }

        [Fact]
        public static void CtorArrayWrongValueType()
        {
            // Can pass variant array, if array type is a valuetype.

            uint[] a = { 42u, 0xffffffffu };
            int[] aAsIntArray = (int[])(object)a;
            ReadOnlySpan<int> span;

            span = new ReadOnlySpan<int>(aAsIntArray);
            span.Validate(42, -1);

            span = new ReadOnlySpan<int>(aAsIntArray, 0, aAsIntArray.Length);
            span.Validate(42, -1);
        }

        [Fact]
        public static void CtorVariantArrayType()
        {
            // For ReadOnlySpan<T>, variant arrays are allowed for string to object
            // and reference type to object.

            ReadOnlySpan<object> span;

            string[] strArray = { "Hello" };
            span = new ReadOnlySpan<object>(strArray);
            span.ValidateReferenceType("Hello");
            span = new ReadOnlySpan<object>(strArray, 0, strArray.Length);
            span.ValidateReferenceType("Hello");

            TestClass c1 = new TestClass();
            TestClass c2 = new TestClass();
            TestClass[] clsArray = { c1, c2 };
            span = new ReadOnlySpan<object>(clsArray);
            span.ValidateReferenceType(c1, c2);
            span = new ReadOnlySpan<object>(clsArray, 0, clsArray.Length);
            span.ValidateReferenceType(c1, c2);
        }
    }
}
