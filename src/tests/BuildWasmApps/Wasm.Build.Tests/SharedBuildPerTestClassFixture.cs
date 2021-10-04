// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

#nullable enable

namespace Wasm.Build.Tests
{
    public class SharedBuildPerTestClassFixture : IDisposable
    {
        public Dictionary<BuildArgs, BuildProduct> _buildPaths = new();

        public void CacheBuild(BuildArgs buildArgs, BuildProduct product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));
            if (buildArgs == null)
                throw new ArgumentNullException(nameof(buildArgs));
            _buildPaths.Add(buildArgs, product);
        }

        public void RemoveFromCache(string buildPath, bool keepDir=true)
        {
            BuildArgs? foundBuildArgs = _buildPaths.Where(kvp => kvp.Value.ProjectDir == buildPath).Select(kvp => kvp.Key).SingleOrDefault();
            if (foundBuildArgs is not null)
                _buildPaths.Remove(foundBuildArgs);

            if (!keepDir)
                RemoveDirectory(buildPath);
        }

        public bool TryGetBuildFor(BuildArgs buildArgs, [NotNullWhen(true)] out BuildProduct? product)
            => _buildPaths.TryGetValue(buildArgs, out product);

        public void Dispose()
        {
            Console.WriteLine ($"============== DELETING THE BUILDS =============");
            foreach (var kvp in _buildPaths.Values)
            {
                RemoveDirectory(kvp.ProjectDir);
            }
        }

        private void RemoveDirectory(string path)
        {
            if (EnvironmentVariables.SkipProjectCleanup == "1")
                return;

            try
            {
                Directory.Delete(path, recursive: true);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to delete '{path}' during test cleanup: {ex}");
                throw;
            }
        }

    }
}
