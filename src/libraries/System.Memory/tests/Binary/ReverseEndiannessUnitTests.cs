// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Buffers.Binary.Tests
{
    public class ReverseEndiannessUnitTests
    {
        [Fact]
        public void ReverseEndianness_ByteAndSByte_DoesNothing()
        {
            byte valueMax = byte.MaxValue;
            byte valueMin = byte.MinValue;
            sbyte signedValueMax = sbyte.MaxValue;
            sbyte signedValueMin = sbyte.MinValue;

            Assert.Equal(valueMax, BinaryPrimitives.ReverseEndianness(valueMax));
            Assert.Equal(valueMin, BinaryPrimitives.ReverseEndianness(valueMin));
            Assert.Equal(signedValueMax, BinaryPrimitives.ReverseEndianness(signedValueMax));
            Assert.Equal(signedValueMin, BinaryPrimitives.ReverseEndianness(signedValueMin));
        }

        [Theory]
        [InlineData(0x0123, 0x2301)]
        [InlineData(0xABCD, 0xCDAB)]
        public void ReverseEndianness_Int16AndUInt16(ushort a, ushort b)
        {
            Assert.Equal((short)b, BinaryPrimitives.ReverseEndianness((short)a));
            Assert.Equal((short)a, BinaryPrimitives.ReverseEndianness((short)b));
            Assert.Equal(b, BinaryPrimitives.ReverseEndianness(a));
            Assert.Equal(a, BinaryPrimitives.ReverseEndianness(b));
        }

        [Theory]
        [InlineData(0x01234567, 0x67452301)]
        [InlineData(0xABCDEF01, 0x01EFCDAB)]
        public void ReverseEndianness_Int32AndUInt32(uint a, uint b)
        {
            Assert.Equal((int)b, BinaryPrimitives.ReverseEndianness((int)a));
            Assert.Equal((int)a, BinaryPrimitives.ReverseEndianness((int)b));
            Assert.Equal(b, BinaryPrimitives.ReverseEndianness(a));
            Assert.Equal(a, BinaryPrimitives.ReverseEndianness(b));
            if (IntPtr.Size == 4)
            {
                Assert.Equal((nint)b, BinaryPrimitives.ReverseEndianness((nint)a));
                Assert.Equal((nint)a, BinaryPrimitives.ReverseEndianness((nint)b));
                Assert.Equal((nuint)b, BinaryPrimitives.ReverseEndianness((nuint)a));
                Assert.Equal((nuint)a, BinaryPrimitives.ReverseEndianness((nuint)b));
            }
        }

        [Theory]
        [InlineData(0x0123456789ABCDEF, 0xEFCDAB8967452301)]
        [InlineData(0xABCDEF0123456789, 0x8967452301EFCDAB)]
        public void ReverseEndianness_Int64AndUInt64(ulong a, ulong b)
        {
            Assert.Equal((long)b, BinaryPrimitives.ReverseEndianness((long)a));
            Assert.Equal((long)a, BinaryPrimitives.ReverseEndianness((long)b));
            Assert.Equal(b, BinaryPrimitives.ReverseEndianness(a));
            Assert.Equal(a, BinaryPrimitives.ReverseEndianness(b));
            if (IntPtr.Size == 8)
            {
                Assert.Equal((nint)b, BinaryPrimitives.ReverseEndianness((nint)a));
                Assert.Equal((nint)a, BinaryPrimitives.ReverseEndianness((nint)b));
                Assert.Equal((nuint)b, BinaryPrimitives.ReverseEndianness((nuint)a));
                Assert.Equal((nuint)a, BinaryPrimitives.ReverseEndianness((nuint)b));
            }
        }

        [Theory]
        [InlineData(0x0123456789ABCDEF, 0xABCDEF0123456789)]
        public void ReverseEndianness_Int128AndUInt128(ulong aLower, ulong aUpper)
        {
            Int128 original = new Int128(aLower, aUpper);
            Int128 reversed = new Int128(BinaryPrimitives.ReverseEndianness(aUpper), BinaryPrimitives.ReverseEndianness(aLower));

            Assert.Equal(reversed, BinaryPrimitives.ReverseEndianness(original));
            Assert.Equal((UInt128)reversed, BinaryPrimitives.ReverseEndianness((UInt128)original));
        }

        public static IEnumerable<object[]> ReverseEndianness_Span_MemberData()
        {
            var r = new Random(42);
            foreach (int length in Enumerable.Range(0, 36))
            {
                yield return new object[] { Enumerable.Range(0, length).Select(_ => (ushort)r.Next(int.MinValue, int.MaxValue)).ToArray() };
                yield return new object[] { Enumerable.Range(0, length).Select(_ => (short)r.Next(int.MinValue, int.MaxValue)).ToArray() };
                yield return new object[] { Enumerable.Range(0, length).Select(_ => (uint)r.Next(int.MinValue, int.MaxValue)).ToArray() };
                yield return new object[] { Enumerable.Range(0, length).Select(_ => r.Next(int.MinValue, int.MaxValue)).ToArray() };
                yield return new object[] { Enumerable.Range(0, length).Select(_ => (ulong)r.NextInt64(long.MinValue, long.MaxValue)).ToArray() };
                yield return new object[] { Enumerable.Range(0, length).Select(_ => r.NextInt64(long.MinValue, long.MaxValue)).ToArray() };
                yield return new object[] { Enumerable.Range(0, length).Select(_ => (nuint)r.NextInt64(long.MinValue, long.MaxValue)).ToArray() };
                yield return new object[] { Enumerable.Range(0, length).Select(_ => (nint)r.NextInt64(long.MinValue, long.MaxValue)).ToArray() };
                yield return new object[] { Enumerable.Range(0, length).Select(_ => new UInt128((ulong)r.NextInt64(long.MinValue, long.MaxValue), (ulong)r.NextInt64(long.MinValue, long.MaxValue))).ToArray() };
                yield return new object[] { Enumerable.Range(0, length).Select(_ => new Int128((ulong)r.NextInt64(long.MinValue, long.MaxValue), (ulong)r.NextInt64(long.MinValue, long.MaxValue))).ToArray() };
            }
        }

        [Theory]
        [MemberData(nameof(ReverseEndianness_Span_MemberData))]
        public void ReverseEndianness_Span_AllElementsReversed<T>(T[] original) where T : struct, INumber<T>
        {
            T[] expected = original.Select(ReverseEndianness).ToArray();
            T[] originalCopy = (T[])original.Clone();

            T[] actual1 = (T[])original.Clone();
            T[] actual2 = new T[original.Length];
            T[] actual3 = new T[original.Length + 1];

            // In-place
            ReverseEndianness<T>(actual1, actual1);
            Assert.Equal(expected, actual1);

            // Different destination
            ReverseEndianness<T>(original, actual2);
            Assert.Equal(originalCopy, original);
            Assert.Equal(expected, actual2);

            // Different larger destination
            ReverseEndianness<T>(original, actual3);
            Assert.Equal(originalCopy, original);
            Assert.Equal(expected, actual3[0..^1]);
            Assert.Equal(default, actual3[^1]);

            foreach (int offset in new[] { 1, 2, 3 })
            {
                if (original.Length > offset)
                {
                    // In-place shifted +offset
                    expected = original.AsSpan(0, original.Length - offset).ToArray().Select(ReverseEndianness).ToArray();
                    actual1 = (T[])original.Clone();
                    ReverseEndianness(actual1.AsSpan(0, actual1.Length - offset), actual1.AsSpan(offset));
                    Assert.Equal(expected, actual1.AsSpan(offset).ToArray());
                    for (int i = 0; i < offset; i++)
                    {
                        Assert.Equal(original[i], actual1[i]);
                    }

                    // In-place shifted -offset
                    expected = original.AsSpan(offset).ToArray().Select(ReverseEndianness).ToArray();
                    actual2 = (T[])original.Clone();
                    ReverseEndianness(actual2.AsSpan(offset), actual2.AsSpan(0, actual2.Length - offset));
                    Assert.Equal(expected, actual2.AsSpan(0, actual2.Length - offset).ToArray());
                    Assert.Equal(original[^offset], actual2[^offset]);
                }
            }

            // Throws if the destination is too short
            if (original.Length > 0)
            {
                T[] destination = new T[original.Length - 1];
                AssertExtensions.Throws<ArgumentException>("destination", () => ReverseEndianness<T>(original, destination));
            }
        }

        private static T ReverseEndianness<T>(T value)
        {
            if (typeof(T) == typeof(ushort)) return (T)(object)BinaryPrimitives.ReverseEndianness((ushort)(object)value);
            if (typeof(T) == typeof(short)) return (T)(object)BinaryPrimitives.ReverseEndianness((short)(object)value);
            if (typeof(T) == typeof(uint)) return (T)(object)BinaryPrimitives.ReverseEndianness((uint)(object)value);
            if (typeof(T) == typeof(int)) return (T)(object)BinaryPrimitives.ReverseEndianness((int)(object)value);
            if (typeof(T) == typeof(ulong)) return (T)(object)BinaryPrimitives.ReverseEndianness((ulong)(object)value);
            if (typeof(T) == typeof(long)) return (T)(object)BinaryPrimitives.ReverseEndianness((long)(object)value);
            if (typeof(T) == typeof(nuint)) return (T)(object)BinaryPrimitives.ReverseEndianness((nuint)(object)value);
            if (typeof(T) == typeof(nint)) return (T)(object)BinaryPrimitives.ReverseEndianness((nint)(object)value);
            if (typeof(T) == typeof(UInt128)) return (T)(object)BinaryPrimitives.ReverseEndianness((UInt128)(object)value);
            if (typeof(T) == typeof(Int128)) return (T)(object)BinaryPrimitives.ReverseEndianness((Int128)(object)value);
            throw new Exception($"Unexpected type {typeof(T)}");
        }

        private static void ReverseEndianness<T>(ReadOnlySpan<T> source, Span<T> destination) where T : struct
        {
            if (typeof(T) == typeof(ushort)) { BinaryPrimitives.ReverseEndianness(MemoryMarshal.Cast<T, ushort>(source), MemoryMarshal.Cast<T, ushort>(destination)); return; }
            if (typeof(T) == typeof(short)) { BinaryPrimitives.ReverseEndianness(MemoryMarshal.Cast<T, short>(source), MemoryMarshal.Cast<T, short>(destination)); return; }
            if (typeof(T) == typeof(uint)) { BinaryPrimitives.ReverseEndianness(MemoryMarshal.Cast<T, uint>(source), MemoryMarshal.Cast<T, uint>(destination)); return; }
            if (typeof(T) == typeof(int)) { BinaryPrimitives.ReverseEndianness(MemoryMarshal.Cast<T, int>(source), MemoryMarshal.Cast<T, int>(destination)); return; }
            if (typeof(T) == typeof(ulong)) { BinaryPrimitives.ReverseEndianness(MemoryMarshal.Cast<T, ulong>(source), MemoryMarshal.Cast<T, ulong>(destination)); return; }
            if (typeof(T) == typeof(long)) { BinaryPrimitives.ReverseEndianness(MemoryMarshal.Cast<T, long>(source), MemoryMarshal.Cast<T, long>(destination)); return; }
            if (typeof(T) == typeof(nuint)) { BinaryPrimitives.ReverseEndianness(MemoryMarshal.Cast<T, nuint>(source), MemoryMarshal.Cast<T, nuint>(destination)); return; }
            if (typeof(T) == typeof(nint)) { BinaryPrimitives.ReverseEndianness(MemoryMarshal.Cast<T, nint>(source), MemoryMarshal.Cast<T, nint>(destination)); return; }
            if (typeof(T) == typeof(UInt128)) { BinaryPrimitives.ReverseEndianness(MemoryMarshal.Cast<T, UInt128>(source), MemoryMarshal.Cast<T, UInt128>(destination)); return; }
            if (typeof(T) == typeof(Int128)) { BinaryPrimitives.ReverseEndianness(MemoryMarshal.Cast<T, Int128>(source), MemoryMarshal.Cast<T, Int128>(destination)); return; }
            throw new Exception($"Unexpected type {typeof(T)}");
        }
    }
}
