// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

            List<RuntimeFile> runtimeFiles = [];
            List<RuntimeFile> nativeFiles = [];

            foreach (string filePath in Directory.EnumerateFiles(SharedFrameworkDirectory))
            {
                if (ignoredExtensions.Contains(Path.GetExtension(filePath)))
                    continue;

                string fileName = Path.GetFileName(filePath);
                string fileVersion = FileUtilities.GetFileVersion(filePath)?.ToString() ?? string.Empty;
                Version assemblyVersion = FileUtilities.GetAssemblyName(filePath)?.Version;
                if (assemblyVersion is null)
                {
                    RuntimeFile nativeFile = new RuntimeFile(fileName, null, fileVersion);
                    nativeFiles.Add(nativeFile);
                }
                else
                {
                    RuntimeFile runtimeFile = new RuntimeFile(fileName, assemblyVersion.ToString(), fileVersion);
                    runtimeFiles.Add(runtimeFile);
                }
            }

            var runtimeLibraries = new[]
            {
                new RuntimeLibrary(
                    type:"package",
                    name: sharedFxName,
                    version: sharedFxVersion,
                    hash: "hash",
                    runtimeAssemblyGroups: new[] { new RuntimeAssetGroup(string.Empty, runtimeFiles) },
                    nativeLibraryGroups: new[] { new RuntimeAssetGroup(string.Empty, nativeFiles) },
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
