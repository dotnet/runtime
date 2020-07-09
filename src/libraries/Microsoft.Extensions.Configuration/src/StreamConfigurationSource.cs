// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Stream based <see cref="IConfigurationSource" />.
    /// </summary>
    public abstract class StreamConfigurationSource : IConfigurationSource
    {
        /// <summary>
        /// The stream containing the configuration data.
        /// </summary>
        public Stream Stream { get; set; }

        /// <summary>
        /// Builds the <see cref="StreamConfigurationProvider"/> for this source.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
        /// <returns>An <see cref="IConfigurationProvider"/></returns>
        public abstract IConfigurationProvider Build(IConfigurationBuilder builder);
    }
}
