// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

#if !NETSTANDARD1_3

namespace Microsoft.Extensions.DependencyModel
{
    public class DependencyContextLoader
    {
        private const string DepsJsonExtension = ".deps.json";

        private readonly string _entryPointDepsLocation;
        private readonly IEnumerable<string> _nonEntryPointDepsPaths;
        private readonly IFileSystem _fileSystem;
        private readonly Func<IDependencyContextReader> _jsonReaderFactory;

        public DependencyContextLoader() : this(
            DependencyContextPaths.Current.Application,
            DependencyContextPaths.Current.NonApplicationPaths,
            FileSystemWrapper.Default,
            () => new DependencyContextJsonReader())
        {
        }

        internal DependencyContextLoader(
            string entryPointDepsLocation,
            IEnumerable<string> nonEntryPointDepsPaths,
            IFileSystem fileSystem,
            Func<IDependencyContextReader> jsonReaderFactory)
        {
            _entryPointDepsLocation = entryPointDepsLocation;
            _nonEntryPointDepsPaths = nonEntryPointDepsPaths;
            _fileSystem = fileSystem;
            _jsonReaderFactory = jsonReaderFactory;
        }

        public static DependencyContextLoader Default { get; } = new DependencyContextLoader();

        private static bool IsEntryAssembly(Assembly assembly)
        {
            return assembly.Equals(Assembly.GetEntryAssembly());
        }

        private static Stream GetResourceStream(Assembly assembly, string name)
        {
            return assembly.GetManifestResourceStream(name);
        }

        public DependencyContext Load(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            DependencyContext context = null;
            using (var reader = _jsonReaderFactory())
            {
                if (IsEntryAssembly(assembly))
                {
                    context = LoadEntryAssemblyContext(reader);
                }

                if (context == null)
                {
                    context = LoadAssemblyContext(assembly, reader);
                }

                if (context != null)
                {
                    foreach (var extraPath in _nonEntryPointDepsPaths)
                    {
                        var extraContext = LoadContext(reader, extraPath);
                        if (extraContext != null)
                        {
                            context = context.Merge(extraContext);
                        }
                    }
                }
            }
            return context;
        }

        private DependencyContext LoadEntryAssemblyContext(IDependencyContextReader reader)
        {
            return LoadContext(reader, _entryPointDepsLocation);
        }

        private DependencyContext LoadContext(IDependencyContextReader reader, string location)
        {
            if (!string.IsNullOrEmpty(location))
            {
                Debug.Assert(_fileSystem.File.Exists(location));
                using (var stream = _fileSystem.File.OpenRead(location))
                {
                    return reader.Read(stream);
                }
            }
            return null;
        }

        private DependencyContext LoadAssemblyContext(Assembly assembly, IDependencyContextReader reader)
        {
            using (var stream = GetResourceStream(assembly, assembly.GetName().Name + DepsJsonExtension))
            {
                if (stream != null)
                {
                    return reader.Read(stream);
                }
            }

            var depsJsonFile = GetDepsJsonPath(assembly);
            if (!string.IsNullOrEmpty(depsJsonFile))
            {
                using (var stream = _fileSystem.File.OpenRead(depsJsonFile))
                {
                    return reader.Read(stream);
                }
            }

            return null;
        }

        private string GetDepsJsonPath(Assembly assembly)
        {
            string depsJsonFile = Path.ChangeExtension(assembly.Location, DepsJsonExtension);
            bool depsJsonFileExists = _fileSystem.File.Exists(depsJsonFile);

            if (!depsJsonFileExists)
            {
                // in some cases (like .NET Framework shadow copy) the Assembly Location 
                // and CodeBase will be different, so also try the CodeBase
                string assemblyCodeBase = GetNormalizedCodeBasePath(assembly);
                if (!string.IsNullOrEmpty(assemblyCodeBase) &&
                    assembly.Location != assemblyCodeBase)
                {
                    depsJsonFile = Path.ChangeExtension(assemblyCodeBase, DepsJsonExtension);
                    depsJsonFileExists = _fileSystem.File.Exists(depsJsonFile);
                }
            }

            return depsJsonFileExists ?
                depsJsonFile :
                null;
        }

        private static string GetNormalizedCodeBasePath(Assembly assembly)
        {
            if (Uri.TryCreate(assembly.CodeBase, UriKind.Absolute, out Uri codeBase)
                && codeBase.IsFile)
            {
                return codeBase.LocalPath;
            }
            else
            {
                return null;
            }
        }
    }
}

#endif
