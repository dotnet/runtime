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

        private static Lazy<DependencyContext> _defaultContext = new Lazy<DependencyContext>(LoadDefault);

        public DependencyContext(string target, string runtime, CompilationOptions compilationOptions, Library[] compileLibraries, Library[] runtimeLibraries)
        {
            Target = target;
            Runtime = runtime;
            CompilationOptions = compilationOptions;
            CompileLibraries = compileLibraries;
            RuntimeLibraries = runtimeLibraries;
        }

        public static DependencyContext Default => _defaultContext.Value;

        public string Target { get; }

        public string Runtime { get; }

        public CompilationOptions CompilationOptions { get; }

        public IReadOnlyList<Library> CompileLibraries { get; }

        public IReadOnlyList<Library> RuntimeLibraries { get; }

        private static DependencyContext LoadDefault()
        {
            var entryAssembly = (Assembly)typeof(Assembly).GetTypeInfo().GetDeclaredMethod("GetEntryAssembly").Invoke(null, null);
            var stream = entryAssembly.GetManifestResourceStream(entryAssembly.GetName().Name + DepsResourceSufix);

            if (stream == null)
            {
                throw new InvalidOperationException("Entry assembly was compiled without `preserveCompilationContext` enabled");
            }

            using (stream)
            {
                return Load(stream);
            }
        }

        public static DependencyContext Load(Stream stream)
        {
            return new DependencyContextReader().Read(stream);
        }
    }
}
