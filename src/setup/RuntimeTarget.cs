using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyModel
{
    public class RuntimeTarget
    {
        public RuntimeTarget(string runtime, IReadOnlyList<RuntimeAssembly> assemblies, IReadOnlyList<string> nativeLibraries)
        {
            Runtime = runtime;
            Assemblies = assemblies;
            NativeLibraries = nativeLibraries;
        }

        public string Runtime { get; }

        public IReadOnlyList<RuntimeAssembly> Assemblies { get; }

        public IReadOnlyList<string> NativeLibraries { get; }
    }
}