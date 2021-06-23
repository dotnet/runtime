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
            => _buildPaths.Add(buildArgs, product);

        public void RemoveFromCache(string buildPath)
        {
            KeyValuePair<BuildArgs, BuildProduct>? foundKvp = _buildPaths.Where(kvp => kvp.Value.ProjectDir == buildPath).SingleOrDefault();
            if (foundKvp == null)
                throw new Exception($"Could not find build path {buildPath} in cache to remove.");

            _buildPaths.Remove(foundKvp.Value.Key);
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
