// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;

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
            var isReferenceAssembly = string.Equals(library.Type, "referenceassembly", StringComparison.OrdinalIgnoreCase);
            if (!isProject &&
                !isPackage &&
                !isReferenceAssembly &&
                !string.Equals(library.Type, "reference", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var refsPath = Path.Combine(_basePath, RefsDirectoryName);
            var isPublished = _fileSystem.Directory.Exists(refsPath);

            // Resolving reference assemblies requires refs folder to exist
            if (isReferenceAssembly && !isPublished)
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

            var paths = new List<string>();

            foreach (var assembly in library.Assemblies)
            {
                bool resolved = false;
                var assemblyFile = Path.GetFileName(assembly);
                foreach (var directory in directories)
                {
                    string fullName;
                    if (ResolverUtils.TryResolveAssemblyFile(_fileSystem, directory, assemblyFile, out fullName))
                    {
                        paths.Add(fullName);
                        resolved = true;
                        break;
                    }
                }

                if (!resolved)
                {
                    return false;
                }
            }

            // only modify the assemblies parameter if we've resolved all files
            assemblies?.AddRange(paths);
            return true;
        }
    }
}

#endif
