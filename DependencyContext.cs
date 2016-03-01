// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyModel
{
    public class DependencyContext
    {
        private const string DepsResourceSufix = ".deps.json";
        private const string DepsFileExtension = ".deps";

        private static readonly Lazy<DependencyContext> _defaultContext = new Lazy<DependencyContext>(LoadDefault);

        public DependencyContext(string target,
            string runtime,
            bool isPortable,
            CompilationOptions compilationOptions,
            CompilationLibrary[] compileLibraries,
            RuntimeLibrary[] runtimeLibraries,
            IReadOnlyList<KeyValuePair<string, string[]>> runtimeGraph)
        {
            Target = target;
            Runtime = runtime;
            IsPortable = isPortable;
            CompilationOptions = compilationOptions;
            CompileLibraries = compileLibraries;
            RuntimeLibraries = runtimeLibraries;
            RuntimeGraph = runtimeGraph;
        }

        public static DependencyContext Default => _defaultContext.Value;

        public string Target { get; }

        public string Runtime { get; }

        public bool IsPortable { get; }

        public CompilationOptions CompilationOptions { get; }

        public IReadOnlyList<CompilationLibrary> CompileLibraries { get; }

        public IReadOnlyList<RuntimeLibrary> RuntimeLibraries { get; }

        public IReadOnlyList<KeyValuePair<string, string[]>> RuntimeGraph { get; }

        private static DependencyContext LoadDefault()
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            return Load(entryAssembly);
        }

        public static DependencyContext Load(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            using (var stream = assembly.GetManifestResourceStream(assembly.GetName().Name + DepsResourceSufix))
            {
                if (stream != null)
                {
                    return new DependencyContextJsonReader().Read(stream);
                }
            }

            var depsFile = Path.ChangeExtension(assembly.Location, DepsFileExtension);
            if (File.Exists(depsFile))
            {
                using (var stream = File.OpenRead(depsFile))
                {
                    return new DependencyContextCsvReader().Read(stream);
                }
            }

            return null;
        }
    }
}
