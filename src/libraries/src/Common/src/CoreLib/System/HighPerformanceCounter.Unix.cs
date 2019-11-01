// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System
{
    internal static class HighPerformanceCounter
    {
        public static ulong TickCount => Interop.Sys.GetTimestamp();

        // Cache the frequency on the managed side to avoid the cost of P/Invoke on every access to Frequency
        public static ulong Frequency { get; } = Interop.Sys.GetTimestampResolution();
    }
}
