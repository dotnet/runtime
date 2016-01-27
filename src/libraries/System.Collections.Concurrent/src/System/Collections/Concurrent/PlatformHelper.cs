// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Threading
{
    /// <summary>
    /// A helper class to get the number of processors, it updates the number of processors every sampling interval
    /// </summary>
    internal static class PlatformHelper
    {
        private const int PROCESSOR_COUNT_REFRESH_INTERVAL_MS = 30000; // How often to refresh the count, in milliseconds.
        private static volatile int s_processorCount; // The last count seen.
        private static volatile int s_lastProcessorCountRefreshTicks; // The last time we refreshed.

        /// <summary>
        /// Gets the number of available processors
        /// </summary>
        internal static int ProcessorCount
        {
            get
            {
                int now = Environment.TickCount;
                if (s_processorCount == 0 || (now - s_lastProcessorCountRefreshTicks) >= PROCESSOR_COUNT_REFRESH_INTERVAL_MS)
                {
                    s_processorCount = Environment.ProcessorCount;
                    s_lastProcessorCountRefreshTicks = now;
                }

                return s_processorCount;
            }
        }
    }
}
