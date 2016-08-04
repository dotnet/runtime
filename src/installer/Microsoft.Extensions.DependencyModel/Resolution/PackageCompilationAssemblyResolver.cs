// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.PlatformAbstractions;

namespace Microsoft.Extensions.DependencyModel.Resolution
{
    public class PackageCompilationAssemblyResolver: ICompilationAssemblyResolver
    {
        private readonly IFileSystem _fileSystem;
        private readonly string _nugetPackageDirectory;

        public PackageCompilationAssemblyResolver()
            : this(EnvironmentWrapper.Default, FileSystemWrapper.Default)
        {
        }

        public PackageCompilationAssemblyResolver(string nugetPackageDirectory)
            : this(FileSystemWrapper.Default, nugetPackageDirectory)
        {
        }

        internal PackageCompilationAssemblyResolver(IEnvironment environment,
            IFileSystem fileSystem)
            : this(fileSystem, GetDefaultPackageDirectory(environment))
        {
        }

        internal PackageCompilationAssemblyResolver(IFileSystem fileSystem, string nugetPackageDirectory)
        {
            _fileSystem = fileSystem;
            _nugetPackageDirectory = nugetPackageDirectory;
        }

        private static string GetDefaultPackageDirectory(IEnvironment environment) => 
            GetDefaultPackageDirectory(RuntimeEnvironment.OperatingSystemPlatform, environment);

        internal static string GetDefaultPackageDirectory(Platform osPlatform, IEnvironment environment)
        {
            var packageDirectory = environment.GetEnvironmentVariable("NUGET_PACKAGES");

            if (!string.IsNullOrEmpty(packageDirectory))
            {
                return packageDirectory;
            }

            string basePath;
            if (osPlatform == Platform.Windows)
            {
                basePath = environment.GetEnvironmentVariable("USERPROFILE");
            }
            else
            {
                basePath = environment.GetEnvironmentVariable("HOME");
            }
            if (string.IsNullOrEmpty(basePath))
            {
                return null;
            }
            return Path.Combine(basePath, ".nuget", "packages");
        }

        public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string> assemblies)
        {
            if (string.IsNullOrEmpty(_nugetPackageDirectory) ||
                !string.Equals(library.Type, "package", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string packagePath;

            if (ResolverUtils.TryResolvePackagePath(_fileSystem, library, _nugetPackageDirectory, out packagePath))
            {
                assemblies.AddRange(ResolverUtils.ResolveFromPackagePath(_fileSystem, library, packagePath));
                return true;
            }
            return false;
        }
    }
}