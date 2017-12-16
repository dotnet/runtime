// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

#if !netstandard
using Internal.Runtime.CompilerServices;
#endif

namespace System
{
    internal static partial class SpanHelpers
    {
        public unsafe static void ClearLessThanPointerSized(byte* ptr, UIntPtr byteLength)
        {
            if (sizeof(UIntPtr) == sizeof(uint))
            {
                Unsafe.InitBlockUnaligned(ptr, 0, (uint)byteLength);
            }
            else
            {
                // PERF: Optimize for common case of length <= uint.MaxValue
                ulong bytesRemaining = (ulong)byteLength;
                uint bytesToClear = (uint)(bytesRemaining & uint.MaxValue);
                Unsafe.InitBlockUnaligned(ptr, 0, bytesToClear);
                bytesRemaining -= bytesToClear;
                ptr += bytesToClear;
                // Clear any bytes > uint.MaxValue
                while (bytesRemaining > 0)
                {
                    bytesToClear = (bytesRemaining >= uint.MaxValue) ? uint.MaxValue : (uint)bytesRemaining;
                    Unsafe.InitBlockUnaligned(ptr, 0, bytesToClear);
                    ptr += bytesToClear;
                    bytesRemaining -= bytesToClear;
                }
            }
        }

        public static unsafe void ClearLessThanPointerSized(ref byte b, UIntPtr byteLength)
        {
            if (sizeof(UIntPtr) == sizeof(uint))
            {
                Unsafe.InitBlockUnaligned(ref b, 0, (uint)byteLength);
            }
            else
            {
                // PERF: Optimize for common case of length <= uint.MaxValue
                ulong bytesRemaining = (ulong)byteLength;
                uint bytesToClear = (uint)(bytesRemaining & uint.MaxValue);
                Unsafe.InitBlockUnaligned(ref b, 0, bytesToClear);
                bytesRemaining -= bytesToClear;
                long byteOffset = bytesToClear;
                // Clear any bytes > uint.MaxValue
                while (bytesRemaining > 0)
                {
                    bytesToClear = (bytesRemaining >= uint.MaxValue) ? uint.MaxValue : (uint)bytesRemaining;
                    ref byte bOffset = ref Unsafe.Add(ref b, (IntPtr)byteOffset);
                    Unsafe.InitBlockUnaligned(ref bOffset, 0, bytesToClear);
                    byteOffset += bytesToClear;
                    bytesRemaining -= bytesToClear;
                }
            }
        }

        public unsafe static void ClearPointerSizedWithoutReferences(ref byte b, UIntPtr byteLength)
        {
            // TODO: Perhaps do switch casing to improve small size perf

            var i = IntPtr.Zero;
            while (i.LessThanEqual(byteLength - sizeof(Reg64)))
            {
                Unsafe.As<byte, Reg64>(ref Unsafe.Add<byte>(ref b, i)) = default(Reg64);
                i += sizeof(Reg64);
            }
            if (i.LessThanEqual(byteLength - sizeof(Reg32)))
            {
                Unsafe.As<byte, Reg32>(ref Unsafe.Add<byte>(ref b, i)) = default(Reg32);
                i += sizeof(Reg32);
            }
            if (i.LessThanEqual(byteLength - sizeof(Reg16)))
            {
                Unsafe.As<byte, Reg16>(ref Unsafe.Add<byte>(ref b, i)) = default(Reg16);
                i += sizeof(Reg16);
            }
            if (i.LessThanEqual(byteLength - sizeof(long)))
            {
                Unsafe.As<byte, long>(ref Unsafe.Add<byte>(ref b, i)) = 0;
                i += sizeof(long);
            }
            // JIT: Should elide this if 64-bit
            if (sizeof(IntPtr) == sizeof(int))
            {
                if (i.LessThanEqual(byteLength - sizeof(int)))
                {
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, i)) = 0;
                    i += sizeof(int);
                }
            }
        }

        public static void ClearPointerSizedWithReferences(ref IntPtr ip, UIntPtr pointerSizeLength)
        {
            // TODO: Perhaps do switch casing to improve small size perf

            var i = IntPtr.Zero;
            var n = IntPtr.Zero;
            while ((n = i + 8).LessThanEqual(pointerSizeLength))
            {
                Unsafe.Add<IntPtr>(ref ip, i + 0) = default(IntPtr);
                Unsafe.Add<IntPtr>(ref ip, i + 1) = default(IntPtr);
                Unsafe.Add<IntPtr>(ref ip, i + 2) = default(IntPtr);
                Unsafe.Add<IntPtr>(ref ip, i + 3) = default(IntPtr);
                Unsafe.Add<IntPtr>(ref ip, i + 4) = default(IntPtr);
                Unsafe.Add<IntPtr>(ref ip, i + 5) = default(IntPtr);
                Unsafe.Add<IntPtr>(ref ip, i + 6) = default(IntPtr);
                Unsafe.Add<IntPtr>(ref ip, i + 7) = default(IntPtr);
                i = n;
            }
            if ((n = i + 4).LessThanEqual(pointerSizeLength))
            {
                Unsafe.Add<IntPtr>(ref ip, i + 0) = default(IntPtr);
                Unsafe.Add<IntPtr>(ref ip, i + 1) = default(IntPtr);
                Unsafe.Add<IntPtr>(ref ip, i + 2) = default(IntPtr);
                Unsafe.Add<IntPtr>(ref ip, i + 3) = default(IntPtr);
                i = n;
            }
            if ((n = i + 2).LessThanEqual(pointerSizeLength))
            {
                Unsafe.Add<IntPtr>(ref ip, i + 0) = default(IntPtr);
                Unsafe.Add<IntPtr>(ref ip, i + 1) = default(IntPtr);
                i = n;
            }
            if ((i + 1).LessThanEqual(pointerSizeLength))
            {
                Unsafe.Add<IntPtr>(ref ip, i) = default(IntPtr);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe static bool LessThanEqual(this IntPtr index, UIntPtr length)
        {
            return (sizeof(UIntPtr) == sizeof(uint))
                ? (int)index <= (int)length
                : (long)index <= (long)length;
        }

        [StructLayout(LayoutKind.Sequential, Size = 64)]
        private struct Reg64 { }
        [StructLayout(LayoutKind.Sequential, Size = 32)]
        private struct Reg32 { }
        [StructLayout(LayoutKind.Sequential, Size = 16)]
        private struct Reg16 { }
    }
}
