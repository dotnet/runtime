// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Xml.Serialization;


#pragma warning disable 8500 // sizeof of managed types

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

        public static bool SequenceEqual<T>(ref T first, ref T second, nuint length) where T : IEquatable<T>?
        {
            bool equal = true;
            while (length > 0)
            {
                nuint toCompare = Math.Min(length, int.MaxValue);
                equal &= MemoryMarshal.CreateSpan(ref first, (int)toCompare).SequenceEqual(MemoryMarshal.CreateSpan(ref second, (int)toCompare));
                first = ref Unsafe.Add(ref first, toCompare);
                second = ref Unsafe.Add(ref second, toCompare);
                length -= toCompare;
            }

            return equal;
        }
    }
}
