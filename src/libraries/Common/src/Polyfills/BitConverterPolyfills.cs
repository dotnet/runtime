// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

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

        public static short ToInt16(ReadOnlySpan<byte> value) => MemoryMarshal.Read<short>(value);
        public static ushort ToUInt16(ReadOnlySpan<byte> value) => MemoryMarshal.Read<ushort>(value);
        public static int ToInt32(ReadOnlySpan<byte> value) => MemoryMarshal.Read<int>(value);
        public static uint ToUInt32(ReadOnlySpan<byte> value) => MemoryMarshal.Read<uint>(value);
        public static long ToInt64(ReadOnlySpan<byte> value) => MemoryMarshal.Read<long>(value);
        public static ulong ToUInt64(ReadOnlySpan<byte> value) => MemoryMarshal.Read<ulong>(value);
        public static float ToSingle(ReadOnlySpan<byte> value) => MemoryMarshal.Read<float>(value);
        public static double ToDouble(ReadOnlySpan<byte> value) => MemoryMarshal.Read<double>(value);
    }
}
