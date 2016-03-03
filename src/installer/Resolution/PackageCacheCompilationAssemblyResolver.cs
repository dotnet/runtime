// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.Extensions.DependencyModel.Resolution
{
    public class PackageCacheCompilationAssemblyResolver: ICompilationAssemblyResolver
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
            if (!string.Equals(library.LibraryType, "package", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(_packageCacheDirectory))
            {
                var hashSplitterPos = library.Hash.IndexOf('-');
                if (hashSplitterPos <= 0 || hashSplitterPos == library.Hash.Length - 1)
                {
                    throw new InvalidOperationException($"Invalid hash entry '{library.Hash}' for package '{library.PackageName}'");
                }

                string packagePath;
                if (ResolverUtils.TryResolvePackagePath(_fileSystem, library, _packageCacheDirectory, out packagePath))
                {
                    var hashAlgorithm = library.Hash.Substring(0, hashSplitterPos);
                    var cacheHashPath = Path.Combine(packagePath, $"{library.PackageName}.{library.Version}.nupkg.{hashAlgorithm}");

                    if (_fileSystem.File.Exists(cacheHashPath) &&
                        _fileSystem.File.ReadAllText(cacheHashPath) == library.Hash.Substring(hashSplitterPos + 1))
                    {
                        assemblies.AddRange(ResolverUtils.ResolveFromPackagePath(_fileSystem, library, packagePath));
                        return true;
                    }
                }
            }
            return false;
        }

        internal static string GetDefaultPackageCacheDirectory(IEnvironment environment)
        {
            return environment.GetEnvironmentVariable("DOTNET_PACKAGES_CACHE");
        }
    }
}