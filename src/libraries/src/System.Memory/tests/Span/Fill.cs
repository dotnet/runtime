// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Xunit;
using static System.TestHelpers;

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        [Fact]
        public static void FillEmpty()
        {
            var span = Span<byte>.Empty;
            span.Fill(1);
        }

        [Fact]
        public static void FillByteLonger()
        {
            const byte fill = 5;
            var expected = new byte[2048];
            for (int i = 0; i < expected.Length; i++)
            {
                expected[i] = fill;
            }
            var actual = new byte[2048];

            var span = new Span<byte>(actual);
            span.Fill(fill);
            Assert.Equal<byte>(expected, actual);
        }

        [Fact]
        public static void FillByteUnaligned()
        {
            const byte fill = 5;
            const int length = 32;
            var expectedFull = new byte[length];
            for (int i = 0; i < length; i++)
            {
                expectedFull[i] = fill;
            }
            var actualFull = new byte[length];

            var start = 1;
            var expectedSpan = new Span<byte>(expectedFull, start, length - start - 1);
            var actualSpan = new Span<byte>(actualFull, start, length - start - 1);
            actualSpan.Fill(fill);

            byte[] actual = actualSpan.ToArray();
            byte[] expected = expectedSpan.ToArray();
            Assert.Equal<byte>(expected, actual);
            Assert.Equal(0, actualFull[0]);
            Assert.Equal(0, actualFull[length - 1]);
        }

        [Fact]
        public static void FillValueTypeWithoutReferences()
        {
            const byte fill = 5;
            for (int length = 0; length < 32; length++)
            {
                var expectedFull = new int[length];
                var actualFull = new int[length];
                for (int i = 0; i < length; i++)
                {
                    expectedFull[i] = fill;
                    actualFull[i] = i;
                }
                var span = new Span<int>(actualFull);
                span.Fill(fill);
                Assert.Equal<int>(expectedFull, actualFull);
            }
        }

        [Fact]
        public static void FillReferenceType()
        {
            string[] actual = { "a", "b", "c" };
            string[] expected = { "d", "d", "d" };

            var span = new Span<string>(actual);
            span.Fill("d");
            Assert.Equal<string>(expected, actual);
        }

        [Fact]
        public static void FillValueTypeWithReferences()
        {
            TestValueTypeWithReference[] actual = {
                new TestValueTypeWithReference() { I = 1, S = "a" },
                new TestValueTypeWithReference() { I = 2, S = "b" },
                new TestValueTypeWithReference() { I = 3, S = "c" } };
            TestValueTypeWithReference[] expected = {
                new TestValueTypeWithReference() { I = 5, S = "d" },
                new TestValueTypeWithReference() { I = 5, S = "d" },
                new TestValueTypeWithReference() { I = 5, S = "d" } };

            var span = new Span<TestValueTypeWithReference>(actual);
            span.Fill(new TestValueTypeWithReference() { I = 5, S = "d" });
            Assert.Equal<TestValueTypeWithReference>(expected, actual);
        }

        [Fact]
        public static unsafe void FillNativeBytes()
        {
            // Arrange
            int length = 50;

            byte* ptr = null;
            try
            {
                ptr = (byte*)Marshal.AllocHGlobal((IntPtr)50);
            }
            // Skipping test if Out-of-Memory, since this test can only be run, if there is enough memory
            catch (OutOfMemoryException)
            {
                Console.WriteLine(
                    $"Span.Fill test {nameof(FillNativeBytes)} skipped due to {nameof(OutOfMemoryException)}.");
                return;
            }

            try
            {
                byte initial = 1;
                for (int i = 0; i < length; i++)
                {
                    *(ptr + i) = initial;
                }
                const byte fill = 5;
                var span = new Span<byte>(ptr, length);

                // Act
                span.Fill(fill);

                // Assert using custom code for perf and to avoid allocating extra memory
                for (int i = 0; i < length; i++)
                {
                    var actual = *(ptr + i);
                    Assert.Equal(fill, actual);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(new IntPtr(ptr));
            }
        }
    }
}
