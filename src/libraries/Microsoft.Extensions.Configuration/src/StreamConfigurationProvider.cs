// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Defines the core behavior of stream-based configuration providers and provides a base for derived classes.
    /// </summary>
    public abstract class StreamConfigurationProvider : ConfigurationProvider
    {
        /// <summary>
        /// Gets the source settings for this provider.
        /// </summary>
        public StreamConfigurationSource Source { get; }

        private bool _loaded;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamConfigurationProvider"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        public StreamConfigurationProvider(StreamConfigurationSource source)
        {
            ArgumentNullException.ThrowIfNull(source);

            Source = source;
        }

        /// <summary>
        /// Loads the configuration data from the stream.
        /// </summary>
        /// <param name="stream">The data stream.</param>
        public abstract void Load(Stream stream);

        /// <summary>
        /// Loads the configuration data from the stream.
        /// </summary>
        /// <remarks>
        /// This method throws an exception on subsequent calls.
        /// </remarks>
        public override void Load()
        {
            if (_loaded)
            {
                throw new InvalidOperationException(SR.StreamConfigurationProvidersAlreadyLoaded);
            }

            if (Source.Stream == null)
            {
                throw new InvalidOperationException(SR.StreamConfigurationSourceStreamCannotBeNull);
            }

            Load(Source.Stream);
            _loaded = true;
        }
    }
}
