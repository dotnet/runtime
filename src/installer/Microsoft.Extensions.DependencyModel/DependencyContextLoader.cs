using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

#if !NETSTANDARD1_3

namespace Microsoft.Extensions.DependencyModel
{
    public class DependencyContextLoader
    {
        private const string DepsJsonExtension = ".deps.json";

        private readonly string _entryPointDepsLocation;
        private readonly string _runtimeDepsLocation;
        private readonly IFileSystem _fileSystem;
        private readonly Func<IDependencyContextReader> _jsonReaderFactory;

        public DependencyContextLoader() : this(
            DependencyContextPaths.Current.Application,
            DependencyContextPaths.Current.SharedRuntime,
            FileSystemWrapper.Default,
            () => new DependencyContextJsonReader())
        {
        }

        internal DependencyContextLoader(
            string entryPointDepsLocation,
            string runtimeDepsLocation,
            IFileSystem fileSystem,
            Func<IDependencyContextReader> jsonReaderFactory)
        {
            _entryPointDepsLocation = entryPointDepsLocation;
            _runtimeDepsLocation = runtimeDepsLocation;
            _fileSystem = fileSystem;
            _jsonReaderFactory = jsonReaderFactory;
        }

        public static DependencyContextLoader Default { get; } = new DependencyContextLoader();

        internal virtual bool IsEntryAssembly(Assembly assembly)
        {
            return assembly.Equals(Assembly.GetEntryAssembly());
        }

        internal virtual Stream GetResourceStream(Assembly assembly, string name)
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

                if (context?.Target.IsPortable == true)
                {
                    var runtimeContext = LoadRuntimeContext(reader);
                    if (runtimeContext != null)
                    {
                        context = context.Merge(runtimeContext);
                    }
                }
            }
            return context;
        }

        private DependencyContext LoadEntryAssemblyContext(IDependencyContextReader reader)
        {
            if (!string.IsNullOrEmpty(_entryPointDepsLocation))
            {
                Debug.Assert(File.Exists(_entryPointDepsLocation));
                using (var stream = _fileSystem.File.OpenRead(_entryPointDepsLocation))
                {
                    return reader.Read(stream);
                }
            }
            return null;
        }

        private DependencyContext LoadRuntimeContext(IDependencyContextReader reader)
        {
            if (!string.IsNullOrEmpty(_runtimeDepsLocation))
            {
                Debug.Assert(File.Exists(_runtimeDepsLocation));
                using (var stream = _fileSystem.File.OpenRead(_runtimeDepsLocation))
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
