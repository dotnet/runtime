// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static System.TestHelpers;

namespace System.SpanTests
{
    public static partial class MemoryMarshalTests
    {
        [Fact]
        public static void SpanGetReferenceArray()
        {
            int[] a = { 91, 92, 93, 94, 95 };
            Span<int> span = new Span<int>(a, 1, 3);
            ref int pinnableReference = ref MemoryMarshal.GetReference(span);
            Assert.True(Unsafe.AreSame(ref a[1], ref pinnableReference));
        }

        [Fact]
        public static void SpanGetReferenceArrayPastEnd()
        {
            // The only real difference between GetReference() and "ref span[0]" is that
            // GetReference() of a zero-length won't throw an IndexOutOfRange.

            int[] a = { 91, 92, 93, 94, 95 };
            Span<int> span = new Span<int>(a, a.Length, 0);
            ref int pinnableReference = ref MemoryMarshal.GetReference(span);
            ref int expected = ref Unsafe.Add<int>(ref a[a.Length - 1], 1);
            Assert.True(Unsafe.AreSame(ref expected, ref pinnableReference));
        }

        [Fact]
        public static void SpanGetReferencePointer()
        {
            unsafe
            {
                int i = 42;
                Span<int> span = new Span<int>(&i, 1);
                ref int pinnableReference = ref MemoryMarshal.GetReference(span);
                Assert.True(Unsafe.AreSame(ref i, ref pinnableReference));
            }
        }

        [Fact]
        public static void SpanGetReferenceEmpty()
        {
            unsafe
            {
                Span<int> span = Span<int>.Empty;
                ref int pinnableReference = ref MemoryMarshal.GetReference(span);
                Assert.True(Unsafe.AreSame(ref Unsafe.AsRef<int>(null), ref pinnableReference));
            }
        }

        [Fact]
        public static void ReadOnlySpanGetReferenceArray()
        {
            int[] a = { 91, 92, 93, 94, 95 };
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(a, 1, 3);
            ref int pinnableReference = ref Unsafe.AsRef(in MemoryMarshal.GetReference(span));
            Assert.True(Unsafe.AreSame(ref a[1], ref pinnableReference));
        }

        [Fact]
        public static void ReadOnlySpanGetReferenceArrayPastEnd()
        {
            // The only real difference between GetReference() and "ref span[0]" is that
            // GetReference() of a zero-length won't throw an IndexOutOfRange.

            int[] a = { 91, 92, 93, 94, 95 };
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(a, a.Length, 0);
            ref int pinnableReference = ref Unsafe.AsRef(in MemoryMarshal.GetReference(span));
            ref int expected = ref Unsafe.Add<int>(ref a[a.Length - 1], 1);
            Assert.True(Unsafe.AreSame(ref expected, ref pinnableReference));
        }

        [Fact]
        public static void ReadOnlySpanGetReferencePointer()
        {
            unsafe
            {
                int i = 42;
                ReadOnlySpan<int> span = new ReadOnlySpan<int>(&i, 1);
                ref int pinnableReference = ref Unsafe.AsRef(in MemoryMarshal.GetReference(span));
                Assert.True(Unsafe.AreSame(ref i, ref pinnableReference));
            }
        }

        [Fact]
        public static void ReadOnlySpanGetReferenceEmpty()
        {
            unsafe
            {
                ReadOnlySpan<int> span = ReadOnlySpan<int>.Empty;
                ref int pinnableReference = ref Unsafe.AsRef(in MemoryMarshal.GetReference(span));
                Assert.True(Unsafe.AreSame(ref Unsafe.AsRef<int>(null), ref pinnableReference));
            }
        }

        [Fact]
        public static void ReadOnlySpanGetReferenceAndReadInteger()
        {
            Assert.Equal(BitConverter.IsLittleEndian ?
                0x65_00_68 :
                0x68_00_65,
                Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref Unsafe.As<char, byte>(
                    ref MemoryMarshal.GetReference("hello world 1".AsSpan())), 0)));

            Assert.Equal(BitConverter.IsLittleEndian ?
                0x6F_00_6C_00_6C_00_65_00 :
                0x68_00_65_00_6C_00_6C_00,
                Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref Unsafe.As<char, byte>(
                    ref MemoryMarshal.GetReference("hello world 2".AsSpan())), 1)));
        }
    }
}
