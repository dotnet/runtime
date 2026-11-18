// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading;

/// <summary>Provides downlevel polyfills for static methods on <see cref="Volatile"/>.</summary>
internal static class VolatilePolyfills
{
    extension(Volatile)
    {
        public static void ReadBarrier() => Interlocked.MemoryBarrier();

        public static void WriteBarrier() => Interlocked.MemoryBarrier();
    }
}
