﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration.Abstractions
{
    /// <summary>
    /// Configuration debug view context provides the data about current item of the configuration
    /// </summary>
    public readonly struct ConfigurationDebugViewContext
    {
        public ConfigurationDebugViewContext(string path, string key, string? value, IConfigurationProvider configurationProvider)
        {
            Path = path;
            Key = key;
            Value = value;
            ConfigurationProvider = configurationProvider;
        }

        /// <summary>
        /// Path of the current item
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Key of the current item
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Value of the current item
        /// </summary>
        public string? Value { get; }

        /// <summary>
        /// Provider used to get the value of the current item
        /// </summary>
        public IConfigurationProvider ConfigurationProvider { get; }
    }
}
