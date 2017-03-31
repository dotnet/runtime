// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        private readonly string _runtimeDepsLocation;
        private readonly IEnumerable<string> _extraDepsLocations;
        private readonly IFileSystem _fileSystem;
        private readonly Func<IDependencyContextReader> _jsonReaderFactory;

        public DependencyContextLoader() : this(
            DependencyContextPaths.Current.Application,
            DependencyContextPaths.Current.SharedRuntime,
            DependencyContextPaths.Current.ExtraPaths,
            FileSystemWrapper.Default,
            () => new DependencyContextJsonReader())
        {
        }

        internal DependencyContextLoader(
            string entryPointDepsLocation,
            string runtimeDepsLocation,
            IEnumerable<string> extraDepsLocations,
            IFileSystem fileSystem,
            Func<IDependencyContextReader> jsonReaderFactory)
        {
            _entryPointDepsLocation = entryPointDepsLocation;
            _runtimeDepsLocation = runtimeDepsLocation;
            _extraDepsLocations = extraDepsLocations;
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
                    var runtimeContext = LoadRuntimeContext(reader);
                    if (runtimeContext != null)
                    {
                        context = context.Merge(runtimeContext);
                    }

                    foreach (var extraPath in _extraDepsLocations)
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

        private DependencyContext LoadRuntimeContext(IDependencyContextReader reader)
        {
            return LoadContext(reader, _runtimeDepsLocation);
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

            var depsJsonFile = Path.ChangeExtension(assembly.Location, DepsJsonExtension);
            if (_fileSystem.File.Exists(depsJsonFile))
            {
                using (var stream = _fileSystem.File.OpenRead(depsJsonFile))
                {
                    return reader.Read(stream);
                }
            }

            return null;
        }
    }
}

#endif
