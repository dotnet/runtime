// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Numerics.Tensors
{
    internal static partial class TensorSpanHelpers // .T
    {
        public static unsafe void Memmove<T>(Span<T> destination, ReadOnlySpan<T> source, nint length, nint dstOffset = 0)
        {
            source.Slice(0, checked((int)length)).CopyTo(destination.Slice(checked((int)dstOffset)));
        }

        public static unsafe void Memmove<T>(ref T[] destination, ref T source, nint length)
        {
            MemoryMarshal.CreateSpan(ref source, checked((int)length)).CopyTo(destination);
        }

        public static unsafe void Memmove<T>(ref T destination, ref T source, nint length)
        {
            MemoryMarshal.CreateSpan(ref source, checked((int)length)).CopyTo(MemoryMarshal.CreateSpan(ref destination, checked((int)length)));
        }

        public static unsafe void Memmove<T>(Span<T> destination, ref T source, nint length)
        {
            MemoryMarshal.CreateSpan(ref source, checked((int)length)).CopyTo(destination);
        }

        public static void Clear<T>(ref T dest, nuint len)
        {
            while (len > 0)
            {
                nuint toClear = Math.Min(len, int.MaxValue);
                MemoryMarshal.CreateSpan(ref dest, (int)toClear).Clear();
                dest = ref Unsafe.Add(ref dest, toClear);
                len -= toClear;
            }
        }

        public static unsafe void Fill<T>(ref T dest, nuint numElements, T value)
        {
            while (numElements > 0)
            {
                nuint toFill = Math.Min(numElements, int.MaxValue);
                MemoryMarshal.CreateSpan(ref dest, (int)toFill).Fill(value);
                dest = ref Unsafe.Add(ref dest, toFill);
                numElements -= toFill;
            }
        }
    }
}
