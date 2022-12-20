// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Microsoft.Extensions.DependencyModel.Resolution
{
    public class ReferenceAssemblyPathResolver: ICompilationAssemblyResolver
    {
        private readonly IFileSystem _fileSystem;
        private readonly string? _defaultReferenceAssembliesPath;
        private readonly string[] _fallbackSearchPaths;

        public ReferenceAssemblyPathResolver()
            : this(FileSystemWrapper.Default, EnvironmentWrapper.Default)
        {
        }

        public ReferenceAssemblyPathResolver(string? defaultReferenceAssembliesPath, string[] fallbackSearchPaths)
            : this(FileSystemWrapper.Default, defaultReferenceAssembliesPath, fallbackSearchPaths)
        {
        }

        internal ReferenceAssemblyPathResolver(IFileSystem fileSystem, IEnvironment environment)
            : this(fileSystem,
                GetDefaultReferenceAssembliesPath(fileSystem, environment),
                GetFallbackSearchPaths(fileSystem, environment))
        {
        }

        internal ReferenceAssemblyPathResolver(IFileSystem fileSystem, string? defaultReferenceAssembliesPath, string[] fallbackSearchPaths)
        {
            ThrowHelper.ThrowIfNull(fileSystem);
            ThrowHelper.ThrowIfNull(fallbackSearchPaths);

            _fileSystem = fileSystem;
            _defaultReferenceAssembliesPath = defaultReferenceAssembliesPath;
            _fallbackSearchPaths = fallbackSearchPaths;
        }

        public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string>? assemblies)
        {
            ThrowHelper.ThrowIfNull(library);

            if (!string.Equals(library.Type, "referenceassembly", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            foreach (string assembly in library.Assemblies)
            {
                if (!TryResolveReferenceAssembly(assembly, out string? fullName))
                {
                    throw new InvalidOperationException(SR.Format(SR.ReferenceAssemblyNotFound, assembly, library.Name));
                }
                assemblies?.Add(fullName);
            }
            return true;
        }

        private bool TryResolveReferenceAssembly(string path, [MaybeNullWhen(false)] out string fullPath)
        {
            fullPath = null;

            if (_defaultReferenceAssembliesPath != null)
            {
                string relativeToReferenceAssemblies = Path.Combine(_defaultReferenceAssembliesPath, path);
                if (_fileSystem.File.Exists(relativeToReferenceAssemblies))
                {
                    fullPath = relativeToReferenceAssemblies;
                    return true;
                }
            }

            string name = Path.GetFileName(path);

            foreach (string fallbackPath in _fallbackSearchPaths)
            {
                string fallbackFile = Path.Combine(fallbackPath, name);
                if (_fileSystem.File.Exists(fallbackFile))
                {
                    fullPath = fallbackFile;
                    return true;
                }
            }

            return false;
        }

        internal static string[] GetFallbackSearchPaths(IFileSystem fileSystem, IEnvironment environment)
        {
            if (!environment.IsWindows())
            {
                return Array.Empty<string>();
            }

            string? windir = environment.GetEnvironmentVariable("WINDIR");
            if (windir == null)
            {
                return Array.Empty<string>();
            }

            string net20Dir = Path.Combine(windir, "Microsoft.NET", "Framework", "v2.0.50727");
            if (!fileSystem.Directory.Exists(net20Dir))
            {
                return Array.Empty<string>();
            }

            return new[] { net20Dir };
        }

        internal static string? GetDefaultReferenceAssembliesPath(IFileSystem fileSystem, IEnvironment environment)
        {
            // Allow setting the reference assemblies path via an environment variable
            string? referenceAssembliesPath = DotNetReferenceAssembliesPathResolver.Resolve(environment, fileSystem);
            if (!string.IsNullOrEmpty(referenceAssembliesPath))
            {
                return referenceAssembliesPath;
            }

            if (!environment.IsWindows())
            {
                // There is no reference assemblies path outside of windows
                // The environment variable can be used to specify one
                return null;
            }

            // References assemblies are in %ProgramFiles(x86)% on
            // 64 bit machines
            string? programFiles = environment.GetEnvironmentVariable("ProgramFiles(x86)");

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
