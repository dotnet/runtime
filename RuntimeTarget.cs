using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.DependencyModel
{
    public class RuntimeTarget
    {
        public RuntimeTarget(string runtime, IEnumerable<RuntimeAssembly> assemblies, IEnumerable<string> nativeLibraries)
        {
            if (string.IsNullOrEmpty(runtime))
            {
                throw new ArgumentException(nameof(runtime));
            }
            if (assemblies == null)
            {
                throw new ArgumentNullException(nameof(assemblies));
            }
            if (nativeLibraries == null)
            {
                throw new ArgumentNullException(nameof(nativeLibraries));
            }
            Runtime = runtime;
            Assemblies = assemblies.ToArray();
            NativeLibraries = nativeLibraries.ToArray();
        }

        public string Runtime { get; }

        public IReadOnlyList<RuntimeAssembly> Assemblies { get; }

        public IReadOnlyList<string> NativeLibraries { get; }
    }
}