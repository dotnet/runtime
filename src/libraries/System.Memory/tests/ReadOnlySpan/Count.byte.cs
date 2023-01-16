// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
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

            Assert.Equal(0, span.Count<byte>(0));
        }
        
        [Fact]
        public static void ZeroLengthCount_RosByte()
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(Array.Empty<byte>());

            Assert.Equal(0, span.Count<byte>(new ReadOnlySpan<byte>(new byte[] { 0, 0 })));
        }
        
        [Fact]
        public static void DefaultFilledCount_Byte()
        {
            int[] lengths = new int[] { 0, 1, 7, 8, 9, 15, 16, 17, 31, 32, 33, 255, 256 };
            foreach (int length in lengths)
            {
                byte[] a = new byte[length];

                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a);
                Assert.Equal(length, span.Count(0));
            }
        }
        
        [Fact]
        public static void DefaultFilledCount_RosByte()
        {
            int[] lengths = new int[] { 0, 1, 7, 8, 9, 15, 16, 17, 31, 32, 33, 255, 256 };
            foreach (int length in lengths)
            {
                byte[] a = new byte[length];

                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a);
                ReadOnlySpan<byte> target0 = new byte[] { 0, 0 };
                Assert.Equal(length / 2,  span.Count(target0));
            }
        }
        
        [Fact]
        public static void TestCount_Byte()
        {
            int[] lengths = new int[] { 0, 1, 7, 8, 9, 15, 16, 17, 31, 32, 33, 255, 256 };
            foreach (int length in lengths)
            {
                byte[] a = Enumerable.Range(1, length).Select(i => (byte)i).ToArray();
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a);

                foreach (byte target in span)
                {
                    Assert.Equal(1, span.Count(target));
                }
            }
        }
        
        [Fact]
        public static void TestCount_RosByte()
        {
            int[] lengths = new int[] { 0, 1, 7, 8, 9, 15, 16, 17, 31, 32, 33, 255, 256 };
            foreach (int length in lengths)
            {
                byte[] a = Enumerable.Range(1, length).Select(i => (byte)i).ToArray();
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a);

                for (int targetIndex = 0; targetIndex < length - 1; targetIndex++)
                {
                    ReadOnlySpan<byte> target = new byte[] { a[targetIndex], a[targetIndex + 1] };
                    Assert.Equal(1, span.Count(target));
                }
            }
        }

        [Fact]
        public static void TestNotCount_Byte()
        {
            var rnd = new Random(42);
            int[] lengths = new int[] { 0, 1, 7, 8, 9, 15, 16, 17, 31, 32, 33, 255, 256 };
            foreach (int length in lengths)
            {
                byte[] a = new byte[length];
                byte target = (byte)rnd.Next(0, 256);
                for (int i = 0; i < length; i++)
                {
                    byte val = (byte)(i + 1);
                    a[i] = val == target ? (byte)(target + 1) : val;
                }

                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a);
                Assert.Equal(0, span.Count(target));
            }
        }

        [Fact]
        public static void TestNotCount_RosByte()
        {
            var rnd = new Random(42);
            int[] lengths = new int[] { 0, 1, 7, 8, 9, 15, 16, 17, 31, 32, 33, 255, 256 };
            foreach (int length in lengths)
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
                Assert.Equal(0, span.Count(target));
            }
        }
        
        [Fact]
        public static void TestAlignmentNotCount_Byte()
        {
            byte[] array = new byte[4 * Vector<byte>.Count];
            for (var i = 0; i < Vector<byte>.Count; i++)
            {
                var span = new ReadOnlySpan<byte>(array, i, 3 * Vector<byte>.Count);
                Assert.Equal(0, span.Count((byte)'1'));

                span = new ReadOnlySpan<byte>(array, i, 3 * Vector<byte>.Count - 3);
                Assert.Equal(0, span.Count((byte)'1'));
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
                Assert.Equal(0, span.Count(target));

                span = new ReadOnlySpan<byte>(array, i, 3 * Vector<byte>.Count - 3);
                Assert.Equal(0, span.Count(target));
            }
        }

        [Fact]
        public static void TestAlignmentCount_Byte()
        {
            byte[] array = new byte[4 * Vector<byte>.Count];
            Array.Fill(array, (byte)5);
            for (var i = 0; i < Vector<byte>.Count; i++)
            {
                var span = new ReadOnlySpan<byte>(array, i, 3 * Vector<byte>.Count);
                Assert.Equal(span.Length, span.Count<byte>(5));

                span = new ReadOnlySpan<byte>(array, i, 3 * Vector<byte>.Count - 3);
                Assert.Equal(span.Length, span.Count<byte>(5));
            }
        }
        
        [Fact]
        public static void TestAlignmentCount_RosByte()
        {
            byte[] array = new byte[4 * Vector<byte>.Count];
            Array.Fill(array, (byte)5);
            for (var i = 0; i < Vector<byte>.Count; i++)
            {
                var span = new ReadOnlySpan<byte>(array, i, 3 * Vector<byte>.Count);
                ReadOnlySpan<byte> target = new byte[] { 5, 5 };
                Assert.Equal(span.Length / 2, span.Count<byte>(target));

                span = new ReadOnlySpan<byte>(array, i, 3 * Vector<byte>.Count - 3);
                Assert.Equal(span.Length / 2, span.Count<byte>(target));
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
                Assert.Equal(2, span.Count<byte>(200));
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
                Assert.Equal(2, span.Count<byte>(target));
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
                Assert.Equal(0, span.Count<byte>(99));
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
                Assert.Equal(0, span.Count<byte>(target));
            }
        }
    }
}
