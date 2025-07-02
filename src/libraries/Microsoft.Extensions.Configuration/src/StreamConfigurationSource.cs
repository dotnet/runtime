// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Defines the core behavior of stream-based configuration sources and provides a base for derived classes.
    /// </summary>
    public abstract class StreamConfigurationSource : IConfigurationSource
    {
        /// <summary>
        /// Gets or sets the stream containing the configuration data.
        /// </summary>
        [DisallowNull]
        public Stream? Stream { get; set; }

        /// <summary>
        /// Builds the <see cref="StreamConfigurationProvider"/> for this source.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
        /// <returns>An <see cref="IConfigurationProvider"/> instance.</returns>
        public abstract IConfigurationProvider Build(IConfigurationBuilder builder);
    }
}
