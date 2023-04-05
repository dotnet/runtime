// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using RecipeEngine.Api.Platforms;

namespace Unity.Cookbook;

static class Data
{
    public static readonly JobConfiguration[] StandardJobConfigurations =
        new []
        {
            // Windows
            new JobConfiguration {SystemType = SystemType.Windows, Architecture = Architecture.x64, Configuration = Configuration.Debug},
            new JobConfiguration {SystemType = SystemType.Windows, Architecture = Architecture.x64, Configuration = Configuration.Release},

            new JobConfiguration {SystemType = SystemType.Windows, Architecture = Architecture.x86, Configuration = Configuration.Debug},
            new JobConfiguration {SystemType = SystemType.Windows, Architecture = Architecture.x86, Configuration = Configuration.Release},

            // macOS
            new JobConfiguration {SystemType = SystemType.MacOS, Architecture = Architecture.x64, Configuration = Configuration.Debug, ExcludeFromPublishing =  true},
            new JobConfiguration {SystemType = SystemType.MacOS, Architecture = Architecture.x64, Configuration = Configuration.Release, ExcludeFromPublishing =  true},

            new JobConfiguration {SystemType = SystemType.MacOS, Architecture = Architecture.arm64, Configuration = Configuration.Debug, ExcludeFromPublishing =  true},
            new JobConfiguration {SystemType = SystemType.MacOS, Architecture = Architecture.arm64, Configuration = Configuration.Release, ExcludeFromPublishing =  true},

            // Linux
            new JobConfiguration {SystemType = SystemType.Ubuntu, Architecture = Architecture.x64, Configuration = Configuration.Debug},
            new JobConfiguration {SystemType = SystemType.Ubuntu, Architecture = Architecture.x64, Configuration = Configuration.Release},
        };
}
