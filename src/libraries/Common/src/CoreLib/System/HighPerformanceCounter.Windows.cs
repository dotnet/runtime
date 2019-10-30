// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    internal static unsafe class HighPerformanceCounter
    {
        public static ulong TickCount
        {
            get
            {
                long counter;
                Interop.Kernel32.QueryPerformanceCounter(&counter);
                return (ulong)counter;
            }
        }

        public static ulong Frequency { get; } = GetFrequency();

        private static ulong GetFrequency()
        {
            long frequency;
            Interop.Kernel32.QueryPerformanceFrequency(&frequency);
            return (ulong)frequency;
        }
    }
}
