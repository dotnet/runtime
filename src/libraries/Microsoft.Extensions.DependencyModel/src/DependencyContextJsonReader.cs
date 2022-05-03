// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Microsoft.Extensions.DependencyModel
{
    public class DependencyContextJsonReader : IDependencyContextReader
    {
        private const int UnseekableStreamInitialRentSize = 4096;
        private static ReadOnlySpan<byte> Utf8Bom => new byte[] { 0xEF, 0xBB, 0xBF };

        private readonly IDictionary<string, string> _stringPool = new Dictionary<string, string>();

        public DependencyContext Read(Stream stream)
        {
            ThrowHelper.ThrowIfNull(stream);

            ArraySegment<byte> buffer = ReadToEnd(stream);
            try
            {
                return Read(new Utf8JsonReader(buffer, isFinalBlock: true, state: default));
            }
            finally
            {
                // Holds document content, clear it before returning it.
                buffer.AsSpan().Clear();
                ArrayPool<byte>.Shared.Return(buffer.Array!);
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

        // Borrowed from https://github.com/dotnet/corefx/blob/b8bc4ff80c5f7baa681e8a569d367356957ba78a/src/System.Text.Json/src/System/Text/Json/Document/JsonDocument.Parse.cs#L290-L362
        private static ArraySegment<byte> ReadToEnd(Stream stream)
        {
            int written = 0;
            byte[]? rented = null;

            ReadOnlySpan<byte> utf8Bom = Utf8Bom;

            try
            {
                if (stream.CanSeek)
                {
                    // Ask for 1 more than the length to avoid resizing later,
                    // which is unnecessary in the common case where the stream length doesn't change.
                    long expectedLength = Math.Max(utf8Bom.Length, stream.Length - stream.Position) + 1;
                    rented = ArrayPool<byte>.Shared.Rent(checked((int)expectedLength));
                }
                else
                {
                    rented = ArrayPool<byte>.Shared.Rent(UnseekableStreamInitialRentSize);
                }

                int lastRead;

                // Read up to 3 bytes to see if it's the UTF-8 BOM, for parity with the behavior
                // of StreamReader..ctor(Stream).
                do
                {
                    // No need for checking for growth, the minimal rent sizes both guarantee it'll fit.
                    Debug.Assert(rented.Length >= utf8Bom.Length);

                    lastRead = stream.Read(
                        rented,
                        written,
                        utf8Bom.Length - written);

                    written += lastRead;
                } while (lastRead > 0 && written < utf8Bom.Length);

                // If we have 3 bytes, and they're the BOM, reset the write position to 0.
                if (written == utf8Bom.Length &&
                    utf8Bom.SequenceEqual(rented.AsSpan(0, utf8Bom.Length)))
                {
                    written = 0;
                }

                do
                {
                    if (rented.Length == written)
                    {
                        byte[] toReturn = rented;
                        rented = ArrayPool<byte>.Shared.Rent(checked(toReturn.Length * 2));
                        Buffer.BlockCopy(toReturn, 0, rented, 0, toReturn.Length);
                        // Holds document content, clear it.
                        ArrayPool<byte>.Shared.Return(toReturn, clearArray: true);
                    }

                    lastRead = stream.Read(rented, written, rented.Length - written);
                    written += lastRead;
                } while (lastRead > 0);

                return new ArraySegment<byte>(rented, 0, written);
            }
            catch
            {
                if (rented != null)
                {
                    // Holds document content, clear it before returning it.
                    rented.AsSpan(0, written).Clear();
                    ArrayPool<byte>.Shared.Return(rented);
                }

                throw;
            }
        }

        private DependencyContext Read(Utf8JsonReader reader)
        {
            reader.ReadStartObject();

            string runtime = string.Empty;
            string framework = string.Empty;
            bool isPortable = true;
            string? runtimeTargetName = null;
            string? runtimeSignature = null;

            CompilationOptions? compilationOptions = null;
            List<Target>? targets = null;
            Dictionary<string, LibraryStub>? libraryStubs = null;
            List<RuntimeFallbacks>? runtimeFallbacks = null;

            while (reader.Read() && reader.IsTokenTypeProperty())
            {
                switch (reader.GetString())
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

            Target? runtimeTarget = SelectRuntimeTarget(targets, runtimeTargetName);
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

            Target? compileTarget = null;

            Target? ridlessTarget = targets.FirstOrDefault(t => !IsRuntimeTarget(t.Name));
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
                throw new FormatException(SR.NoRuntimeTarget);
            }

            return new DependencyContext(
                new TargetInfo(framework, runtime, runtimeSignature, isPortable),
                compilationOptions,
                CreateLibraries(compileTarget?.Libraries, false, libraryStubs).Cast<CompilationLibrary>().ToArray(),
                CreateLibraries(runtimeTarget.Libraries, true, libraryStubs).Cast<RuntimeLibrary>().ToArray(),
                runtimeFallbacks ?? Enumerable.Empty<RuntimeFallbacks>());
        }

        private static Target? SelectRuntimeTarget([NotNull] List<Target>? targets, string? runtimeTargetName)
        {
            Target? target;

            if (targets == null || targets.Count == 0)
            {
                throw new FormatException(SR.NoTargetsSection);
            }

            if (!string.IsNullOrEmpty(runtimeTargetName))
            {
                target = targets.FirstOrDefault(t => t.Name == runtimeTargetName);
                if (target == null)
                {
                    throw new FormatException(SR.Format(SR.TargetNotFound, runtimeTargetName));
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

        private static void ReadRuntimeTarget(ref Utf8JsonReader reader, out string? runtimeTargetName, out string? runtimeSignature)
        {
            runtimeTargetName = null;
            runtimeSignature = null;

            reader.ReadStartObject();

            while (reader.TryReadStringProperty(out string? propertyName, out string? propertyValue))
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

        private static CompilationOptions ReadCompilationOptions(ref Utf8JsonReader reader)
        {
            IEnumerable<string?>? defines = null;
            string? languageVersion = null;
            string? platform = null;
            bool? allowUnsafe = null;
            bool? warningsAsErrors = null;
            bool? optimize = null;
            string? keyFile = null;
            bool? delaySign = null;
            bool? publicSign = null;
            string? debugType = null;
            bool? emitEntryPoint = null;
            bool? generateXmlDocumentation = null;

            reader.ReadStartObject();

            while (reader.Read() && reader.IsTokenTypeProperty())
            {
                switch (reader.GetString())
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
                defines ?? Enumerable.Empty<string?>(),
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

        private List<Target> ReadTargets(ref Utf8JsonReader reader)
        {
            reader.ReadStartObject();

            var targets = new List<Target>();

            while (reader.Read() && reader.IsTokenTypeProperty())
            {
                string? targetName = reader.GetString();

                if (string.IsNullOrEmpty(targetName))
                {
                    throw new FormatException(SR.Format(SR.RequiredFieldNotSpecified, nameof(targetName)));
                }

                targets.Add(ReadTarget(ref reader, targetName));
            }

            reader.CheckEndObject();

            return targets;
        }

        private Target ReadTarget(ref Utf8JsonReader reader, string targetName)
        {
            reader.ReadStartObject();

            var libraries = new List<TargetLibrary>();

            while (reader.Read() && reader.IsTokenTypeProperty())
            {
                string? targetLibraryName = reader.GetString();

                if (string.IsNullOrEmpty(targetLibraryName))
                {
                    throw new FormatException(SR.Format(SR.RequiredFieldNotSpecified, nameof(targetLibraryName)));
                }

                libraries.Add(ReadTargetLibrary(ref reader, targetLibraryName));
            }

            reader.CheckEndObject();

            return new Target(targetName, libraries);
        }

        private TargetLibrary ReadTargetLibrary(ref Utf8JsonReader reader, string targetLibraryName)
        {
            IEnumerable<Dependency>? dependencies = null;
            List<RuntimeFile>? runtimes = null;
            List<RuntimeFile>? natives = null;
            List<string>? compilations = null;
            List<RuntimeTargetEntryStub>? runtimeTargets = null;
            List<ResourceAssembly>? resources = null;
            bool? compileOnly = null;

            reader.ReadStartObject();

            while (reader.Read() && reader.IsTokenTypeProperty())
            {
                switch (reader.GetString())
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

        private IEnumerable<Dependency> ReadTargetLibraryDependencies(ref Utf8JsonReader reader)
        {
            var dependencies = new List<Dependency>();

            reader.ReadStartObject();

            while (reader.TryReadStringProperty(out string? name, out string? version))
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new FormatException(SR.Format(SR.RequiredFieldNotSpecified, nameof(name)));
                }
                if (string.IsNullOrEmpty(version))
                {
                    throw new FormatException(SR.Format(SR.RequiredFieldNotSpecified, nameof(version)));
                }

                dependencies.Add(new Dependency(Pool(name), Pool(version)));
            }

            reader.CheckEndObject();

            return dependencies;
        }

        private static List<string> ReadPropertyNames(ref Utf8JsonReader reader)
        {
            var runtimes = new List<string>();

            reader.ReadStartObject();

            while (reader.Read() && reader.IsTokenTypeProperty())
            {
                string? libraryName = reader.GetString();

                if (string.IsNullOrEmpty(libraryName))
                {
                    throw new FormatException(SR.Format(SR.RequiredFieldNotSpecified, nameof(libraryName)));
                }
                reader.Skip();

                runtimes.Add(libraryName);
            }

            reader.CheckEndObject();

            return runtimes;
        }

        private static List<RuntimeFile> ReadRuntimeFiles(ref Utf8JsonReader reader)
        {
            var runtimeFiles = new List<RuntimeFile>();

            reader.ReadStartObject();

            while (reader.Read() && reader.IsTokenTypeProperty())
            {
                string? assemblyVersion = null;
                string? fileVersion = null;

                string? path = reader.GetString();

                if (string.IsNullOrEmpty(path))
                {
                    throw new FormatException(SR.Format(SR.RequiredFieldNotSpecified, nameof(path)));
                }

                reader.ReadStartObject();

                while (reader.TryReadStringProperty(out string? propertyName, out string? propertyValue))
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

        private List<RuntimeTargetEntryStub> ReadTargetLibraryRuntimeTargets(ref Utf8JsonReader reader)
        {
            var runtimeTargets = new List<RuntimeTargetEntryStub>();

            reader.ReadStartObject();

            while (reader.Read() && reader.IsTokenTypeProperty())
            {
                string? runtimePath = reader.GetString();

                if (string.IsNullOrEmpty(runtimePath))
                {
                    throw new FormatException(SR.Format(SR.RequiredFieldNotSpecified, nameof(runtimePath)));
                }

                var runtimeTarget = new RuntimeTargetEntryStub
                {
                    Path = runtimePath
                };

                reader.ReadStartObject();

                while (reader.TryReadStringProperty(out string? propertyName, out string? propertyValue))
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

        private List<ResourceAssembly> ReadTargetLibraryResources(ref Utf8JsonReader reader)
        {
            var resources = new List<ResourceAssembly>();

            reader.ReadStartObject();

            while (reader.Read() && reader.IsTokenTypeProperty())
            {
                string? path = reader.GetString();

                if (string.IsNullOrEmpty(path))
                {
                    throw new FormatException(SR.Format(SR.RequiredFieldNotSpecified, nameof(path)));
                }

                string? locale = null;

                reader.ReadStartObject();

                while (reader.TryReadStringProperty(out string? propertyName, out string? propertyValue))
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

        private Dictionary<string, LibraryStub> ReadLibraries(ref Utf8JsonReader reader)
        {
            var libraries = new Dictionary<string, LibraryStub>();

            reader.ReadStartObject();

            while (reader.Read() && reader.IsTokenTypeProperty())
            {
                string? libraryName = reader.GetString();

                if (string.IsNullOrEmpty(libraryName))
                {
                    throw new FormatException(SR.Format(SR.RequiredFieldNotSpecified, nameof(libraryName)));
                }

                libraries.Add(Pool(libraryName), ReadOneLibrary(ref reader));
            }

            reader.CheckEndObject();

            return libraries;
        }

        private LibraryStub ReadOneLibrary(ref Utf8JsonReader reader)
        {
            string? hash = null;
            string? type = null;
            bool serviceable = false;
            string? path = null;
            string? hashPath = null;
            string? runtimeStoreManifestName = null;

            reader.ReadStartObject();

            while (reader.Read() && reader.IsTokenTypeProperty())
            {
                switch (reader.GetString())
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

            if (string.IsNullOrEmpty(type))
            {
                throw new FormatException(SR.Format(SR.RequiredFieldNotSpecified, nameof(type)));
            }

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

        private static List<RuntimeFallbacks> ReadRuntimes(ref Utf8JsonReader reader)
        {
            var runtimeFallbacks = new List<RuntimeFallbacks>();

            reader.ReadStartObject();

            while (reader.Read() && reader.IsTokenTypeProperty())
            {
                string? runtime = reader.GetString();
                string?[] fallbacks = reader.ReadStringArray();

                if (string.IsNullOrEmpty(runtime))
                {
                    throw new FormatException(SR.Format(SR.RequiredFieldNotSpecified, nameof(runtime)));
                }

                runtimeFallbacks.Add(new RuntimeFallbacks(runtime, fallbacks));
            }

            reader.CheckEndObject();

            return runtimeFallbacks;
        }

        private IEnumerable<Library> CreateLibraries(IEnumerable<TargetLibrary>? libraries, bool runtime, Dictionary<string, LibraryStub>? libraryStubs)
        {
            if (libraries == null)
            {
                return Enumerable.Empty<Library>();
            }
            return libraries
                .Select(property => CreateLibrary(property, runtime, libraryStubs))
                .Where(library => library != null)!;
        }

        private Library? CreateLibrary(TargetLibrary targetLibrary, bool runtime, Dictionary<string, LibraryStub>? libraryStubs)
        {
            string nameWithVersion = targetLibrary.Name;

            if (libraryStubs == null || !libraryStubs.TryGetValue(nameWithVersion, out LibraryStub stub))
            {
                throw new InvalidOperationException(SR.Format(SR.LibraryInformationNotFound, nameWithVersion));
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
                    foreach (IGrouping<string?, RuntimeTargetEntryStub> ridGroup in targetLibrary.RuntimeTargets.GroupBy(e => e.Rid))
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

        [return: NotNullIfNotNull("s")]
        private string? Pool(string? s)
        {
            if (s == null)
            {
                return null;
            }

            if (!_stringPool.TryGetValue(s, out string? result))
            {
                _stringPool[s] = s;
                result = s;
            }
            return result;
        }

        private sealed class Target
        {
            public string Name;

            public IEnumerable<TargetLibrary> Libraries;

            public Target(string name, IEnumerable<TargetLibrary> libraries)
            {
                Name = name;
                Libraries = libraries;
            }
        }

        private struct TargetLibrary
        {
            public string Name;

            public IEnumerable<Dependency> Dependencies;

            public List<RuntimeFile>? Runtimes;

            public List<RuntimeFile>? Natives;

            public List<string>? Compilations;

            public List<RuntimeTargetEntryStub>? RuntimeTargets;

            public List<ResourceAssembly>? Resources;

            public bool? CompileOnly;
        }

        private struct RuntimeTargetEntryStub
        {
            public string? Type;

            public string Path;

            public string? Rid;

            public string? AssemblyVersion;

            public string? FileVersion;
        }

        private struct LibraryStub
        {
            public string? Hash;

            public string Type;

            public bool Serviceable;

            public string? Path;

            public string? HashPath;

            public string? RuntimeStoreManifestName;
        }
    }
}
