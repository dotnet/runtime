// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;

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

            // On Windows, a recently killed process (e.g. a web server whose working directory
            // is inside the build output) can keep handles open briefly, so retry transient failures.
            const int maxAttempts = 10;
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    if (Directory.Exists(path))
                        Directory.Delete(path, recursive: true);
                    return;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException && attempt < maxAttempts)
                {
                    Console.WriteLine($"Attempt {attempt} to delete '{path}' failed, retrying: {ex.Message}");
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed to delete '{path}' during test cleanup: {ex}");
                    throw;
                }
            }
        }

    }
}
