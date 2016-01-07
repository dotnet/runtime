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
        private static Lazy<string> _refsLocation = new Lazy<string>(GetRefsLocation);

        public CompilationLibrary(string libraryType, string packageName, string version, string hash, string[] assemblies, Dependency[] dependencies, bool serviceable)
            : base(libraryType, packageName, version, hash,  dependencies, serviceable)
        {
            Assemblies = assemblies;
        }

        public IReadOnlyList<string> Assemblies { get; }

        public IEnumerable<string> ResolveReferencePaths()
        {
            var basePath = _refsLocation.Value;

            foreach (var assembly in Assemblies)
            {
                var fullName = Path.Combine(basePath, Path.GetFileName(assembly));
                if (!File.Exists(fullName))
                {
                    throw new InvalidOperationException($"Can not resolve assembly {assembly} location");
                }
                yield return fullName;
            }
        }

        private static string GetRefsLocation()
        {
            var entryAssembly = (Assembly)typeof(Assembly).GetTypeInfo().GetDeclaredMethod("GetEntryAssembly").Invoke(null, null);
            if (entryAssembly == null)
            {
                throw new InvalidOperationException("Could not determine entry assembly");
            }

            return Path.Combine(Path.GetDirectoryName(entryAssembly.Location), "refs");
        }
    }
}