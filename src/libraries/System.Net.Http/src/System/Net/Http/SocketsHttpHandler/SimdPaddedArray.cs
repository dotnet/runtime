// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.Intrinsics.X86;

namespace System.Net.Http
{
    /// <summary>
    /// An array that ensures valid address space before
    /// and after the array's bytes, to support SIMD loads.
    /// </summary>
    internal readonly struct SimdPaddedArray
    {
        private static int PaddingLength =>
            Avx2.IsSupported ? 31 :
            0;

        private readonly byte[] _array;

        public int Length => _array.Length - PaddingLength;
        public byte[] BackingArray => _array;


        public SimdPaddedArray(int length)
            : this(new byte[length + PaddingLength])
        {
        }

        private SimdPaddedArray(byte[] array)
        {
            _array = array;
        }

        public static SimdPaddedArray FromPool(int length) => new SimdPaddedArray(ArrayPool<byte>.Shared.Rent(length));

        public Span<byte> AsSpan() => AsSpan(0, Length);
        public Span<byte> AsSpan(int offset, int length)
        {
            Debug.Assert(length - offset <= Length);
            return new Span<byte>(_array, offset, length);
        }

        public Memory<byte> AsMemory() => AsMemory(0, Length);
        public Memory<byte> AsMemory(int offset, int length)
        {
            Debug.Assert(length - offset <= Length);
            return new Memory<byte>(_array, offset, length);
        }

        public ref byte this[int index]
        {
            get
            {
                Debug.Assert(index < Length);
                return ref _array[index];
            }
        }
    }
}
