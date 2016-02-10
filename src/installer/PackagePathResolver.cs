// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.Extensions.DependencyModel
{
    public class PackagePathResolver
    {
        private static string _nugetPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES") ?? GetDefaultPackageDirectory();
        private static string _packageCache = Environment.GetEnvironmentVariable("DOTNET_PACKAGES_CACHE");

        internal static bool TryResolvePackageCachePath(CompilationLibrary library, out string packagePath)
        {
            packagePath = null;

            if (!string.IsNullOrEmpty(_packageCache))
            {
                var hashSplitterPos = library.Hash.IndexOf('-');
                if (hashSplitterPos <= 0 || hashSplitterPos == library.Hash.Length - 1)
                {
                    throw new InvalidOperationException($"Invalid hash entry '{library.Hash}' for package '{library.PackageName}'");
                }

                var hashAlgorithm = library.Hash.Substring(0, hashSplitterPos);

                var cacheHashPath = Path.Combine(_packageCache, $"{library.PackageName}.{library.Version}.nupkg.{hashAlgorithm}");

                if (File.Exists(cacheHashPath) &&
                    File.ReadAllText(cacheHashPath) == library.Hash.Substring(hashSplitterPos + 1))
                {
                    if (TryResolvePackagePath(library, _nugetPackages, out packagePath))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        internal static bool TryResolvePackagePath(CompilationLibrary library, out string packagePath)
        {
            packagePath = null;

            if (!string.IsNullOrEmpty(_nugetPackages) &&
                TryResolvePackagePath(library, _nugetPackages, out packagePath))
            {
                return true;
            }
            return false;
        }

        private static string GetDefaultPackageDirectory()
        {
            string basePath;
            if (PlatformServices.Default.Runtime.OperatingSystemPlatform == Platform.Windows)
            {
                basePath = Environment.GetEnvironmentVariable("USERPROFILE");
            }
            else
            {
                basePath = Environment.GetEnvironmentVariable("HOME");
            }
            if (string.IsNullOrEmpty(basePath))
            {
                return null;
            }
            return Path.Combine(basePath, ".nuget", "packages");
        }

        private static bool TryResolvePackagePath(CompilationLibrary library,  string basePath, out string packagePath)
        {
            packagePath = Path.Combine(basePath, library.PackageName, library.Version);
            if (Directory.Exists(packagePath))
            {
                return true;
            }
            return false;
        }

    }
}
