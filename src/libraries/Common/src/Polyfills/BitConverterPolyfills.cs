// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System;

/// <summary>Provides downlevel polyfills for static methods on <see cref="BitConverter"/>.</summary>
internal static class BitConverterPolyfills
{
    extension(BitConverter)
    {
        public static uint SingleToUInt32Bits(float value)
        {
            unsafe
            {
                return *(uint*)&value;
            }
        }

        public static ulong DoubleToUInt64Bits(double value)
        {
            unsafe
            {
                return *(ulong*)&value;
            }
        }

        // ---- ReadOnlySpan<byte> read overloads (host-endian, semantics match BCL net5+) ----

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ToInt16(ReadOnlySpan<byte> value)
        {
            if (value.Length < sizeof(short))
                ThrowHelper_ArgumentOutOfRange(nameof(value));
            unsafe
            {
                fixed (byte* p = value) return *(short*)p;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToUInt16(ReadOnlySpan<byte> value)
        {
            if (value.Length < sizeof(ushort))
                ThrowHelper_ArgumentOutOfRange(nameof(value));
            unsafe
            {
                fixed (byte* p = value) return *(ushort*)p;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt32(ReadOnlySpan<byte> value)
        {
            if (value.Length < sizeof(int))
                ThrowHelper_ArgumentOutOfRange(nameof(value));
            unsafe
            {
                fixed (byte* p = value) return *(int*)p;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUInt32(ReadOnlySpan<byte> value)
        {
            if (value.Length < sizeof(uint))
                ThrowHelper_ArgumentOutOfRange(nameof(value));
            unsafe
            {
                fixed (byte* p = value) return *(uint*)p;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToInt64(ReadOnlySpan<byte> value)
        {
            if (value.Length < sizeof(long))
                ThrowHelper_ArgumentOutOfRange(nameof(value));
            unsafe
            {
                fixed (byte* p = value) return *(long*)p;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ToUInt64(ReadOnlySpan<byte> value)
        {
            if (value.Length < sizeof(ulong))
                ThrowHelper_ArgumentOutOfRange(nameof(value));
            unsafe
            {
                fixed (byte* p = value) return *(ulong*)p;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToSingle(ReadOnlySpan<byte> value)
        {
            if (value.Length < sizeof(float))
                ThrowHelper_ArgumentOutOfRange(nameof(value));
            unsafe
            {
                fixed (byte* p = value) return *(float*)p;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ToDouble(ReadOnlySpan<byte> value)
        {
            if (value.Length < sizeof(double))
                ThrowHelper_ArgumentOutOfRange(nameof(value));
            unsafe
            {
                fixed (byte* p = value) return *(double*)p;
            }
        }

        // ---- Span<byte> TryWriteBytes overloads (host-endian, return false on too-short) ----

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteBytes(Span<byte> destination, short value)
        {
            if (destination.Length < sizeof(short)) return false;
            unsafe
            {
                fixed (byte* p = destination) *(short*)p = value;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteBytes(Span<byte> destination, ushort value)
        {
            if (destination.Length < sizeof(ushort)) return false;
            unsafe
            {
                fixed (byte* p = destination) *(ushort*)p = value;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteBytes(Span<byte> destination, int value)
        {
            if (destination.Length < sizeof(int)) return false;
            unsafe
            {
                fixed (byte* p = destination) *(int*)p = value;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteBytes(Span<byte> destination, uint value)
        {
            if (destination.Length < sizeof(uint)) return false;
            unsafe
            {
                fixed (byte* p = destination) *(uint*)p = value;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteBytes(Span<byte> destination, long value)
        {
            if (destination.Length < sizeof(long)) return false;
            unsafe
            {
                fixed (byte* p = destination) *(long*)p = value;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteBytes(Span<byte> destination, ulong value)
        {
            if (destination.Length < sizeof(ulong)) return false;
            unsafe
            {
                fixed (byte* p = destination) *(ulong*)p = value;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteBytes(Span<byte> destination, float value)
        {
            if (destination.Length < sizeof(float)) return false;
            unsafe
            {
                fixed (byte* p = destination) *(float*)p = value;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteBytes(Span<byte> destination, double value)
        {
            if (destination.Length < sizeof(double)) return false;
            unsafe
            {
                fixed (byte* p = destination) *(double*)p = value;
            }
            return true;
        }
    }

    private static void ThrowHelper_ArgumentOutOfRange(string paramName) =>
        throw new ArgumentOutOfRangeException(paramName);
}
