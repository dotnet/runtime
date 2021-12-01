// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration.Memory
{
    /// <summary>
    /// Represents in-memory data as an <see cref="IConfigurationSource"/>.
    /// </summary>
    public class MemoryConfigurationSource : IConfigurationSource
    {
        /// <summary>
        /// The initial key value configuration pairs.
        /// </summary>
        public IEnumerable<KeyValuePair<string, string?>>? InitialData { get; set; }

        /// <summary>
        /// Builds the <see cref="MemoryConfigurationProvider"/> for this source.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
        /// <returns>A <see cref="MemoryConfigurationProvider"/></returns>
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new MemoryConfigurationProvider(this);
        }
    }
}
