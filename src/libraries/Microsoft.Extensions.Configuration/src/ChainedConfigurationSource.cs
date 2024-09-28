// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Represents a chained <see cref="IConfiguration"/> as an <see cref="IConfigurationSource"/>.
    /// </summary>
    public class ChainedConfigurationSource : IConfigurationSource
    {
        /// <summary>
        /// Gets or sets the chained configuration.
        /// </summary>
        [DisallowNull]
        public IConfiguration? Configuration { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether the chained configuration
        /// is disposed when the configuration provider is disposed.
        /// </summary>
        public bool ShouldDisposeConfiguration { get; set; }

        /// <summary>
        /// Builds the <see cref="ChainedConfigurationProvider"/> for this source.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
        /// <returns>A <see cref="ChainedConfigurationProvider"/> instance.</returns>
        public IConfigurationProvider Build(IConfigurationBuilder builder)
            => new ChainedConfigurationProvider(this);
    }
}
