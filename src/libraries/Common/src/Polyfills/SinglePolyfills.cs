// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System;

/// <summary>Provides downlevel polyfills for static methods on <see cref="float"/>.</summary>
internal static class SinglePolyfills
{
    extension(float)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool IsFinite(float f)
        {
            uint bits = *(uint*)&f;
            return (~bits & 0x7F80_0000U) != 0;
        }
    }
}
