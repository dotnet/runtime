// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Extensions.DependencyModel
{
    public class DependencyContextJsonReader : IDependencyContextReader
    {
        private readonly IDictionary<string, string> _stringPool = new Dictionary<string, string>();

        public DependencyContext Read(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            using (var streamReader = new StreamReader(stream))
            {
                using (var reader = new JsonTextReader(streamReader))
                {
                    var root = JObject.Load(reader);
                    return Read(root);
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stringPool.Clear();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private bool IsRuntimeTarget(string name) => name.Contains(DependencyContextStrings.VersionSeperator);

        private DependencyContext Read(JObject root)
        {
            var runtime = string.Empty;
            var target = string.Empty;
            var isPortable = true;
            string runtimeTargetName = null;
            string runtimeSignature = null;

            var runtimeTargetInfo = root[DependencyContextStrings.RuntimeTargetPropertyName];

            // This fallback is temporary
            if (runtimeTargetInfo is JValue)
            {
                runtimeTargetName = runtimeTargetInfo.Value<string>();
            }
            else
            {
                var runtimeTargetObject = (JObject) runtimeTargetInfo;
                runtimeTargetName = runtimeTargetObject?[DependencyContextStrings.RuntimeTargetNamePropertyName]?.Value<string>();
                runtimeSignature = runtimeTargetObject?[DependencyContextStrings.RuntimeTargetSignaturePropertyName]?.Value<string>();
            }

            var libraryStubs = ReadLibraryStubs((JObject)root[DependencyContextStrings.LibrariesPropertyName]);
            var targetsObject = (JObject)root[DependencyContextStrings.TargetsPropertyName];

            JObject runtimeTarget = null;
            JObject compileTarget = null;

            if (targetsObject == null)
            {
                throw new FormatException("Dependency file does not have 'targets' section");
            }

            if (!string.IsNullOrEmpty(runtimeTargetName))
            {
                runtimeTarget = (JObject)targetsObject[runtimeTargetName];
                if (runtimeTarget == null)
                {
                    throw new FormatException($"Target with name {runtimeTargetName} not found");
                }
            }
            else
            {
                var runtimeTargetProperty = targetsObject.Properties()
                    .FirstOrDefault(p => IsRuntimeTarget(p.Name));

                runtimeTarget = (JObject)runtimeTargetProperty?.Value;
                runtimeTargetName = runtimeTargetProperty?.Name;
            }

            if (runtimeTargetName != null)
            {
                var seperatorIndex = runtimeTargetName.IndexOf(DependencyContextStrings.VersionSeperator);
                if (seperatorIndex > -1 && seperatorIndex < runtimeTargetName.Length)
                {
                    runtime = runtimeTargetName.Substring(seperatorIndex + 1);
                    target = runtimeTargetName.Substring(0, seperatorIndex);
                    isPortable = false;
                }
                else
                {
                    target = runtimeTargetName;
                }
            }

            var ridlessTargetProperty = targetsObject.Properties().FirstOrDefault(p => !IsRuntimeTarget(p.Name));
            if (ridlessTargetProperty != null)
            {
                compileTarget = (JObject)ridlessTargetProperty.Value;
                if (runtimeTarget == null)
                {
                    runtimeTarget = compileTarget;
                    target = ridlessTargetProperty.Name;
                }
            }

            if (runtimeTarget == null)
            {
                throw new FormatException("No runtime target found");
            }

            return new DependencyContext(
                new TargetInfo(target, runtime, runtimeSignature, isPortable),
                ReadCompilationOptions((JObject)root[DependencyContextStrings.CompilationOptionsPropertName]),
                ReadLibraries(compileTarget, false, libraryStubs).Cast<CompilationLibrary>().ToArray(),
                ReadLibraries(runtimeTarget, true, libraryStubs).Cast<RuntimeLibrary>().ToArray(),
                ReadRuntimeGraph((JObject)root[DependencyContextStrings.RuntimesPropertyName]).ToArray()
                );
        }

        private IEnumerable<RuntimeFallbacks> ReadRuntimeGraph(JObject runtimes)
        {
            if (runtimes == null)
            {
                yield break;
            }

            foreach (var pair in runtimes)
            {
                yield return new RuntimeFallbacks(pair.Key, pair.Value.Values<string>().ToArray());
            }
        }

        private CompilationOptions ReadCompilationOptions(JObject compilationOptionsObject)
        {
            if (compilationOptionsObject == null)
            {
                return CompilationOptions.Default;
            }

            return new CompilationOptions(
                compilationOptionsObject[DependencyContextStrings.DefinesPropertyName]?.Values<string>().ToArray() ?? Enumerable.Empty<string>(),
                // ToArray is here to prevent IEnumerable<string> holding to json object graph
                compilationOptionsObject[DependencyContextStrings.LanguageVersionPropertyName]?.Value<string>(),
                compilationOptionsObject[DependencyContextStrings.PlatformPropertyName]?.Value<string>(),
                compilationOptionsObject[DependencyContextStrings.AllowUnsafePropertyName]?.Value<bool>(),
                compilationOptionsObject[DependencyContextStrings.WarningsAsErrorsPropertyName]?.Value<bool>(),
                compilationOptionsObject[DependencyContextStrings.OptimizePropertyName]?.Value<bool>(),
                compilationOptionsObject[DependencyContextStrings.KeyFilePropertyName]?.Value<string>(),
                compilationOptionsObject[DependencyContextStrings.DelaySignPropertyName]?.Value<bool>(),
                compilationOptionsObject[DependencyContextStrings.PublicSignPropertyName]?.Value<bool>(),
                compilationOptionsObject[DependencyContextStrings.DebugTypePropertyName]?.Value<string>(),
                compilationOptionsObject[DependencyContextStrings.EmitEntryPointPropertyName]?.Value<bool>(),
                compilationOptionsObject[DependencyContextStrings.GenerateXmlDocumentationPropertyName]?.Value<bool>()
                );
        }

        private IEnumerable<Library> ReadLibraries(JObject librariesObject, bool runtime, Dictionary<string, LibraryStub> libraryStubs)
        {
            if (librariesObject == null)
            {
                return Enumerable.Empty<Library>();
            }
            return librariesObject.Properties().Select(property => ReadLibrary(property, runtime, libraryStubs));
        }

        private Library ReadLibrary(JProperty property, bool runtime, Dictionary<string, LibraryStub> libraryStubs)
        {
            var nameWithVersion = property.Name;
            LibraryStub stub;

            if (!libraryStubs.TryGetValue(nameWithVersion, out stub))
            {
                throw new InvalidOperationException($"Cannot find library information for {nameWithVersion}");
            }

            var seperatorPosition = nameWithVersion.IndexOf(DependencyContextStrings.VersionSeperator);

            var name = Pool(nameWithVersion.Substring(0, seperatorPosition));
            var version = Pool(nameWithVersion.Substring(seperatorPosition + 1));

            var libraryObject = (JObject)property.Value;

            var dependencies = ReadDependencies(libraryObject);

            if (runtime)
            {
                var runtimeTargetsObject = (JObject)libraryObject[DependencyContextStrings.RuntimeTargetsPropertyName];

                var entries = ReadRuntimeTargetEntries(runtimeTargetsObject).ToArray();

                var runtimeAssemblyGroups = new List<RuntimeAssetGroup>();
                var nativeLibraryGroups = new List<RuntimeAssetGroup>();
                foreach (var ridGroup in entries.GroupBy(e => e.Rid))
                {
                    var groupRuntimeAssemblies = ridGroup
                        .Where(e => e.Type == DependencyContextStrings.RuntimeAssetType)
                        .Select(e => e.Path)
                        .ToArray();

                    if (groupRuntimeAssemblies.Any())
                    {
                        runtimeAssemblyGroups.Add(new RuntimeAssetGroup(
                            ridGroup.Key,
                            groupRuntimeAssemblies.Where(a => Path.GetFileName(a) != "_._")));
                    }

                    var groupNativeLibraries = ridGroup
                        .Where(e => e.Type == DependencyContextStrings.NativeAssetType)
                        .Select(e => e.Path)
                        .ToArray();

                    if (groupNativeLibraries.Any())
                    {
                        nativeLibraryGroups.Add(new RuntimeAssetGroup(
                            ridGroup.Key,
                            groupNativeLibraries.Where(a => Path.GetFileName(a) != "_._")));
                    }
                }

                var runtimeAssemblies = ReadAssetList(libraryObject, DependencyContextStrings.RuntimeAssembliesKey)
                    .ToArray();
                if (runtimeAssemblies.Any())
                {
                    runtimeAssemblyGroups.Add(new RuntimeAssetGroup(string.Empty, runtimeAssemblies));
                }

                var nativeLibraries = ReadAssetList(libraryObject, DependencyContextStrings.NativeLibrariesKey)
                    .ToArray();
                if(nativeLibraries.Any())
                {
                    nativeLibraryGroups.Add(new RuntimeAssetGroup(string.Empty, nativeLibraries));
                }

                var resourceAssemblies = ReadResourceAssemblies((JObject)libraryObject[DependencyContextStrings.ResourceAssembliesPropertyName]);

                return new RuntimeLibrary(
                    type: stub.Type,
                    name: name,
                    version: version,
                    hash: stub.Hash,
                    runtimeAssemblyGroups: runtimeAssemblyGroups,
                    nativeLibraryGroups: nativeLibraryGroups,
                    resourceAssemblies: resourceAssemblies,
                    dependencies: dependencies,
                    serviceable: stub.Serviceable);
            }
            else
            {
                var assemblies = ReadAssetList(libraryObject, DependencyContextStrings.CompileTimeAssembliesKey);
                return new CompilationLibrary(stub.Type, name, version, stub.Hash, assemblies, dependencies, stub.Serviceable);
            }
        }

        private IEnumerable<ResourceAssembly> ReadResourceAssemblies(JObject resourcesObject)
        {
            if (resourcesObject == null)
            {
                yield break;
            }
            foreach (var resourceProperty in resourcesObject)
            {
                yield return new ResourceAssembly(
                    locale: Pool(resourceProperty.Value[DependencyContextStrings.LocalePropertyName]?.Value<string>()),
                    path: resourceProperty.Key
                    );
            }
        }

        private IEnumerable<RuntimeTargetEntryStub> ReadRuntimeTargetEntries(JObject runtimeTargetObject)
        {
            if (runtimeTargetObject == null)
            {
                yield break;
            }
            foreach (var libraryProperty in runtimeTargetObject)
            {
                var libraryObject = (JObject)libraryProperty.Value;
                yield return new RuntimeTargetEntryStub()
                {
                    Path = libraryProperty.Key,
                    Rid = Pool(libraryObject[DependencyContextStrings.RidPropertyName].Value<string>()),
                    Type = Pool(libraryObject[DependencyContextStrings.AssetTypePropertyName].Value<string>())
                };
            }
        }

        private static string[] ReadAssetList(JObject libraryObject, string name)
        {
            var assembliesObject = (JObject)libraryObject[name];

            if (assembliesObject == null)
            {
                return new string[] { };
            }

            return assembliesObject.Properties().Select(property => property.Name).ToArray();
        }

        private Dependency[] ReadDependencies(JObject libraryObject)
        {
            var dependenciesObject = (JObject)libraryObject[DependencyContextStrings.DependenciesPropertyName];

            if (dependenciesObject == null)
            {
                return new Dependency[] { };
            }

            return dependenciesObject.Properties()
                .Select(property => new Dependency(Pool(property.Name), Pool((string)property.Value))).ToArray();
        }

        private Dictionary<string, LibraryStub> ReadLibraryStubs(JObject librariesObject)
        {
            var libraries = new Dictionary<string, LibraryStub>();
            if (librariesObject != null)
            {
                foreach (var libraryProperty in librariesObject)
                {
                    var value = (JObject)libraryProperty.Value;
                    var stub = new LibraryStub
                    {
                        Name = Pool(libraryProperty.Key),
                        Hash = value[DependencyContextStrings.Sha512PropertyName]?.Value<string>(),
                        Type = Pool(value[DependencyContextStrings.TypePropertyName].Value<string>()),
                        Serviceable = value[DependencyContextStrings.ServiceablePropertyName]?.Value<bool>() == true
                    };
                    libraries.Add(stub.Name, stub);
                }
            }
            return libraries;
        }

        private string Pool(string s)
        {
            if (s == null)
            {
                return null;
            }

            string result;
            if (!_stringPool.TryGetValue(s, out result))
            {
                _stringPool[s] = s;
                result = s;
            }
            return result;
        }

        private struct RuntimeTargetEntryStub
        {
            public string Type;
            public string Path;
            public string Rid;
        }

        private struct LibraryStub
        {
            public string Name;

            public string Hash;

            public string Type;

            public bool Serviceable;
        }
    }
}
