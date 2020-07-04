// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    public partial class Stopwatch
    {
        private static unsafe long QueryPerformanceFrequency()
        {
            long resolution;

            Interop.BOOL result = Interop.Kernel32.QueryPerformanceFrequency(&resolution);
            // The P/Invoke is documented to never fail on Windows XP or later
            Debug.Assert(result != Interop.BOOL.FALSE);

            return resolution;
        }

        private static unsafe long QueryPerformanceCounter()
        {
            long timestamp;

            Interop.BOOL result = Interop.Kernel32.QueryPerformanceCounter(&timestamp);
            // The P/Invoke is documented to never fail on Windows XP or later
            Debug.Assert(result != Interop.BOOL.FALSE);

            return timestamp;
        }
    }
}
