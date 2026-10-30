// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System;

/// <summary>Provides downlevel polyfills for static methods on <see cref="Math"/>.</summary>
internal static class MathPolyfills
{
    extension(Math)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong BigMul(ulong a, ulong b, out ulong low)
        {
            ulong lowerLow = (ulong)(uint)a * (uint)b;
            ulong higherLow = (ulong)(uint)(a >> 32) * (uint)b;
            ulong lowerHigh = (ulong)(uint)a * (uint)(b >> 32);
            ulong higherHigh = (ulong)(uint)(a >> 32) * (uint)(b >> 32);

            ulong cross = (lowerLow >> 32) + (higherLow & uint.MaxValue) + lowerHigh;
            ulong upper = (higherLow >> 32) + (cross >> 32) + higherHigh;
            low = (cross << 32) | (lowerLow & uint.MaxValue);
            return upper;
        }
    }
}
