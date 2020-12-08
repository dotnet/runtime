// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Configuration
{
    public class ConfigurationLocation
    {
        private readonly Configuration _config;

        internal ConfigurationLocation(Configuration config, string locationSubPath)
        {
            _config = config;
            Path = locationSubPath;
        }

        public string Path { get; }

        public Configuration OpenConfiguration()
        {
            return _config.OpenLocationConfiguration(Path);
        }
    }
}
