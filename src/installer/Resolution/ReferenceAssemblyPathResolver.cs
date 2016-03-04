// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.Extensions.DependencyModel.Resolution
{
    public class ReferenceAssemblyPathResolver: ICompilationAssemblyResolver
    {
        private readonly IFileSystem _fileSystem;
        private readonly string _defaultReferenceAssembliesPath;
        private readonly string[] _fallbackSearchPaths;

        public ReferenceAssemblyPathResolver()
            : this(FileSystemWrapper.Default, PlatformServices.Default.Runtime, EnvironmentWrapper.Default)
        {
        }

        public ReferenceAssemblyPathResolver(string defaultReferenceAssembliesPath, string[] fallbackSearchPaths)
            : this(FileSystemWrapper.Default, defaultReferenceAssembliesPath, fallbackSearchPaths)
        {
        }

        internal ReferenceAssemblyPathResolver(IFileSystem fileSystem, IRuntimeEnvironment runtimeEnvironment, IEnvironment environment)
            : this(fileSystem,
                GetDefaultReferenceAssembliesPath(runtimeEnvironment, environment),
                GetFallbackSearchPaths(fileSystem, runtimeEnvironment, environment))
        {
        }

        internal ReferenceAssemblyPathResolver(IFileSystem fileSystem, string defaultReferenceAssembliesPath, string[] fallbackSearchPaths)
        {
            _fileSystem = fileSystem;
            _defaultReferenceAssembliesPath = defaultReferenceAssembliesPath;
            _fallbackSearchPaths = fallbackSearchPaths;
        }

        public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string> assemblies)
        {
            if (!string.Equals(library.Type, "referenceassembly", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            foreach (var assembly in library.Assemblies)
            {
                string fullName;
                if (!TryResolveReferenceAssembly(assembly, out fullName))
                {
                    throw new InvalidOperationException($"Can not find reference assembly '{assembly}' file for package {library.Name}");
                }
                assemblies.Add(fullName);
            }
            return true;
        }

        private bool TryResolveReferenceAssembly(string path, out string fullPath)
        {
            fullPath = null;

            if (_defaultReferenceAssembliesPath != null)
            {
                var relativeToReferenceAssemblies = Path.Combine(_defaultReferenceAssembliesPath, path);
                if (_fileSystem.File.Exists(relativeToReferenceAssemblies))
                {
                    fullPath = relativeToReferenceAssemblies;
                    return true;
                }
            }

            var name = Path.GetFileName(path);
            foreach (var fallbackPath in _fallbackSearchPaths)
            {
                var fallbackFile = Path.Combine(fallbackPath, name);
                if (_fileSystem.File.Exists(fallbackFile))
                {
                    fullPath = fallbackFile;
                    return true;
                }
            }

            return false;
        }

        internal static string[] GetFallbackSearchPaths(IFileSystem fileSystem, IRuntimeEnvironment runtimeEnvironment, IEnvironment environment)
        {
            if (runtimeEnvironment.OperatingSystemPlatform != Platform.Windows)
            {
                return new string[0];
            }

            var net20Dir = Path.Combine(environment.GetEnvironmentVariable("WINDIR"), "Microsoft.NET", "Framework", "v2.0.50727");

            if (!fileSystem.Directory.Exists(net20Dir))
            {
                return new string[0];
            }
            return new[] { net20Dir };
        }

        internal static string GetDefaultReferenceAssembliesPath(IRuntimeEnvironment runtimeEnvironment, IEnvironment environment)
        {
            // Allow setting the reference assemblies path via an environment variable
            var referenceAssembliesPath = environment.GetEnvironmentVariable("DOTNET_REFERENCE_ASSEMBLIES_PATH");

            if (!string.IsNullOrEmpty(referenceAssembliesPath))
            {
                return referenceAssembliesPath;
            }

            if (runtimeEnvironment.OperatingSystemPlatform != Platform.Windows)
            {
                // There is no reference assemblies path outside of windows
                // The environment variable can be used to specify one
                return null;
            }

            // References assemblies are in %ProgramFiles(x86)% on
            // 64 bit machines
            var programFiles = environment.GetEnvironmentVariable("ProgramFiles(x86)");

            if (string.IsNullOrEmpty(programFiles))
            {
                // On 32 bit machines they are in %ProgramFiles%
                programFiles = environment.GetEnvironmentVariable("ProgramFiles");
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

    }
}