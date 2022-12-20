// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System
{
    public static partial class GC
    {
        /// <summary>
        /// Returns, in a specified time-out period, the status of a registered notification for determining whether a full,
        /// blocking garbage collection by the common language runtime is imminent.
        /// </summary>
        /// <param name="timeout">The timeout on waiting for a full GC approach</param>
        /// <returns>The status of a registered full GC notification</returns>
        public static GCNotificationStatus WaitForFullGCApproach(TimeSpan timeout)
            => WaitForFullGCApproach(WaitHandle.ToTimeoutMilliseconds(timeout));

        /// <summary>
        /// Returns the status of a registered notification about whether a blocking garbage collection
        /// has completed. May wait indefinitely for a full collection.
        /// </summary>
        /// <param name="timeout">The timeout on waiting for a full collection</param>
        /// <returns>The status of a registered full GC notification</returns>
        public static GCNotificationStatus WaitForFullGCComplete(TimeSpan timeout)
            => WaitForFullGCComplete(WaitHandle.ToTimeoutMilliseconds(timeout));
    }
}
