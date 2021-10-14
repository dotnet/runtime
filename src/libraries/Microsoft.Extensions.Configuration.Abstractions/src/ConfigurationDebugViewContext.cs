// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration.Abstractions
{
    public readonly struct ConfigurationDebugViewContext
    {
        public ConfigurationDebugViewContext(string path, string key, string value, IConfigurationProvider configurationProvider)
        {
            Path = path;
            Key = key;
            Value = value;
            ConfigurationProvider = configurationProvider;
        }

        public string Path { get; }

        public string Key { get; }

        public string Value { get; }

        public IConfigurationProvider ConfigurationProvider { get; }
    }
}
