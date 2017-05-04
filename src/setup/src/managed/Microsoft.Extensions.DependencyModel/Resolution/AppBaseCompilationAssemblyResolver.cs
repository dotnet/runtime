// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.PlatformAbstractions;

#if !NETSTANDARD1_3

namespace Microsoft.Extensions.DependencyModel.Resolution
{
    public class AppBaseCompilationAssemblyResolver : ICompilationAssemblyResolver
    {
        private static string RefsDirectoryName = "refs";
        private readonly IFileSystem _fileSystem;
        private readonly string _basePath;
        private readonly DependencyContextPaths _dependencyContextPaths;

        public AppBaseCompilationAssemblyResolver()
            : this(FileSystemWrapper.Default)
        {
        }

        public AppBaseCompilationAssemblyResolver(string basePath)
            : this(FileSystemWrapper.Default, basePath, DependencyContextPaths.Current)
        {
        }

        internal AppBaseCompilationAssemblyResolver(IFileSystem fileSystem)
            : this(fileSystem, ApplicationEnvironment.ApplicationBasePath, DependencyContextPaths.Current)
        {
        }

        internal AppBaseCompilationAssemblyResolver(IFileSystem fileSystem, string basePath, DependencyContextPaths dependencyContextPaths)
        {
            _fileSystem = fileSystem;
            _basePath = basePath;
            _dependencyContextPaths = dependencyContextPaths;
        }

        public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string> assemblies)
        {
            var isProject = string.Equals(library.Type, "project", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(library.Type, "msbuildproject", StringComparison.OrdinalIgnoreCase);

            var isPackage = string.Equals(library.Type, "package", StringComparison.OrdinalIgnoreCase);
            if (!isProject &&
                !isPackage &&
                !string.Equals(library.Type, "referenceassembly", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var refsPath = Path.Combine(_basePath, RefsDirectoryName);
            var isPublished = _fileSystem.Directory.Exists(refsPath);

            // Resolving reference assebmlies requires refs folder to exist
            if (!isProject && !isPackage && !isPublished)
            {
                return false;
            }

            var directories = new List<string>()
            {
                _basePath
            };

            if (isPublished)
            {
                directories.Insert(0, refsPath);
            }

            // Only packages can come from shared runtime
            var sharedPath = _dependencyContextPaths.SharedRuntime;
            if (isPublished && isPackage && !string.IsNullOrEmpty(sharedPath))
            {
                var sharedDirectory = Path.GetDirectoryName(sharedPath);
                var sharedRefs = Path.Combine(sharedDirectory, RefsDirectoryName);
                if (_fileSystem.Directory.Exists(sharedRefs))
                {
                    directories.Add(sharedRefs);
                }
                directories.Add(sharedDirectory);
            }

            foreach (var assembly in library.Assemblies)
            {
                bool resolved = false;
                var assemblyFile = Path.GetFileName(assembly);
                foreach (var directory in directories)
                {
                    string fullName;
                    if (ResolverUtils.TryResolveAssemblyFile(_fileSystem, directory, assemblyFile, out fullName))
                    {
                        assemblies.Add(fullName);
                        resolved = true;
                        break;
                    }
                }

                if (!resolved)
                {
                    // throw in case when we are published app and nothing found
                    // because we cannot rely on nuget package cache in this case
                    if (isPublished)
                    {
                    throw new InvalidOperationException(
                        $"Cannot find assembly file {assemblyFile} at '{string.Join(",", directories)}'");
                }
                    return false;
            }
            }

            return true;
        }
    }
}

#endif
