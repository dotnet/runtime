// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Framework;
using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.Extensions.DependencyModel;
using System.Collections.Generic;
using NuGet.RuntimeModel;

namespace Microsoft.DotNet.Build.Tasks
{
    public partial class GenerateTestSharedFrameworkDepsFile : BuildTask
    {
        // we don't care about these values in the deps file
        const string rid = "rid";
        const string tfm = "netcoreapp0.0";
        const string fullTfm = ".NETCoreApp,Version=v0.0";

        [Required]
        public string SharedFrameworkDirectory { get; set; }

        [Required]
        public string[] RuntimeGraphFiles { get; set; }

        [Required]
        public string TargetRuntimeIdentifier { get; set; }

        public override bool Execute()
        {
            var sharedFxDir = new DirectoryInfo(SharedFrameworkDirectory);
            if (!sharedFxDir.Exists)
            {
                Log.LogError($"{nameof(SharedFrameworkDirectory)} '{SharedFrameworkDirectory}' does not exist.");
                return false;
            }

            // directory is the version folder, parent is the shared framework name.
            string sharedFxVersion = sharedFxDir.Name;
            string sharedFxName = sharedFxDir.Parent.Name;

            var ignoredExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".pdb",
                ".json",
                ".config",
                ".xml"
            };

            var isAssemblyTofileNames = Directory.EnumerateFiles(SharedFrameworkDirectory)
                .Where(file => !ignoredExtensions.Contains(Path.GetExtension(file)))
                .ToLookup(file => IsManagedAssembly(file), file => Path.GetFileName(file));

            var managedFileNames = isAssemblyTofileNames[true];
            var nativeFileNames = isAssemblyTofileNames[false];

            var runtimeLibraries = new[]
            {
                new RuntimeLibrary(
                    type:"package",
                    name: sharedFxName,
                    version: sharedFxVersion,
                    hash: "hash",
                    runtimeAssemblyGroups: new[] { new RuntimeAssetGroup(string.Empty, managedFileNames.Select(f => $"runtimes/{rid}/lib/{tfm}/{f}")) },
                    nativeLibraryGroups: new[] { new RuntimeAssetGroup(string.Empty, nativeFileNames.Select(f => $"runtimes/{rid}/native/{f}")) },
                    resourceAssemblies: Enumerable.Empty<ResourceAssembly>(),
                    dependencies: Enumerable.Empty<Dependency>(),
                    serviceable: true)
            };

            var targetInfo = new TargetInfo(fullTfm, rid, "runtimeSignature", isPortable: false);

            var runtimeFallbacks = GetRuntimeFallbacks(RuntimeGraphFiles, TargetRuntimeIdentifier);

            var dependencyContext = new DependencyContext(
                targetInfo,
                CompilationOptions.Default,
                Enumerable.Empty<CompilationLibrary>(),
                runtimeLibraries,
                runtimeFallbacks);

            using (var depsFileStream = File.Create(Path.Combine(SharedFrameworkDirectory, $"{sharedFxName}.deps.json")))
            {
                new DependencyContextWriter().Write(dependencyContext, depsFileStream);
            }

            return !Log.HasLoggedErrors;
        }

        private static bool IsManagedAssembly(string file)
        {
            bool result = false;
            try
            {
                using (var peReader = new PEReader(File.OpenRead(file)))
                {
                    result = peReader.HasMetadata && peReader.GetMetadataReader().IsAssembly;
                }
            }
            catch (BadImageFormatException)
            { }

            return result;
        }

        private static IEnumerable<RuntimeFallbacks> GetRuntimeFallbacks(string[] runtimeGraphFiles, string runtime)
        {
            RuntimeGraph runtimeGraph = RuntimeGraph.Empty;

            foreach (string runtimeGraphFile in runtimeGraphFiles)
            {
                runtimeGraph = RuntimeGraph.Merge(runtimeGraph, JsonRuntimeFormat.ReadRuntimeGraph(runtimeGraphFile));
            }

            foreach (string rid in runtimeGraph.Runtimes.Select(p => p.Key))
            {
                IEnumerable<string> ridFallback = runtimeGraph.ExpandRuntime(rid);

                if (ridFallback.Contains(runtime))
                {
                    // ExpandRuntime return runtime itself as first item so we are skiping it
                    yield return new RuntimeFallbacks(rid, ridFallback.Skip(1));
                }
            }
        }

    }
}
