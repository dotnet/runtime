// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Microsoft.Extensions.DependencyModel.Resolution
{
    public class PackageCompilationAssemblyResolver : ICompilationAssemblyResolver
    {
        private readonly IFileSystem _fileSystem;
        private readonly string[] _nugetPackageDirectories;

        public PackageCompilationAssemblyResolver()
            : this(EnvironmentWrapper.Default, FileSystemWrapper.Default)
        {
        }

        public PackageCompilationAssemblyResolver(string nugetPackageDirectory)
            : this(FileSystemWrapper.Default, [nugetPackageDirectory])
        {
        }

        internal PackageCompilationAssemblyResolver(IEnvironment environment,
            IFileSystem fileSystem)
            : this(fileSystem, GetDefaultProbeDirectories(environment))
        {
        }

        internal PackageCompilationAssemblyResolver(IFileSystem fileSystem, string[] nugetPackageDirectories)
        {
            ArgumentNullException.ThrowIfNull(fileSystem);
            ArgumentNullException.ThrowIfNull(nugetPackageDirectories);

            _fileSystem = fileSystem;
            _nugetPackageDirectories = nugetPackageDirectories;
        }

        internal static string[] GetDefaultProbeDirectories(IEnvironment environment)
        {
            object? probeDirectories = environment.GetAppContextData("PROBING_DIRECTORIES");

            string? listOfDirectories = probeDirectories as string;

            if (!string.IsNullOrEmpty(listOfDirectories))
            {
                return listOfDirectories.Split([Path.PathSeparator], StringSplitOptions.RemoveEmptyEntries);
            }

            string? packageDirectory = environment.GetEnvironmentVariable("NUGET_PACKAGES");

            if (!string.IsNullOrEmpty(packageDirectory))
            {
                return [packageDirectory];
            }

            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            if (string.IsNullOrEmpty(basePath))
            {
                return [string.Empty];
            }

            return [Path.Combine(basePath, ".nuget", "packages")];
        }

        public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string>? assemblies)
        {
            ArgumentNullException.ThrowIfNull(library);

            if (_nugetPackageDirectories == null || _nugetPackageDirectories.Length == 0 ||
                !string.Equals(library.Type, "package", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            foreach (string directory in _nugetPackageDirectories)
            {
                string packagePath;

                if (ResolverUtils.TryResolvePackagePath(_fileSystem, library, directory, out packagePath))
                {
                    if (TryResolveFromPackagePath(_fileSystem, library, packagePath, out IEnumerable<string>? fullPathsFromPackage))
                    {
                        assemblies?.AddRange(fullPathsFromPackage);
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool TryResolveFromPackagePath(IFileSystem fileSystem, CompilationLibrary library, string basePath, [MaybeNullWhen(false)] out IEnumerable<string> results)
        {
            List<string> paths = [];

            foreach (string assembly in library.Assemblies)
            {
                if (!ResolverUtils.TryResolveAssemblyFile(fileSystem, basePath, assembly, out string fullName))
                {
                    // if one of the files can't be found, skip this package path completely.
                    // there are package paths that don't include all of the "ref" assemblies
                    // (ex. ones created by 'dotnet store')
                    results = null;
                    return false;
                }

                paths.Add(fullName);
            }

            results = paths;
            return true;
        }
    }
}
