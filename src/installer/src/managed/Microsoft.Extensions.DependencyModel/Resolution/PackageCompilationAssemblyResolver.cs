// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Extensions.DependencyModel.Resolution
{
    public class PackageCompilationAssemblyResolver: ICompilationAssemblyResolver
    {
        private readonly IFileSystem _fileSystem;
        private readonly string[] _nugetPackageDirectories;

        public PackageCompilationAssemblyResolver()
            : this(EnvironmentWrapper.Default, FileSystemWrapper.Default)
        {
        }

        public PackageCompilationAssemblyResolver(string nugetPackageDirectory)
            : this(FileSystemWrapper.Default, new string[] { nugetPackageDirectory })
        {
        }

        internal PackageCompilationAssemblyResolver(IEnvironment environment,
            IFileSystem fileSystem)
            : this(fileSystem, GetDefaultProbeDirectories(environment))
        {
        }

        internal PackageCompilationAssemblyResolver(IFileSystem fileSystem, string[] nugetPackageDirectories)
        {
            _fileSystem = fileSystem;
            _nugetPackageDirectories = nugetPackageDirectories;
        }

        internal static string[] GetDefaultProbeDirectories(IEnvironment environment)
        {
#if !NETSTANDARD1_3            
#if NETSTANDARD1_6
            var probeDirectories = AppContext.GetData("PROBING_DIRECTORIES");
#else
            var probeDirectories = AppDomain.CurrentDomain.GetData("PROBING_DIRECTORIES");
#endif

            var listOfDirectories = probeDirectories as string;

            if (!string.IsNullOrEmpty(listOfDirectories))
            {
                return listOfDirectories.Split(new char[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries);
            }
#endif

            var packageDirectory = environment.GetEnvironmentVariable("NUGET_PACKAGES");

            if (!string.IsNullOrEmpty(packageDirectory))
            {
                return new string[] { packageDirectory };
            }

            string basePath;
            if (environment.IsWindows())
            {
                basePath = environment.GetEnvironmentVariable("USERPROFILE");
            }
            else
            {
                basePath = environment.GetEnvironmentVariable("HOME");
            }

            if (string.IsNullOrEmpty(basePath))
            {
                return new string[] { string.Empty };
            }

            return new string[] { Path.Combine(basePath, ".nuget", "packages") };
        }

        public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string> assemblies)
        {
            if (_nugetPackageDirectories == null || _nugetPackageDirectories.Length == 0 ||
                !string.Equals(library.Type, "package", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            foreach (var directory in _nugetPackageDirectories)
            {
                string packagePath;

                if (ResolverUtils.TryResolvePackagePath(_fileSystem, library, directory, out packagePath))
                {
                    IEnumerable<string> fullPathsFromPackage;
                    if (TryResolveFromPackagePath(_fileSystem, library, packagePath, out fullPathsFromPackage))
                    {
                        assemblies.AddRange(fullPathsFromPackage);
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool TryResolveFromPackagePath(IFileSystem fileSystem, CompilationLibrary library, string basePath, out IEnumerable<string> results)
        {
            var paths = new List<string>();

            foreach (var assembly in library.Assemblies)
            {
                string fullName;
                if (!ResolverUtils.TryResolveAssemblyFile(fileSystem, basePath, assembly, out fullName))
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
