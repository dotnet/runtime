
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using BundleTests;
using BundleTests.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.Extensions.DependencyModel;
using Microsoft.NET.HostModel.AppHost;
using Microsoft.NET.HostModel.Bundle;
using Xunit;
using static Microsoft.DotNet.CoreSetup.Test.NetCoreAppBuilder;

namespace AppHost.Bundle.Tests
{
    internal sealed class AppWithSubDirs : IDisposable
    {
        private const string AppName = nameof(AppWithSubDirs);

        private static RepoDirectoriesProvider s_provider => RepoDirectoriesProvider.Default;

        private readonly TempDirectory _outDir = NoSdkTestBase.TempRoot.CreateDirectory();

        public string BundleFxDependent(BundleOptions options, Version? bundleVersion = null)
        {
            // First write out the app to a temp directory
            var tempDir = _outDir.CreateDirectory("temp");

            // Now bundle it
            WriteAppToDirectory(tempDir.Path, selfContained: false);

            string singleFile = BundleApp(
                options,
                RuntimeInformationExtensions.GetExeFileNameForCurrentPlatform(AppName),
                tempDir.Path,
                _outDir.Path,
                selfContained: false,
                bundleVersion);

            if (options != BundleOptions.BundleAllContent)
            {
                CopySentenceSubDir(_outDir.Path);
            }

            return singleFile;
        }

        public string BundleSelfContained(BundleOptions options, Version? bundleVersion = null)
        {
            // First write out the app to a temp directory
            var tempDir = _outDir.CreateDirectory("temp");
            WriteAppToDirectory(tempDir.Path, selfContained: true);

            // Now bundle it
            string singleFile = BundleApp(
                options,
                RuntimeInformationExtensions.GetExeFileNameForCurrentPlatform(AppName),
                tempDir.Path,
                _outDir.Path,
                selfContained: true,
                bundleVersion);

            if (options != BundleOptions.BundleAllContent)
            {
                CopySentenceSubDir(_outDir.Path);
            }

            return singleFile;
        }

        private static string BundleApp(
            BundleOptions options,
            string hostName,
            string srcDir,
            string outDir,
            bool selfContained,
            Version? bundleVersion)
        {
            var rid = s_provider.TargetRID;

            var bundler = new Bundler(
                hostName,
                outDir,
                options,
                BundleHelper.GetTargetOS(rid),
                BundleHelper.GetTargetArch(rid),
                targetFrameworkVersion: bundleVersion,
                macosCodesign: true);

            // Get all files in the source directory and all sub-directories.
            string[] sources = Directory.GetFiles(srcDir, searchPattern: "*", searchOption: SearchOption.AllDirectories);

            // Sort the file names to keep the bundle construction deterministic.
            Array.Sort(sources, StringComparer.Ordinal);

            List<FileSpec> fileSpecs = new List<FileSpec>(sources.Length);
            foreach (var file in sources)
            {
                fileSpecs.Add(new FileSpec(file, Path.GetRelativePath(srcDir, file)));
            }

            // If this is a self-contained app, add the runtimepack assets to the bundle
            if (selfContained)
            {
                var runtimePackAssets = s_provider.RuntimePackPath;
                var assets = Directory.GetFiles(runtimePackAssets, searchPattern: "*.dll", searchOption: SearchOption.AllDirectories);
                foreach (var asset in assets)
                {
                    fileSpecs.Add(new FileSpec(asset, Path.GetRelativePath(runtimePackAssets, asset)));
                }
                var asmName = "System.Private.CoreLib.dll";
                var spcPath = Path.Combine(
                    s_provider.CoreClrPath,
                    asmName);
                fileSpecs.Add(new FileSpec(spcPath, asmName));
            }

            return bundler.GenerateBundle(fileSpecs);
        }

        private static ImmutableArray<MetadataReference> _refAssemblies = default;
        public static ImmutableArray<MetadataReference> RefAssemblies
        {
            get
            {
                if (_refAssemblies.IsDefault)
                {
                    var refDirPath = Path.Combine(
                        s_provider.BaseArtifactsFolder,
                        "bin", "microsoft.netcore.app.ref", "ref/",
                        s_provider.Tfm);
                    var references = Directory.GetFiles(refDirPath, "*.dll")
                        .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
                        .ToImmutableArray();
                    ImmutableInterlocked.InterlockedInitialize(
                        ref _refAssemblies,
                        references);
                }
                return _refAssemblies;
            }
        }

        private static void WriteAppToDirectory(string tempDir, bool selfContained)
        {
            var srcPath = Path.Combine(
                s_provider.TestAssetsFolder,
                "TestProjects/AppWithSubDirs/Program.cs");

            var comp = CSharpCompilation.Create(
                assemblyName: AppName,
                syntaxTrees: new[] { CSharpSyntaxTree.ParseText(File.ReadAllText(srcPath)) },
                references: RefAssemblies);
            var emitResult = comp.Emit(Path.Combine(tempDir, $"{AppName}.dll"));
            Assert.True(emitResult.Success);

            if (!selfContained)
            {
                WriteBasicRuntimeConfig(
                    Path.Combine(tempDir, $"{AppName}.runtimeconfig.json"),
                    s_provider.Tfm,
                    s_provider.MicrosoftNETCoreAppVersion,
                    selfContained);

                DependencyContext dependencyContext = new DependencyContext(
                    new Microsoft.Extensions.DependencyModel.TargetInfo($".NETCoreApp,Version=v{s_provider.Tfm[3..]}", null, null, true),
                    Microsoft.Extensions.DependencyModel.CompilationOptions.Default,
                    Enumerable.Empty<CompilationLibrary>(),
                    new[]
                    {
                        new RuntimeLibrary(
                            RuntimeLibraryType.project.ToString(),
                            AppName,
                            "1.0.0",
                            string.Empty,
                            new []
                            {
                                new RuntimeAssetGroup(null, $"{AppName}.dll")
                            },
                            Array.Empty<RuntimeAssetGroup>(),
                            Enumerable.Empty<ResourceAssembly>(),
                            Enumerable.Empty<Dependency>(),
                            false)
                    },
                    Enumerable.Empty<RuntimeFallbacks>());

                DependencyContextWriter writer = new DependencyContextWriter();
                using (FileStream stream = new FileStream(Path.Combine(tempDir, $"{AppName}.deps.json"), FileMode.Create))
                {
                    writer.Write(dependencyContext, stream);
                };
            }
            else
            {
                WriteBasicSelfContainedDepsJson(Path.Combine(tempDir, $"{AppName}.deps.json"));
            }

            HostWriter.CreateAppHost(
                GetAppHostPath(AppName, selfContained),
                Path.Combine(tempDir, RuntimeInformationExtensions.GetExeFileNameForCurrentPlatform(AppName)),
                $"{AppName}.dll",
                windowsGraphicalUserInterface: false,
                assemblyToCopyResourcesFrom: null,
                enableMacOSCodeSign: true);

            CopySentenceSubDir(tempDir);
        }

        private static string GetAppHostPath(
            string appName,
            bool selfContained)
        {
            return selfContained
                ? Path.Combine(s_provider.CoreClrPath, "corehost", RuntimeInformationExtensions.GetExeFileNameForCurrentPlatform("singlefilehost"))
                : Path.Combine(s_provider.HostArtifacts, RuntimeInformationExtensions.GetExeFileNameForCurrentPlatform("apphost"));
        }

        private static void WriteBasicRuntimeConfig(
            string outputPath,
            string tfm,
            string version,
            bool selfContained)
        {
            var frameworkText = @$"
            {{
                ""name"": ""Microsoft.NETCore.App"",
                ""version"": ""{version}""
            }}";
            string text;
            if (selfContained)
            {
                text = @$"
{{
    ""runtimeOptions"": {{
        ""tfm"": ""{tfm}"",
        ""includedFrameworks"": [
{frameworkText}
    ]
    }}
}}";
            }
            else
            {
                text = @$"
{{
    ""runtimeOptions"": {{
        ""tfm"": ""{tfm}"",
        ""framework"":
{frameworkText}
    }}
}}";
            }
            File.WriteAllText(outputPath, text);
        }

        private static void WriteBasicSelfContainedDepsJson(string outputPath)
        {
            var runtimePackDir = s_provider.RuntimePackPath;
            var runtimeFiles = Directory.GetFiles(runtimePackDir, "*.dll")
                .Append(
                    //System.Private.CoreLib is built and stored separately
                    Path.Combine(s_provider.CoreClrPath, "System.Private.CoreLib.dll"))
                .Where(f =>
                {
                    using (var fs = File.OpenRead(f))
                    using (var peReader = new PEReader(fs))
                    {
                        return peReader.HasMetadata && peReader.GetMetadataReader().IsAssembly;
                    }
                })
                .Select(f =>
                {
                    var fileVersion = FileVersionInfo.GetVersionInfo(f).FileVersion;
                    var asmVersion = AssemblyName.GetAssemblyName(f).Version!.ToString();
                    return KeyValuePair.Create(Path.GetFileName(f), (JsonNode?)new JsonObject(new KeyValuePair<string, JsonNode?>[] {
                        new("assemblyVersion", asmVersion),
                        new("fileVersion", fileVersion)
                    }));
                });
            var version = s_provider.MicrosoftNETCoreAppVersion;
            var shortVersion = s_provider.Tfm[3..]; // trim "net" from beginning
            var targetRid = s_provider.TargetRID;
            var value = new JsonObject(new KeyValuePair<string, JsonNode?>[]
            {
                new("runtimeTarget", new JsonObject(new KeyValuePair<string, JsonNode?>[] {
                    new("name", JsonValue.Create($".NETCoreApp,Version=v{shortVersion}/{targetRid}")),
                    new("signature", JsonValue.Create(""))
                })),
                new("compilationOptions", new JsonObject()),
                new("targets", new JsonObject(new KeyValuePair<string, JsonNode?>[] {
                    new($".NETCoreApp,Version=v{shortVersion}", new JsonObject()),
                    new($".NETCoreApp,Version=v{shortVersion}/{targetRid}", new JsonObject(new KeyValuePair<string, JsonNode?>[] {
                        new("AppWithSubDirs/1.0.0", new JsonObject(new KeyValuePair<string, JsonNode?>[] {
                            new("dependencies", new JsonObject(new KeyValuePair<string, JsonNode?>[] {
                                new($"runtimepack.Microsoft.NETCore.App.Runtime.{targetRid}", version)
                            })),
                            new("runtime", new JsonObject(new KeyValuePair<string, JsonNode?>[] {
                                new("AppWithSubDirs.dll", new JsonObject())
                            }))
                        })),
                        new($"runtimepack.Microsoft.NETCore.App.Runtime.{targetRid}/{version}", new JsonObject(new KeyValuePair<string, JsonNode?>[] {
                            new("runtime", new JsonObject(runtimeFiles))
                        }))
                    }))
                })),
                new("libraries", new JsonObject(new KeyValuePair<string, JsonNode?>[] {
                    new("AppWithSubDirs/1.0.0", new JsonObject(new KeyValuePair<string, JsonNode?>[] {
                        new("type", "project"),
                        new("serviceable", false),
                        new("sha512", "")
                    })),
                    new($"runtimepack.Microsoft.NETCore.App.Runtime.{targetRid}/{version}", new JsonObject(new KeyValuePair<string, JsonNode?>[] {
                        new("type", "runtimepack"),
                        new("serviceable", false),
                        new("sha512", "")
                    }))
                }))
            });

            var text = value.ToJsonString(new JsonSerializerOptions() { WriteIndented = true });
            File.WriteAllText(outputPath, text);
        }

        // Specific to the AppWithSubDirs app
        private static void CopySentenceSubDir(string targetDir)
        {
            TestArtifact.CopyRecursive(
                Path.Combine(
                    s_provider.TestAssetsFolder,
                    "TestProjects",
                    "AppWithSubDirs",
                    "Sentence"),
                Path.Combine(targetDir, "Sentence"),
                overwrite: false);

            BundleHelper.AddLongNameContentToAppWithSubDirs(targetDir);
        }

        public void Dispose()
        {
            _outDir.Dispose();
        }
    }
}
