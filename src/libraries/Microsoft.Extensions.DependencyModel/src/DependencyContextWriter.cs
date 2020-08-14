// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Extensions.DependencyModel
{
    public partial class DependencyContextWriter
    {
        private void WriteCore(DependencyContext context, UnifiedJsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject();
            WriteRuntimeTargetInfo(context, ref jsonWriter);
            WriteCompilationOptions(context.CompilationOptions, ref jsonWriter);
            WriteTargets(context, ref jsonWriter);
            WriteLibraries(context, ref jsonWriter);
            if (context.RuntimeGraph.Any())
            {
                WriteRuntimeGraph(context, ref jsonWriter);
            }
            jsonWriter.WriteEndObject();
            jsonWriter.Flush();
        }

        private void WriteRuntimeTargetInfo(DependencyContext context, ref UnifiedJsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject(DependencyContextStrings.RuntimeTargetPropertyName, escape: false);
            if (context.Target.IsPortable)
            {
                jsonWriter.WriteString(DependencyContextStrings.RuntimeTargetNamePropertyName,
                    context.Target.Framework, escape: false);
            }
            else
            {
                jsonWriter.WriteString(DependencyContextStrings.RuntimeTargetNamePropertyName,
                    context.Target.Framework + DependencyContextStrings.VersionSeparator + context.Target.Runtime, escape: false);
            }
            jsonWriter.WriteString(DependencyContextStrings.RuntimeTargetSignaturePropertyName,
                context.Target.RuntimeSignature, escape: false);
            jsonWriter.WriteEndObject();
        }

        private void WriteRuntimeGraph(DependencyContext context, ref UnifiedJsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject(DependencyContextStrings.RuntimesPropertyName, escape: false);
            foreach (RuntimeFallbacks runtimeFallback in context.RuntimeGraph)
            {
                jsonWriter.WriteStartArray(runtimeFallback.Runtime);
                foreach (string fallback in runtimeFallback.Fallbacks)
                {
                    jsonWriter.WriteStringValue(fallback);
                }
                jsonWriter.WriteEndArray();
            }
            jsonWriter.WriteEndObject();
        }

        private void WriteCompilationOptions(CompilationOptions compilationOptions, ref UnifiedJsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject(DependencyContextStrings.CompilationOptionsPropertName, escape: false);
            if (compilationOptions.Defines?.Any() == true)
            {
                jsonWriter.WriteStartArray(DependencyContextStrings.DefinesPropertyName, escape: false);
                foreach (string define in compilationOptions.Defines)
                {
                    jsonWriter.WriteStringValue(define);
                }
                jsonWriter.WriteEndArray();
            }
            AddStringPropertyIfNotNull(DependencyContextStrings.LanguageVersionPropertyName, compilationOptions.LanguageVersion, ref jsonWriter);
            AddStringPropertyIfNotNull(DependencyContextStrings.PlatformPropertyName, compilationOptions.Platform, ref jsonWriter);
            AddBooleanPropertyIfNotNull(DependencyContextStrings.AllowUnsafePropertyName, compilationOptions.AllowUnsafe, ref jsonWriter);
            AddBooleanPropertyIfNotNull(DependencyContextStrings.WarningsAsErrorsPropertyName, compilationOptions.WarningsAsErrors, ref jsonWriter);
            AddBooleanPropertyIfNotNull(DependencyContextStrings.OptimizePropertyName, compilationOptions.Optimize, ref jsonWriter);
            AddStringPropertyIfNotNull(DependencyContextStrings.KeyFilePropertyName, compilationOptions.KeyFile, ref jsonWriter);
            AddBooleanPropertyIfNotNull(DependencyContextStrings.DelaySignPropertyName, compilationOptions.DelaySign, ref jsonWriter);
            AddBooleanPropertyIfNotNull(DependencyContextStrings.PublicSignPropertyName, compilationOptions.PublicSign, ref jsonWriter);
            AddBooleanPropertyIfNotNull(DependencyContextStrings.EmitEntryPointPropertyName, compilationOptions.EmitEntryPoint, ref jsonWriter);
            AddBooleanPropertyIfNotNull(DependencyContextStrings.GenerateXmlDocumentationPropertyName, compilationOptions.GenerateXmlDocumentation, ref jsonWriter);
            AddStringPropertyIfNotNull(DependencyContextStrings.DebugTypePropertyName, compilationOptions.DebugType, ref jsonWriter);
            jsonWriter.WriteEndObject();
        }

        private void AddStringPropertyIfNotNull(string name, string value, ref UnifiedJsonWriter jsonWriter)
        {
            if (value != null)
            {
                jsonWriter.WriteString(name, value, escape: true);
            }
        }

        private void AddBooleanPropertyIfNotNull(string name, bool? value, ref UnifiedJsonWriter jsonWriter)
        {
            if (value.HasValue)
            {
                jsonWriter.WriteBoolean(name, value.Value, escape: true);
            }
        }

        private void WriteTargets(DependencyContext context, ref UnifiedJsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject(DependencyContextStrings.TargetsPropertyName, escape: false);
            if (context.Target.IsPortable)
            {
                WritePortableTarget(context.Target.Framework, context.RuntimeLibraries, context.CompileLibraries, ref jsonWriter);
            }
            else
            {
                WriteTarget(context.Target.Framework, context.CompileLibraries, ref jsonWriter);
                WriteTarget(context.Target.Framework + DependencyContextStrings.VersionSeparator + context.Target.Runtime,
                    context.RuntimeLibraries, ref jsonWriter);
            }
            jsonWriter.WriteEndObject();
        }

        private void WriteTarget(string key, IReadOnlyList<Library> libraries, ref UnifiedJsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject(key);
            int count = libraries.Count;
            for (int i = 0; i < count; i++)
            {
                Library library = libraries[i];
                WriteTargetLibrary(library.Name + DependencyContextStrings.VersionSeparator + library.Version, library, ref jsonWriter);
            }
            jsonWriter.WriteEndObject();
        }

        private void WritePortableTarget(string key, IReadOnlyList<RuntimeLibrary> runtimeLibraries, IReadOnlyList<CompilationLibrary> compilationLibraries, ref UnifiedJsonWriter jsonWriter)
        {
            Dictionary<string, RuntimeLibrary> runtimeLookup = runtimeLibraries.ToDictionary(l => l.Name, StringComparer.OrdinalIgnoreCase);
            Dictionary<string, CompilationLibrary> compileLookup = compilationLibraries.ToDictionary(l => l.Name, StringComparer.OrdinalIgnoreCase);

            jsonWriter.WriteStartObject(key);

            foreach (string packageName in runtimeLookup.Keys.Concat(compileLookup.Keys).Distinct())
            {
                runtimeLookup.TryGetValue(packageName, out RuntimeLibrary runtimeLibrary);

                compileLookup.TryGetValue(packageName, out CompilationLibrary compilationLibrary);

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

                Library library = (Library)compilationLibrary ?? (Library)runtimeLibrary;

                WritePortableTargetLibrary(library.Name + DependencyContextStrings.VersionSeparator + library.Version,
                    runtimeLibrary, compilationLibrary, ref jsonWriter);
            }
            jsonWriter.WriteEndObject();
        }

        private void AddCompilationAssemblies(IEnumerable<string> compilationAssemblies, ref UnifiedJsonWriter jsonWriter)
        {
            if (!compilationAssemblies.Any())
            {
                return;
            }

            WriteAssetList(DependencyContextStrings.CompileTimeAssembliesKey, compilationAssemblies, ref jsonWriter);
        }

        private void AddAssets(string key, RuntimeAssetGroup group, ref UnifiedJsonWriter jsonWriter)
        {
            if (group == null || !group.RuntimeFiles.Any())
            {
                return;
            }

            WriteAssetList(key, group.RuntimeFiles, ref jsonWriter);
        }

        private void AddDependencies(IEnumerable<Dependency> dependencies, ref UnifiedJsonWriter jsonWriter)
        {
            if (!dependencies.Any())
            {
                return;
            }

            jsonWriter.WriteStartObject(DependencyContextStrings.DependenciesPropertyName, escape: false);
            foreach (Dependency dependency in dependencies)
            {
                jsonWriter.WriteString(dependency.Name, dependency.Version);
            }
            jsonWriter.WriteEndObject();
        }

        private void AddResourceAssemblies(IEnumerable<ResourceAssembly> resourceAssemblies, ref UnifiedJsonWriter jsonWriter)
        {
            if (!resourceAssemblies.Any())
            {
                return;
            }

            jsonWriter.WriteStartObject(DependencyContextStrings.ResourceAssembliesPropertyName, escape: false);
            foreach (ResourceAssembly resourceAssembly in resourceAssemblies)
            {
                jsonWriter.WriteStartObject(NormalizePath(resourceAssembly.Path));
                jsonWriter.WriteString(DependencyContextStrings.LocalePropertyName, resourceAssembly.Locale, escape: false);
                jsonWriter.WriteEndObject();
            }
            jsonWriter.WriteEndObject();
        }

        private void WriteTargetLibrary(string key, Library library, ref UnifiedJsonWriter jsonWriter)
        {
            if (library is RuntimeLibrary runtimeLibrary)
            {
                jsonWriter.WriteStartObject(key);

                AddDependencies(runtimeLibrary.Dependencies, ref jsonWriter);

                // Add runtime-agnostic assets
                AddAssets(DependencyContextStrings.RuntimeAssembliesKey, runtimeLibrary.RuntimeAssemblyGroups.GetDefaultGroup(), ref jsonWriter);
                AddAssets(DependencyContextStrings.NativeLibrariesKey, runtimeLibrary.NativeLibraryGroups.GetDefaultGroup(), ref jsonWriter);
                AddResourceAssemblies(runtimeLibrary.ResourceAssemblies, ref jsonWriter);

                jsonWriter.WriteEndObject();
            }
            else if (library is CompilationLibrary compilationLibrary)
            {
                jsonWriter.WriteStartObject(key);
                AddDependencies(compilationLibrary.Dependencies, ref jsonWriter);
                AddCompilationAssemblies(compilationLibrary.Assemblies, ref jsonWriter);
                jsonWriter.WriteEndObject();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private void WritePortableTargetLibrary(string key, RuntimeLibrary runtimeLibrary, CompilationLibrary compilationLibrary, ref UnifiedJsonWriter jsonWriter)
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
            AddDependencies(dependencies, ref jsonWriter);


            if (runtimeLibrary != null)
            {
                // Add runtime-agnostic assets
                AddAssets(DependencyContextStrings.RuntimeAssembliesKey, runtimeLibrary.RuntimeAssemblyGroups.GetDefaultGroup(), ref jsonWriter);
                AddAssets(DependencyContextStrings.NativeLibrariesKey, runtimeLibrary.NativeLibraryGroups.GetDefaultGroup(), ref jsonWriter);
                AddResourceAssemblies(runtimeLibrary.ResourceAssemblies, ref jsonWriter);

                // Add runtime-specific assets
                bool wroteObjectStart = false;
                wroteObjectStart = AddRuntimeSpecificAssetGroups(DependencyContextStrings.RuntimeAssetType, runtimeLibrary.RuntimeAssemblyGroups, wroteObjectStart, ref jsonWriter);
                wroteObjectStart = AddRuntimeSpecificAssetGroups(DependencyContextStrings.NativeAssetType, runtimeLibrary.NativeLibraryGroups, wroteObjectStart, ref jsonWriter);

                if (wroteObjectStart)
                {
                    jsonWriter.WriteEndObject();
                }
            }

            if (compilationLibrary != null)
            {
                AddCompilationAssemblies(compilationLibrary.Assemblies, ref jsonWriter);
            }

            if (compilationLibrary != null && runtimeLibrary == null)
            {
                jsonWriter.WriteBoolean(DependencyContextStrings.CompilationOnlyPropertyName, true, escape: false);
            }

            jsonWriter.WriteEndObject();
        }

        private bool AddRuntimeSpecificAssetGroups(string assetType, IEnumerable<RuntimeAssetGroup> assetGroups, bool wroteObjectStart, ref UnifiedJsonWriter jsonWriter)
        {
            IEnumerable<RuntimeAssetGroup> groups = assetGroups.Where(g => !string.IsNullOrEmpty(g.Runtime));
            if (!wroteObjectStart && groups.Any())
            {
                jsonWriter.WriteStartObject(DependencyContextStrings.RuntimeTargetsPropertyName, escape: false);
                wroteObjectStart = true;
            }
            foreach (RuntimeAssetGroup group in groups)
            {
                if (group.RuntimeFiles.Any())
                {
                    AddRuntimeSpecificAssets(group.RuntimeFiles, group.Runtime, assetType, ref jsonWriter);
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
                    jsonWriter.WriteString(DependencyContextStrings.RidPropertyName, group.Runtime, escape: false);
                    jsonWriter.WriteString(DependencyContextStrings.AssetTypePropertyName, assetType, escape: false);
                    jsonWriter.WriteEndObject();
                }
            }
            return wroteObjectStart;
        }

        private void AddRuntimeSpecificAssets(IEnumerable<RuntimeFile> assets, string runtime, string assetType, ref UnifiedJsonWriter jsonWriter)
        {
            foreach (RuntimeFile asset in assets)
            {
                jsonWriter.WriteStartObject(NormalizePath(asset.Path));

                jsonWriter.WriteString(DependencyContextStrings.RidPropertyName, runtime, escape: false);
                jsonWriter.WriteString(DependencyContextStrings.AssetTypePropertyName, assetType, escape: false);

                if (asset.AssemblyVersion != null)
                {
                    jsonWriter.WriteString(DependencyContextStrings.AssemblyVersionPropertyName, asset.AssemblyVersion, escape: false);
                }

                if (asset.FileVersion != null)
                {
                    jsonWriter.WriteString(DependencyContextStrings.FileVersionPropertyName, asset.FileVersion, escape: false);
                }

                jsonWriter.WriteEndObject();
            }
        }

        private void WriteAssetList(string key, IEnumerable<string> assetPaths, ref UnifiedJsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject(key, escape: false);
            foreach (string assembly in assetPaths)
            {
                jsonWriter.WriteStartObject(NormalizePath(assembly));
                jsonWriter.WriteEndObject();
            }
            jsonWriter.WriteEndObject();
        }

        private void WriteAssetList(string key, IEnumerable<RuntimeFile> runtimeFiles, ref UnifiedJsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject(key, escape: false);

            foreach (RuntimeFile runtimeFile in runtimeFiles)
            {
                jsonWriter.WriteStartObject(NormalizePath(runtimeFile.Path));

                if (runtimeFile.AssemblyVersion != null)
                {
                    jsonWriter.WriteString(DependencyContextStrings.AssemblyVersionPropertyName, runtimeFile.AssemblyVersion, escape: false);
                }

                if (runtimeFile.FileVersion != null)
                {
                    jsonWriter.WriteString(DependencyContextStrings.FileVersionPropertyName, runtimeFile.FileVersion, escape: false);
                }

                jsonWriter.WriteEndObject();
            }

            jsonWriter.WriteEndObject();
        }

        private void WriteLibraries(DependencyContext context, ref UnifiedJsonWriter jsonWriter)
        {
            IEnumerable<IGrouping<string, Library>> allLibraries =
                context.RuntimeLibraries.Cast<Library>().Concat(context.CompileLibraries)
                    .GroupBy(library => library.Name + DependencyContextStrings.VersionSeparator + library.Version);

            jsonWriter.WriteStartObject(DependencyContextStrings.LibrariesPropertyName, escape: false);
            foreach (IGrouping<string, Library> libraryGroup in allLibraries)
            {
                WriteLibrary(libraryGroup.Key, libraryGroup.First(), ref jsonWriter);
            }
            jsonWriter.WriteEndObject();
        }

        private void WriteLibrary(string key, Library library, ref UnifiedJsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject(key);
            jsonWriter.WriteString(DependencyContextStrings.TypePropertyName, library.Type, escape: false);
            jsonWriter.WriteBoolean(DependencyContextStrings.ServiceablePropertyName, library.Serviceable, escape: false);
            jsonWriter.WriteString(DependencyContextStrings.Sha512PropertyName, library.Hash, escape: false);

            if (library.Path != null)
            {
                jsonWriter.WriteString(DependencyContextStrings.PathPropertyName, library.Path, escape: false);
            }

            if (library.HashPath != null)
            {
                jsonWriter.WriteString(DependencyContextStrings.HashPathPropertyName, library.HashPath, escape: false);
            }

            if (library.RuntimeStoreManifestName != null)
            {
                jsonWriter.WriteString(DependencyContextStrings.RuntimeStoreManifestPropertyName, library.RuntimeStoreManifestName, escape: false);
            }

            jsonWriter.WriteEndObject();
        }

        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }
    }
}
