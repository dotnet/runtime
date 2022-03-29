// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Microsoft.Extensions.DependencyModel
{
    public class DependencyContextWriter
    {
        public void Write(DependencyContext context!!, Stream stream!!)
        {
            // Custom encoder is required to fix https://github.com/dotnet/core-setup/issues/7137
            // Since the JSON is only written to a file that is read by the SDK (and not transmitted over the wire),
            // it is safe to skip escaping certain characters in this scenario
            // (that would otherwise be escaped, by default, as part of defense-in-depth, such as +).
            var options = new JsonWriterOptions { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            using (var jsonWriter = new Utf8JsonWriter(stream, options))
            {
                jsonWriter.WriteStartObject();
                WriteRuntimeTargetInfo(context, jsonWriter);
                WriteCompilationOptions(context.CompilationOptions, jsonWriter);
                WriteTargets(context, jsonWriter);
                WriteLibraries(context, jsonWriter);
                if (context.RuntimeGraph.Any())
                {
                    WriteRuntimeGraph(context, jsonWriter);
                }
                jsonWriter.WriteEndObject();
            }
        }

        private static void WriteRuntimeTargetInfo(DependencyContext context, Utf8JsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject(DependencyContextStrings.RuntimeTargetPropertyName);
            if (context.Target.IsPortable)
            {
                jsonWriter.WriteString(DependencyContextStrings.RuntimeTargetNamePropertyName,
                    context.Target.Framework);
            }
            else
            {
                jsonWriter.WriteString(DependencyContextStrings.RuntimeTargetNamePropertyName,
                    context.Target.Framework + DependencyContextStrings.VersionSeparator + context.Target.Runtime);
            }
            jsonWriter.WriteString(DependencyContextStrings.RuntimeTargetSignaturePropertyName,
                context.Target.RuntimeSignature);
            jsonWriter.WriteEndObject();
        }

        private static void WriteRuntimeGraph(DependencyContext context, Utf8JsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject(DependencyContextStrings.RuntimesPropertyName);
            foreach (RuntimeFallbacks runtimeFallback in context.RuntimeGraph)
            {
                jsonWriter.WriteStartArray(runtimeFallback.Runtime);
                foreach (string? fallback in runtimeFallback.Fallbacks)
                {
                    jsonWriter.WriteStringValue(fallback);
                }
                jsonWriter.WriteEndArray();
            }
            jsonWriter.WriteEndObject();
        }

        private static void WriteCompilationOptions(CompilationOptions compilationOptions, Utf8JsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject(DependencyContextStrings.CompilationOptionsPropertName);
            if (compilationOptions.Defines?.Any() == true)
            {
                jsonWriter.WriteStartArray(DependencyContextStrings.DefinesPropertyName);
                foreach (string? define in compilationOptions.Defines)
                {
                    jsonWriter.WriteStringValue(define);
                }
                jsonWriter.WriteEndArray();
            }
            AddStringPropertyIfNotNull(DependencyContextStrings.LanguageVersionPropertyName, compilationOptions.LanguageVersion, jsonWriter);
            AddStringPropertyIfNotNull(DependencyContextStrings.PlatformPropertyName, compilationOptions.Platform, jsonWriter);
            AddBooleanPropertyIfNotNull(DependencyContextStrings.AllowUnsafePropertyName, compilationOptions.AllowUnsafe, jsonWriter);
            AddBooleanPropertyIfNotNull(DependencyContextStrings.WarningsAsErrorsPropertyName, compilationOptions.WarningsAsErrors, jsonWriter);
            AddBooleanPropertyIfNotNull(DependencyContextStrings.OptimizePropertyName, compilationOptions.Optimize, jsonWriter);
            AddStringPropertyIfNotNull(DependencyContextStrings.KeyFilePropertyName, compilationOptions.KeyFile, jsonWriter);
            AddBooleanPropertyIfNotNull(DependencyContextStrings.DelaySignPropertyName, compilationOptions.DelaySign, jsonWriter);
            AddBooleanPropertyIfNotNull(DependencyContextStrings.PublicSignPropertyName, compilationOptions.PublicSign, jsonWriter);
            AddBooleanPropertyIfNotNull(DependencyContextStrings.EmitEntryPointPropertyName, compilationOptions.EmitEntryPoint, jsonWriter);
            AddBooleanPropertyIfNotNull(DependencyContextStrings.GenerateXmlDocumentationPropertyName, compilationOptions.GenerateXmlDocumentation, jsonWriter);
            AddStringPropertyIfNotNull(DependencyContextStrings.DebugTypePropertyName, compilationOptions.DebugType, jsonWriter);
            jsonWriter.WriteEndObject();
        }

        private static void AddStringPropertyIfNotNull(string name, string? value, Utf8JsonWriter jsonWriter)
        {
            if (value != null)
            {
                jsonWriter.WriteString(name, value);
            }
        }

        private static void AddBooleanPropertyIfNotNull(string name, bool? value, Utf8JsonWriter jsonWriter)
        {
            if (value.HasValue)
            {
                jsonWriter.WriteBoolean(name, value.Value);
            }
        }

        private static void WriteTargets(DependencyContext context, Utf8JsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject(DependencyContextStrings.TargetsPropertyName);
            if (context.Target.IsPortable)
            {
                WritePortableTarget(context.Target.Framework, context.RuntimeLibraries, context.CompileLibraries, jsonWriter);
            }
            else
            {
                WriteTarget(context.Target.Framework, context.CompileLibraries, jsonWriter);
                WriteTarget(context.Target.Framework + DependencyContextStrings.VersionSeparator + context.Target.Runtime,
                    context.RuntimeLibraries, jsonWriter);
            }
            jsonWriter.WriteEndObject();
        }

        private static void WriteTarget(string key, IReadOnlyList<Library> libraries, Utf8JsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject(key);
            int count = libraries.Count;
            for (int i = 0; i < count; i++)
            {
                Library library = libraries[i];
                WriteTargetLibrary(library.Name + DependencyContextStrings.VersionSeparator + library.Version, library, jsonWriter);
            }
            jsonWriter.WriteEndObject();
        }

        private static void WritePortableTarget(string key, IReadOnlyList<RuntimeLibrary> runtimeLibraries, IReadOnlyList<CompilationLibrary> compilationLibraries, Utf8JsonWriter jsonWriter)
        {
            Dictionary<string, RuntimeLibrary> runtimeLookup = runtimeLibraries.LibraryCollectionToDictionary();
            Dictionary<string, CompilationLibrary> compileLookup = compilationLibraries.LibraryCollectionToDictionary();

            jsonWriter.WriteStartObject(key);

            foreach (string packageName in runtimeLookup.Keys.Concat(compileLookup.Keys).Distinct())
            {
                runtimeLookup.TryGetValue(packageName, out RuntimeLibrary? runtimeLibrary);

                compileLookup.TryGetValue(packageName, out CompilationLibrary? compilationLibrary);

                if (compilationLibrary != null && runtimeLibrary != null)
                {
                    Debug.Assert(compilationLibrary.Serviceable == runtimeLibrary.Serviceable);
                    Debug.Assert(compilationLibrary.Version == runtimeLibrary.Version);
                    Debug.Assert(compilationLibrary.Hash == runtimeLibrary.Hash);
                    Debug.Assert(compilationLibrary.Type == runtimeLibrary.Type);
                    Debug.Assert(compilationLibrary.Path == runtimeLibrary.Path);
                    Debug.Assert(compilationLibrary.HashPath == runtimeLibrary.HashPath);
                    Debug.Assert(compilationLibrary.RuntimeStoreManifestName == null);
                }

                Library library = (Library?)compilationLibrary ?? (Library)runtimeLibrary!;

                WritePortableTargetLibrary(library.Name + DependencyContextStrings.VersionSeparator + library.Version,
                    runtimeLibrary, compilationLibrary, jsonWriter);
            }
            jsonWriter.WriteEndObject();
        }

        private static void AddCompilationAssemblies(IEnumerable<string> compilationAssemblies, Utf8JsonWriter jsonWriter)
        {
            if (!compilationAssemblies.Any())
            {
                return;
            }

            WriteAssetList(DependencyContextStrings.CompileTimeAssembliesKey, compilationAssemblies, jsonWriter);
        }

        private static void AddAssets(string key, RuntimeAssetGroup? group, Utf8JsonWriter jsonWriter)
        {
            if (group == null || !group.RuntimeFiles.Any())
            {
                return;
            }

            WriteAssetList(key, group.RuntimeFiles, jsonWriter);
        }

        private static void AddDependencies(IEnumerable<Dependency> dependencies, Utf8JsonWriter jsonWriter)
        {
            if (!dependencies.Any())
            {
                return;
            }

            jsonWriter.WriteStartObject(DependencyContextStrings.DependenciesPropertyName);
            foreach (Dependency dependency in dependencies)
            {
                jsonWriter.WriteString(dependency.Name, dependency.Version);
            }
            jsonWriter.WriteEndObject();
        }

        private static void AddResourceAssemblies(IEnumerable<ResourceAssembly> resourceAssemblies, Utf8JsonWriter jsonWriter)
        {
            if (!resourceAssemblies.Any())
            {
                return;
            }

            jsonWriter.WriteStartObject(DependencyContextStrings.ResourceAssembliesPropertyName);
            foreach (ResourceAssembly resourceAssembly in resourceAssemblies)
            {
                jsonWriter.WriteStartObject(NormalizePath(resourceAssembly.Path));
                jsonWriter.WriteString(DependencyContextStrings.LocalePropertyName, resourceAssembly.Locale);
                jsonWriter.WriteEndObject();
            }
            jsonWriter.WriteEndObject();
        }

        private static void WriteTargetLibrary(string key, Library library, Utf8JsonWriter jsonWriter)
        {
            if (library is RuntimeLibrary runtimeLibrary)
            {
                jsonWriter.WriteStartObject(key);

                AddDependencies(runtimeLibrary.Dependencies, jsonWriter);

                // Add runtime-agnostic assets
                AddAssets(DependencyContextStrings.RuntimeAssembliesKey, runtimeLibrary.RuntimeAssemblyGroups.GetDefaultGroup(), jsonWriter);
                AddAssets(DependencyContextStrings.NativeLibrariesKey, runtimeLibrary.NativeLibraryGroups.GetDefaultGroup(), jsonWriter);
                AddResourceAssemblies(runtimeLibrary.ResourceAssemblies, jsonWriter);

                jsonWriter.WriteEndObject();
            }
            else if (library is CompilationLibrary compilationLibrary)
            {
                jsonWriter.WriteStartObject(key);
                AddDependencies(compilationLibrary.Dependencies, jsonWriter);
                AddCompilationAssemblies(compilationLibrary.Assemblies, jsonWriter);
                jsonWriter.WriteEndObject();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private static void WritePortableTargetLibrary(string key, RuntimeLibrary? runtimeLibrary, CompilationLibrary? compilationLibrary, Utf8JsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject(key);

            var dependencies = new HashSet<Dependency>();
            if (runtimeLibrary != null)
            {
                dependencies.UnionWith(runtimeLibrary.Dependencies);
            }

            if (compilationLibrary != null)
            {
                dependencies.UnionWith(compilationLibrary.Dependencies);
            }
            AddDependencies(dependencies, jsonWriter);


            if (runtimeLibrary != null)
            {
                // Add runtime-agnostic assets
                AddAssets(DependencyContextStrings.RuntimeAssembliesKey, runtimeLibrary.RuntimeAssemblyGroups.GetDefaultGroup(), jsonWriter);
                AddAssets(DependencyContextStrings.NativeLibrariesKey, runtimeLibrary.NativeLibraryGroups.GetDefaultGroup(), jsonWriter);
                AddResourceAssemblies(runtimeLibrary.ResourceAssemblies, jsonWriter);

                // Add runtime-specific assets
                bool wroteObjectStart = false;
                wroteObjectStart = AddRuntimeSpecificAssetGroups(DependencyContextStrings.RuntimeAssetType, runtimeLibrary.RuntimeAssemblyGroups, wroteObjectStart, jsonWriter);
                wroteObjectStart = AddRuntimeSpecificAssetGroups(DependencyContextStrings.NativeAssetType, runtimeLibrary.NativeLibraryGroups, wroteObjectStart, jsonWriter);

                if (wroteObjectStart)
                {
                    jsonWriter.WriteEndObject();
                }
            }

            if (compilationLibrary != null)
            {
                AddCompilationAssemblies(compilationLibrary.Assemblies, jsonWriter);
            }

            if (compilationLibrary != null && runtimeLibrary == null)
            {
                jsonWriter.WriteBoolean(DependencyContextStrings.CompilationOnlyPropertyName, true);
            }

            jsonWriter.WriteEndObject();
        }

        private static bool AddRuntimeSpecificAssetGroups(string assetType, IEnumerable<RuntimeAssetGroup> assetGroups, bool wroteObjectStart, Utf8JsonWriter jsonWriter)
        {
            IEnumerable<RuntimeAssetGroup> groups = assetGroups.Where(g => !string.IsNullOrEmpty(g.Runtime));
            if (!wroteObjectStart && groups.Any())
            {
                jsonWriter.WriteStartObject(DependencyContextStrings.RuntimeTargetsPropertyName);
                wroteObjectStart = true;
            }
            foreach (RuntimeAssetGroup group in groups)
            {
                if (group.RuntimeFiles.Any())
                {
                    AddRuntimeSpecificAssets(group.RuntimeFiles, group.Runtime, assetType, jsonWriter);
                }
                else
                {
                    // Add a placeholder item
                    // We need to generate a pseudo-path because there could be multiple different asset groups with placeholders
                    // Only the last path segment matters, the rest is basically just a GUID.
                    string pseudoPathFolder = assetType == DependencyContextStrings.RuntimeAssetType ?
                        "lib" :
                        "native";

                    jsonWriter.WriteStartObject($"runtime/{group.Runtime}/{pseudoPathFolder}/_._");
                    jsonWriter.WriteString(DependencyContextStrings.RidPropertyName, group.Runtime);
                    jsonWriter.WriteString(DependencyContextStrings.AssetTypePropertyName, assetType);
                    jsonWriter.WriteEndObject();
                }
            }
            return wroteObjectStart;
        }

        private static void AddRuntimeSpecificAssets(IEnumerable<RuntimeFile> assets, string? runtime, string? assetType, Utf8JsonWriter jsonWriter)
        {
            foreach (RuntimeFile asset in assets)
            {
                jsonWriter.WriteStartObject(NormalizePath(asset.Path));

                jsonWriter.WriteString(DependencyContextStrings.RidPropertyName, runtime);
                jsonWriter.WriteString(DependencyContextStrings.AssetTypePropertyName, assetType);

                if (asset.AssemblyVersion != null)
                {
                    jsonWriter.WriteString(DependencyContextStrings.AssemblyVersionPropertyName, asset.AssemblyVersion);
                }

                if (asset.FileVersion != null)
                {
                    jsonWriter.WriteString(DependencyContextStrings.FileVersionPropertyName, asset.FileVersion);
                }

                jsonWriter.WriteEndObject();
            }
        }

        private static void WriteAssetList(string key, IEnumerable<string> assetPaths, Utf8JsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject(key);
            foreach (string assembly in assetPaths)
            {
                jsonWriter.WriteStartObject(NormalizePath(assembly));
                jsonWriter.WriteEndObject();
            }
            jsonWriter.WriteEndObject();
        }

        private static void WriteAssetList(string key, IEnumerable<RuntimeFile> runtimeFiles, Utf8JsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject(key);

            foreach (RuntimeFile runtimeFile in runtimeFiles)
            {
                jsonWriter.WriteStartObject(NormalizePath(runtimeFile.Path));

                if (runtimeFile.AssemblyVersion != null)
                {
                    jsonWriter.WriteString(DependencyContextStrings.AssemblyVersionPropertyName, runtimeFile.AssemblyVersion);
                }

                if (runtimeFile.FileVersion != null)
                {
                    jsonWriter.WriteString(DependencyContextStrings.FileVersionPropertyName, runtimeFile.FileVersion);
                }

                jsonWriter.WriteEndObject();
            }

            jsonWriter.WriteEndObject();
        }

        private static void WriteLibraries(DependencyContext context, Utf8JsonWriter jsonWriter)
        {
            IEnumerable<IGrouping<string, Library>> allLibraries =
                context.RuntimeLibraries.Cast<Library>().Concat(context.CompileLibraries)
                    .GroupBy(library => library.Name + DependencyContextStrings.VersionSeparator + library.Version);

            jsonWriter.WriteStartObject(DependencyContextStrings.LibrariesPropertyName);
            foreach (IGrouping<string, Library> libraryGroup in allLibraries)
            {
                WriteLibrary(libraryGroup.Key, libraryGroup.First(), jsonWriter);
            }
            jsonWriter.WriteEndObject();
        }

        private static void WriteLibrary(string key, Library library, Utf8JsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject(key);
            jsonWriter.WriteString(DependencyContextStrings.TypePropertyName, library.Type);
            jsonWriter.WriteBoolean(DependencyContextStrings.ServiceablePropertyName, library.Serviceable);
            jsonWriter.WriteString(DependencyContextStrings.Sha512PropertyName, library.Hash);

            if (library.Path != null)
            {
                jsonWriter.WriteString(DependencyContextStrings.PathPropertyName, library.Path);
            }

            if (library.HashPath != null)
            {
                jsonWriter.WriteString(DependencyContextStrings.HashPathPropertyName, library.HashPath);
            }

            if (library.RuntimeStoreManifestName != null)
            {
                jsonWriter.WriteString(DependencyContextStrings.RuntimeStoreManifestPropertyName, library.RuntimeStoreManifestName);
            }

            jsonWriter.WriteEndObject();
        }

        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }
    }
}
