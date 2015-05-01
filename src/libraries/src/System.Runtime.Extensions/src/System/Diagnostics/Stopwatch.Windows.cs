// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Diagnostics
{
    public partial class Stopwatch
    {
        private static bool QueryPerformanceFrequency(out long value)
        {
            return Interop.mincore.QueryPerformanceFrequency(out value);
        }

        private static bool QueryPerformanceCounter(out long value)
        {
            return Interop.mincore.QueryPerformanceCounter(out value);
        }
    }
}