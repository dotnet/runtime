// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

/// <summary>Provides downlevel polyfills for static methods on <see cref="ulong"/>.</summary>
internal static class UInt64Polyfills
{
    extension(ulong)
    {
        public static ulong PopCount(ulong value)
        {
            ulong count = 0;
            while (value != 0)
            {
                value &= value - 1;
                count++;
            }

            return count;
        }
    }
}
