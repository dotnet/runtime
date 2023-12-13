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

            // The Windows arm64 tests do not have agents to be executed on.
            new JobConfiguration {SystemType = SystemType.Windows, Architecture = Architecture.arm64, Configuration = Configuration.Debug, ExcludeFromTesting =  true},
            new JobConfiguration {SystemType = SystemType.Windows, Architecture = Architecture.arm64, Configuration = Configuration.Release, ExcludeFromTesting =  true},

            // macOS
            // x64 and arm64 artifacts are published as a part of combined x64arm64 fat build job.
            new JobConfiguration {SystemType = SystemType.MacOS, Architecture = Architecture.x64, Configuration = Configuration.Debug, ExcludeFromPublishing =  true},
            new JobConfiguration {SystemType = SystemType.MacOS, Architecture = Architecture.x64, Configuration = Configuration.Release, ExcludeFromPublishing =  true},

            // The arm64 tests are currently broken and should not be included by the top level test jobs.
            new JobConfiguration {SystemType = SystemType.MacOS, Architecture = Architecture.arm64, Configuration = Configuration.Debug, ExcludeFromPublishing =  true, ExcludeFromTesting =  true},
            new JobConfiguration {SystemType = SystemType.MacOS, Architecture = Architecture.arm64, Configuration = Configuration.Release, ExcludeFromPublishing =  true, ExcludeFromTesting =  true},

            // Linux
            new JobConfiguration {SystemType = SystemType.Ubuntu, Architecture = Architecture.x64, Configuration = Configuration.Debug},
            new JobConfiguration {SystemType = SystemType.Ubuntu, Architecture = Architecture.x64, Configuration = Configuration.Release},
        };
}
