// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

/// <summary>Provides downlevel polyfills for static methods on <see cref="BitConverter"/>.</summary>
internal static class BitConverterPolyfills
{
    extension(BitConverter)
    {
        public static int SingleToInt32Bits(float value)
        {
            unsafe { return *(int*)&value; }
        }

        public static float Int32BitsToSingle(int value)
        {
            unsafe { return *(float*)&value; }
        }

        public static uint SingleToUInt32Bits(float value)
        {
            unsafe { return *(uint*)&value; }
        }

        public static float UInt32BitsToSingle(uint value)
        {
            unsafe { return *(float*)&value; }
        }

        public static ulong DoubleToUInt64Bits(double value)
        {
            unsafe { return *(ulong*)&value; }
        }
    }
}
