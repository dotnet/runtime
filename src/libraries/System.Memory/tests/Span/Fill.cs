// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
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

        [Fact]
        public static void FillWithRecognizedType()
        {
            RunTest<sbyte>(0x20);
            RunTest<byte>(0x20);
            RunTest<bool>(true);
            RunTest<short>(0x1234);
            RunTest<ushort>(0x1234);
            RunTest<char>('x');
            RunTest<int>(0x12345678);
            RunTest<uint>(0x12345678);
            RunTest<long>(0x0123456789abcdef);
            RunTest<ulong>(0x0123456789abcdef);
            RunTest<nint>(unchecked((nint)0x0123456789abcdef));
            RunTest<nuint>(unchecked((nuint)0x0123456789abcdef));
            RunTest<Half>((Half)1.0);
            RunTest<float>(1.0f);
            RunTest<double>(1.0);
            RunTest<StringComparison>(StringComparison.CurrentCultureIgnoreCase); // should be treated as underlying primitive
            RunTest<string>("Hello world!"); // ref type, no SIMD
            RunTest<decimal>(1.0m); // 128-bit struct
            RunTest<Guid>(new Guid("29e07627-2481-4f43-8fbf-09cf21180239")); // 128-bit struct
            RunTest<My96BitStruct>(new(0x11111111, 0x22222222, 0x33333333)); // 96-bit struct, no SIMD
            RunTest<My256BitStruct>(new(0x1111111111111111, 0x2222222222222222, 0x3333333333333333, 0x4444444444444444));
            RunTest<My512BitStruct>(new(
                0x1111111111111111, 0x2222222222222222, 0x3333333333333333, 0x4444444444444444,
                0x5555555555555555, 0x6666666666666666, 0x7777777777777777, 0x8888888888888888)); // 512-bit struct, no SIMD
            RunTest<MyRefContainingStruct>(new("Hello world!")); // struct contains refs, no SIMD

            static void RunTest<T>(T value)
            {
                T[] arr = new T[128];

                // Run tests for lengths := 0 to 64, ensuring we don't overrun our buffer

                for (int i = 0; i <= 64; i++)
                {
                    arr.AsSpan(0, i).Fill(value);
                    Assert.Equal(Enumerable.Repeat(value, i), arr.Take(i)); // first i entries should've been populated with 'value'
                    Assert.Equal(Enumerable.Repeat(default(T), arr.Length - i), arr.Skip(i)); // remaining entries should contain default(T)
                    Array.Clear(arr);
                }
            }
        }

        private readonly struct My96BitStruct
        {
            public My96BitStruct(int data0, int data1, int data2)
            {
                Data0 = data0;
                Data1 = data1;
                Data2 = data2;
            }

            public readonly int Data0;
            public readonly int Data1;
            public readonly int Data2;
        }

        private readonly struct My256BitStruct
        {
            public My256BitStruct(ulong data0, ulong data1, ulong data2, ulong data3)
            {
                Data0 = data0;
                Data1 = data1;
                Data2 = data2;
                Data3 = data3;
            }

            public readonly ulong Data0;
            public readonly ulong Data1;
            public readonly ulong Data2;
            public readonly ulong Data3;
        }

        private readonly struct My512BitStruct
        {
            public My512BitStruct(ulong data0, ulong data1, ulong data2, ulong data3, ulong data4, ulong data5, ulong data6, ulong data7)
            {
                Data0 = data0;
                Data1 = data1;
                Data2 = data2;
                Data3 = data3;
                Data4 = data4;
                Data5 = data5;
                Data6 = data6;
                Data7 = data7;
            }

            public readonly ulong Data0;
            public readonly ulong Data1;
            public readonly ulong Data2;
            public readonly ulong Data3;
            public readonly ulong Data4;
            public readonly ulong Data5;
            public readonly ulong Data6;
            public readonly ulong Data7;
        }

        private readonly struct MyRefContainingStruct
        {
            public MyRefContainingStruct(object data)
            {
                Data = data;
            }

            public readonly object Data;
        }
    }
}
