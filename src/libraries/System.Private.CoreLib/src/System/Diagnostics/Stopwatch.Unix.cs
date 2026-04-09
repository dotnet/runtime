// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    public partial class Stopwatch
    {
        private static long GetFrequency()
        {
            const long SecondsToNanoSeconds = 1000000000;
            return SecondsToNanoSeconds;
        }

        public static long GetTimestamp() => Interop.Sys.GetTimestamp();
    }
}
