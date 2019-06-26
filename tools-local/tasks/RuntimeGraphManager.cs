// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyModel;
using NuGet.Packaging;
using NuGet.ProjectModel;
using NuGet.RuntimeModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.Build.Tasks
{
    internal class RuntimeGraphManager
    {
        private const string RuntimeJsonFileName = "runtime.json";

        public RuntimeGraph Collect(LockFile lockFile)
        {
            string userPackageFolder = lockFile.PackageFolders.FirstOrDefault()?.Path;
            var fallBackFolders = lockFile.PackageFolders.Skip(1).Select(f => f.Path);
            var packageResolver = new FallbackPackagePathResolver(userPackageFolder, fallBackFolders);

            var graph = RuntimeGraph.Empty;
            foreach (var library in lockFile.Libraries)
            {
                if (string.Equals(library.Type, "package", StringComparison.OrdinalIgnoreCase))
                {
                    var runtimeJson = library.Files.FirstOrDefault(f => f == RuntimeJsonFileName);
                    if (runtimeJson != null)
                    {
                        var libraryPath = packageResolver.GetPackageDirectory(library.Name, library.Version);
                        var runtimeJsonFullName = Path.Combine(libraryPath, runtimeJson);
                        graph = RuntimeGraph.Merge(graph, JsonRuntimeFormat.ReadRuntimeGraph(runtimeJsonFullName));
                    }
                }
            }
            return graph;
        }

        public IEnumerable<RuntimeFallbacks> Expand(RuntimeGraph runtimeGraph, string runtime)
        {
            var importers = FindImporters(runtimeGraph, runtime);
            foreach (var importer in importers)
            {
                // ExpandRuntime return runtime itself as first item so we are skiping it
                yield return new RuntimeFallbacks(importer, runtimeGraph.ExpandRuntime(importer).Skip(1));
            }
        }

        private IEnumerable<string> FindImporters(RuntimeGraph runtimeGraph, string runtime)
        {
            foreach (var runtimePair in runtimeGraph.Runtimes)
            {
                var expanded = runtimeGraph.ExpandRuntime(runtimePair.Key);
                if (expanded.Contains(runtime))
                {
                    yield return runtimePair.Key;
                }
            }
        }
    }
}
