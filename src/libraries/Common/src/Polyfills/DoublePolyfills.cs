// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System;

/// <summary>Provides downlevel polyfills for static methods on <see cref="double"/>.</summary>
internal static class DoublePolyfills
{
    extension(double)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFinite(double d)
        {
            long bits = BitConverter.DoubleToInt64Bits(d);
            return ((ulong)~bits & 0x7FF0_0000_0000_0000UL) != 0;
        }
    }
}
