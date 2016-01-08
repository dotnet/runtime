// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Microsoft.Extensions.DependencyModel
{
    public class CompilationLibrary : Library
    {
        private static Lazy<Assembly> _entryAssembly = new Lazy<Assembly>(GetEntryAssembly);

        public CompilationLibrary(string libraryType, string packageName, string version, string hash, string[] assemblies, Dependency[] dependencies, bool serviceable)
            : base(libraryType, packageName, version, hash,  dependencies, serviceable)
        {
            Assemblies = assemblies;
        }

        public IReadOnlyList<string> Assemblies { get; }

        public IEnumerable<string> ResolveReferencePaths()
        {
            var entryAssembly = _entryAssembly.Value;
            var entryAssemblyName = entryAssembly.GetName().Name;
            var basePath = GetRefsLocation();

            foreach (var assembly in Assemblies)
            {
                if (Path.GetFileNameWithoutExtension(assembly) == entryAssemblyName)
                {
                    yield return entryAssembly.Location;
                    continue;
                }

                var fullName = Path.Combine(basePath, Path.GetFileName(assembly));
                if (!File.Exists(fullName))
                {
                    throw new InvalidOperationException($"Can not resolve assembly {assembly} location");
                }
                yield return fullName;
            }
        }
        private static Assembly GetEntryAssembly()
        {
            var entryAssembly = (Assembly)typeof(Assembly).GetTypeInfo().GetDeclaredMethod("GetEntryAssembly").Invoke(null, null);
            if (entryAssembly == null)
            {
                throw new InvalidOperationException("Could not determine entry assembly");
            }
            return entryAssembly;
        }

        private static string GetRefsLocation()
        {
            return Path.Combine(Path.GetDirectoryName(_entryAssembly.Value.Location), "refs");
        }
    }
}