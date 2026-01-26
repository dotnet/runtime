// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public class NetCoreAppBuilder
    {
        public string Name { get; set; }
        public string Framework { get; set; }
        public string Runtime { get; set; }

        private TestApp _sourceApp;
        private bool _includeLocalPathsInDepsJson = true;

        public Action<RuntimeConfig> RuntimeConfigCustomizer { get; set; }

        public List<RuntimeLibraryBuilder> RuntimeLibraries { get; } = new List<RuntimeLibraryBuilder>();

        public List<RuntimeFallbacksBuilder> RuntimeFallbacks { get; } = new List<RuntimeFallbacksBuilder>();

        internal class BuildContext
        {
            public TestApp App { get; set; }
        }

        public abstract class FileBuilder
        {
            public string Path { get; set; }

            public string SourcePath { get; set; }

            // If set, path relative to the app location where the file will be created.
            public string LocalPath { get; set; }

            public FileBuilder(string path)
            {
                Path = path;
            }

            internal void Build(BuildContext context)
            {
                string path = ToDiskPath(LocalPath ?? Path);
                string absolutePath = System.IO.Path.Combine(context.App.Location, path);
                if (SourcePath != null)
                {
                    FileUtils.EnsureFileDirectoryExists(absolutePath);
                    File.Copy(SourcePath, absolutePath);
                }
                else if ((LocalPath == null || LocalPath.Length > 0)
                    && !File.Exists(absolutePath))
                {
                    FileUtils.CreateEmptyFile(absolutePath);
                }
            }

            protected static string ToDiskPath(string assetPath)
            {
                return assetPath.Replace('/', System.IO.Path.DirectorySeparatorChar);
            }
        }

        public abstract class FileBuilder<T> : FileBuilder
            where T : FileBuilder
        {
            public FileBuilder(string path)
                : base(path)
            {
            }

            public T CopyFromFile(string sourcePath)
            {
                SourcePath = sourcePath;
                return this as T;
            }

            public T WithLocalPath(string localPath)
            {
                LocalPath = localPath;
                return this as T;
            }

            public T NotOnDisk()
            {
                LocalPath = string.Empty;
                return this as T;
            }
        }

        public class RuntimeFileBuilder : FileBuilder<RuntimeFileBuilder>
        {
            public string AssemblyVersion { get; set; }
            public string FileVersion { get; set; }

            public RuntimeFileBuilder(string path)
                : base(path)
            {
            }

            public RuntimeFileBuilder WithVersion(string assemblyVersion, string fileVersion)
            {
                AssemblyVersion = assemblyVersion;
                FileVersion = fileVersion;
                return this;
            }

            internal new RuntimeFile Build(BuildContext context)
            {
                base.Build(context);

                // TODO: Pass in LocalPath once we can upgrade to a Microsoft.Extensions.DependencyModel version that supports it.
                return new RuntimeFile(Path, AssemblyVersion, FileVersion);
            }
        }

        public class ResourceAssemblyBuilder : FileBuilder<ResourceAssemblyBuilder>
        {
            public string Locale { get; set; }

            public ResourceAssemblyBuilder(string path)
                : base(path)
            {
                int i = path.IndexOf('/');
                if (i > 0)
                {
                    Locale = path.Substring(0, i);
                }
            }

            public ResourceAssemblyBuilder WithLocale(string locale)
            {
                Locale = locale;
                return this;
            }

            internal new ResourceAssembly Build(BuildContext context)
            {
                base.Build(context);

                // TODO: Pass in LocalPath once we can upgrade to a Microsoft.Extensions.DependencyModel version that supports it.
                return new ResourceAssembly(Path, Locale);
            }
        }

        public class RuntimeAssetGroupBuilder
        {
            public string Runtime { get; set; }

            public bool IncludeMainAssembly { get; set; }

            public List<RuntimeFileBuilder> Assets { get; } = new List<RuntimeFileBuilder>();

            public RuntimeAssetGroupBuilder(string runtime)
            {
                Runtime = runtime ?? string.Empty;
            }

            public RuntimeAssetGroupBuilder WithMainAssembly()
            {
                IncludeMainAssembly = true;
                return this;
            }

            public RuntimeAssetGroupBuilder WithAsset(RuntimeFileBuilder asset)
            {
                Assets.Add(asset);
                return this;
            }

            public RuntimeAssetGroupBuilder WithAsset(string path, Action<RuntimeFileBuilder> customizer = null)
            {
                RuntimeFileBuilder runtimeFile = new RuntimeFileBuilder(path);
                customizer?.Invoke(runtimeFile);
                return WithAsset(runtimeFile);
            }

            internal RuntimeAssetGroup Build(BuildContext context)
            {
                IEnumerable<RuntimeFileBuilder> assets = Assets;
                if (IncludeMainAssembly)
                {
                    assets = assets.Append(new RuntimeFileBuilder(Path.GetFileName(context.App.AppDll)));
                }

                return new RuntimeAssetGroup(
                    Runtime,
                    assets.Select(a => a.Build(context)));
            }
        }

        public enum RuntimeLibraryType
        {
            project,
            package,
            runtimepack,
        }

        public class RuntimeLibraryBuilder
        {
            public string Type { get; set; }
            public string Name { get; set; }
            public string Version { get; set; }

            public List<RuntimeAssetGroupBuilder> AssemblyGroups { get; } = new List<RuntimeAssetGroupBuilder>();
            public List<RuntimeAssetGroupBuilder> NativeLibraryGroups { get; } = new List<RuntimeAssetGroupBuilder>();
            public List<ResourceAssemblyBuilder> ResourceAssemblies { get; } = new List<ResourceAssemblyBuilder>();

            public RuntimeLibraryBuilder(RuntimeLibraryType type, string name, string version)
            {
                Type = type.ToString();
                Name = name;
                Version = version;
            }

            public RuntimeLibraryBuilder WithAssemblyGroup(string runtime, Action<RuntimeAssetGroupBuilder> customizer = null)
            {
                return WithRuntimeAssetGroup(runtime, AssemblyGroups, customizer);
            }

            public RuntimeLibraryBuilder WithNativeLibraryGroup(string runtime, Action<RuntimeAssetGroupBuilder> customizer = null)
            {
                return WithRuntimeAssetGroup(runtime, NativeLibraryGroups, customizer);
            }

            private RuntimeLibraryBuilder WithRuntimeAssetGroup(
                string runtime,
                IList<RuntimeAssetGroupBuilder> list,
                Action<RuntimeAssetGroupBuilder> customizer)
            {
                RuntimeAssetGroupBuilder runtimeAssetGroup = new RuntimeAssetGroupBuilder(runtime);
                customizer?.Invoke(runtimeAssetGroup);

                list.Add(runtimeAssetGroup);
                return this;
            }

            public RuntimeLibraryBuilder WithResourceAssembly(string path, Action<ResourceAssemblyBuilder> customizer = null)
            {
                ResourceAssemblyBuilder resourceAssembly = new ResourceAssemblyBuilder(path);
                customizer?.Invoke(resourceAssembly);
                ResourceAssemblies.Add(resourceAssembly);
                return this;
            }

            internal RuntimeLibrary Build(BuildContext context)
            {
                return new RuntimeLibrary(
                    Type,
                    Name,
                    Version,
                    string.Empty,
                    AssemblyGroups.Select(g => g.Build(context)).ToList(),
                    NativeLibraryGroups.Select(g => g.Build(context)).ToList(),
                    ResourceAssemblies.Select(ra => ra.Build(context)).ToList(),
                    Enumerable.Empty<Dependency>(),
                    false);
            }
        }

        public class RuntimeFallbacksBuilder
        {
            public string Runtime { get; set; }
            public List<string> Fallbacks { get; } = new List<string>();

            public RuntimeFallbacksBuilder(string runtime, params string[] fallbacks)
            {
                Runtime = runtime;
                Fallbacks.AddRange(fallbacks);
            }

            public RuntimeFallbacksBuilder WithFallback(params string[] fallback)
            {
                Fallbacks.AddRange(fallback);
                return this;
            }

            internal RuntimeFallbacks Build()
            {
                return new RuntimeFallbacks(Runtime, Fallbacks);
            }
        }

        public static NetCoreAppBuilder PortableForNETCoreApp(TestApp sourceApp)
        {
            return new NetCoreAppBuilder()
            {
                _sourceApp = sourceApp,
                Name = sourceApp.Name,
                Framework = ".NETCoreApp,Version=v3.0",
                Runtime = null
            };
        }

        public static NetCoreAppBuilder ForNETCoreApp(string name, string runtime, string version = "3.0")
        {
            return new NetCoreAppBuilder()
            {
                _sourceApp = null,
                Name = name,
                Framework = $".NETCoreApp,Version=v{version}",
                Runtime = runtime
            };
        }

        public NetCoreAppBuilder WithRuntimeConfig(Action<RuntimeConfig> runtimeConfigCustomizer)
        {
            RuntimeConfigCustomizer = runtimeConfigCustomizer;
            return this;
        }

        public NetCoreAppBuilder WithRuntimeLibrary(
            RuntimeLibraryType type,
            string name,
            string version,
            Action<RuntimeLibraryBuilder> customizer = null)
        {
            RuntimeLibraryBuilder runtimeLibrary = new RuntimeLibraryBuilder(type, name, version);
            customizer?.Invoke(runtimeLibrary);

            RuntimeLibraries.Add(runtimeLibrary);
            return this;
        }

        public NetCoreAppBuilder WithProject(string name, string version, Action<RuntimeLibraryBuilder> customizer = null)
        {
            return WithRuntimeLibrary(RuntimeLibraryType.project, name, version, customizer);
        }

        public NetCoreAppBuilder WithProject(Action<RuntimeLibraryBuilder> customizer = null)
        {
            return WithRuntimeLibrary(RuntimeLibraryType.project, Name, "1.0.0", customizer);
        }

        public NetCoreAppBuilder WithPackage(string name, string version, Action<RuntimeLibraryBuilder> customizer = null)
        {
            return WithRuntimeLibrary(RuntimeLibraryType.package, name, version, customizer);
        }

        public NetCoreAppBuilder WithRuntimePack(string name, string version, Action<RuntimeLibraryBuilder> customizer = null)
        {
            return WithRuntimeLibrary(RuntimeLibraryType.runtimepack, $"runtimepack.{name}", version, customizer);
        }

        public NetCoreAppBuilder WithRuntimeFallbacks(string runtime, params string[] fallbacks)
        {
            RuntimeFallbacks.Add(new RuntimeFallbacksBuilder(runtime, fallbacks));
            return this;
        }

        public NetCoreAppBuilder WithStandardRuntimeFallbacks()
        {
            return
                WithRuntimeFallbacks("win10-x64", "win10", "win-x64", "win", "any")
                .WithRuntimeFallbacks("win10-x86", "win10", "win-x86", "win", "any")
                .WithRuntimeFallbacks("win10", "win", "any")
                .WithRuntimeFallbacks("win-arm64", "win", "any")
                .WithRuntimeFallbacks("win-x64", "win", "any")
                .WithRuntimeFallbacks("win-x86", "win", "any")
                .WithRuntimeFallbacks("win", "any")
                .WithRuntimeFallbacks("linux-arm64", "linux", "any")
                .WithRuntimeFallbacks("linux-riscv64", "linux", "any")
                .WithRuntimeFallbacks("linux-x64", "linux", "any")
                .WithRuntimeFallbacks("linux-musl-arm64", "linux-musl", "linux", "any")
                .WithRuntimeFallbacks("linux-musl-riscv64", "linux-musl", "linux", "any")
                .WithRuntimeFallbacks("linux-musl-x64", "linux-musl", "linux", "any")
                .WithRuntimeFallbacks("linux", "any")
                .WithRuntimeFallbacks("osx.10.12-x64", "osx-x64", "osx", "any")
                .WithRuntimeFallbacks("osx-arm64", "osx", "any")
                .WithRuntimeFallbacks("osx-x64", "osx", "any");
        }

        public NetCoreAppBuilder WithLocalPathsInDepsJson(bool includeLocalPaths)
        {
            _includeLocalPathsInDepsJson = includeLocalPaths;
            return this;
        }

        public NetCoreAppBuilder WithCustomizer(Action<NetCoreAppBuilder> customizer)
        {
            customizer?.Invoke(this);
            return this;
        }

        private DependencyContext BuildDependencyContext(BuildContext context)
        {
            return new DependencyContext(
                new TargetInfo(Framework, Runtime, null, Runtime == null),
                CompilationOptions.Default,
                Enumerable.Empty<CompilationLibrary>(),
                RuntimeLibraries.Select(rl => rl.Build(context)),
                RuntimeFallbacks.Select(rf => rf.Build()));
        }

        public TestApp Build()
        {
            return Build(_sourceApp.Copy());
        }

        public TestApp Build(TestApp testApp)
        {
            RuntimeConfig runtimeConfig = null;
            if (File.Exists(testApp.RuntimeConfigJson))
            {
                runtimeConfig = RuntimeConfig.FromFile(testApp.RuntimeConfigJson);
            }
            else if (RuntimeConfigCustomizer != null)
            {
                runtimeConfig = new RuntimeConfig(testApp.RuntimeConfigJson);
            }

            if (runtimeConfig != null)
            {
                RuntimeConfigCustomizer?.Invoke(runtimeConfig);
                runtimeConfig.Save();
            }

            BuildContext buildContext = new BuildContext()
            {
                App = testApp
            };
            DependencyContext dependencyContext = BuildDependencyContext(buildContext);

            DependencyContextWriter writer = new DependencyContextWriter();
            using (FileStream stream = new FileStream(testApp.DepsJson, FileMode.Create))
            {
                writer.Write(dependencyContext, stream);
            }

            // Add localPath properties
            // TODO: Remove once we can upgrade to a Microsoft.Extensions.DependencyModel version that supports localPath.
            if (_includeLocalPathsInDepsJson)
            {
                AddLocalPathsToJson(testApp.DepsJson, buildContext);
            }

            return testApp;
        }

        private void AddLocalPathsToJson(string depsJsonPath, BuildContext buildContext)
        {
            // Read the generated JSON
            string jsonContent = File.ReadAllText(depsJsonPath);

            // Parse and modify the JSON to add localPath properties
            JsonNode rootNode = JsonNode.Parse(jsonContent);
            JsonNode targets = rootNode["targets"][$"{Framework}{(Runtime is null ? "" : $"/{Runtime}")}"];
            foreach (var l in RuntimeLibraries)
            {
                JsonNode library = targets[$"{l.Name}/{l.Version}"];
                AddLocalPathsForFiles(library, l.AssemblyGroups, "runtime");
                AddLocalPathsForFiles(library, l.NativeLibraryGroups, "native");

                // Add localPath for resources
                JsonNode assets = library["resources"];
                foreach (var resource in l.ResourceAssemblies)
                {
                    if (string.IsNullOrEmpty(resource.LocalPath))
                        continue;

                    assets[resource.Path]["localPath"] = resource.LocalPath;
                }
            }

            // Write the modified JSON back to the file
            var options = new JsonSerializerOptions { WriteIndented = true };
            string modifiedJson = rootNode.ToJsonString(options);
            File.WriteAllText(depsJsonPath, modifiedJson);
        }

        private void AddLocalPathsForFiles(JsonNode library, List<RuntimeAssetGroupBuilder> groups, string assetType)
        {
            foreach (var group in groups)
            {
                JsonNode assets = library[!string.IsNullOrEmpty(group.Runtime) ? "runtimeTargets" : assetType];
                foreach (var asset in group.Assets)
                {
                    if (string.IsNullOrEmpty(asset.LocalPath))
                        continue;

                    assets[asset.Path]["localPath"] = asset.LocalPath.Replace('\\', '/'); // .deps.json explicitly uses forward slashes;
                }
            }
        }
    }
}
