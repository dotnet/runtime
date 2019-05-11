// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
