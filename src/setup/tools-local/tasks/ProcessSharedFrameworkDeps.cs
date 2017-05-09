using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Extensions.DependencyModel;
using NuGet.Common;
using NuGet.ProjectModel;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.Build.Tasks
{
    public class ProcessSharedFrameworkDeps : Task
    {
        [Required]
        public string AssetsFilePath { get; set; }

        [Required]
        public string DepsFilePath { get; set; }

        [Required]
        public string[] PackagesToRemove { get; set; }

        [Required]
        public string Runtime { get; set; }

        public override bool Execute()
        {
            DependencyContext context;
            using (var depsStream = File.OpenRead(DepsFilePath))
            {
                context = new DependencyContextJsonReader().Read(depsStream);
            }
            LockFile lockFile = LockFileUtilities.GetLockFile(AssetsFilePath, NullLogger.Instance);

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

            return true;
        }
    }
}
