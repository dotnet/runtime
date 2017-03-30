// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyModel.Resolution
{
    public class PackageCacheCompilationAssemblyResolver : ICompilationAssemblyResolver
    {
        private readonly IFileSystem _fileSystem;
        private readonly string _packageCacheDirectory;

        public PackageCacheCompilationAssemblyResolver()
            : this(FileSystemWrapper.Default, EnvironmentWrapper.Default)
        {
        }

        public PackageCacheCompilationAssemblyResolver(string packageCacheDirectory)
            : this(FileSystemWrapper.Default, packageCacheDirectory)
        {
        }

        internal PackageCacheCompilationAssemblyResolver(IFileSystem fileSystem, IEnvironment environment)
            : this(fileSystem, GetDefaultPackageCacheDirectory(environment))
        {
        }

        internal PackageCacheCompilationAssemblyResolver(IFileSystem fileSystem, string packageCacheDirectory)
        {
            _packageCacheDirectory = packageCacheDirectory;
            _fileSystem = fileSystem;
        }

        public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string> assemblies)
        {
            if (!string.Equals(library.Type, "package", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(_packageCacheDirectory))
            {
                string packagePath;
                if (ResolverUtils.TryResolvePackagePath(_fileSystem, library, _packageCacheDirectory, out packagePath))
                {
                    assemblies.AddRange(ResolverUtils.ResolveFromPackagePath(_fileSystem, library, packagePath));
                    return true;
                }
            }
            return false;
        }

        internal static string GetDefaultPackageCacheDirectory(IEnvironment environment)
        {
            return environment.GetEnvironmentVariable("DOTNET_HOSTING_OPTIMIZATION_CACHE");
        }
    }
}