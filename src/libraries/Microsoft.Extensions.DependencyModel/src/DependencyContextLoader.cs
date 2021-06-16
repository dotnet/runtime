// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;

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

        [RequiresAssemblyFiles(Message = "DependencyContext for an assembly from a application published as single-file is not supported. The method will return null. Make sure the calling code can handle this case.")]
        public DependencyContext Load(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            DependencyContext context = null;
            using (IDependencyContextReader reader = _jsonReaderFactory())
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
                    foreach (string extraPath in _nonEntryPointDepsPaths)
                    {
                        DependencyContext extraContext = LoadContext(reader, extraPath);
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
                using (Stream stream = _fileSystem.File.OpenRead(location))
                {
                    return reader.Read(stream);
                }
            }
            return null;
        }

        [RequiresAssemblyFiles(Message = "DependencyContext for an assembly from a application published as single-file is not supported. The method will return null. Make sure the calling code can handle this case.")]
        private DependencyContext LoadAssemblyContext(Assembly assembly, IDependencyContextReader reader)
        {
            using (Stream stream = GetResourceStream(assembly, assembly.GetName().Name + DepsJsonExtension))
            {
                if (stream != null)
                {
                    return reader.Read(stream);
                }
            }

            string depsJsonFile = GetDepsJsonPath(assembly);
            if (!string.IsNullOrEmpty(depsJsonFile))
            {
                using (Stream stream = _fileSystem.File.OpenRead(depsJsonFile))
                {
                    return reader.Read(stream);
                }
            }

            return null;
        }

        [RequiresAssemblyFiles(Message = "The use of DependencyContextLoader is not supported when publishing as single-file")]
        private string GetDepsJsonPath(Assembly assembly)
        {
            // Assemblies loaded in memory (e.g. single file) return empty string from Location.
            // In these cases, don't try probing next to the assembly.
            string assemblyLocation = assembly.Location;
            if (string.IsNullOrEmpty(assemblyLocation))
            {
                return null;
            }

            string depsJsonFile = Path.ChangeExtension(assemblyLocation, DepsJsonExtension);
            bool depsJsonFileExists = _fileSystem.File.Exists(depsJsonFile);

            if (!depsJsonFileExists)
            {
                // in some cases (like .NET Framework shadow copy) the Assembly Location
                // and CodeBase will be different, so also try the CodeBase
                string assemblyCodeBase = GetNormalizedCodeBasePath(assembly);
                if (!string.IsNullOrEmpty(assemblyCodeBase) &&
                    assemblyLocation != assemblyCodeBase)
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
