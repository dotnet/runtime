// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Stream based configuration provider
    /// </summary>
    public abstract class StreamConfigurationProvider : ConfigurationProvider
    {
        /// <summary>
        /// The source settings for this provider.
        /// </summary>
        public StreamConfigurationSource Source { get; }

        private bool _loaded;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="source">The source.</param>
        public StreamConfigurationProvider(StreamConfigurationSource source)
        {
            ThrowHelper.ThrowIfNull(source);

            Source = source;
        }

        /// <summary>
        /// Load the configuration data from the stream.
        /// </summary>
        /// <param name="stream">The data stream.</param>
        public abstract void Load(Stream stream);

        /// <summary>
        /// Load the configuration data from the stream. Will throw after the first call.
        /// </summary>
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
