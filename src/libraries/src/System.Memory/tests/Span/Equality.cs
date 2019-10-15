// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        [Fact]
        public static void EqualityTrue()
        {
            int[] a = { 91, 92, 93, 94, 95 };
            Span<int> left = new Span<int>(a, 2, 3);
            Span<int> right = new Span<int>(a, 2, 3);

            Assert.True(left == right);
            Assert.True(!(left != right));
        }

        [Fact]
        public static void EqualityReflexivity()
        {
            int[] a = { 91, 92, 93, 94, 95 };
            Span<int> left = new Span<int>(a, 2, 3);

#pragma warning disable 1718 // Comparison made to same variable; did you mean to compare something else?
            Assert.True(left == left);
            Assert.True(!(left != left));
#pragma warning restore 1718
        }

        [Fact]
        public static void EqualityIncludesLength()
        {
            int[] a = { 91, 92, 93, 94, 95 };
            Span<int> left = new Span<int>(a, 2, 1);
            Span<int> right = new Span<int>(a, 2, 3);

            Assert.False(left == right);
            Assert.False(!(left != right));
        }

        [Fact]
        public static void EqualityIncludesBase()
        {
            int[] a = { 91, 92, 93, 94, 95 };
            Span<int> left = new Span<int>(a, 1, 3);
            Span<int> right = new Span<int>(a, 2, 3);

            Assert.False(left == right);
            Assert.False(!(left != right));
        }

        [Fact]
        public static void EqualityBasedOnLocationNotConstructor()
        {
            unsafe
            {
                int[] a = { 91, 92, 93, 94, 95 };
                fixed (int* pa = a)
                {
                    Span<int> left = new Span<int>(a, 2, 3);
                    Span<int> right = new Span<int>(pa + 2, 3);

                    Assert.True(left == right);
                    Assert.True(!(left != right));
                }
            }
        }

        [Fact]
        public static void EqualityComparesRangeNotContent()
        {
            Span<int> left = new Span<int>(new int[] { 0, 1, 2 }, 1, 1);
            Span<int> right = new Span<int>(new int[] { 0, 1, 2 }, 1, 1);

            Assert.False(left == right);
            Assert.False(!(left != right));
        }

        [Fact]
        public static void EmptySpansNotUnified()
        {
            Span<int> left = new Span<int>(new int[0]);
            Span<int> right = new Span<int>(new int[0]);

            Assert.False(left == right);
            Assert.False(!(left != right));
        }

        [Fact]
        public static void CannotCallEqualsOnSpan()
        {
            Span<int> left = new Span<int>(new int[0]);

            try
            {
#pragma warning disable 0618
                bool result = left.Equals(new object());
#pragma warning restore 0618
                Assert.True(false);
            }
            catch (Exception ex)
            {
                Assert.True(ex is NotSupportedException);
            }
        }
    }
}
