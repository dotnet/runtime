// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Xunit;

namespace Microsoft.NET.Sdk.WebAssembly.Tests;

public class GenerateWasmBootJsonTests
{
    [Fact]
    public void ReadRuntimeConfigFiles_NullMainConfigPath_ReturnsNull()
    {
        var result = GenerateWasmBootJson.ReadRuntimeConfigFiles(null, null);

        Assert.Null(result);
    }

    [Fact]
    public void ReadRuntimeConfigFiles_MainConfigNotExists_ReturnsNull()
    {
        using var dir = new TempDirectory();
        var nonExistentPath = Path.Combine(dir.Path, "does-not-exist.runtimeconfig.json");
        var result = GenerateWasmBootJson.ReadRuntimeConfigFiles(nonExistentPath, null);

        Assert.Null(result);
    }

    [Fact]
    public void ReadRuntimeConfigFiles_DevConfigPreservesBooleanAndNumericTypes()
    {
        using var dir = new TempDirectory();
        var mainConfig = WriteRuntimeConfig(dir.Path, "App.runtimeconfig.json",
            configProperties: new() { ["key1"] = "value1" });
        // Write dev config with native JSON boolean and number (not string) values.
        var devConfigPath = Path.Combine(dir.Path, "App.runtimeconfig.dev.json");
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

        var result = GenerateWasmBootJson.ReadRuntimeConfigFiles(mainConfig, devConfigPath);

        Assert.NotNull(result?.runtimeOptions?.configProperties);
        var props = result!.runtimeOptions!.configProperties!;
        Assert.Equal(JsonValueKind.True, ((JsonElement)props["System.HotReload.Enable"]).ValueKind);
        Assert.Equal(JsonValueKind.Number, ((JsonElement)props["System.HotReload.MaxRetries"]).ValueKind);
        Assert.Equal(10, ((JsonElement)props["System.HotReload.MaxRetries"]).GetInt32());
    }

    [Fact]
    public void ReadRuntimeConfigFiles_MainConfigOnly_ReturnsMainProperties()
    {
        using var dir = new TempDirectory();
        var mainConfig = WriteRuntimeConfig(dir.Path, "App.runtimeconfig.json",
            configProperties: new() { ["key1"] = "value1", ["key2"] = "42" });

        var result = GenerateWasmBootJson.ReadRuntimeConfigFiles(mainConfig, null);

        Assert.NotNull(result);
        Assert.NotNull(result.runtimeOptions?.configProperties);
        Assert.Equal("value1", result.runtimeOptions!.configProperties!["key1"].ToString());
        Assert.Equal("42", result.runtimeOptions.configProperties["key2"].ToString());
    }

    [Fact]
    public void ReadRuntimeConfigFiles_DevConfigNotExists_ReturnsMainPropertiesUnchanged()
    {
        using var dir = new TempDirectory();
        var mainConfig = WriteRuntimeConfig(dir.Path, "App.runtimeconfig.json",
            configProperties: new() { ["key1"] = "value1" });
        var devConfigPath = Path.Combine(dir.Path, "App.runtimeconfig.dev.json");

        var result = GenerateWasmBootJson.ReadRuntimeConfigFiles(mainConfig, devConfigPath);

        Assert.NotNull(result);
        Assert.Equal("value1", result.runtimeOptions?.configProperties?["key1"].ToString());
    }

    [Fact]
    public void ReadRuntimeConfigFiles_DevConfigAddsNewProperty()
    {
        using var dir = new TempDirectory();
        var mainConfig = WriteRuntimeConfig(dir.Path, "App.runtimeconfig.json",
            configProperties: new() { ["key1"] = "value1" });
        var devConfig = WriteRuntimeConfig(dir.Path, "App.runtimeconfig.dev.json",
            configProperties: new() { ["key2"] = "value2" });

        var result = GenerateWasmBootJson.ReadRuntimeConfigFiles(mainConfig, devConfig);

        Assert.NotNull(result?.runtimeOptions?.configProperties);
        Assert.Equal("value1", result!.runtimeOptions!.configProperties!["key1"].ToString());
        Assert.Equal("value2", result.runtimeOptions.configProperties["key2"].ToString());
    }

    [Fact]
    public void ReadRuntimeConfigFiles_DevConfigOverridesMainProperty()
    {
        using var dir = new TempDirectory();
        var mainConfig = WriteRuntimeConfig(dir.Path, "App.runtimeconfig.json",
            configProperties: new() { ["System.Runtime.Feature"] = "false" });
        var devConfig = WriteRuntimeConfig(dir.Path, "App.runtimeconfig.dev.json",
            configProperties: new() { ["System.Runtime.Feature"] = "true" });

        var result = GenerateWasmBootJson.ReadRuntimeConfigFiles(mainConfig, devConfig);

        Assert.NotNull(result?.runtimeOptions?.configProperties);
        Assert.Equal("true", result!.runtimeOptions!.configProperties!["System.Runtime.Feature"].ToString());
    }

    [Fact]
    public void ReadRuntimeConfigFiles_DevConfigMergesWhenMainHasNoConfigProperties()
    {
        using var dir = new TempDirectory();
        var mainConfig = WriteRuntimeConfig(dir.Path, "App.runtimeconfig.json",
            configProperties: null);
        var devConfig = WriteRuntimeConfig(dir.Path, "App.runtimeconfig.dev.json",
            configProperties: new() { ["System.HotReload.Enable"] = "true" });

        var result = GenerateWasmBootJson.ReadRuntimeConfigFiles(mainConfig, devConfig);

        Assert.NotNull(result?.runtimeOptions?.configProperties);
        Assert.Equal("true", result!.runtimeOptions!.configProperties!["System.HotReload.Enable"].ToString());
    }

    [Fact]
    public void ReadRuntimeConfigFiles_DevConfigEmptyProperties_DoesNotAlterResult()
    {
        using var dir = new TempDirectory();
        var mainConfig = WriteRuntimeConfig(dir.Path, "App.runtimeconfig.json",
            configProperties: new() { ["key1"] = "value1" });
        var devConfig = WriteRuntimeConfig(dir.Path, "App.runtimeconfig.dev.json",
            configProperties: new());

        var result = GenerateWasmBootJson.ReadRuntimeConfigFiles(mainConfig, devConfig);

        Assert.NotNull(result?.runtimeOptions?.configProperties);
        Assert.Single(result!.runtimeOptions!.configProperties!);
        Assert.Equal("value1", result.runtimeOptions.configProperties["key1"].ToString());
    }

    private static string WriteRuntimeConfig(string dir, string fileName, Dictionary<string, string>? configProperties)
    {
        var path = Path.Combine(dir, fileName);
        using var stream = File.OpenWrite(path);
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
        writer.WriteStartObject();
        writer.WritePropertyName("runtimeOptions");
        writer.WriteStartObject();
        if (configProperties is not null)
        {
            writer.WritePropertyName("configProperties");
            writer.WriteStartObject();
            foreach (var (key, value) in configProperties)
                writer.WriteString(key, value);
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
        writer.WriteEndObject();
        return path;
    }

    private sealed class TempDirectory : System.IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());

        public TempDirectory() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            // Silently ignore cleanup failures to avoid masking actual test failures.
            try { Directory.Delete(Path, recursive: true); } catch { }
        }
    }
}
