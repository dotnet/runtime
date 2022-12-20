// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading.RateLimiting
{
    /// <summary>
    /// Controls the behavior of <see cref="RateLimiter.AcquireAsync"/> when not enough resources can be leased.
    /// </summary>
    public enum QueueProcessingOrder
    {
        /// <summary>
        /// Lease the oldest queued <see cref="RateLimiter.AcquireAsync"/> call.
        /// </summary>
        OldestFirst,
        /// <summary>
        /// Lease the newest queued <see cref="RateLimiter.AcquireAsync"/> call.
        /// </summary>
        NewestFirst
    }
}
