// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void ZeroLengthCount_Byte()
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(Array.Empty<byte>());

            int count = span.Count<byte>(0);
            Assert.Equal(0, count);
        }
        
        [Fact]
        public static void ZeroLengthCount_RosByte()
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(Array.Empty<byte>());

            int count = span.Count<byte>(new ReadOnlySpan<byte>(new byte[] { 0, 0 }));
            Assert.Equal(0, count);
        }
        
        [Fact]
        public static void DefaultFilledCount_Byte()
        {
            for (int length = 0; length <= byte.MaxValue; length++)
            {
                byte[] a = new byte[length];
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a);

                for (int i = 0; i < length; i++)
                {
                    byte target0 = default;

                    int count = span.Count(target0);
                    Assert.Equal(length,  count);
                }
            }
        }
        
        [Fact]
        public static void DefaultFilledCount_RosByte()
        {
            for (int length = 0; length <= byte.MaxValue; length++)
            {
                byte[] a = new byte[length];
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a);

                for (int i = 0; i < length; i++)
                {
                    ReadOnlySpan<byte> target0 = new byte[] { 0, 0 };

                    int count = span.Count(target0);
                    Assert.Equal(length / 2,  count);
                }
            }
        }
        
        [Fact]
        public static void TestCount_Byte()
        {
            for (int length = 0; length <= byte.MaxValue; length++)
            {
                byte[] a = new byte[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = (byte)(i + 1);
                }
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a);

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    byte target = a[targetIndex];

                    int count = span.Count(target);
                    Assert.Equal(1, count);
                }
            }
        }
        
        [Fact]
        public static void TestCount_RosByte()
        {
            for (int length = 0; length <= byte.MaxValue; length++)
            {
                byte[] a = new byte[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = (byte)(i + 1);
                }
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a);

                for (int targetIndex = 0; targetIndex < length - 1; targetIndex++)
                {
                    ReadOnlySpan<byte> target = stackalloc byte[] { a[targetIndex], a[targetIndex + 1] };

                    int count = span.Count(target);
                    Assert.Equal(1, count);
                }
            }
        }

        [Fact]
        public static void TestNotCount_Byte()
        {
            var rnd = new Random(42);
            for (int length = 0; length <= byte.MaxValue; length++)
            {
                byte[] a = new byte[length];
                byte target = (byte)rnd.Next(0, 256);
                for (int i = 0; i < length; i++)
                {
                    byte val = (byte)(i + 1);
                    a[i] = val == target ? (byte)(target + 1) : val;
                }
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a);

                int count = span.Count(target);
                Assert.Equal(0, count);
            }
        }
        

        [Fact]
        public static void TestNotCount_RosByte()
        {
            var rnd = new Random(42);
            for (int length = 0; length <= byte.MaxValue; length++)
            {
                byte[] a = new byte[length];
                byte targetVal = (byte)rnd.Next(0, 256);
                ReadOnlySpan<byte> target = new byte[] { targetVal, 0 };
                for (int i = 0; i < length; i++)
                {
                    byte val = (byte)(i + 1);
                    a[i] = val == targetVal ? (byte)(targetVal + 1) : val;
                }
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a);

                int count = span.Count(target);
                Assert.Equal(0, count);
            }
        }
        
        [Fact]
        public static void TestAlignmentNotCount_Byte()
        {
            byte[] array = new byte[4 * Vector<byte>.Count];
            for (var i = 0; i < Vector<byte>.Count; i++)
            {
                var span = new ReadOnlySpan<byte>(array, i, 3 * Vector<byte>.Count);

                int count = span.Count((byte)'1');
                Assert.Equal(0, count);

                span = new ReadOnlySpan<byte>(array, i, 3 * Vector<byte>.Count - 3);

                count = span.Count((byte)'1');
                Assert.Equal(0, count);
            }
        }

        [Fact]
        public static void TestAlignmentNotCount_RosByte()
        {
            byte[] array = new byte[4 * Vector<byte>.Count];
            for (var i = 0; i < Vector<byte>.Count; i++)
            {
                var span = new ReadOnlySpan<byte>(array, i, 3 * Vector<byte>.Count);
                ReadOnlySpan<byte> target = new byte[] { 1, 0 };

                int count = span.Count(target);
                Assert.Equal(0, count);

                span = new ReadOnlySpan<byte>(array, i, 3 * Vector<byte>.Count - 3);

                count = span.Count(target);
                Assert.Equal(0, count);
            }
        }

        [Fact]
        public static void TestAlignmentCount_Byte()
        {
            byte[] array = new byte[4 * Vector<byte>.Count];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = 5;
            }
            for (var i = 0; i < Vector<byte>.Count; i++)
            {
                var span = new ReadOnlySpan<byte>(array, i, 3 * Vector<byte>.Count);

                int count = span.Count<byte>(5);
                Assert.Equal(span.Length, count);

                span = new ReadOnlySpan<byte>(array, i, 3 * Vector<byte>.Count - 3);

                count = span.Count<byte>(5);
                Assert.Equal(span.Length, count);
            }
        }
        
        [Fact]
        public static void TestAlignmentCount_RosByte()
        {
            byte[] array = new byte[4 * Vector<byte>.Count];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = 5;
            }
            for (var i = 0; i < Vector<byte>.Count; i++)
            {
                var span = new ReadOnlySpan<byte>(array, i, 3 * Vector<byte>.Count);
                ReadOnlySpan<byte> target = new byte[] { 5, 5 };

                int count = span.Count<byte>(target);
                Assert.Equal(span.Length / 2, count);

                span = new ReadOnlySpan<byte>(array, i, 3 * Vector<byte>.Count - 3);

                count = span.Count<byte>(target);
                Assert.Equal(span.Length / 2, count);
            }
        }

        [Fact]
        public static void TestMultipleCount_Byte()
        {
            for (int length = 2; length <= byte.MaxValue; length++)
            {
                byte[] a = new byte[length];
                for (int i = 0; i < length; i++)
                {
                    byte val = (byte)(i + 1);
                    a[i] = val == 200 ? (byte)201 : val;
                }

                a[length - 1] = 200;
                a[length - 2] = 200;

                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a);

                int count = span.Count<byte>(200);
                Assert.Equal(2, count);
            }
        }
        
        [Fact]
        public static void TestMultipleCount_RosByte()
        {
            for (int length = 4; length <= byte.MaxValue; length++)
            {
                byte[] a = new byte[length];
                for (int i = 0; i < length; i++)
                {
                    byte val = (byte)(i + 1);
                    a[i] = val == 200 ? (byte)201 : val;
                }
                a[0] = 200;
                a[1] = 200;
                a[length - 1] = 200;
                a[length - 2] = 200;

                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a);
                ReadOnlySpan<byte> target = new byte[] { 200, 200 };

                int count = span.Count<byte>(target);
                Assert.Equal(2, count);
            }
        }

        [Fact]
        public static void MakeSureNoCountChecksGoOutOfRange_Byte()
        {
            for (int length = 0; length <= byte.MaxValue; length++)
            {
                byte[] a = new byte[length + 2];
                a[0] = 99;
                a[^1] = 99;
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a, 1, length);

                int count = span.Count<byte>(99);
                Assert.Equal(0, count);
            }
        }

        [Fact]
        public static void MakeSureNoCountChecksGoOutOfRange_RosByte()
        {
            for (int length = 0; length <= byte.MaxValue; length++)
            {
                byte[] a = new byte[length + 4];
                a[0] = 99;
                a[1] = 99;
                a[^1] = 99;
                a[^2] = 99;

                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a, 2, length);
                ReadOnlySpan<byte> target = new byte[] { 99, 99 };

                int count = span.Count<byte>(target);
                Assert.Equal(0, count);
            }
        }
    }
}
