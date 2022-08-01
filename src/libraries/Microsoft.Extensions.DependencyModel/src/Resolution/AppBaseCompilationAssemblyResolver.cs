// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

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
            ThrowHelper.ThrowIfNull(fileSystem);
            ThrowHelper.ThrowIfNull(basePath);
            ThrowHelper.ThrowIfNull(dependencyContextPaths);

            _fileSystem = fileSystem;
            _basePath = basePath;
            _dependencyContextPaths = dependencyContextPaths;
        }

        public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string>? assemblies)
        {
            ThrowHelper.ThrowIfNull(library);

            bool isProject = string.Equals(library.Type, "project", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(library.Type, "msbuildproject", StringComparison.OrdinalIgnoreCase);

            bool isPackage = string.Equals(library.Type, "package", StringComparison.OrdinalIgnoreCase);
            bool isReferenceAssembly = string.Equals(library.Type, "referenceassembly", StringComparison.OrdinalIgnoreCase);
            if (!isProject &&
                !isPackage &&
                !isReferenceAssembly &&
                !string.Equals(library.Type, "reference", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string refsPath = Path.Combine(_basePath, RefsDirectoryName);
            bool isPublished = _fileSystem.Directory.Exists(refsPath);

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
            string? sharedPath = _dependencyContextPaths.SharedRuntime;
            if (isPublished && isPackage && !string.IsNullOrEmpty(sharedPath))
            {
                string? sharedDirectory = Path.GetDirectoryName(sharedPath);
                Debug.Assert(sharedDirectory != null);

                string sharedRefs = Path.Combine(sharedDirectory, RefsDirectoryName);
                if (_fileSystem.Directory.Exists(sharedRefs))
                {
                    directories.Add(sharedRefs);
                }
                directories.Add(sharedDirectory);
            }

            var paths = new List<string>();

            foreach (string assembly in library.Assemblies)
            {
                bool resolved = false;
                string assemblyFile = Path.GetFileName(assembly);
                foreach (string directory in directories)
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
