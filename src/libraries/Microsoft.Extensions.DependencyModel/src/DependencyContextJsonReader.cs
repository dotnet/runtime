// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Extensions.DependencyModel
{
    public partial class DependencyContextJsonReader : IDependencyContextReader
    {
        private readonly IDictionary<string, string> _stringPool = new Dictionary<string, string>();

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

        private DependencyContext ReadCore(UnifiedJsonReader reader)
        {
            reader.ReadStartObject();

            string runtime = string.Empty;
            string framework = string.Empty;
            bool isPortable = true;
            string runtimeTargetName = null;
            string runtimeSignature = null;

            CompilationOptions compilationOptions = null;
            List<Target> targets = null;
            Dictionary<string, LibraryStub> libraryStubs = null;
            List<RuntimeFallbacks> runtimeFallbacks = null;

            while (reader.Read() && reader.IsTokenTypeProperty())
            {
                switch (reader.GetStringValue())
                {
                    case DependencyContextStrings.RuntimeTargetPropertyName:
                        ReadRuntimeTarget(ref reader, out runtimeTargetName, out runtimeSignature);
                        break;
                    case DependencyContextStrings.CompilationOptionsPropertName:
                        compilationOptions = ReadCompilationOptions(ref reader);
                        break;
                    case DependencyContextStrings.TargetsPropertyName:
                        targets = ReadTargets(ref reader);
                        break;
                    case DependencyContextStrings.LibrariesPropertyName:
                        libraryStubs = ReadLibraries(ref reader);
                        break;
                    case DependencyContextStrings.RuntimesPropertyName:
                        runtimeFallbacks = ReadRuntimes(ref reader);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            if (compilationOptions == null)
            {
                compilationOptions = CompilationOptions.Default;
            }

            Target runtimeTarget = SelectRuntimeTarget(targets, runtimeTargetName);
            runtimeTargetName = runtimeTarget?.Name;

            if (runtimeTargetName != null)
            {
                int separatorIndex = runtimeTargetName.IndexOf(DependencyContextStrings.VersionSeparator);
                if (separatorIndex > -1 && separatorIndex < runtimeTargetName.Length)
                {
                    runtime = runtimeTargetName.Substring(separatorIndex + 1);
                    framework = runtimeTargetName.Substring(0, separatorIndex);
                    isPortable = false;
                }
                else
                {
                    framework = runtimeTargetName;
                }
            }

            Target compileTarget = null;

            Target ridlessTarget = targets.FirstOrDefault(t => !IsRuntimeTarget(t.Name));
            if (ridlessTarget != null)
            {
                compileTarget = ridlessTarget;
                if (runtimeTarget == null)
                {
                    runtimeTarget = compileTarget;
                    framework = ridlessTarget.Name;
                }
            }

            if (runtimeTarget == null)
            {
                throw new FormatException("No runtime target found");
            }

            return new DependencyContext(
                new TargetInfo(framework, runtime, runtimeSignature, isPortable),
                compilationOptions,
                CreateLibraries(compileTarget?.Libraries, false, libraryStubs).Cast<CompilationLibrary>().ToArray(),
                CreateLibraries(runtimeTarget.Libraries, true, libraryStubs).Cast<RuntimeLibrary>().ToArray(),
                runtimeFallbacks ?? Enumerable.Empty<RuntimeFallbacks>());
        }

        private static Target SelectRuntimeTarget(List<Target> targets, string runtimeTargetName)
        {
            Target target;

            if (targets == null || targets.Count == 0)
            {
                throw new FormatException("Dependency file does not have 'targets' section");
            }

            if (!string.IsNullOrEmpty(runtimeTargetName))
            {
                target = targets.FirstOrDefault(t => t.Name == runtimeTargetName);
                if (target == null)
                {
                    throw new FormatException($"Target with name {runtimeTargetName} not found");
                }
            }
            else
            {
                target = targets.FirstOrDefault(t => IsRuntimeTarget(t.Name));
            }

            return target;
        }

        private static bool IsRuntimeTarget(string name)
        {
            return name.Contains(DependencyContextStrings.VersionSeparator);
        }

        private static void ReadRuntimeTarget(ref UnifiedJsonReader reader, out string runtimeTargetName, out string runtimeSignature)
        {
            runtimeTargetName = null;
            runtimeSignature = null;

            reader.ReadStartObject();

            while (reader.TryReadStringProperty(out string propertyName, out string propertyValue))
            {
                switch (propertyName)
                {
                    case DependencyContextStrings.RuntimeTargetNamePropertyName:
                        runtimeTargetName = propertyValue;
                        break;
                    case DependencyContextStrings.RuntimeTargetSignaturePropertyName:
                        runtimeSignature = propertyValue;
                        break;
                }
            }

            reader.CheckEndObject();
        }

        private static CompilationOptions ReadCompilationOptions(ref UnifiedJsonReader reader)
        {
            IEnumerable<string> defines = null;
            string languageVersion = null;
            string platform = null;
            bool? allowUnsafe = null;
            bool? warningsAsErrors = null;
            bool? optimize = null;
            string keyFile = null;
            bool? delaySign = null;
            bool? publicSign = null;
            string debugType = null;
            bool? emitEntryPoint = null;
            bool? generateXmlDocumentation = null;

            reader.ReadStartObject();

            while (reader.Read() && reader.IsTokenTypeProperty())
            {
                switch (reader.GetStringValue())
                {
                    case DependencyContextStrings.DefinesPropertyName:
                        defines = reader.ReadStringArray();
                        break;
                    case DependencyContextStrings.LanguageVersionPropertyName:
                        languageVersion = reader.ReadAsString();
                        break;
                    case DependencyContextStrings.PlatformPropertyName:
                        platform = reader.ReadAsString();
                        break;
                    case DependencyContextStrings.AllowUnsafePropertyName:
                        allowUnsafe = reader.ReadAsNullableBoolean();
                        break;
                    case DependencyContextStrings.WarningsAsErrorsPropertyName:
                        warningsAsErrors = reader.ReadAsNullableBoolean();
                        break;
                    case DependencyContextStrings.OptimizePropertyName:
                        optimize = reader.ReadAsNullableBoolean();
                        break;
                    case DependencyContextStrings.KeyFilePropertyName:
                        keyFile = reader.ReadAsString();
                        break;
                    case DependencyContextStrings.DelaySignPropertyName:
                        delaySign = reader.ReadAsNullableBoolean();
                        break;
                    case DependencyContextStrings.PublicSignPropertyName:
                        publicSign = reader.ReadAsNullableBoolean();
                        break;
                    case DependencyContextStrings.DebugTypePropertyName:
                        debugType = reader.ReadAsString();
                        break;
                    case DependencyContextStrings.EmitEntryPointPropertyName:
                        emitEntryPoint = reader.ReadAsNullableBoolean();
                        break;
                    case DependencyContextStrings.GenerateXmlDocumentationPropertyName:
                        generateXmlDocumentation = reader.ReadAsNullableBoolean();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            reader.CheckEndObject();

            return new CompilationOptions(
                defines ?? Enumerable.Empty<string>(),
                languageVersion,
                platform,
                allowUnsafe,
                warningsAsErrors,
                optimize,
                keyFile,
                delaySign,
                publicSign,
                debugType,
                emitEntryPoint,
                generateXmlDocumentation);
        }

        private List<Target> ReadTargets(ref UnifiedJsonReader reader)
        {
            reader.ReadStartObject();

            var targets = new List<Target>();

            while (reader.Read() && reader.IsTokenTypeProperty())
            {
                targets.Add(ReadTarget(ref reader, reader.GetStringValue()));
            }

            reader.CheckEndObject();

            return targets;
        }

        private Target ReadTarget(ref UnifiedJsonReader reader, string targetName)
        {
            reader.ReadStartObject();

            var libraries = new List<TargetLibrary>();

            while (reader.Read() && reader.IsTokenTypeProperty())
            {
                libraries.Add(ReadTargetLibrary(ref reader, reader.GetStringValue()));
            }

            reader.CheckEndObject();

            return new Target()
            {
                Name = targetName,
                Libraries = libraries
            };
        }

        private TargetLibrary ReadTargetLibrary(ref UnifiedJsonReader reader, string targetLibraryName)
        {
            IEnumerable<Dependency> dependencies = null;
            List<RuntimeFile> runtimes = null;
            List<RuntimeFile> natives = null;
            List<string> compilations = null;
            List<RuntimeTargetEntryStub> runtimeTargets = null;
            List<ResourceAssembly> resources = null;
            bool? compileOnly = null;

            reader.ReadStartObject();

            while (reader.Read() && reader.IsTokenTypeProperty())
            {
                switch (reader.GetStringValue())
                {
                    case DependencyContextStrings.DependenciesPropertyName:
                        dependencies = ReadTargetLibraryDependencies(ref reader);
                        break;
                    case DependencyContextStrings.RuntimeAssembliesKey:
                        runtimes = ReadRuntimeFiles(ref reader);
                        break;
                    case DependencyContextStrings.NativeLibrariesKey:
                        natives = ReadRuntimeFiles(ref reader);
                        break;
                    case DependencyContextStrings.CompileTimeAssembliesKey:
                        compilations = ReadPropertyNames(ref reader);
                        break;
                    case DependencyContextStrings.RuntimeTargetsPropertyName:
                        runtimeTargets = ReadTargetLibraryRuntimeTargets(ref reader);
                        break;
                    case DependencyContextStrings.ResourceAssembliesPropertyName:
                        resources = ReadTargetLibraryResources(ref reader);
                        break;
                    case DependencyContextStrings.CompilationOnlyPropertyName:
                        compileOnly = reader.ReadAsNullableBoolean();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            reader.CheckEndObject();

            return new TargetLibrary()
            {
                Name = targetLibraryName,
                Dependencies = dependencies ?? Enumerable.Empty<Dependency>(),
                Runtimes = runtimes,
                Natives = natives,
                Compilations = compilations,
                RuntimeTargets = runtimeTargets,
                Resources = resources,
                CompileOnly = compileOnly
            };
        }

        private IEnumerable<Dependency> ReadTargetLibraryDependencies(ref UnifiedJsonReader reader)
        {
            var dependencies = new List<Dependency>();

            reader.ReadStartObject();

            while (reader.TryReadStringProperty(out string name, out string version))
            {
                dependencies.Add(new Dependency(Pool(name), Pool(version)));
            }

            reader.CheckEndObject();

            return dependencies;
        }

        private static List<string> ReadPropertyNames(ref UnifiedJsonReader reader)
        {
            var runtimes = new List<string>();

            reader.ReadStartObject();

            while (reader.Read() && reader.IsTokenTypeProperty())
            {
                string libraryName = reader.GetStringValue();
                reader.Skip();

                runtimes.Add(libraryName);
            }

            reader.CheckEndObject();

            return runtimes;
        }

        private static List<RuntimeFile> ReadRuntimeFiles(ref UnifiedJsonReader reader)
        {
            var runtimeFiles = new List<RuntimeFile>();

            reader.ReadStartObject();

            while (reader.Read() && reader.IsTokenTypeProperty())
            {
                string assemblyVersion = null;
                string fileVersion = null;

                string path = reader.GetStringValue();

                reader.ReadStartObject();

                while (reader.TryReadStringProperty(out string propertyName, out string propertyValue))
                {
                    switch (propertyName)
                    {
                        case DependencyContextStrings.AssemblyVersionPropertyName:
                            assemblyVersion = propertyValue;
                            break;
                        case DependencyContextStrings.FileVersionPropertyName:
                            fileVersion = propertyValue;
                            break;
                    }
                }

                reader.CheckEndObject();

                runtimeFiles.Add(new RuntimeFile(path, assemblyVersion, fileVersion));
            }

            reader.CheckEndObject();

            return runtimeFiles;
        }

        private List<RuntimeTargetEntryStub> ReadTargetLibraryRuntimeTargets(ref UnifiedJsonReader reader)
        {
            var runtimeTargets = new List<RuntimeTargetEntryStub>();

            reader.ReadStartObject();

            while (reader.Read() && reader.IsTokenTypeProperty())
            {
                var runtimeTarget = new RuntimeTargetEntryStub
                {
                    Path = reader.GetStringValue()
                };

                reader.ReadStartObject();

                while (reader.TryReadStringProperty(out string propertyName, out string propertyValue))
                {
                    switch (propertyName)
                    {
                        case DependencyContextStrings.RidPropertyName:
                            runtimeTarget.Rid = Pool(propertyValue);
                            break;
                        case DependencyContextStrings.AssetTypePropertyName:
                            runtimeTarget.Type = Pool(propertyValue);
                            break;
                        case DependencyContextStrings.AssemblyVersionPropertyName:
                            runtimeTarget.AssemblyVersion = propertyValue;
                            break;
                        case DependencyContextStrings.FileVersionPropertyName:
                            runtimeTarget.FileVersion = propertyValue;
                            break;
                    }
                }

                reader.CheckEndObject();

                runtimeTargets.Add(runtimeTarget);
            }

            reader.CheckEndObject();

            return runtimeTargets;
        }

        private List<ResourceAssembly> ReadTargetLibraryResources(ref UnifiedJsonReader reader)
        {
            var resources = new List<ResourceAssembly>();

            reader.ReadStartObject();

            while (reader.Read() && reader.IsTokenTypeProperty())
            {
                string path = reader.GetStringValue();
                string locale = null;

                reader.ReadStartObject();

                while (reader.TryReadStringProperty(out string propertyName, out string propertyValue))
                {
                    if (propertyName == DependencyContextStrings.LocalePropertyName)
                    {
                        locale = propertyValue;
                    }
                }

                reader.CheckEndObject();

                if (locale != null)
                {
                    resources.Add(new ResourceAssembly(path, Pool(locale)));
                }
            }

            reader.CheckEndObject();

            return resources;
        }

        private Dictionary<string, LibraryStub> ReadLibraries(ref UnifiedJsonReader reader)
        {
            var libraries = new Dictionary<string, LibraryStub>();

            reader.ReadStartObject();

            while (reader.Read() && reader.IsTokenTypeProperty())
            {
                string libraryName = reader.GetStringValue();

                libraries.Add(Pool(libraryName), ReadOneLibrary(ref reader));
            }

            reader.CheckEndObject();

            return libraries;
        }

        private LibraryStub ReadOneLibrary(ref UnifiedJsonReader reader)
        {
            string hash = null;
            string type = null;
            bool serviceable = false;
            string path = null;
            string hashPath = null;
            string runtimeStoreManifestName = null;

            reader.ReadStartObject();

            while (reader.Read() && reader.IsTokenTypeProperty())
            {
                switch (reader.GetStringValue())
                {
                    case DependencyContextStrings.Sha512PropertyName:
                        hash = reader.ReadAsString();
                        break;
                    case DependencyContextStrings.TypePropertyName:
                        type = reader.ReadAsString();
                        break;
                    case DependencyContextStrings.ServiceablePropertyName:
                        serviceable = reader.ReadAsBoolean(defaultValue: false);
                        break;
                    case DependencyContextStrings.PathPropertyName:
                        path = reader.ReadAsString();
                        break;
                    case DependencyContextStrings.HashPathPropertyName:
                        hashPath = reader.ReadAsString();
                        break;
                    case DependencyContextStrings.RuntimeStoreManifestPropertyName:
                        runtimeStoreManifestName = reader.ReadAsString();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            reader.CheckEndObject();

            return new LibraryStub()
            {
                Hash = hash,
                Type = Pool(type),
                Serviceable = serviceable,
                Path = path,
                HashPath = hashPath,
                RuntimeStoreManifestName = runtimeStoreManifestName
            };
        }

        private static List<RuntimeFallbacks> ReadRuntimes(ref UnifiedJsonReader reader)
        {
            var runtimeFallbacks = new List<RuntimeFallbacks>();

            reader.ReadStartObject();

            while (reader.Read() && reader.IsTokenTypeProperty())
            {
                string runtime = reader.GetStringValue();
                string[] fallbacks = reader.ReadStringArray();

                runtimeFallbacks.Add(new RuntimeFallbacks(runtime, fallbacks));
            }

            reader.CheckEndObject();

            return runtimeFallbacks;
        }

        private IEnumerable<Library> CreateLibraries(IEnumerable<TargetLibrary> libraries, bool runtime, Dictionary<string, LibraryStub> libraryStubs)
        {
            if (libraries == null)
            {
                return Enumerable.Empty<Library>();
            }
            return libraries
                .Select(property => CreateLibrary(property, runtime, libraryStubs))
                .Where(library => library != null);
        }

        private Library CreateLibrary(TargetLibrary targetLibrary, bool runtime, Dictionary<string, LibraryStub> libraryStubs)
        {
            string nameWithVersion = targetLibrary.Name;

            if (libraryStubs == null || !libraryStubs.TryGetValue(nameWithVersion, out LibraryStub stub))
            {
                throw new InvalidOperationException($"Cannot find library information for {nameWithVersion}");
            }

            int separatorPosition = nameWithVersion.IndexOf(DependencyContextStrings.VersionSeparator);

            string name = Pool(nameWithVersion.Substring(0, separatorPosition));
            string version = Pool(nameWithVersion.Substring(separatorPosition + 1));

            if (runtime)
            {
                // Runtime section of this library was trimmed by type:platform
                bool? isCompilationOnly = targetLibrary.CompileOnly;
                if (isCompilationOnly == true)
                {
                    return null;
                }

                var runtimeAssemblyGroups = new List<RuntimeAssetGroup>();
                var nativeLibraryGroups = new List<RuntimeAssetGroup>();
                if (targetLibrary.RuntimeTargets != null)
                {
                    foreach (IGrouping<string, RuntimeTargetEntryStub> ridGroup in targetLibrary.RuntimeTargets.GroupBy(e => e.Rid))
                    {
                        RuntimeFile[] groupRuntimeAssemblies = ridGroup
                            .Where(e => e.Type == DependencyContextStrings.RuntimeAssetType)
                            .Select(e => new RuntimeFile(e.Path, e.AssemblyVersion, e.FileVersion))
                            .ToArray();

                        if (groupRuntimeAssemblies.Any())
                        {
                            runtimeAssemblyGroups.Add(new RuntimeAssetGroup(
                                ridGroup.Key,
                                groupRuntimeAssemblies.Where(a => Path.GetFileName(a.Path) != "_._")));
                        }

                        RuntimeFile[] groupNativeLibraries = ridGroup
                            .Where(e => e.Type == DependencyContextStrings.NativeAssetType)
                            .Select(e => new RuntimeFile(e.Path, e.AssemblyVersion, e.FileVersion))
                            .ToArray();

                        if (groupNativeLibraries.Any())
                        {
                            nativeLibraryGroups.Add(new RuntimeAssetGroup(
                                ridGroup.Key,
                                groupNativeLibraries.Where(a => Path.GetFileName(a.Path) != "_._")));
                        }
                    }
                }

                if (targetLibrary.Runtimes != null && targetLibrary.Runtimes.Count > 0)
                {
                    runtimeAssemblyGroups.Add(new RuntimeAssetGroup(string.Empty, targetLibrary.Runtimes));
                }

                if (targetLibrary.Natives != null && targetLibrary.Natives.Count > 0)
                {
                    nativeLibraryGroups.Add(new RuntimeAssetGroup(string.Empty, targetLibrary.Natives));
                }

                return new RuntimeLibrary(
                    type: stub.Type,
                    name: name,
                    version: version,
                    hash: stub.Hash,
                    runtimeAssemblyGroups: runtimeAssemblyGroups,
                    nativeLibraryGroups: nativeLibraryGroups,
                    resourceAssemblies: targetLibrary.Resources ?? Enumerable.Empty<ResourceAssembly>(),
                    dependencies: targetLibrary.Dependencies,
                    serviceable: stub.Serviceable,
                    path: stub.Path,
                    hashPath: stub.HashPath,
                    runtimeStoreManifestName: stub.RuntimeStoreManifestName);
            }
            else
            {
                IEnumerable<string> assemblies = targetLibrary.Compilations ?? Enumerable.Empty<string>();
                return new CompilationLibrary(
                    stub.Type,
                    name,
                    version,
                    stub.Hash,
                    assemblies,
                    targetLibrary.Dependencies,
                    stub.Serviceable,
                    stub.Path,
                    stub.HashPath);
            }
        }

        private string Pool(string s)
        {
            if (s == null)
            {
                return null;
            }

            if (!_stringPool.TryGetValue(s, out string result))
            {
                _stringPool[s] = s;
                result = s;
            }
            return result;
        }

        private class Target
        {
            public string Name;

            public IEnumerable<TargetLibrary> Libraries;
        }

        private struct TargetLibrary
        {
            public string Name;

            public IEnumerable<Dependency> Dependencies;

            public List<RuntimeFile> Runtimes;

            public List<RuntimeFile> Natives;

            public List<string> Compilations;

            public List<RuntimeTargetEntryStub> RuntimeTargets;

            public List<ResourceAssembly> Resources;

            public bool? CompileOnly;
        }

        private struct RuntimeTargetEntryStub
        {
            public string Type;

            public string Path;

            public string Rid;

            public string AssemblyVersion;

            public string FileVersion;
        }

        private struct LibraryStub
        {
            public string Hash;

            public string Type;

            public bool Serviceable;

            public string Path;

            public string HashPath;

            public string RuntimeStoreManifestName;
        }
    }
}
