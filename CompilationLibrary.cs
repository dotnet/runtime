// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.Extensions.DependencyModel
{
    public class CompilationLibrary : Library
    {
        public CompilationLibrary(string libraryType, string packageName, string version, string hash, string[] assemblies, Dependency[] dependencies, bool serviceable)
            : base(libraryType, packageName, version, hash,  dependencies, serviceable)
        {
            Assemblies = assemblies;
        }

        public IReadOnlyList<string> Assemblies { get; }

        public IEnumerable<string> ResolveReferencePaths()
        {
            var entryAssembly = Assembly.GetEntryAssembly();

            string basePath;

            var appBase = PlatformServices.Default.Application.ApplicationBasePath;
            var refsDir = Path.Combine(appBase, "refs");
            var hasRefs = Directory.Exists(refsDir);
            var isProject = string.Equals(LibraryType, "project", StringComparison.OrdinalIgnoreCase);
            var isReferenceAssembly = string.Equals(LibraryType, "referenceassembly", StringComparison.OrdinalIgnoreCase);

            if (!isProject && PackagePathResolver.TryResolvePackageCachePath(this, out basePath))
            {
                return ResolveFromPackagePath(basePath);
            }
            if (hasRefs || isProject)
            {
                var directories = new List<string>()
                {
                    appBase
                };

                if (hasRefs)
                {
                    directories.Add(refsDir);
                }
                return ResolveFromDirectories(directories.ToArray());
            }
            if (isReferenceAssembly)
            {
                return ResolveFromReferenceAssemblies();
            }
            if (PackagePathResolver.TryResolvePackagePath(this, out basePath))
            {
                return ResolveFromPackagePath(basePath);
            }
            throw new InvalidOperationException($"Can not find compilation library location for package '{PackageName}'");
        }

        private IEnumerable<string> ResolveFromPackagePath(string basePath)
        {
            foreach (var assembly in Assemblies)
            {
                string fullName;
                if (!TryResolveAssemblyFile(basePath, assembly, out fullName))
                {
                    throw new InvalidOperationException($"Can not find assembly file for package {PackageName} at '{fullName}'");
                }
                yield return fullName;
            }
        }

        private IEnumerable<string> ResolveFromReferenceAssemblies()
        {
            foreach (var assembly in Assemblies)
            {
                string fullName;
                if (!ReferenceAssemblyPathResolver.TryResolveReferenceAssembly(assembly, out fullName))
                {
                    throw new InvalidOperationException($"Can not find refernce assembly file for package {PackageName}: '{assembly}'");
                }
                yield return fullName;
            }
        }

        private IEnumerable<string> ResolveFromDirectories(string[] directories)
        {
            foreach (var assembly in Assemblies)
            {
                var assemblyFile = Path.GetFileName(assembly);
                foreach (var directory in directories)
                {
                    string fullName;
                    if (TryResolveAssemblyFile(directory, assemblyFile, out fullName))
                    {
                        yield return fullName;
                        break;
                    }

                    var errorMessage = $"Can not find assembly file {assemblyFile} at '{string.Join(",", directories)}'";
                    throw new InvalidOperationException(errorMessage);
                }
            }
        }

        private bool TryResolveAssemblyFile(string basePath, string assemblyPath, out string fullName)
        {
            fullName = Path.Combine(basePath, assemblyPath);
            if (File.Exists(fullName))
            {
                return true;
            }
            return false;
        }
    }
}