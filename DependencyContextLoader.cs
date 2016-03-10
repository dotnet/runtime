using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.Extensions.DependencyModel
{
    public class DependencyContextLoader
    {
        private static Lazy<string[]> _depsFiles = new Lazy<string[]>(GetHostDepsList);

        private const string DepsJsonExtension = ".deps.json";
        private const string DepsExtension = ".deps";

        private readonly string _entryPointDepsLocation;
        private readonly string _runtimeDepsLocation;
        private readonly IFileSystem _fileSystem;
        private readonly IDependencyContextReader _jsonReader;
        private readonly IDependencyContextReader _csvReader;

        public DependencyContextLoader() : this(
            GetDefaultEntrypointDepsLocation(),
            GetDefaultRuntimeDepsLocation(),
            FileSystemWrapper.Default,
            new DependencyContextJsonReader(),
            new DependencyContextCsvReader())
        {
        }

        internal DependencyContextLoader(
            string entryPointDepsLocation,
            string runtimeDepsLocation,
            IFileSystem fileSystem,
            IDependencyContextReader jsonReader,
            IDependencyContextReader csvReader)
        {
            _entryPointDepsLocation = entryPointDepsLocation;
            _runtimeDepsLocation = runtimeDepsLocation;
            _fileSystem = fileSystem;
            _jsonReader = jsonReader;
            _csvReader = csvReader;
        }

        public static DependencyContextLoader Default { get; } = new DependencyContextLoader();

        internal virtual bool IsEntryAssembly(Assembly assembly)
        {
            return assembly.GetName() == Assembly.GetEntryAssembly().GetName();
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

            if (IsEntryAssembly(assembly))
            {
                context = LoadEntryAssemblyContext();
            }

            if (context == null)
            {
                context = LoadAssemblyContext(assembly);
            }

            if (context?.IsPortable == true)
            {
                var runtimeContext = LoadRuntimeContext();
                if (runtimeContext != null)
                {
                    context = context.Merge(runtimeContext);
                }
            }
            return context;
        }

        private DependencyContext LoadEntryAssemblyContext()
        {
            if (!string.IsNullOrEmpty(_entryPointDepsLocation))
            {
                Debug.Assert(File.Exists(_entryPointDepsLocation));
                using (var stream = _fileSystem.File.OpenRead(_entryPointDepsLocation))
                {
                    return _jsonReader.Read(stream);
                }
            }
            return null;
        }

        private DependencyContext LoadRuntimeContext()
        {
            if (!string.IsNullOrEmpty(_runtimeDepsLocation))
            {
                Debug.Assert(File.Exists(_runtimeDepsLocation));
                using (var stream = _fileSystem.File.OpenRead(_runtimeDepsLocation))
                {
                    return _jsonReader.Read(stream);
                }
            }
            return null;
        }

        private DependencyContext LoadAssemblyContext(Assembly assembly)
        {
            using (var stream = GetResourceStream(assembly, assembly.GetName().Name + DepsJsonExtension))
            {
                if (stream != null)
                {
                    return _jsonReader.Read(stream);
                }
            }

            var depsJsonFile = Path.ChangeExtension(assembly.Location, DepsJsonExtension);
            if (_fileSystem.File.Exists(depsJsonFile))
            {
                using (var stream = _fileSystem.File.OpenRead(depsJsonFile))
                {
                    return _jsonReader.Read(stream);
                }
            }

            var depsFile = Path.ChangeExtension(assembly.Location, DepsExtension);
            if (_fileSystem.File.Exists(depsFile))
            {
                using (var stream = _fileSystem.File.OpenRead(depsFile))
                {
                    return _csvReader.Read(stream);
                }
            }

            return null;
        }

        private static string GetDefaultRuntimeDepsLocation()
        {
            var deps = _depsFiles.Value;
            if (deps != null && deps.Length > 1)
            {
                return deps[1];
            }
            return null;
        }

        private static string GetDefaultEntrypointDepsLocation()
        {
            var deps = _depsFiles.Value;
            if (deps != null && deps.Length > 0)
            {
                return deps[0];
            }
            return null;
        }

        private static string[] GetHostDepsList()
        {
            // TODO: Were going to replace this with AppContext.GetData
            var appDomainType = typeof(object).GetTypeInfo().Assembly?.GetType("System.AppDomain");
            var currentDomain = appDomainType?.GetProperty("CurrentDomain")?.GetValue(null);
            var deps = appDomainType?.GetMethod("GetData")?.Invoke(currentDomain, new[] { "APP_CONTEXT_DEPS_FILES" });

            return (deps as string)?.Split(new [] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

    }
}
