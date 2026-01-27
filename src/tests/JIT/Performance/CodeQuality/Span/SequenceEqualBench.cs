// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Xunit;

#pragma warning disable xUnit1013

namespace Span
{
    /// <summary>
    /// Benchmarks for validating SequenceEqual performance including the fast path
    /// when comparing a span to itself (Unsafe.AreSame optimization).
    ///
    /// To run these benchmarks:
    /// 1. Build the runtime with ./build.sh clr+libs -rc release
    /// 2. Navigate to this file and run through the test runner
    ///
    /// The benchmarks compare three scenarios:
    /// - Same reference (fast path with Unsafe.AreSame)
    /// - Different references, same content (full comparison)
    /// - Different references, different content (early exit on mismatch)
    /// </summary>
    public class SequenceEqualBench
    {
        const int Iterations = 1_000_000;

        // Copying the result of a computation to Sink<T>.Instance is a way
        // to prevent the jit from considering the computation dead and removing it.
        private sealed class Sink<T>
        {
            public T Data;
            public static Sink<T> Instance = new Sink<T>();
        }

        // Test data classes to prevent JIT from folding values
        private static class ByteTestData
        {
            public static readonly byte[] SmallArray = CreateByteArray(16);
            public static readonly byte[] MediumArray = CreateByteArray(256);
            public static readonly byte[] LargeArray = CreateByteArray(4096);

            public static readonly byte[] SmallArrayCopy = CreateByteArray(16);
            public static readonly byte[] MediumArrayCopy = CreateByteArray(256);
            public static readonly byte[] LargeArrayCopy = CreateByteArray(4096);

            public static readonly byte[] SmallArrayDifferent = CreateDifferentByteArray(16);
            public static readonly byte[] MediumArrayDifferent = CreateDifferentByteArray(256);
            public static readonly byte[] LargeArrayDifferent = CreateDifferentByteArray(4096);

            private static byte[] CreateByteArray(int length)
            {
                var arr = new byte[length];
                for (int i = 0; i < length; i++)
                    arr[i] = (byte)(i & 0xFF);
                return arr;
            }

            private static byte[] CreateDifferentByteArray(int length)
            {
                var arr = new byte[length];
                for (int i = 0; i < length; i++)
                    arr[i] = (byte)((length - i) & 0xFF);
                return arr;
            }
        }

        private static class CharTestData
        {
            public static readonly char[] SmallArray = CreateCharArray(16);
            public static readonly char[] MediumArray = CreateCharArray(256);
            public static readonly char[] LargeArray = CreateCharArray(4096);

            public static readonly char[] SmallArrayCopy = CreateCharArray(16);
            public static readonly char[] MediumArrayCopy = CreateCharArray(256);
            public static readonly char[] LargeArrayCopy = CreateCharArray(4096);

            public static readonly char[] SmallArrayDifferent = CreateDifferentCharArray(16);
            public static readonly char[] MediumArrayDifferent = CreateDifferentCharArray(256);
            public static readonly char[] LargeArrayDifferent = CreateDifferentCharArray(4096);

            private static char[] CreateCharArray(int length)
            {
                var arr = new char[length];
                for (int i = 0; i < length; i++)
                    arr[i] = (char)('A' + (i % 26));
                return arr;
            }

            private static char[] CreateDifferentCharArray(int length)
            {
                var arr = new char[length];
                for (int i = 0; i < length; i++)
                    arr[i] = (char)('Z' - (i % 26));
                return arr;
            }
        }

        #region Byte SequenceEqual - Same Reference (Fast Path)

        [Fact]
        public static void TestSequenceEqualByte_SameReference_Small()
        {
            var span = ByteTestData.SmallArray.AsSpan();
            var sink = Sink<bool>.Instance;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < Iterations; i++)
            {
                sink.Data = SequenceEqualSameReference(span);
            }

            sw.Stop();
            Console.WriteLine($"SequenceEqual byte (same ref, 16 bytes): {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
            Assert.True(sink.Data);
        }

        [Fact]
        public static void TestSequenceEqualByte_SameReference_Medium()
        {
            var span = ByteTestData.MediumArray.AsSpan();
            var sink = Sink<bool>.Instance;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < Iterations; i++)
            {
                sink.Data = SequenceEqualSameReference(span);
            }

            sw.Stop();
            Console.WriteLine($"SequenceEqual byte (same ref, 256 bytes): {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
            Assert.True(sink.Data);
        }

        [Fact]
        public static void TestSequenceEqualByte_SameReference_Large()
        {
            var span = ByteTestData.LargeArray.AsSpan();
            var sink = Sink<bool>.Instance;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < Iterations; i++)
            {
                sink.Data = SequenceEqualSameReference(span);
            }

            sw.Stop();
            Console.WriteLine($"SequenceEqual byte (same ref, 4096 bytes): {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
            Assert.True(sink.Data);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool SequenceEqualSameReference(ReadOnlySpan<byte> span)
        {
            return span.SequenceEqual(span);
        }

        #endregion

        #region Byte SequenceEqual - Different Reference, Same Content

        [Fact]
        public static void TestSequenceEqualByte_DiffRef_SameContent_Small()
        {
            var span1 = ByteTestData.SmallArray.AsSpan();
            var span2 = ByteTestData.SmallArrayCopy.AsSpan();
            var sink = Sink<bool>.Instance;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < Iterations; i++)
            {
                sink.Data = SequenceEqualDifferentRef(span1, span2);
            }

            sw.Stop();
            Console.WriteLine($"SequenceEqual byte (diff ref, same content, 16 bytes): {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
            Assert.True(sink.Data);
        }

        [Fact]
        public static void TestSequenceEqualByte_DiffRef_SameContent_Medium()
        {
            var span1 = ByteTestData.MediumArray.AsSpan();
            var span2 = ByteTestData.MediumArrayCopy.AsSpan();
            var sink = Sink<bool>.Instance;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < Iterations; i++)
            {
                sink.Data = SequenceEqualDifferentRef(span1, span2);
            }

            sw.Stop();
            Console.WriteLine($"SequenceEqual byte (diff ref, same content, 256 bytes): {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
            Assert.True(sink.Data);
        }

        [Fact]
        public static void TestSequenceEqualByte_DiffRef_SameContent_Large()
        {
            var span1 = ByteTestData.LargeArray.AsSpan();
            var span2 = ByteTestData.LargeArrayCopy.AsSpan();
            var sink = Sink<bool>.Instance;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < Iterations; i++)
            {
                sink.Data = SequenceEqualDifferentRef(span1, span2);
            }

            sw.Stop();
            Console.WriteLine($"SequenceEqual byte (diff ref, same content, 4096 bytes): {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
            Assert.True(sink.Data);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool SequenceEqualDifferentRef(ReadOnlySpan<byte> span1, ReadOnlySpan<byte> span2)
        {
            return span1.SequenceEqual(span2);
        }

        #endregion

        #region Byte SequenceEqual - Different Reference, Different Content

        [Fact]
        public static void TestSequenceEqualByte_DiffRef_DiffContent_Small()
        {
            var span1 = ByteTestData.SmallArray.AsSpan();
            var span2 = ByteTestData.SmallArrayDifferent.AsSpan();
            var sink = Sink<bool>.Instance;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < Iterations; i++)
            {
                sink.Data = SequenceEqualDifferentRef(span1, span2);
            }

            sw.Stop();
            Console.WriteLine($"SequenceEqual byte (diff ref, diff content, 16 bytes): {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
            Assert.False(sink.Data);
        }

        [Fact]
        public static void TestSequenceEqualByte_DiffRef_DiffContent_Medium()
        {
            var span1 = ByteTestData.MediumArray.AsSpan();
            var span2 = ByteTestData.MediumArrayDifferent.AsSpan();
            var sink = Sink<bool>.Instance;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < Iterations; i++)
            {
                sink.Data = SequenceEqualDifferentRef(span1, span2);
            }

            sw.Stop();
            Console.WriteLine($"SequenceEqual byte (diff ref, diff content, 256 bytes): {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
            Assert.False(sink.Data);
        }

        [Fact]
        public static void TestSequenceEqualByte_DiffRef_DiffContent_Large()
        {
            var span1 = ByteTestData.LargeArray.AsSpan();
            var span2 = ByteTestData.LargeArrayDifferent.AsSpan();
            var sink = Sink<bool>.Instance;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < Iterations; i++)
            {
                sink.Data = SequenceEqualDifferentRef(span1, span2);
            }

            sw.Stop();
            Console.WriteLine($"SequenceEqual byte (diff ref, diff content, 4096 bytes): {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
            Assert.False(sink.Data);
        }

        #endregion

        #region Char SequenceEqual - Same Reference (Fast Path)

        [Fact]
        public static void TestSequenceEqualChar_SameReference_Small()
        {
            var span = CharTestData.SmallArray.AsSpan();
            var sink = Sink<bool>.Instance;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < Iterations; i++)
            {
                sink.Data = SequenceEqualCharSameReference(span);
            }

            sw.Stop();
            Console.WriteLine($"SequenceEqual char (same ref, 16 chars): {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
            Assert.True(sink.Data);
        }

        [Fact]
        public static void TestSequenceEqualChar_SameReference_Medium()
        {
            var span = CharTestData.MediumArray.AsSpan();
            var sink = Sink<bool>.Instance;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < Iterations; i++)
            {
                sink.Data = SequenceEqualCharSameReference(span);
            }

            sw.Stop();
            Console.WriteLine($"SequenceEqual char (same ref, 256 chars): {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
            Assert.True(sink.Data);
        }

        [Fact]
        public static void TestSequenceEqualChar_SameReference_Large()
        {
            var span = CharTestData.LargeArray.AsSpan();
            var sink = Sink<bool>.Instance;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < Iterations; i++)
            {
                sink.Data = SequenceEqualCharSameReference(span);
            }

            sw.Stop();
            Console.WriteLine($"SequenceEqual char (same ref, 4096 chars): {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
            Assert.True(sink.Data);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool SequenceEqualCharSameReference(ReadOnlySpan<char> span)
        {
            return span.SequenceEqual(span);
        }

        #endregion

        #region Char SequenceEqual - Different Reference, Same Content

        [Fact]
        public static void TestSequenceEqualChar_DiffRef_SameContent_Small()
        {
            var span1 = CharTestData.SmallArray.AsSpan();
            var span2 = CharTestData.SmallArrayCopy.AsSpan();
            var sink = Sink<bool>.Instance;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < Iterations; i++)
            {
                sink.Data = SequenceEqualCharDifferentRef(span1, span2);
            }

            sw.Stop();
            Console.WriteLine($"SequenceEqual char (diff ref, same content, 16 chars): {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
            Assert.True(sink.Data);
        }

        [Fact]
        public static void TestSequenceEqualChar_DiffRef_SameContent_Medium()
        {
            var span1 = CharTestData.MediumArray.AsSpan();
            var span2 = CharTestData.MediumArrayCopy.AsSpan();
            var sink = Sink<bool>.Instance;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < Iterations; i++)
            {
                sink.Data = SequenceEqualCharDifferentRef(span1, span2);
            }

            sw.Stop();
            Console.WriteLine($"SequenceEqual char (diff ref, same content, 256 chars): {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
            Assert.True(sink.Data);
        }

        [Fact]
        public static void TestSequenceEqualChar_DiffRef_SameContent_Large()
        {
            var span1 = CharTestData.LargeArray.AsSpan();
            var span2 = CharTestData.LargeArrayCopy.AsSpan();
            var sink = Sink<bool>.Instance;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < Iterations; i++)
            {
                sink.Data = SequenceEqualCharDifferentRef(span1, span2);
            }

            sw.Stop();
            Console.WriteLine($"SequenceEqual char (diff ref, same content, 4096 chars): {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
            Assert.True(sink.Data);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool SequenceEqualCharDifferentRef(ReadOnlySpan<char> span1, ReadOnlySpan<char> span2)
        {
            return span1.SequenceEqual(span2);
        }

        #endregion

        #region Char SequenceEqual - Different Reference, Different Content

        [Fact]
        public static void TestSequenceEqualChar_DiffRef_DiffContent_Small()
        {
            var span1 = CharTestData.SmallArray.AsSpan();
            var span2 = CharTestData.SmallArrayDifferent.AsSpan();
            var sink = Sink<bool>.Instance;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < Iterations; i++)
            {
                sink.Data = SequenceEqualCharDifferentRef(span1, span2);
            }

            sw.Stop();
            Console.WriteLine($"SequenceEqual char (diff ref, diff content, 16 chars): {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
            Assert.False(sink.Data);
        }

        [Fact]
        public static void TestSequenceEqualChar_DiffRef_DiffContent_Medium()
        {
            var span1 = CharTestData.MediumArray.AsSpan();
            var span2 = CharTestData.MediumArrayDifferent.AsSpan();
            var sink = Sink<bool>.Instance;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < Iterations; i++)
            {
                sink.Data = SequenceEqualCharDifferentRef(span1, span2);
            }

            sw.Stop();
            Console.WriteLine($"SequenceEqual char (diff ref, diff content, 256 chars): {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
            Assert.False(sink.Data);
        }

        [Fact]
        public static void TestSequenceEqualChar_DiffRef_DiffContent_Large()
        {
            var span1 = CharTestData.LargeArray.AsSpan();
            var span2 = CharTestData.LargeArrayDifferent.AsSpan();
            var sink = Sink<bool>.Instance;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < Iterations; i++)
            {
                sink.Data = SequenceEqualCharDifferentRef(span1, span2);
            }

            sw.Stop();
            Console.WriteLine($"SequenceEqual char (diff ref, diff content, 4096 chars): {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
            Assert.False(sink.Data);
        }

        #endregion

        /// <summary>
        /// Entry point when running as a console application.
        /// </summary>
        public static int Main()
        {
            Console.WriteLine("SequenceEqual Performance Benchmarks");
            Console.WriteLine("====================================");
            Console.WriteLine();

            Console.WriteLine("Byte Benchmarks - Same Reference (Fast Path):");
            TestSequenceEqualByte_SameReference_Small();
            TestSequenceEqualByte_SameReference_Medium();
            TestSequenceEqualByte_SameReference_Large();
            Console.WriteLine();

            Console.WriteLine("Byte Benchmarks - Different Reference, Same Content:");
            TestSequenceEqualByte_DiffRef_SameContent_Small();
            TestSequenceEqualByte_DiffRef_SameContent_Medium();
            TestSequenceEqualByte_DiffRef_SameContent_Large();
            Console.WriteLine();

            Console.WriteLine("Byte Benchmarks - Different Reference, Different Content:");
            TestSequenceEqualByte_DiffRef_DiffContent_Small();
            TestSequenceEqualByte_DiffRef_DiffContent_Medium();
            TestSequenceEqualByte_DiffRef_DiffContent_Large();
            Console.WriteLine();

            Console.WriteLine("Char Benchmarks - Same Reference (Fast Path):");
            TestSequenceEqualChar_SameReference_Small();
            TestSequenceEqualChar_SameReference_Medium();
            TestSequenceEqualChar_SameReference_Large();
            Console.WriteLine();

            Console.WriteLine("Char Benchmarks - Different Reference, Same Content:");
            TestSequenceEqualChar_DiffRef_SameContent_Small();
            TestSequenceEqualChar_DiffRef_SameContent_Medium();
            TestSequenceEqualChar_DiffRef_SameContent_Large();
            Console.WriteLine();

            Console.WriteLine("Char Benchmarks - Different Reference, Different Content:");
            TestSequenceEqualChar_DiffRef_DiffContent_Small();
            TestSequenceEqualChar_DiffRef_DiffContent_Medium();
            TestSequenceEqualChar_DiffRef_DiffContent_Large();
            Console.WriteLine();

            Console.WriteLine("All benchmarks completed successfully.");
            return 100;
        }
    }
}
