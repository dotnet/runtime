// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.Extensions.DependencyModel.Resolution
{
    public class AppBaseCompilationAssemblyResolver : ICompilationAssemblyResolver
    {
        private readonly IFileSystem _fileSystem;
        private readonly string _basePath;

        public AppBaseCompilationAssemblyResolver()
            : this(FileSystemWrapper.Default)
        {
        }

        public AppBaseCompilationAssemblyResolver(string basePath) : this(FileSystemWrapper.Default, basePath)
        {
        }

        internal AppBaseCompilationAssemblyResolver(IFileSystem fileSystem)
            : this(fileSystem, PlatformServices.Default.Application.ApplicationBasePath)
        {
        }

        internal AppBaseCompilationAssemblyResolver(IFileSystem fileSystem, string basePath)
        {
            _fileSystem = fileSystem;
            _basePath = basePath;
        }

        public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string> assemblies)
        {
            if (!string.Equals(library.LibraryType, "package", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(library.LibraryType, "project", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(library.LibraryType, "referenceassembly", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var directories = new List<string>()
            {
                _basePath
            };

            var refsPath = Path.Combine(_basePath, "refs");
            var hasRefs = _fileSystem.Directory.Exists(refsPath);

            if (hasRefs)
            {
                directories.Insert(0, refsPath);
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
                    throw new InvalidOperationException(
                        $"Can not find assembly file {assemblyFile} at '{string.Join(",", directories)}'");
                }
            }

            return true;
        }
    }
}