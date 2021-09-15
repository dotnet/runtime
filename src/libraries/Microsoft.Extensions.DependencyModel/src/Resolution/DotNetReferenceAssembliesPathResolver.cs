// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.Extensions.DependencyModel.Resolution
{
    public class DotNetReferenceAssembliesPathResolver
    {
        public static readonly string DotNetReferenceAssembliesPathEnv = "DOTNET_REFERENCE_ASSEMBLIES_PATH";

        internal static string? Resolve(IEnvironment environment, IFileSystem fileSystem)
        {
            string? path = environment.GetEnvironmentVariable(DotNetReferenceAssembliesPathEnv);
            if (!string.IsNullOrEmpty(path))
            {
                return path;
            }

            return GetDefaultDotNetReferenceAssembliesPath(fileSystem);
        }

        public static string? Resolve()
        {
            return Resolve(EnvironmentWrapper.Default, FileSystemWrapper.Default);
        }

        private static string? GetDefaultDotNetReferenceAssembliesPath(IFileSystem fileSystem)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return null;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) &&
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
