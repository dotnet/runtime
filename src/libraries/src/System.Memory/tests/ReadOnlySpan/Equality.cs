// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

#pragma warning disable 1718 //Comparison made to same variable; did you mean to compare something else?

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void EqualityTrue()
        {
            int[] a = { 91, 92, 93, 94, 95 };
            ReadOnlySpan<int> left = new ReadOnlySpan<int>(a, 2, 3);
            ReadOnlySpan<int> right = new ReadOnlySpan<int>(a, 2, 3);

            Assert.True(left == right);
            Assert.True(!(left != right));
        }

        [Fact]
        public static void EqualityReflexivity()
        {
            int[] a = { 91, 92, 93, 94, 95 };
            ReadOnlySpan<int> left = new ReadOnlySpan<int>(a, 2, 3);

            Assert.True(left == left);
            Assert.True(!(left != left));
        }

        [Fact]
        public static void EqualityIncludesLength()
        {
            int[] a = { 91, 92, 93, 94, 95 };
            ReadOnlySpan<int> left = new ReadOnlySpan<int>(a, 2, 1);
            ReadOnlySpan<int> right = new ReadOnlySpan<int>(a, 2, 3);

            Assert.False(left == right);
            Assert.False(!(left != right));
        }

        [Fact]
        public static void EqualityIncludesBase()
        {
            int[] a = { 91, 92, 93, 94, 95 };
            ReadOnlySpan<int> left = new ReadOnlySpan<int>(a, 1, 3);
            ReadOnlySpan<int> right = new ReadOnlySpan<int>(a, 2, 3);

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
                    ReadOnlySpan<int> left = new ReadOnlySpan<int>(a, 2, 3);
                    ReadOnlySpan<int> right = new ReadOnlySpan<int>(pa + 2, 3);

                    Assert.True(left == right);
                    Assert.True(!(left != right));
                }
            }
        }

        [Fact]
        public static void EqualityComparesRangeNotContent()
        {
            ReadOnlySpan<int> left = new ReadOnlySpan<int>(new int[] { 0, 1, 2 }, 1, 1);
            ReadOnlySpan<int> right = new ReadOnlySpan<int>(new int[] { 0, 1, 2 }, 1, 1);

            Assert.False(left == right);
            Assert.False(!(left != right));
        }

        [Fact]
        public static void EmptySpansNotUnified()
        {
            ReadOnlySpan<int> left = new ReadOnlySpan<int>(new int[0]);
            ReadOnlySpan<int> right = new ReadOnlySpan<int>(new int[0]);

            Assert.False(left == right);
            Assert.False(!(left != right));
        }

        [Fact]
        public static void CannotCallEqualsOnSpan()
        {
            ReadOnlySpan<int> left = new ReadOnlySpan<int>(new int[0]);
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
