// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

/// <summary>Provides downlevel polyfills for static methods on <see cref="Math"/>.</summary>
internal static class MathPolyfills
{
    extension(Math)
    {
        public static ulong BigMul(ulong left, ulong right, out ulong low)
        {
            ulong lowerLow = (uint)left * (ulong)(uint)right;
            ulong higherLow = (left >> 32) * (uint)right;
            ulong lowerHigh = (uint)left * (right >> 32);
            ulong higherHigh = (left >> 32) * (right >> 32);

            ulong cross = (lowerLow >> 32) + (higherLow & uint.MaxValue) + lowerHigh;
            ulong upper = (higherLow >> 32) + (cross >> 32) + higherHigh;
            low = (cross << 32) | (lowerLow & uint.MaxValue);
            return upper;
        }
    }
}
