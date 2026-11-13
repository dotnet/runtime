// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.NET.Sdk.WebAssembly;
using Xunit;

#nullable enable

namespace Wasm.Build.Tests;

    [TestCategory("no-workload")]
    public class GenerateWasmBootJsonTests
    {
        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public void DeferredReadyToRunManifest(bool deferred, bool publish)
        {
            using var dir = new TempDirectory();
            const string name = "App.r2r.wasm";
            const string route = "App.r2r.012345abcd.wasm";
            string assetPath = Path.Combine(dir.Path, name);
            var resource = new TaskItem(assetPath, new Dictionary<string, string>
            {
                ["AssetTraitName"] = "WasmResource",
                ["AssetTraitValue"] = "readyToRunComposite",
                ["OriginalItemSpec"] = name,
                ["PayloadSize"] = "16",
                ["TableSize"] = "2",
                ["Integrity"] = "test-hash"
            });
            var endpoint = new TaskItem(route, new Dictionary<string, string> { ["AssetFile"] = assetPath });
            string output = Path.Combine(dir.Path, "boot.json");
            var task = new GenerateWasmBootJson
            {
                BuildEngine = new TestBuildEngine(),
                AssemblyPath = typeof(GenerateWasmBootJsonTests).Assembly.Location,
                Resources = [resource],
                Endpoints = [endpoint],
                TargetFrameworkVersion = "v12.0",
                UseMonoRuntime = "false",
                FingerprintAssets = true,
                IsPublish = publish,
                EnableDeferredR2R = deferred,
                OutputPath = output
            };
            Assert.True(task.Execute());
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(output));
            JsonElement resources = document.RootElement.GetProperty("resources");
            JsonElement asset = Assert.Single(resources.GetProperty(deferred ? "r2rAssembly" : "coreAssembly").EnumerateArray());
            Assert.Equal(name, asset.GetProperty("virtualPath").GetString());
            Assert.Equal(route, asset.GetProperty("name").GetString());
            Assert.Equal("sha256-test-hash", asset.GetProperty("hash").GetString());
            Assert.Equal(16, asset.GetProperty("payloadSize").GetInt32());
            Assert.Equal(2, asset.GetProperty("tableSize").GetInt32());
            Assert.True(asset.GetProperty("isCompositeImage").GetBoolean());
            if (deferred)
                Assert.Empty(resources.GetProperty("coreAssembly").EnumerateArray());
            else
                Assert.False(resources.TryGetProperty("r2rAssembly", out _));
            Assert.False(resources.TryGetProperty("lazyAssembly", out _));
            string? originalHash = resources.GetProperty("hash").GetString();
            resource.SetMetadata("Integrity", "changed-hash");
            Assert.True(task.Execute());
            using JsonDocument changed = JsonDocument.Parse(File.ReadAllText(output));
            Assert.NotEqual(originalHash, changed.RootElement.GetProperty("resources").GetProperty("hash").GetString());
        }

        [Theory]
        [InlineData(false, false, false, "v12.0")]
        [InlineData(true, true, false, "v12.0")]
        [InlineData(true, false, true, "v12.0")]
        [InlineData(true, false, false, "v10.0")]
        public void DeferredReadyToRunRejectsUnsupportedConfigurations(bool publish, bool mono, bool threads, string framework)
        {
            using var dir = new TempDirectory();
            string output = Path.Combine(dir.Path, "boot.json");
            var engine = new TestBuildEngine(throwOnError: false);
            var task = new GenerateWasmBootJson
            {
                BuildEngine = engine,
                AssemblyPath = typeof(GenerateWasmBootJsonTests).Assembly.Location,
                Resources = [],
                Endpoints = [],
                TargetFrameworkVersion = framework,
                UseMonoRuntime = mono.ToString(),
                IsPublish = publish,
                IsMultiThreaded = threads,
                EnableDeferredR2R = true,
                OutputPath = output
            };
            Assert.False(task.Execute());
            Assert.Contains("Deferred ReadyToRun requires", Assert.Single(engine.Errors).Message);
            Assert.False(File.Exists(output));
        }

        [Theory]
        [InlineData("v12.0", false, false, true)]
        [InlineData("v12.0", false, true, true)]
        [InlineData("v13.0", false, false, true)]
        [InlineData("v12.0", true, false, false)]
        [InlineData("v11.0", false, false, false)]
        [InlineData("v11.0", false, true, false)]
        [InlineData("v11.0", true, false, false)]
        [InlineData("v10.0", false, false, false)]
        [InlineData("v9.0", true, false, false)]
        public void AppSettingsManifestVersion(string framework, bool mono, bool filesystemUsage, bool expectAssets)
        {
            using var dir = new TempDirectory();
            string path = Path.Combine(dir.Path, "boot.json");
            var task = new GenerateWasmBootJson
            {
                BuildEngine = new TestBuildEngine(),
                AssemblyPath = typeof(GenerateWasmBootJsonTests).Assembly.Location,
                Resources = [],
                Endpoints = [],
                ConfigurationFiles = [new TaskItem("appsettings.json")],
                TargetFrameworkVersion = framework,
                UseMonoRuntime = mono.ToString(),
                FilesystemUsageDetected = filesystemUsage,
                OutputPath = path
            };
            Assert.True(task.Execute());
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
            JsonElement root = document.RootElement;
            if (expectAssets)
            {
                Assert.False(root.TryGetProperty("appsettings", out _));
                JsonElement asset = Assert.Single(root.GetProperty("resources").GetProperty("appsettings").EnumerateArray());
                Assert.Equal("../appsettings.json", asset.GetProperty("name").GetString());
                Assert.Equal("appsettings.json", asset.GetProperty("virtualPath").GetString());
                Assert.Equal(filesystemUsage, asset.GetProperty("loadIntoVfs").GetBoolean());
            }
            else
            {
                Assert.Equal("../appsettings.json", Assert.Single(root.GetProperty("appsettings").EnumerateArray()).GetString());
                Assert.False(root.GetProperty("resources").TryGetProperty("appsettings", out _));
            }
        }

        [Theory]
        [InlineData(true, false, false)]
        [InlineData(true, true, false)]
        [InlineData(true, false, true)]
        [InlineData(true, true, true)]
        [InlineData(false, false, false)]
        [InlineData(false, true, true)]
        public void AppSettingsAssets(bool useAssets, bool filesystemUsage, bool bundlerFriendly)
        {
            string[] names = ["../appsettings.json", "../appsettings.Development.json"];
            var config = new BootJsonData { appsettings = names.ToList() };
            var task = new GenerateWasmBootJson();
            var helper = new BootJsonBuilderHelper(task.Log, "0", false, false, new Version(12, 0), false);
            string imports = helper.TransformResourcesToAssets(config, bundlerFriendly,
                useAppSettingsAssets: useAssets, filesystemUsageDetected: filesystemUsage);
            var assets = Assert.IsType<AssetsData>(config.resources);
            if (useAssets)
            {
                Assert.Null(config.appsettings);
                Assert.Equal(names, assets.appsettings.Select(asset => asset.name));
                Assert.Equal(new[] { "appsettings.json", "appsettings.Development.json" }, assets.appsettings.Select(asset => asset.virtualPath));
                Assert.All(assets.appsettings, asset => {
                    Assert.Equal(filesystemUsage, asset.loadIntoVfs);
                    Assert.Equal("no-cache", asset.cache);
                    Assert.Equal(bundlerFriendly, asset.resolvedUrl is not null);
                });
            }
            else
            {
                Assert.Null(assets.appsettings);
                Assert.NotNull(config.appsettings);
                if (!bundlerFriendly)
                    Assert.Equal(names, config.appsettings);
            }
            Assert.Equal(bundlerFriendly, imports.Contains("import "));
        }

        [Fact]
        public void ReadRuntimeConfigFiles_NullMainConfigPath_ReturnsNull()
        {
            RuntimeConfigData? result = InvokeReadRuntimeConfigFiles(null, null);

            Assert.Null(result);
        }

        [Fact]
        public void ReadRuntimeConfigFiles_MainConfigNotExists_ReturnsNull()
        {
            using var dir = new TempDirectory();
            string nonExistentPath = Path.Combine(dir.Path, "does-not-exist.runtimeconfig.json");
            RuntimeConfigData? result = InvokeReadRuntimeConfigFiles(nonExistentPath, null);

            Assert.Null(result);
        }

        [Fact]
        public void ReadRuntimeConfigFiles_DevConfigPreservesBooleanAndNumericTypes()
        {
            using var dir = new TempDirectory();
            string mainConfig = WriteRuntimeConfig(dir.Path, "App.runtimeconfig.json",
                configProperties: new() { ["key1"] = "value1" });
            string devConfigPath = Path.Combine(dir.Path, "App.runtimeconfig.dev.json");
            File.WriteAllText(devConfigPath, """
                {
                  "runtimeOptions": {
                    "configProperties": {
                      "System.HotReload.Enable": true,
                      "System.HotReload.MaxRetries": 10
                    }
                  }
                }
                """);

            RuntimeConfigData? result = InvokeReadRuntimeConfigFiles(mainConfig, devConfigPath);

            Assert.NotNull(result?.runtimeOptions?.configProperties);
            Dictionary<string, object> props = result!.runtimeOptions!.configProperties!;
            Assert.Equal(JsonValueKind.True, ((JsonElement)props["System.HotReload.Enable"]).ValueKind);
            Assert.Equal(JsonValueKind.Number, ((JsonElement)props["System.HotReload.MaxRetries"]).ValueKind);
            Assert.Equal(10, ((JsonElement)props["System.HotReload.MaxRetries"]).GetInt32());
        }

        [Fact]
        public void ReadRuntimeConfigFiles_MainConfigOnly_ReturnsMainProperties()
        {
            using var dir = new TempDirectory();
            string mainConfig = WriteRuntimeConfig(dir.Path, "App.runtimeconfig.json",
                configProperties: new() { ["key1"] = "value1", ["key2"] = "42" });

            RuntimeConfigData? result = InvokeReadRuntimeConfigFiles(mainConfig, null);

            Assert.NotNull(result);
            Assert.NotNull(result.runtimeOptions?.configProperties);
            Assert.Equal("value1", result.runtimeOptions!.configProperties!["key1"].ToString());
            Assert.Equal("42", result.runtimeOptions.configProperties["key2"].ToString());
        }

        [Fact]
        public void ReadRuntimeConfigFiles_DevConfigNotExists_ReturnsMainPropertiesUnchanged()
        {
            using var dir = new TempDirectory();
            string mainConfig = WriteRuntimeConfig(dir.Path, "App.runtimeconfig.json",
                configProperties: new() { ["key1"] = "value1" });
            string devConfigPath = Path.Combine(dir.Path, "App.runtimeconfig.dev.json");

            RuntimeConfigData? result = InvokeReadRuntimeConfigFiles(mainConfig, devConfigPath);

            Assert.NotNull(result);
            Assert.Equal("value1", result.runtimeOptions?.configProperties?["key1"].ToString());
        }

        [Fact]
        public void ReadRuntimeConfigFiles_DevConfigAddsNewProperty()
        {
            using var dir = new TempDirectory();
            string mainConfig = WriteRuntimeConfig(dir.Path, "App.runtimeconfig.json",
                configProperties: new() { ["key1"] = "value1" });
            string devConfig = WriteRuntimeConfig(dir.Path, "App.runtimeconfig.dev.json",
                configProperties: new() { ["key2"] = "value2" });

            RuntimeConfigData? result = InvokeReadRuntimeConfigFiles(mainConfig, devConfig);

            Assert.NotNull(result?.runtimeOptions?.configProperties);
            Assert.Equal("value1", result!.runtimeOptions!.configProperties!["key1"].ToString());
            Assert.Equal("value2", result.runtimeOptions.configProperties["key2"].ToString());
        }

        [Fact]
        public void ReadRuntimeConfigFiles_DevConfigOverridesMainProperty()
        {
            using var dir = new TempDirectory();
            string mainConfig = WriteRuntimeConfig(dir.Path, "App.runtimeconfig.json",
                configProperties: new() { ["System.Runtime.Feature"] = "false" });
            string devConfig = WriteRuntimeConfig(dir.Path, "App.runtimeconfig.dev.json",
                configProperties: new() { ["System.Runtime.Feature"] = "true" });

            RuntimeConfigData? result = InvokeReadRuntimeConfigFiles(mainConfig, devConfig);

            Assert.NotNull(result?.runtimeOptions?.configProperties);
            Assert.Equal("true", result!.runtimeOptions!.configProperties!["System.Runtime.Feature"].ToString());
        }

        [Fact]
        public void ReadRuntimeConfigFiles_DevConfigMergesWhenMainHasNoConfigProperties()
        {
            using var dir = new TempDirectory();
            string mainConfig = WriteRuntimeConfig(dir.Path, "App.runtimeconfig.json",
                configProperties: null);
            string devConfig = WriteRuntimeConfig(dir.Path, "App.runtimeconfig.dev.json",
                configProperties: new() { ["System.HotReload.Enable"] = "true" });

            RuntimeConfigData? result = InvokeReadRuntimeConfigFiles(mainConfig, devConfig);

            Assert.NotNull(result?.runtimeOptions?.configProperties);
            Assert.Equal("true", result!.runtimeOptions!.configProperties!["System.HotReload.Enable"].ToString());
        }

        [Fact]
        public void ReadRuntimeConfigFiles_DevConfigEmptyProperties_DoesNotAlterResult()
        {
            using var dir = new TempDirectory();
            string mainConfig = WriteRuntimeConfig(dir.Path, "App.runtimeconfig.json",
                configProperties: new() { ["key1"] = "value1" });
            string devConfig = WriteRuntimeConfig(dir.Path, "App.runtimeconfig.dev.json",
                configProperties: new());

            RuntimeConfigData? result = InvokeReadRuntimeConfigFiles(mainConfig, devConfig);

            Assert.NotNull(result?.runtimeOptions?.configProperties);
            Assert.Single(result!.runtimeOptions!.configProperties!);
            Assert.Equal("value1", result.runtimeOptions.configProperties["key1"].ToString());
        }

        private sealed class TestBuildEngine(bool throwOnError = true) : IBuildEngine
        {
            public List<BuildErrorEventArgs> Errors { get; } = [];
            public bool ContinueOnError => false;
            public int LineNumberOfTaskNode => 0;
            public int ColumnNumberOfTaskNode => 0;
            public string ProjectFileOfTaskNode => string.Empty;
            public void LogErrorEvent(BuildErrorEventArgs e)
            {
                Errors.Add(e);
                if (throwOnError)
                    throw new InvalidOperationException(e.Message);
            }
            public void LogWarningEvent(BuildWarningEventArgs e) => throw new InvalidOperationException(e.Message);
            public void LogMessageEvent(BuildMessageEventArgs e) { }
            public void LogCustomEvent(CustomBuildEventArgs e) { }
            public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
                => throw new NotSupportedException();
        }

        private static string WriteRuntimeConfig(string dir, string fileName, Dictionary<string, string>? configProperties)
        {
            string path = Path.Combine(dir, fileName);
            using var stream = File.OpenWrite(path);
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
            writer.WriteStartObject();
            writer.WritePropertyName("runtimeOptions");
            writer.WriteStartObject();
            if (configProperties is not null)
            {
                writer.WritePropertyName("configProperties");
                writer.WriteStartObject();
                foreach ((string key, string value) in configProperties)
                {
                    writer.WriteString(key, value);
                }
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
            writer.WriteEndObject();
            return path;
        }

        private static RuntimeConfigData? InvokeReadRuntimeConfigFiles(string? mainConfigPath, string? devConfigPath)
        {
            MethodInfo? method = typeof(GenerateWasmBootJson).GetMethod(
                "ReadRuntimeConfigFiles",
                BindingFlags.Static | BindingFlags.NonPublic);

            if (method is null)
            {
                throw new InvalidOperationException(
                    $"{nameof(GenerateWasmBootJson)}.{nameof(InvokeReadRuntimeConfigFiles)} could not find private method " +
                    "'ReadRuntimeConfigFiles(string?, string?)'. The production method signature may have changed.");
            }

            object? result = method.Invoke(null, new object?[] { mainConfigPath, devConfigPath });
            return (RuntimeConfigData?)result;
        }

        private sealed class TempDirectory : IDisposable
        {
            public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());

            public TempDirectory() => Directory.CreateDirectory(Path);

            public void Dispose()
            {
                try
                {
                    Directory.Delete(Path, recursive: true);
                }
                catch (DirectoryNotFoundException)
                {
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }
}
