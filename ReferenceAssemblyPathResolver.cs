// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.Extensions.DependencyModel
{
    public class ReferenceAssemblyPathResolver
    {
        private static readonly Lazy<string> _defaultReferenceAssembliesPath = new Lazy<string>(GetDefaultReferenceAssembliesPath);
        private static readonly Lazy<string[]> _fallbackSearchPaths = new Lazy<string[]>(GetFallbackSearchPaths);

        private static string[] GetFallbackSearchPaths()
        {
            if (PlatformServices.Default.Runtime.OperatingSystemPlatform != Platform.Windows)
            {
                return new string[0];
            }

            var net20Dir = Path.Combine(Environment.GetEnvironmentVariable("WINDIR"), "Microsoft.NET", "Framework", "v2.0.50727");

            if (!Directory.Exists(net20Dir))
            {
                return new string[0];
            }
            return new[] { net20Dir };
        }

        public static string GetDefaultReferenceAssembliesPath()
        {
            // Allow setting the reference assemblies path via an environment variable
            var referenceAssembliesPath = Environment.GetEnvironmentVariable("DOTNET_REFERENCE_ASSEMBLIES_PATH");

            if (!string.IsNullOrEmpty(referenceAssembliesPath))
            {
                return referenceAssembliesPath;
            }

            if (PlatformServices.Default.Runtime.OperatingSystemPlatform != Platform.Windows)
            {
                // There is no reference assemblies path outside of windows
                // The environment variable can be used to specify one
                return null;
            }

            // References assemblies are in %ProgramFiles(x86)% on
            // 64 bit machines
            var programFiles = Environment.GetEnvironmentVariable("ProgramFiles(x86)");

            if (string.IsNullOrEmpty(programFiles))
            {
                // On 32 bit machines they are in %ProgramFiles%
                programFiles = Environment.GetEnvironmentVariable("ProgramFiles");
            }

            if (string.IsNullOrEmpty(programFiles))
            {
                // Reference assemblies aren't installed
                return null;
            }

            return Path.Combine(
                programFiles,
                "Reference Assemblies", "Microsoft", "Framework");
        }

        public static bool TryResolveReferenceAssembly(string path, out string fullPath)
        {
            fullPath = null;

            var refereneAssembliesPath = _defaultReferenceAssembliesPath.Value;
            if (refereneAssembliesPath == null)
            {
                return false;
            }

            var relativeToReferenceAssemblies = Path.Combine(refereneAssembliesPath, path);
            if (File.Exists(relativeToReferenceAssemblies))
            {
                fullPath = relativeToReferenceAssemblies;
                return true;
            }

            var name = Path.GetFileName(path);
            foreach (var fallbackPath in _fallbackSearchPaths.Value)
            {
                var fallbackFile = Path.Combine(fallbackPath, name);
                if (File.Exists(fallbackFile))
                {
                    fullPath = fallbackFile;
                    return true;
                }
            }

            return false;
        }
    }
}