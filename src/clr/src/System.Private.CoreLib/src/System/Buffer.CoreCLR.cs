// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable SA1121 // explicitly using type aliases instead of built-in types
#if BIT64
using nuint = System.UInt64;
#else
using nuint = System.UInt32;
#endif

namespace System
{
    public partial class Buffer
    {
        // Copies from one primitive array to another primitive array without
        // respecting types.  This calls memmove internally.  The count and
        // offset parameters here are in bytes.  If you want to use traditional
        // array element indices and counts, use Array.Copy.
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void BlockCopy(Array src, int srcOffset,
            Array dst, int dstOffset, int count);

        // Returns a bool to indicate if the array is of primitive data types
        // or not.
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool IsPrimitiveTypeArray(Array array);

        // Gets the length of the array in bytes.  The array must be an
        // array of primitives.
        //
        // This essentially does the following:
        // return array.length * sizeof(array.UnderlyingElementType).
        //
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int _ByteLength(Array array);

        // This method has a slightly different behavior on arm and other platforms.
        // On arm this method behaves like memcpy and does not handle overlapping buffers.
        // While on other platforms it behaves like memmove and handles overlapping buffers.
        // This behavioral difference is unfortunate but intentional because
        // 1. This method is given access to other internal dlls and this close to release we do not want to change it.
        // 2. It is difficult to get this right for arm and again due to release dates we would like to visit it later.
#if ARM
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void Memcpy(byte* dest, byte* src, int len);
#else // ARM
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void Memcpy(byte* dest, byte* src, int len)
        {
            Debug.Assert(len >= 0, "Negative length in memcpy!");
            Memmove(dest, src, (nuint)len);
        }
#endif // ARM

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern unsafe void __Memmove(byte* dest, byte* src, nuint len);

        // Used by ilmarshalers.cpp
        internal static unsafe void Memcpy(byte* pDest, int destIndex, byte[] src, int srcIndex, int len)
        {
            Debug.Assert((srcIndex >= 0) && (destIndex >= 0) && (len >= 0), "Index and length must be non-negative!");
            Debug.Assert(src.Length - srcIndex >= len, "not enough bytes in src");
            // If dest has 0 elements, the fixed statement will throw an
            // IndexOutOfRangeException.  Special-case 0-byte copies.
            if (len == 0)
                return;
            fixed (byte* pSrc = src)
            {
                Memcpy(pDest + destIndex, pSrc + srcIndex, len);
            }
        }
    }
}
