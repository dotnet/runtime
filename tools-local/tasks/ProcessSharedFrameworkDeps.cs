// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Extensions.DependencyModel;
using NuGet.Common;
using NuGet.ProjectModel;
using System;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.Build.Tasks
{
    public partial class ProcessSharedFrameworkDeps : Task
    {
        [Required]
        public string AssetsFilePath { get; set; }

        [Required]
        public string DepsFilePath { get; set; }

        [Required]
        public string[] PackagesToRemove { get; set; }

        [Required]
        public string Runtime { get; set; }

        [Required]
        public string BuildTasksAssemblyPath { get; set; }

        public override bool Execute()
        {
            EnsureInitialized(BuildTasksAssemblyPath);

            ExecuteCore();

            return true;
        }

        private void ExecuteCore()
        {
            DependencyContext context;
            using (var depsStream = File.OpenRead(DepsFilePath))
            {
                context = new DependencyContextJsonReader().Read(depsStream);
            }
            
            LockFile lockFile = LockFileUtilities.GetLockFile(AssetsFilePath, NullLogger.Instance);
            if (lockFile == null)
            {
                throw new ArgumentException($"Could not load a LockFile at '{AssetsFilePath}'.", nameof(AssetsFilePath));
            }

            var manager = new RuntimeGraphManager();
            var graph = manager.Collect(lockFile);
            var expandedGraph = manager.Expand(graph, Runtime);

            var trimmedRuntimeLibraries = context.RuntimeLibraries;

            if (PackagesToRemove != null && PackagesToRemove.Any())
            {
                trimmedRuntimeLibraries = RuntimeReference.RemoveReferences(context.RuntimeLibraries, PackagesToRemove);
            }

            context = new DependencyContext(
                context.Target,
                context.CompilationOptions,
                context.CompileLibraries,
                trimmedRuntimeLibraries,
                expandedGraph
                );

            using (var depsStream = File.Create(DepsFilePath))
            {
                new DependencyContextWriter().Write(context, depsStream);
            }
        }

        partial void EnsureInitialized(string buildTasksAssemblyPath);
    }
}
