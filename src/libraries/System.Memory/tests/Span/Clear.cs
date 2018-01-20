// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Runtime.CompilerServices;
using static System.TestHelpers;

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        [Fact]
        public static void ClearEmpty()
        {
            var span = Span<byte>.Empty;
            span.Clear();
        }

        [Fact]
        public static void ClearEmptyWithReference()
        {
            var span = Span<string>.Empty;
            span.Clear();
        }

        [Fact]
        public static void ClearByteLonger()
        {
            const byte initial = 5;
            var actual = new byte[2048];
            for (int i = 0; i < actual.Length; i++)
            {
                actual[i] = initial;
            }
            var expected = new byte[actual.Length];

            var span = new Span<byte>(actual);
            span.Clear();
            Assert.Equal<byte>(expected, actual);
        }

        [Fact]
        public static void ClearByteUnaligned()
        {
            const byte initial = 5;
            const int length = 32;
            var actualFull = new byte[length];
            for (int i = 0; i < length; i++)
            {
                actualFull[i] = initial;
            }
            var expectedFull = new byte[length];

            var start = 1;
            var expectedSpan = new Span<byte>(expectedFull, start, length - start - 1);
            var actualSpan = new Span<byte>(actualFull, start, length - start - 1);
            actualSpan.Clear();

            byte[] actual = actualSpan.ToArray();
            byte[] expected = expectedSpan.ToArray();
            Assert.Equal<byte>(expected, actual);
            Assert.Equal(initial, actualFull[0]);
            Assert.Equal(initial, actualFull[length - 1]);
        }

        [Fact]
        public unsafe static void ClearByteUnalignedFixed()
        {
            const byte initial = 5;
            const int length = 32;
            var actualFull = new byte[length];
            for (int i = 0; i < length; i++)
            {
                actualFull[i] = initial;
            }
            var expectedFull = new byte[length];

            var start = 1;
            var expectedSpan = new Span<byte>(expectedFull, start, length - start - 1);
            fixed (byte* p = actualFull)
            {
                var actualSpan = new Span<byte>(p + start, length - start - 1);
                actualSpan.Clear();

                byte[] actual = actualSpan.ToArray();
                byte[] expected = expectedSpan.ToArray();
                Assert.Equal<byte>(expected, actual);
                Assert.Equal(initial, actualFull[0]);
                Assert.Equal(initial, actualFull[length - 1]);
            }
        }

        [Fact]
        public static void ClearIntPtrOffset()
        {
            IntPtr initial = IntPtr.Zero + 5;
            const int length = 32;
            var actualFull = new IntPtr[length];
            for (int i = 0; i < length; i++)
            {
                actualFull[i] = initial;
            }
            var expectedFull = new IntPtr[length];

            var start = 2;
            var expectedSpan = new Span<IntPtr>(expectedFull, start, length - start - 1);
            var actualSpan = new Span<IntPtr>(actualFull, start, length - start - 1);
            actualSpan.Clear();

            IntPtr[] actual = actualSpan.ToArray();
            IntPtr[] expected = expectedSpan.ToArray();
            Assert.Equal<IntPtr>(expected, actual);
            Assert.Equal(initial, actualFull[0]);
            Assert.Equal(initial, actualFull[length - 1]);
        }

        [Fact]
        public static void ClearIntPtrLonger()
        {
            IntPtr initial = IntPtr.Zero + 5;
            var actual = new IntPtr[2048];
            for (int i = 0; i < actual.Length; i++)
            {
                actual[i] = initial;
            }
            var expected = new IntPtr[actual.Length];

            var span = new Span<IntPtr>(actual);
            span.Clear();
            Assert.Equal<IntPtr>(expected, actual);
        }

        [Fact]
        public static void ClearValueTypeWithoutReferences()
        {
            int[] actual = { 1, 2, 3 };
            int[] expected = { 0, 0, 0 };

            var span = new Span<int>(actual);
            span.Clear();
            Assert.Equal<int>(expected, actual);
        }

        [Fact]
        public static void ClearValueTypeWithoutReferencesLonger()
        {
            int[] actual = new int[2048];
            for (int i = 0; i < actual.Length; i++)
            {
                actual[i] = i + 1;
            }
            int[] expected = new int[actual.Length];

            var span = new Span<int>(actual);
            span.Clear();
            Assert.Equal<int>(expected, actual);
        }

        [Fact]
        public static void ClearValueTypeWithoutReferencesPointerSize()
        {
            long[] actual = new long[15];
            for (int i = 0; i < actual.Length; i++)
            {
                actual[i] = i + 1;
            }
            long[] expected = new long[actual.Length];

            var span = new Span<long>(actual);
            span.Clear();
            Assert.Equal<long>(expected, actual);
        }

        [Fact]
        public static void ClearReferenceType()
        {
            string[] actual = { "a", "b", "c" };
            string[] expected = { null, null, null };

            var span = new Span<string>(actual);
            span.Clear();
            Assert.Equal<string>(expected, actual);
        }

        [Fact]
        public static void ClearReferenceTypeLonger()
        {
            string[] actual = new string[2048];
            for (int i = 0; i < actual.Length; i++)
            {
                actual[i] = (i + 1).ToString();
            }
            string[] expected = new string[actual.Length];

            var span = new Span<string>(actual);
            span.Clear();
            Assert.Equal<string>(expected, actual);
        }

        [Fact]
        public static void ClearEnumType()
        {
            TestEnum[] actual = { TestEnum.e0, TestEnum.e1, TestEnum.e2 };
            TestEnum[] expected = { default, default, default };

            var span = new Span<TestEnum>(actual);
            span.Clear();
            Assert.Equal<TestEnum>(expected, actual);
        }

        [Fact]
        public static void ClearValueTypeWithReferences()
        {
            TestValueTypeWithReference[] actual = {
                new TestValueTypeWithReference() { I = 1, S = "a" },
                new TestValueTypeWithReference() { I = 2, S = "b" },
                new TestValueTypeWithReference() { I = 3, S = "c" } };
            TestValueTypeWithReference[] expected = {
                default,
                default,
                default };

            var span = new Span<TestValueTypeWithReference>(actual);
            span.Clear();
            Assert.Equal<TestValueTypeWithReference>(expected, actual);
        }

        // NOTE: ClearLongerThanUintMaxValueBytes test is constrained to run on Windows and MacOSX because it causes
        //       problems on Linux due to the way deferred memory allocation works. On Linux, the allocation can
        //       succeed even if there is not enough memory but then the test may get killed by the OOM killer at the
        //       time the memory is accessed which triggers the full memory allocation.
        [Fact]
        [OuterLoop]
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.OSX)]
        unsafe static void ClearLongerThanUintMaxValueBytes()
        {
            if (sizeof(IntPtr) == sizeof(long))
            {
                // Arrange
                IntPtr bytes = (IntPtr)(((long)int.MaxValue) * sizeof(int));
                int length = (int)(((long)bytes) / sizeof(int));

                if (!AllocationHelper.TryAllocNative(bytes, out IntPtr memory))
                {
                    Console.WriteLine($"Span.Clear test {nameof(ClearLongerThanUintMaxValueBytes)} skipped (could not alloc memory).");
                    return;
                }

                try
                {
                    ref int data = ref Unsafe.AsRef<int>(memory.ToPointer());

                    int initial = 5;
                    for (int i = 0; i < length; i++)
                    {
                        Unsafe.Add(ref data, i) = initial;
                    }

                    Span<int> span = new Span<int>(memory.ToPointer(), length);

                    // Act
                    span.Clear();

                    // Assert using custom code for perf and to avoid allocating extra memory
                    for (int i = 0; i < length; i++)
                    {
                        var actual = Unsafe.Add(ref data, i);
                        if (actual != 0)
                        {
                            Assert.Equal(0, actual);
                        }
                    }
                }
                finally
                {
                    AllocationHelper.ReleaseNative(ref memory);
                }
            }
        }
    }
}
