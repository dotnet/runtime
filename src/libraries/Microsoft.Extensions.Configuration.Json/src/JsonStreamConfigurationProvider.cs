// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.Extensions.Configuration.Json
{
    /// <summary>
    /// Provides configuration key-value pairs that are obtained from a JSON stream.
    /// </summary>
    public class JsonStreamConfigurationProvider : StreamConfigurationProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonStreamConfigurationProvider"/> class.
        /// </summary>
        /// <param name="source">The <see cref="JsonStreamConfigurationSource"/>.</param>
        public JsonStreamConfigurationProvider(JsonStreamConfigurationSource source) : base(source) { }

        /// <summary>
        /// Loads JSON configuration key-value pairs from a stream into a provider.
        /// </summary>
        /// <param name="stream">The JSON <see cref="Stream"/> to load configuration data from.</param>
        public override void Load(Stream stream)
        {
            Data = JsonConfigurationFileParser.Parse(stream);
        }
    }
}
