// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Caching.Memory
{
    /// <summary>
    /// Represents a callback delegate that will be fired after an entry is evicted from the cache.
    /// </summary>
    public class PostEvictionCallbackRegistration
    {
        /// <summary>
        /// Gets or sets the callback delegate that will be fired after an entry is evicted from the cache.
        /// </summary>
        public PostEvictionDelegate? EvictionCallback { get; set; }

        /// <summary>
        /// Gets or sets the state to pass to the callback delegate.
        /// </summary>
        public object? State { get; set; }
    }
}
