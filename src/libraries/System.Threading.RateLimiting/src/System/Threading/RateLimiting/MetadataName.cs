// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading.RateLimiting
{
    /// <summary>
    /// Contains some common metadata name-type pairs and helper method to create a metadata name.
    /// </summary>
    public static class MetadataName
    {
        /// <summary>
        /// Metadata put on a failed lease acquisition to specify when to retry acquiring a lease.
        /// For example, used in <see cref="TokenBucketRateLimiter"/> which periodically replenishes leases.
        /// </summary>
        public static MetadataName<TimeSpan> RetryAfter { get; } = Create<TimeSpan>("RETRY_AFTER");

        /// <summary>
        /// Metadata put on a failed lease acquisition to specify the reason the lease failed.
        /// </summary>
        public static MetadataName<string> ReasonPhrase { get; } = Create<string>("REASON_PHRASE");

        /// <summary>
        /// Create a strongly-typed metadata name.
        /// </summary>
        /// <typeparam name="T">Type that the metadata will contain.</typeparam>
        /// <param name="name">Name of the metadata.</param>
        /// <returns></returns>
        public static MetadataName<T> Create<T>(string name) => new MetadataName<T>(name);
    }
}
