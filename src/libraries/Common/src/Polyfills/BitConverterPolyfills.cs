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
    }

    private static void ThrowHelper_ArgumentOutOfRange(string paramName) =>
        throw new ArgumentOutOfRangeException(paramName);
}
