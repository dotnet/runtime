// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.Extensions.DependencyModel.Resolution
{
    internal static class ResolverUtils
    {
        internal static bool TryResolvePackagePath(IFileSystem fileSystem, CompilationLibrary library, string basePath, out string packagePath)
        {
            string? path = library.Path;
            if (string.IsNullOrEmpty(path))
            {
                path = Path.Combine(library.Name, library.Version);
            }

            packagePath = Path.Combine(basePath, path);

            if (fileSystem.Directory.Exists(packagePath))
            {
                return true;
            }
            return false;
        }

        internal static bool TryResolveAssemblyFile(IFileSystem fileSystem, string basePath, string assemblyPath, out string fullName)
        {
            fullName = Path.Combine(basePath, assemblyPath);
            if (fileSystem.File.Exists(fullName))
            {
                return true;
            }
            return false;
        }
    }
}
