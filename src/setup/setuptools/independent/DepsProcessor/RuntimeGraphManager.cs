using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.ProjectModel.Compilation;
using Microsoft.DotNet.ProjectModel.Graph;
using Microsoft.Extensions.DependencyModel;
using NuGet.RuntimeModel;

namespace Microsoft.DotNet.ProjectModel
{
    public class RuntimeGraphManager
    {
        private const string RuntimeJsonFileName = "runtime.json";

        public NuGet.RuntimeModel.RuntimeGraph Collect(IEnumerable<LibraryExport> exports)
        {
            var graph = RuntimeGraph.Empty;
            foreach (var export in exports)
            {
                if (export.Library.Identity.Type == LibraryType.Package)
                {
                    PackageDescription description = (PackageDescription) export.Library;
                    var runtimeJson =  description.PackageLibrary.Files.FirstOrDefault(f => f == RuntimeJsonFileName);
                    if (runtimeJson != null)
                    {
                        // Convert the package-name to lower-case in the path to lookup runtime.json
                        string lowercasedPackageName = description.Identity.Name.ToLower();
                        string pathToPackage = export.Library.Path.Replace(description.Identity.Name, lowercasedPackageName);

                        var runtimeJsonFullName = Path.Combine(pathToPackage, runtimeJson);
                        graph = RuntimeGraph.Merge(graph, JsonRuntimeFormat.ReadRuntimeGraph(runtimeJsonFullName));
                    }
                }
            }
            return graph;
        }

        public IEnumerable<RuntimeFallbacks> Expand(RuntimeGraph runtimeGraph, IEnumerable<string> runtimes)
        {
            foreach (var runtime in runtimes)
            {
                var importers = FindImporters(runtimeGraph, runtime);
                foreach (var importer in importers)
                {
                    // ExpandRuntime return runtime itself as first item so we are skiping it
                    yield return new RuntimeFallbacks(importer, runtimeGraph.ExpandRuntime(importer).Skip(1));
                }
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
