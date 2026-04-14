// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    }
}
