// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.PlatformAbstractions;

namespace Microsoft.Extensions.DependencyModel.Resolution
{
    public class DotNetReferenceAssembliesPathResolver
    {
        public static readonly string DotNetReferenceAssembliesPathEnv = "DOTNET_REFERENCE_ASSEMBLIES_PATH";

        internal static string Resolve(IEnvironment envirnment, IFileSystem fileSystem)
        {
            var path = envirnment.GetEnvironmentVariable(DotNetReferenceAssembliesPathEnv);
            if (!string.IsNullOrEmpty(path))
            {
                return path;
            }

            return GetDefaultDotNetReferenceAssembliesPath(fileSystem);
        }

        public static string Resolve()
        {
            return Resolve(EnvironmentWrapper.Default, FileSystemWrapper.Default);
        }

        private static string GetDefaultDotNetReferenceAssembliesPath(IFileSystem fileSystem)
        {
            var os = RuntimeEnvironment.OperatingSystemPlatform;

            if (os == Platform.Windows)
            {
                return null;
            }

            if (os == Platform.Darwin &&
                fileSystem.Directory.Exists("/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/xbuild-frameworks"))
            {
                return "/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/xbuild-frameworks";
            }

            if (fileSystem.Directory.Exists("/usr/local/lib/mono/xbuild-frameworks"))
            {
                return "/usr/local/lib/mono/xbuild-frameworks";
            }

            if (fileSystem.Directory.Exists("/usr/lib/mono/xbuild-frameworks"))
            {
                return "/usr/lib/mono/xbuild-frameworks";
            }

            return null;
        }
    }
}
