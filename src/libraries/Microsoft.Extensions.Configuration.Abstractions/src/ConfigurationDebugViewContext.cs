// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Provides the data about current item of the configuration.
    /// </summary>
    public readonly struct ConfigurationDebugViewContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ConfigurationDebugViewContext"/>.
        /// </summary>
        /// <param name="path">The path of the current item of the configuration.</param>
        /// <param name="key">The key of the current item of the configuration.</param>
        /// <param name="value">The value of the current item of the configuration.</param>
        /// <param name="configurationProvider">The <see cref="IConfigurationProvider" /> to use to get the value of the current item.</param>
        public ConfigurationDebugViewContext(string path, string key, string? value, IConfigurationProvider configurationProvider)
        {
            Path = path;
            Key = key;
            Value = value;
            ConfigurationProvider = configurationProvider;
        }

        /// <summary>
        /// Gets the path of the current item.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the key of the current item.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Gets the value of the current item.
        /// </summary>
        public string? Value { get; }

        /// <summary>
        /// Gets the <see cref="IConfigurationProvider" /> that was used to get the value of the current item.
        /// </summary>
        public IConfigurationProvider ConfigurationProvider { get; }
    }
}
