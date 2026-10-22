// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.NET.Build.Tasks;
using Microsoft.NET.HostModel.AppHost;
using Microsoft.NET.HostModel.Bundle;
using Xunit;

namespace Crossgen2Tasks.Tests;

public class Crossgen2CacheTests
{
    [CachePlatformTheory]
    [InlineData("")]
    [InlineData("false")]
    [InlineData("invalid")]
    [InlineData("true")]
    [InlineData("TRUE")]
    public void Configuration(string setting) => RemoteExecutor.Invoke(value =>
    {
        using var fixture = new Fixture();
        Environment.SetEnvironmentVariable("CROSSGEN2_EXPERIMENTAL_CACHE", value);
        MockCompiler first = fixture.Create();
        Assert.True(first.Execute());
        Assert.Equal(1, first.Executions);
        MockCompiler second = fixture.Create();
        Assert.True(second.Execute());
        Assert.Equal(value.Equals("true", StringComparison.OrdinalIgnoreCase) ? 0 : 1, second.Executions);
        Assert.Equal(value.Equals("true", StringComparison.OrdinalIgnoreCase), Directory.Exists(fixture.Cache));
        if (value == "invalid")
        {
            Assert.Contains("Crossgen2 caching is disabled because CROSSGEN2_EXPERIMENTAL_CACHE must be 'true' or 'false'.", second.Engine.Messages);
        }
    }, setting).Dispose();

    [CachePlatformTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void NormalExecutionCleansDeclaredFiles(bool enabled, bool supported) => RemoteExecutor.Invoke((enable, support) =>
    {
        using var fixture = new Fixture();
        Environment.SetEnvironmentVariable("CROSSGEN2_EXPERIMENTAL_CACHE", enable);
        Directory.CreateDirectory(Path.GetDirectoryName(fixture.Output)!);
        File.WriteAllText(fixture.Output, "stale image");
        File.WriteAllText(fixture.Map, "stale symbols");
        string neighbor = Path.Combine(Path.GetDirectoryName(fixture.Output)!, "unrelated.txt");
        File.WriteAllText(neighbor, "neighbor");
        MockCompiler task = fixture.Create();
        task.ProduceMap = false;
        task.WarningsDetected = true;
        if (!bool.Parse(support))
            task.Crossgen2ExtraCommandLineArgs = "--unrecognized";
        task.BeforeExecute = () =>
        {
            Assert.False(File.Exists(fixture.Output));
            Assert.False(File.Exists(fixture.Map));
        };

        Assert.True(task.Execute());
        Assert.Equal(1, task.Executions);
        Assert.False(task.WarningsDetected);
        bool cacheable = bool.Parse(enable) && bool.Parse(support);
        Assert.False(File.Exists(fixture.Map));
        Assert.Equal(cacheable, Directory.Exists(fixture.Cache));
        Assert.Equal("neighbor", File.ReadAllText(neighbor));
        Assert.True(File.Exists(fixture.Output));
    }, enabled.ToString(), supported.ToString()).Dispose();

    [CachePlatformTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void NormalExecutionCleansDefaultSymbolsWithoutMetadata(bool enabled) => RemoteExecutor.Invoke(enable =>
    {
        using var fixture = new Fixture();
        Environment.SetEnvironmentVariable("CROSSGEN2_EXPERIMENTAL_CACHE", enable);
        Directory.CreateDirectory(Path.GetDirectoryName(fixture.Output)!);
        File.WriteAllText(fixture.Map, "stale symbols");
        MockCompiler task = fixture.Create();
        task.CompilationEntry.RemoveMetadata("OutputPDBImage");
        task.CompilationEntry.SetMetadata("EmitSymbols", "false");
        task.BeforeExecute = () => Assert.False(File.Exists(fixture.Map));

        Assert.True(task.Execute());
        Assert.Equal(1, task.Executions);
        Assert.False(File.Exists(fixture.Map));
        Assert.Equal(bool.Parse(enable), Directory.Exists(fixture.Cache));
    }, enabled.ToString()).Dispose();

    [CachePlatformTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void OutputPreparationFailuresDoNotExecuteOrStore(bool enabled) => RemoteExecutor.Invoke(enable =>
    {
        using var fixture = new Fixture();
        Environment.SetEnvironmentVariable("CROSSGEN2_EXPERIMENTAL_CACHE", enable);
        Directory.CreateDirectory(fixture.Output);
        MockCompiler task = fixture.Create();
        task.WarningsDetected = true;

        Assert.False(task.Execute());
        Assert.Equal(-1, task.ExitCode);
        Assert.Equal(0, task.Executions);
        Assert.False(task.WarningsDetected);
        Assert.NotEmpty(task.Engine.Errors);
        Assert.False(Directory.Exists(fixture.Cache));
    }, enabled.ToString()).Dispose();

    [CachePlatformFact]
    public void PdbOnlyCompilationPreservesItsNativeImageInput() => RemoteExecutor.Invoke(() =>
    {
        using var fixture = new Fixture();
        byte[] image = File.ReadAllBytes(fixture.Input);
        Directory.CreateDirectory(Path.GetDirectoryName(fixture.Map)!);
        File.WriteAllText(fixture.Map, "stale symbols");
        MockCompiler task = fixture.Create();
        task.CrossgenTool = new TaskItem(fixture.Tool);
        task.CrossgenTool.SetMetadata("JitPath", fixture.Jit);
        task.CompilationEntry.SetMetadata("CreatePDBCommand", $"/CreatePDB \"{Path.GetDirectoryName(fixture.Map)}\"");
        task.BeforeExecute = () =>
        {
            Assert.Equal(image, File.ReadAllBytes(fixture.Input));
            Assert.False(File.Exists(fixture.Map));
        };

        Assert.True(task.Execute());
        Assert.Equal(1, task.Executions);
        Assert.Equal(image, File.ReadAllBytes(fixture.Input));
        Assert.False(Directory.Exists(fixture.Cache));
    }).Dispose();

    [CachePlatformTheory]
    [InlineData("")]
    [InlineData("false")]
    [InlineData("invalid")]
    public void DisabledConfigurationBypassesAnExactWarmEntry(string setting) => RemoteExecutor.Invoke(value =>
    {
        using var fixture = new Fixture();
        Assert.True(fixture.Create().Execute());
        Environment.SetEnvironmentVariable("CROSSGEN2_EXPERIMENTAL_CACHE", value);
        MockCompiler disabled = fixture.Create();
        Assert.True(disabled.Execute());
        Assert.Equal(1, disabled.Executions);
        Environment.SetEnvironmentVariable("CROSSGEN2_EXPERIMENTAL_CACHE", "true");
        MockCompiler warm = fixture.Create();
        Assert.True(warm.Execute());
        Assert.Equal(0, warm.Executions);
    }, setting).Dispose();

    [CachePlatformTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void RoundTripPreservesTaskOutputsWithoutWarningReplay(bool showWarnings, bool emitWarning) => RemoteExecutor.Invoke((show, emit) =>
    {
        using var fixture = new Fixture();
        string neighbor = Path.Combine(fixture.Root, "out", "unrelated.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(neighbor)!);
        File.WriteAllText(neighbor, "do not delete");
        MockCompiler miss = fixture.Create();
        miss.ShowCompilerWarnings = bool.Parse(show);
        miss.EmitWarning = bool.Parse(emit);
        Assert.True(miss.Execute());
        Assert.Equal(bool.Parse(emit) && !bool.Parse(show), miss.WarningsDetected);
        byte[] image = File.ReadAllBytes(fixture.Output);
        byte[] map = File.ReadAllBytes(fixture.Map);
        File.Delete(fixture.Output);
        File.WriteAllText(fixture.Map, "stale");
        DateTime before = DateTime.UtcNow;
        MockCompiler hit = fixture.Create();
        hit.ShowCompilerWarnings = miss.ShowCompilerWarnings;
        hit.WarningsDetected = !miss.WarningsDetected;
        Assert.True(hit.Execute());
        Assert.Equal(0, hit.Executions);
        Assert.Equal(0, hit.ExitCode);
        Assert.Equal(image, File.ReadAllBytes(fixture.Output));
        Assert.Equal(map, File.ReadAllBytes(fixture.Map));
        Assert.Equal(miss.WarningsDetected, hit.WarningsDetected);
        Assert.Empty(hit.Engine.Warnings);
        Assert.DoesNotContain(hit.Engine.Messages, message => message.Contains("warning:", StringComparison.Ordinal));
        Assert.InRange(File.GetLastWriteTimeUtc(fixture.Output), before.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
        Assert.Equal("do not delete", File.ReadAllText(neighbor));
    }, showWarnings.ToString(), emitWarning.ToString()).Dispose();

    [CachePlatformTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void InvalidTaskOutputsDoNotLeakWarningStateIntoFallbackExecution(bool truncated) => RemoteExecutor.Invoke(value =>
    {
        using var fixture = new Fixture();
        MockCompiler miss = fixture.Create();
        miss.EmitWarning = true;
        Assert.True(miss.Execute());
        string entry = Directory.GetDirectories(Path.Combine(fixture.Cache, InvocationCacheEntry.VersionDirectory)).Single();
        string manifestPath = Path.Combine(entry, "manifest");
        long metadataPosition;
        using (var reader = new BinaryReader(File.OpenRead(manifestPath)))
        {
            Assert.Equal(InvocationCacheEntry.FormatVersion, reader.ReadInt32());
            Assert.True(reader.ReadBoolean());
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                reader.ReadString();
                if (reader.ReadBoolean())
                {
                    reader.ReadInt64();
                    reader.ReadBytes(32);
                }
            }
            metadataPosition = reader.BaseStream.Position;
        }
        using (var writer = new BinaryWriter(File.OpenWrite(manifestPath)))
        {
            writer.BaseStream.Position = metadataPosition;
            if (!bool.Parse(value))
            {
                writer.Write(true);
                writer.Write((byte)0);
            }
            writer.BaseStream.SetLength(writer.BaseStream.Position);
        }
        File.WriteAllBytes(Path.Combine(entry, "manifest.sha256"), SHA256.HashData(File.ReadAllBytes(manifestPath)));

        MockCompiler fallback = fixture.Create();
        fallback.WarningsDetected = true;
        fallback.BeforeExecute = () => Directory.Delete(entry, recursive: true);
        Assert.True(fallback.Execute());
        Assert.Equal(1, fallback.Executions);
        Assert.False(fallback.WarningsDetected);
        Assert.Contains(fallback.Engine.Messages, message => message.Contains("restore failed", StringComparison.Ordinal));

        MockCompiler warm = fixture.Create();
        warm.WarningsDetected = true;
        Assert.True(warm.Execute());
        Assert.Equal(0, warm.Executions);
        Assert.False(warm.WarningsDetected);
        Assert.Empty(warm.Engine.Warnings);
        Assert.DoesNotContain(warm.Engine.Messages, message => message.Contains("warning:", StringComparison.Ordinal));
    }, truncated.ToString()).Dispose();

    [CachePlatformTheory]
    [InlineData("input")]
    [InlineData("reference")]
    [InlineData("profile")]
    [InlineData("tool")]
    [InlineData("jit")]
    [InlineData("explicit-jit")]
    [InlineData("support")]
    [InlineData("pdb")]
    public void FullContentInvalidation(string dependency) => RemoteExecutor.Invoke(value =>
    {
        using var fixture = new Fixture();
        MockCompiler initial = fixture.Create();
        if (value == "explicit-jit")
        {
            initial.Crossgen2Tool.SetMetadata("JitPath", fixture.Jit);
        }
        Assert.True(initial.Execute());
        string path = value switch
        {
            "input" => fixture.Input,
            "reference" => fixture.Reference,
            "profile" => fixture.Profile,
            "tool" => fixture.Tool,
            "jit" or "explicit-jit" => fixture.Jit,
            "support" => fixture.Support,
            "pdb" => Path.ChangeExtension(fixture.Input, ".pdb"),
            _ => throw new InvalidOperationException()
        };
        DateTime stamp = File.GetLastWriteTimeUtc(path);
        byte[] bytes = File.ReadAllBytes(path);
        Guid? mvid = value is "input" or "reference" ? ReadMvid(path) : null;
        // DOS-header padding is ignored by the PE reader and does not change MVID/version.
        bytes[value is "input" or "reference" ? 32 : bytes.Length - 1] ^= 1;
        File.WriteAllBytes(path, bytes);
        File.SetLastWriteTimeUtc(path, stamp);
        Assert.Equal(bytes.Length, new FileInfo(path).Length);
        Assert.Equal(stamp, File.GetLastWriteTimeUtc(path));
        if (mvid.HasValue)
        {
            Assert.Equal(mvid.Value, ReadMvid(path));
        }
        MockCompiler changed = fixture.Create();
        if (value == "explicit-jit")
        {
            changed.Crossgen2Tool.SetMetadata("JitPath", fixture.Jit);
        }
        Assert.True(changed.Execute());
        Assert.Equal(1, changed.Executions);
        MockCompiler warm = fixture.Create();
        if (value == "explicit-jit")
        {
            warm.Crossgen2Tool.SetMetadata("JitPath", fixture.Jit);
        }
        Assert.True(warm.Execute());
        Assert.Equal(0, warm.Executions);
    }, dependency).Dispose();

    [WindowsCacheTheory]
    [InlineData("bundle")]
    [InlineData("external-runtime")]
    public void ManagedSingleFileUsesExperimentalToolchainIdentity(string change) => RemoteExecutor.Invoke(value =>
    {
        using var fixture = new Fixture();
        string testDirectory = Path.GetDirectoryName(typeof(Crossgen2CacheTests).Assembly.Location)!;
        string source = Path.Combine(fixture.Root, "bundle-source");
        Directory.CreateDirectory(source);
        string host = Path.Combine(source, "crossgen2.exe");
        string assembly = Path.Combine(source, "testhost.dll");
        HostWriter.CreateAppHost(Path.Combine(testDirectory, "bundle-apphost.exe"), host, "testhost.dll");
        File.Copy(Path.Combine(testDirectory, "testhost.dll"), assembly);
        string runtime = Path.Combine(fixture.Root, "external-runtime", "coreclr.dll");
        Directory.CreateDirectory(Path.GetDirectoryName(runtime)!);
        File.WriteAllText(runtime, "runtime-one");

        void Bundle()
        {
            var bundler = new Bundler("crossgen2.exe", fixture.Root, BundleOptions.None,
                OSPlatform.Windows, Architecture.X64, targetFrameworkVersion: typeof(object).Assembly.GetName().Version, appAssemblyName: "testhost");
            Assert.Equal(fixture.Tool, bundler.GenerateBundle(new[]
            {
                new FileSpec(host, "crossgen2.exe"),
                new FileSpec(assembly, "testhost.dll"),
                new FileSpec(Path.ChangeExtension(typeof(Crossgen2CacheTests).Assembly.Location, ".runtimeconfig.json"), "testhost.runtimeconfig.json")
            }));
            Assert.True(Bundler.IsBundle(fixture.Tool, out long offset));
            Assert.True(offset > 0);
        }

        Bundle();
        MockCompiler miss = fixture.Create();
        Assert.True(miss.Execute());
        Assert.Equal(1, miss.Executions);
        Assert.Contains(miss.Engine.Messages, m => m.Contains("cache stored:", StringComparison.Ordinal));
        if (value == "bundle")
        {
            byte[] bytes = File.ReadAllBytes(assembly);
            bytes[32] ^= 1;
            File.WriteAllBytes(assembly, bytes);
            Bundle();
        }
        else
        {
            File.WriteAllText(runtime, "runtime-two");
        }
        MockCompiler next = fixture.Create();
        Assert.True(next.Execute());
        Assert.Equal(value == "bundle" ? 1 : 0, next.Executions);
        MockCompiler warm = fixture.Create();
        Assert.True(warm.Execute());
        Assert.Equal(0, warm.Executions);
    }, change).Dispose();

    [CachePlatformTheory]
    [InlineData("timestamp")]
    [InlineData("pdb-add")]
    [InlineData("pdb-remove")]
    [InlineData("reference-order")]
    [InlineData("options")]
    [InlineData("stderr-policy")]
    [InlineData("output-importance")]
    [InlineData("explicit-jit")]
    public void IdentityChanges(string change) => RemoteExecutor.Invoke(value =>
    {
        using var fixture = new Fixture();
        string pdb = Path.ChangeExtension(fixture.Input, ".pdb");
        if (value == "pdb-add")
        {
            File.Delete(pdb);
        }
        Assert.True(fixture.Create().Execute());
        MockCompiler changed = fixture.Create();
        switch (value)
        {
            case "timestamp":
                File.SetLastWriteTimeUtc(fixture.Input, DateTime.UtcNow.AddHours(1));
                break;
            case "pdb-add":
                File.Copy(Path.ChangeExtension(typeof(Crossgen2CacheTests).Assembly.Location, ".pdb"), pdb);
                break;
            case "pdb-remove":
                File.Delete(pdb);
                break;
            case "reference-order":
                changed.ImplementationAssemblyReferences = changed.ImplementationAssemblyReferences.Reverse().ToArray();
                break;
            case "options":
                changed.ShowCompilerWarnings = true;
                break;
            case "stderr-policy":
                changed.LogStandardErrorAsError = false;
                break;
            case "output-importance":
                changed.StandardOutputImportance = "High";
                break;
            case "explicit-jit":
                changed.Crossgen2Tool.SetMetadata("JitPath", fixture.Jit);
                break;
        }
        Assert.True(changed.Execute());
        Assert.Equal(value == "timestamp" ? 0 : 1, changed.Executions);
    }, change).Dispose();

    [CachePlatformTheory]
    [InlineData("extra-output")]
    [InlineData("response-file")]
    [InlineData("composite-extra")]
    [InlineData("composite")]
    [InlineData("legacy")]
    [InlineData("managed")]
    [InlineData("apphost")]
    [InlineData("environment")]
    [InlineData("loader")]
    [InlineData("container")]
    [InlineData("symbols")]
    [InlineData("symbol-path")]
    [InlineData("arch")]
    [InlineData("non-native")]
    [InlineData("managed-assembly")]
    [InlineData("native-reference")]
    [InlineData("target-os")]
    [InlineData("unreadable-profile")]
    [InlineData("bad-input")]
    [InlineData("invalid-root")]
    [InlineData("relative-reference")]
    [InlineData("path-injection")]
    public void UnsupportedAndUnreadableInvocationsBypass(string shape) => RemoteExecutor.Invoke(value =>
    {
        using var fixture = new Fixture();
        Assert.True(fixture.Create().Execute());
        MockCompiler task = fixture.Create();
        switch (value)
        {
            case "extra-output": task.Crossgen2ExtraCommandLineArgs = "--out:\"elsewhere.dll\""; break;
            case "response-file": task.Crossgen2ExtraCommandLineArgs = "@hidden.rsp"; break;
            case "composite-extra": task.Crossgen2CompositeExtraCommandLineArgs = "--map"; break;
            case "composite":
                task.CompilationEntry.SetMetadata("CreateCompositeImage", "true");
                task.ReadyToRunCompositeBuildInput = new[] { new TaskItem(fixture.Input) };
                break;
            case "legacy":
                task.UseCrossgen2 = false;
                task.CrossgenTool = new TaskItem(fixture.Tool);
                task.CrossgenTool.SetMetadata("JitPath", fixture.Jit);
                break;
            case "managed": task.Crossgen2Tool.SetMetadata("DotNetHostPath", fixture.Tool); break;
            case "apphost": File.WriteAllText(Path.ChangeExtension(fixture.Tool, ".runtimeconfig.json"), "{}"); break;
            case "environment": task.EnvironmentVariables = new[] { "SOMETHING=1" }; break;
            case "loader": Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", fixture.Root); break;
            case "container": task.Crossgen2ContainerFormat = "wasm"; break;
            case "symbols":
                if (OperatingSystem.IsWindows())
                {
                    task.CompilationEntry.SetMetadata("OutputPDBImage", fixture.Map + ".wrong");
                }
                else
                {
                    task.Crossgen2Tool.SetMetadata("PerfmapFormatVersion", "0");
                }
                break;
            case "symbol-path": task.CompilationEntry.SetMetadata("OutputPDBImage", fixture.Map + ".wrong"); break;
            case "arch": task.Crossgen2Tool.SetMetadata("TargetArch", "arm64"); break;
            case "non-native": File.WriteAllText(fixture.Tool, "not a native compiler"); break;
            case "managed-assembly": File.Copy(fixture.Input, fixture.Tool, overwrite: true); break;
            case "native-reference": task.ImplementationAssemblyReferences = new[] { new TaskItem(fixture.Tool) }; break;
            case "target-os": task.Crossgen2Tool.SetMetadata("TargetOS", OperatingSystem.IsWindows() ? "linux" : "windows"); break;
            case "unreadable-profile":
                File.Delete(fixture.Profile);
                Directory.CreateDirectory(fixture.Profile);
                break;
            case "bad-input": File.WriteAllText(fixture.Input, "not a PE"); break;
            case "invalid-root": Environment.SetEnvironmentVariable("CROSSGEN2_EXPERIMENTAL_CACHE_PATH", fixture.Profile); break;
            case "relative-reference": task.ImplementationAssemblyReferences = new[] { new TaskItem("relative.dll") }; break;
            case "path-injection": task.ImplementationAssemblyReferences = new[] { new TaskItem(fixture.Reference + "\"\n--map") }; break;
        }
        Assert.True(task.Execute());
        Assert.Equal(1, task.Executions);
        string expectedDiagnostic = value switch
        {
            "invalid-root" => "Crossgen2 caching is disabled",
            "unreadable-profile" => "Crossgen2 cache bypassed:",
            _ => "Crossgen2 cache bypass:"
        };
        Assert.Contains(task.Engine.Messages, m => m.Contains(expectedDiagnostic, StringComparison.Ordinal));
    }, shape).Dispose();

    [CachePlatformTheory]
    [InlineData("outputs-in-cache")]
    [InlineData("cache-in-output")]
    public void OverlappingCacheAndOutputsFailBeforeExecution(string overlap) => RemoteExecutor.Invoke(value =>
    {
        using var fixture = new Fixture();
        Assert.True(fixture.Create().Execute());
        string entry = Assert.Single(Directory.GetDirectories(Path.Combine(fixture.Cache, InvocationCacheEntry.VersionDirectory)));
        string manifest = Path.Combine(entry, "manifest");
        byte[] cachedManifest = File.ReadAllBytes(manifest);
        byte[] image = File.ReadAllBytes(fixture.Output);
        byte[] symbols = File.ReadAllBytes(fixture.Map);
        string cacheDirectory = value == "outputs-in-cache"
            ? fixture.Root + Path.DirectorySeparatorChar : Path.Combine(fixture.Output, "cache");
        Environment.SetEnvironmentVariable("CROSSGEN2_EXPERIMENTAL_CACHE_PATH", cacheDirectory);
        MockCompiler task = fixture.Create();

        Assert.False(task.Execute());

        Assert.Equal(0, task.Executions);
        Assert.Equal(image, File.ReadAllBytes(fixture.Output));
        Assert.Equal(symbols, File.ReadAllBytes(fixture.Map));
        Assert.Equal(cachedManifest, File.ReadAllBytes(manifest));
        Assert.Contains(task.Engine.Errors, message =>
            message.Contains("Cache outputs must be distinct files outside the cache.", StringComparison.Ordinal));
    }, overlap).Dispose();

    [CachePlatformTheory]
    [InlineData("profile")]
    [InlineData("jit")]
    [InlineData("support")]
    public void MissingDeclaredInputsParticipateInSuccessfulInvocationIdentity(string input) => RemoteExecutor.Invoke(value =>
    {
        using var fixture = new Fixture();
        string path = value switch
        {
            "profile" => fixture.Profile,
            "jit" => fixture.Jit,
            "support" => fixture.Support,
            _ => throw new InvalidOperationException()
        };
        byte[] original = File.ReadAllBytes(path);
        File.Delete(path);
        MockCompiler miss = fixture.Create();
        Assert.True(miss.Execute());
        Assert.Equal(1, miss.Executions);
        MockCompiler hit = fixture.Create();
        Assert.True(hit.Execute());
        Assert.Equal(0, hit.Executions);
        File.WriteAllBytes(path, original);
        MockCompiler changed = fixture.Create();
        Assert.True(changed.Execute());
        Assert.Equal(1, changed.Executions);
    }, input).Dispose();

    [CachePlatformTheory]
    [InlineData("--embed-pgo-data")]
    [InlineData("--target-allows-runtime-code-generation:true")]
    [InlineData("--target-allows-runtime-code-generation:false")]
    [InlineData("--verify-type-and-field-layout")]
    [InlineData("--enable-cached-interface-dispatch-support")]
    [InlineData("--embed-pgo-data;--target-allows-runtime-code-generation:true")]
    public void AllowlistedExtraArgumentsAreCached(string args) => RemoteExecutor.Invoke(value =>
    {
        using var fixture = new Fixture();
        MockCompiler miss = fixture.Create();
        miss.Crossgen2ExtraCommandLineArgs = value;
        Assert.True(miss.Execute());
        Assert.Equal(1, miss.Executions);
        Assert.DoesNotContain(miss.Engine.Messages, m => m.Contains("cache bypass:", StringComparison.Ordinal));
        MockCompiler hit = fixture.Create();
        hit.Crossgen2ExtraCommandLineArgs = value;
        Assert.True(hit.Execute());
        Assert.Equal(0, hit.Executions);
    }, args).Dispose();

    [CachePlatformTheory]
    [InlineData("--target-allows-runtime-code-generation:maybe")]
    [InlineData("--embed-pgo-data;--out:\"elsewhere.dll\"")]
    [InlineData("--unknown-flag")]
    public void UnrecognizedExtraArgumentsStillBypass(string args) => RemoteExecutor.Invoke(value =>
    {
        using var fixture = new Fixture();
        MockCompiler task = fixture.Create();
        task.Crossgen2ExtraCommandLineArgs = value;
        Assert.True(task.Execute());
        Assert.Equal(1, task.Executions);
        Assert.Contains(task.Engine.Messages, m => m.Contains("cache bypass:", StringComparison.Ordinal));
    }, args).Dispose();

    [CachePlatformTheory]
    [InlineData(null)]
    [InlineData("pe")]
    public void DefaultOrExplicitPeContainerFormatIsCached(string format) => RemoteExecutor.Invoke(value =>
    {
        using var fixture = new Fixture();
        MockCompiler miss = fixture.Create();
        miss.Crossgen2ContainerFormat = value == "<null>" ? null : value;
        Assert.True(miss.Execute());
        Assert.Equal(1, miss.Executions);
        Assert.DoesNotContain(miss.Engine.Messages, m => m.Contains("cache bypass:", StringComparison.Ordinal));
        MockCompiler hit = fixture.Create();
        hit.Crossgen2ContainerFormat = value == "<null>" ? null : value;
        Assert.True(hit.Execute());
        Assert.Equal(0, hit.Executions);
    }, format ?? "<null>").Dispose();

    [CachePlatformTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailuresAreNotCached(bool logError) => RemoteExecutor.Invoke(value =>
    {
        using var fixture = new Fixture();
        MockCompiler failure = fixture.Create();
        failure.Result = bool.Parse(value) ? 0 : 7;
        failure.EmitError = bool.Parse(value);
        Assert.Equal(bool.Parse(value), failure.Execute());
        Assert.NotEmpty(failure.Engine.Errors);
        Assert.False(Directory.Exists(fixture.Cache));
        MockCompiler retry = fixture.Create();
        Assert.True(retry.Execute());
        Assert.Equal(1, retry.Executions);
    }, logError.ToString()).Dispose();

    [CachePlatformTheory]
    [InlineData("corruption")]
    [InlineData("manifest-corruption")]
    [InlineData("partial-restore")]
    [InlineData("optional-absent")]
    [InlineData("symbols-disabled")]
    public void RestorationHandlesMissingAndStaleOutputs(string scenario) => RemoteExecutor.Invoke(value =>
    {
        using var fixture = new Fixture();
        MockCompiler miss = fixture.Create();
        miss.ProduceMap = value != "optional-absent";
        Assert.True(miss.Execute());
        File.WriteAllText(fixture.Map, "stale map");
        if (value == "corruption")
        {
            string entry = Directory.GetDirectories(Path.Combine(fixture.Cache, InvocationCacheEntry.VersionDirectory)).Single();
            File.WriteAllText(Path.Combine(entry, "outputs", "0"), "corrupt");
        }
        if (value == "manifest-corruption")
        {
            string entry = Directory.GetDirectories(Path.Combine(fixture.Cache, InvocationCacheEntry.VersionDirectory)).Single();
            File.WriteAllText(Path.Combine(entry, "manifest"), "corrupt");
        }
        if (value == "partial-restore")
        {
            File.Delete(fixture.Map);
            Directory.CreateDirectory(fixture.Map);
        }
        MockCompiler next = fixture.Create();
        if (value == "symbols-disabled")
        {
            next.CompilationEntry.SetMetadata("EmitSymbols", "false");
        }
        if (value == "partial-restore")
        {
            Assert.False(next.Execute());
            Assert.NotEmpty(next.Engine.Errors);
            Assert.Equal(0, next.Executions);
            Assert.Contains(next.Engine.Messages, m => m.Contains("restore failed", StringComparison.Ordinal));
        }
        else
        {
            Assert.True(next.Execute());
            Assert.Equal(value is "corruption" or "manifest-corruption" or "symbols-disabled" ? 1 : 0, next.Executions);
            Assert.Equal("image", File.ReadAllText(fixture.Output));
            if (value is "optional-absent" or "symbols-disabled")
            {
                Assert.False(File.Exists(fixture.Map));
                MockCompiler warm = fixture.Create();
                warm.CompilationEntry.SetMetadata("EmitSymbols", value == "symbols-disabled" ? "false" : "true");
                Assert.True(warm.Execute());
                Assert.Equal(0, warm.Executions);
                Assert.False(File.Exists(fixture.Map));
            }
        }
    }, scenario).Dispose();

    [CachePlatformTheory]
    [InlineData("present")]
    [InlineData("absent")]
    [InlineData("corruption")]
    [InlineData("disabled")]
    [InlineData("directory-change")]
    public void SeparateSymbolDirectoryIsCached(string scenario) => RemoteExecutor.Invoke(value =>
    {
        using var fixture = new Fixture();
        string directory = Path.Combine(fixture.Root, "PDB");
        Directory.CreateDirectory(directory);
        string symbols = Path.Combine(directory, Path.GetFileName(fixture.Map));
        string neighbor = Path.Combine(directory, "unrelated.txt");
        File.WriteAllText(neighbor, "do not delete");
        Directory.CreateDirectory(Path.GetDirectoryName(fixture.Map)!);
        File.WriteAllText(fixture.Map, "unrelated adjacent symbols");
        File.WriteAllText(symbols, "stale symbols");

        MockCompiler Create()
        {
            MockCompiler task = fixture.Create();
            task.CompilationEntry.SetMetadata("OutputPDBImage", symbols);
            task.ProduceMap = value != "absent";
            return task;
        }

        MockCompiler miss = Create();
        Assert.True(miss.Execute());
        Assert.Equal(1, miss.Executions);
        Assert.Contains(miss.Engine.Messages, m => m.Contains("cache stored:", StringComparison.Ordinal));
        Assert.Equal(value != "absent", File.Exists(symbols));
        File.Delete(fixture.Output);
        File.WriteAllText(symbols, "stale symbols");
        if (value == "corruption")
        {
            string entry = Directory.GetDirectories(Path.Combine(fixture.Cache, InvocationCacheEntry.VersionDirectory)).Single();
            File.WriteAllText(Path.Combine(entry, "outputs", "1"), "corrupt");
        }
        if (value == "directory-change")
        {
            directory = Path.Combine(fixture.Root, "other PDB");
            Directory.CreateDirectory(directory);
            symbols = Path.Combine(directory, Path.GetFileName(fixture.Map));
        }
        MockCompiler next = Create();
        if (value == "disabled")
        {
            next.CompilationEntry.SetMetadata("EmitSymbols", "false");
        }
        Assert.True(next.Execute());
        Assert.Equal(value is "corruption" or "disabled" or "directory-change" ? 1 : 0, next.Executions);
        Assert.Equal("image", File.ReadAllText(fixture.Output));
        if (value is "absent" or "disabled")
        {
            Assert.False(File.Exists(symbols));
        }
        else
        {
            Assert.Equal("map", File.ReadAllText(symbols));
        }
        MockCompiler warm = Create();
        warm.CompilationEntry.SetMetadata("EmitSymbols", value == "disabled" ? "false" : "true");
        Assert.True(warm.Execute());
        Assert.Equal(value == "corruption" ? 1 : 0, warm.Executions);
        Assert.Equal("do not delete", File.ReadAllText(neighbor));
        Assert.Equal("unrelated adjacent symbols", File.ReadAllText(fixture.Map));
    }, scenario).Dispose();

    [CachePlatformFact]
    public void SharedCacheSupportsDistinctOutputsAndRacingPublication() => RemoteExecutor.Invoke(() =>
    {
        using var fixture = new Fixture();
        Parallel.For(0, 8, i =>
        {
            MockCompiler task = fixture.Create(i + ".dll");
            Assert.True(task.Execute());
            Assert.Equal(1, task.Executions);
        });
        Parallel.For(0, 8, i =>
        {
            MockCompiler task = fixture.Create(i + ".dll");
            Assert.True(task.Execute());
            Assert.Equal(0, task.Executions);
        });
        MockCompiler owner = fixture.Create();
        InvocationCache cache = fixture.CreateCache(owner.Log);
        File.WriteAllText(fixture.Output, "image");
        string key = new string('a', 64);
        InvocationOutputs outputs = InvocationOutputs.FromFiles(fixture.Output, fixture.Map);
        Parallel.For(0, 8, _ => cache.Store(key, outputs,
            writer => writer.Write(true)));
        bool warningsDetected = false;
        Assert.True(cache.TryRestore(key, outputs, DateTime.UtcNow,
            reader => warningsDetected = reader.ReadBoolean()));
        Assert.True(warningsDetected);
        Assert.Empty(Directory.GetDirectories(Path.Combine(fixture.Cache, InvocationCacheEntry.VersionDirectory), "*.tmp"));
    }).Dispose();

    [CachePlatformFact]
    public void DefaultCacheLocationMatchesSharedPolicy() => RemoteExecutor.Invoke(() =>
    {
        using var fixture = new Fixture();
        Environment.SetEnvironmentVariable("CROSSGEN2_EXPERIMENTAL_CACHE_PATH", null);
        Environment.SetEnvironmentVariable("XDG_CACHE_HOME", fixture.Cache);
        MockCompiler task = fixture.Create();
        string expected = OperatingSystem.IsWindows()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "crossgen2")
            : Path.Combine(fixture.Cache, "crossgen2");
        Assert.Equal(expected, fixture.CreateCache(task.Log).CacheDirectory);
    }).Dispose();

    [WindowsCacheTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void SymbolWriterIdentityDependsOnSymbolGeneration(bool symbols) => RemoteExecutor.Invoke(value =>
    {
        using var fixture = new Fixture();
        MockCompiler first = fixture.Create();
        first.CompilationEntry.SetMetadata("EmitSymbols", value);
        Assert.True(first.Execute());
        Assert.Contains(first.Engine.Messages, m => m.Contains("cache stored:", StringComparison.Ordinal));
        File.WriteAllText(fixture.SymbolWriter, "changed symbol writer");
        MockCompiler next = fixture.Create();
        next.CompilationEntry.SetMetadata("EmitSymbols", value);
        Assert.True(next.Execute());
        Assert.Equal(bool.Parse(value) ? 1 : 0, next.Executions);
        File.Delete(fixture.SymbolWriter);
        MockCompiler missing = fixture.Create();
        missing.CompilationEntry.SetMetadata("EmitSymbols", value);
        Assert.True(missing.Execute());
        Assert.Equal(bool.Parse(value) ? 1 : 0, missing.Executions);
        if (bool.Parse(value))
        {
            Assert.Contains(missing.Engine.Messages, m => m.Contains("cache stored:", StringComparison.Ordinal));
            MockCompiler hit = fixture.Create();
            hit.CompilationEntry.SetMetadata("EmitSymbols", value);
            Assert.True(hit.Execute());
            Assert.Equal(0, hit.Executions);
        }
    }, symbols.ToString()).Dispose();

    [WindowsCacheTheory]
    [InlineData("input.dll", ".ni.pdb")]
    [InlineData("input.exe", ".ni.pdb")]
    [InlineData("input.ni.dll", ".pdb")]
    [InlineData("input.NI.EXE", ".pdb")]
    public void NativePdbFilenameMatchesCompilerConvention(string name, string extension) => RemoteExecutor.Invoke((file, suffix) =>
    {
        using var fixture = new Fixture();
        MockCompiler first = fixture.Create(file);
        string image = first.CompilationEntry.GetMetadata("OutputR2RImage");
        string pdb = Path.ChangeExtension(image, suffix);
        first.CompilationEntry.SetMetadata("OutputPDBImage", pdb);
        Assert.True(first.Execute());
        Assert.Contains(first.Engine.Messages, m => m.Contains("cache stored:", StringComparison.Ordinal));
        File.Delete(image);
        File.WriteAllText(pdb, "stale");
        MockCompiler hit = fixture.Create(file);
        hit.CompilationEntry.SetMetadata("OutputPDBImage", pdb);
        Assert.True(hit.Execute());
        Assert.Equal(0, hit.Executions);
        Assert.Equal("map", File.ReadAllText(pdb));
    }, name, extension).Dispose();

    [WindowsCacheTheory]
    [InlineData("drive-relative")]
    [InlineData("root-relative")]
    [InlineData("alternate-stream")]
    [InlineData("trailing-dot")]
    [InlineData("trailing-space")]
    [InlineData("device")]
    public void WindowsAmbiguousPathsAreRejected(string shape)
    {
        string root = Path.GetPathRoot(Path.GetTempPath())!;
        string path = shape switch
        {
            "drive-relative" => root.TrimEnd('\\') + "input.dll",
            "root-relative" => @"\input.dll",
            "alternate-stream" => Path.Combine(root, "input.dll:stream"),
            "trailing-dot" => Path.Combine(root, "out.", "input.dll"),
            "trailing-space" => Path.Combine(root, "out ", "input.dll"),
            "device" => @"\\?\" + Path.Combine(root, "input.dll"),
            _ => throw new InvalidOperationException()
        };
        Assert.Throws<InvalidDataException>(() => Crossgen2Cache.InputPath(path));
    }

    [WindowsCacheTheory]
    [InlineData("cache")]
    [InlineData("input")]
    public void WindowsCaseAliasesAreRejected(string location) => RemoteExecutor.Invoke(value =>
    {
        using var fixture = new Fixture();
        MockCompiler task = fixture.Create();
        InvocationCache cache = fixture.CreateCache(task.Log);
        string image = value == "cache" ? Path.Combine(fixture.Cache.ToUpperInvariant(), "output.dll") : fixture.Input.ToUpperInvariant();
        string[] paths = new[] { Crossgen2Cache.InputPath(image), fixture.Map };
        if (value == "cache")
        {
            Assert.Throws<ArgumentException>(() => cache.ValidateOutputs(InvocationOutputs.FromFiles(paths)));
        }
        else
        {
            Assert.Throws<InvalidDataException>(() => Crossgen2Cache.DescribeInvocation(new[] { fixture.Tool, fixture.Jit, fixture.Support },
                fixture.Input, task.ImplementationAssemblyReferences, task.Crossgen2PgoFiles,
                paths, "", "", "", fixture.Root));
        }
    }, location).Dispose();

    [WindowsCacheTheory]
    [InlineData("non-portable")]
    [InlineData("mismatched")]
    [InlineData("drive-relative")]
    [InlineData("absent")]
    public void ExternalInputSymbolReadersBypass(string shape) => RemoteExecutor.Invoke(value =>
    {
        using var fixture = new Fixture();
        string pdb = Path.ChangeExtension(fixture.Input, ".pdb");
        if (value == "non-portable")
        {
            File.WriteAllText(pdb, "not a portable PDB");
        }
        else if (value is "mismatched" or "drive-relative")
        {
            using FileStream stream = File.Open(fixture.Input, FileMode.Open, FileAccess.ReadWrite);
            using var pe = new PEReader(stream, PEStreamOptions.LeaveOpen);
            DebugDirectoryEntry entry = pe.ReadDebugDirectory().Single(e => e.Type == DebugDirectoryEntryType.CodeView);
            if (value == "mismatched")
            {
                stream.Position = entry.DataPointer + 4;
                int first = stream.ReadByte();
                stream.Position--;
                stream.WriteByte((byte)(first ^ 1));
            }
            else
            {
                stream.Position = entry.DataPointer + 24;
                stream.Write(System.Text.Encoding.UTF8.GetBytes(Path.GetPathRoot(fixture.Root)!.TrimEnd('\\') + "input.pdb\0"));
            }
        }
        else
        {
            File.Delete(pdb);
        }
        MockCompiler task = fixture.Create();
        Assert.True(task.Execute());
        Assert.Equal(1, task.Executions);
        if (value == "absent")
        {
            Assert.Contains(task.Engine.Messages, m => m.Contains("cache stored:", StringComparison.Ordinal));
            MockCompiler hit = fixture.Create();
            Assert.True(hit.Execute());
            Assert.Equal(0, hit.Executions);
        }
        else
        {
            Assert.Contains(task.Engine.Messages, m => m.Contains("cache bypass:", StringComparison.Ordinal));
            Assert.False(Directory.Exists(fixture.Cache));
        }
    }, shape).Dispose();

    [RealCrossgen2Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void RealCompilerMissAndHitMatchUncachedExecution(bool symbols, bool separateDirectory) => RemoteExecutor.Invoke((value, separate) =>
    {
        using var fixture = new Fixture();
        string tool = Environment.GetEnvironmentVariable("CROSSGEN2_CACHE_TEST_TOOL")!;
        string runtime = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        string output = Path.Combine(fixture.Root, "out", "System.Linq.dll");
        string symbolsPath = Path.ChangeExtension(output, Fixture.SymbolExtension);
        if (bool.Parse(separate))
        {
            string directory = Path.Combine(fixture.Root, "PDB");
            Directory.CreateDirectory(directory);
            symbolsPath = Path.Combine(directory, Path.GetFileName(symbolsPath));
        }
        MockCompiler Create()
        {
            MockCompiler task = fixture.Create("System.Linq.dll");
            task.RealTool = true;
            task.Crossgen2Tool.ItemSpec = tool;
            task.CompilationEntry.ItemSpec = Path.Combine(runtime, "System.Linq.dll");
            task.CompilationEntry.SetMetadata("EmitSymbols", value);
            task.CompilationEntry.SetMetadata("OutputPDBImage", symbolsPath);
            task.ImplementationAssemblyReferences = Directory.GetFiles(runtime, "*.dll").Where(HasMetadata).Select(p => new TaskItem(p)).ToArray();
            task.Crossgen2PgoFiles = Array.Empty<ITaskItem>();
            return task;
        }

        Environment.SetEnvironmentVariable("CROSSGEN2_EXPERIMENTAL_CACHE", "false");
        MockCompiler uncached = Create();
        Assert.True(uncached.Execute(), string.Join(Environment.NewLine, uncached.Engine.Messages));
        Assert.Equal(1, uncached.Executions);
        byte[] image = File.ReadAllBytes(output);
        byte[]? map = bool.Parse(value) ? File.ReadAllBytes(symbolsPath) : null;
        Environment.SetEnvironmentVariable("CROSSGEN2_EXPERIMENTAL_CACHE", "true");
        MockCompiler miss = Create();
        Assert.True(miss.Execute(), string.Join(Environment.NewLine, miss.Engine.Messages));
        Assert.Equal(1, miss.Executions);
        Assert.Contains(miss.Engine.Messages, m => m.Contains("cache stored:", StringComparison.Ordinal));
        byte[] cachedImage = File.ReadAllBytes(output);
        if (OperatingSystem.IsWindows() && bool.Parse(value))
        {
            Assert.Equal(NormalizeNativePdbIdentity(image), NormalizeNativePdbIdentity(cachedImage));
        }
        else
        {
            Assert.Equal(image, cachedImage);
            if (map is not null)
            {
                Assert.Equal(map, File.ReadAllBytes(symbolsPath));
            }
        }
        byte[]? cachedSymbols = bool.Parse(value) ? File.ReadAllBytes(symbolsPath) : null;
        File.Delete(output);
        File.Delete(symbolsPath);
        MockCompiler hit = Create();
        Assert.True(hit.Execute(), string.Join(Environment.NewLine, hit.Engine.Messages));
        Assert.Equal(0, hit.Executions);
        Assert.Equal(cachedImage, File.ReadAllBytes(output));
        if (cachedSymbols is not null)
        {
            Assert.Equal(cachedSymbols, File.ReadAllBytes(symbolsPath));
        }
    }, symbols.ToString(), separateDirectory.ToString()).Dispose();

    private static byte[] NormalizeNativePdbIdentity(byte[] image)
    {
        const int DebugDirectoryEntrySize = 28;
        const int GuidSize = 16;
        byte[] normalized = (byte[])image.Clone();
        using var pe = new PEReader(new MemoryStream(image));
        Assert.True(pe.PEHeaders.TryGetDirectoryOffset(pe.PEHeaders.PEHeader!.DebugTableDirectory, out int directory));
        DebugDirectoryEntry[] entries = pe.ReadDebugDirectory().ToArray();
        int index = Array.FindIndex(entries, entry => entry.Type == DebugDirectoryEntryType.CodeView &&
            pe.ReadCodeViewDebugDirectoryData(entry).Path.EndsWith(".ni.pdb", StringComparison.OrdinalIgnoreCase));
        Assert.True(index >= 0);
        // The native PDB writer generates a fresh timestamp/GUID and patches them into the image.
        Array.Clear(normalized, directory + index * DebugDirectoryEntrySize + sizeof(uint), sizeof(uint));
        Array.Clear(normalized, entries[index].DataPointer + sizeof(uint), GuidSize);
        return normalized;
    }

    private static Guid ReadMvid(string path)
    {
        using FileStream stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        MetadataReader reader = pe.GetMetadataReader();
        return reader.GetGuid(reader.GetModuleDefinition().Mvid);
    }

    private static bool HasMetadata(string path)
    {
        using FileStream stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        return pe.HasMetadata;
    }

    private sealed class Fixture : IDisposable
    {
        internal string Root { get; } = Path.Combine(Path.GetTempPath(), "crossgen-cache-tests-" + Guid.NewGuid().ToString("N"));
        internal string Cache => Path.Combine(Root, "cache");
        internal string Input => Path.Combine(Root, "input.dll");
        internal string Reference => Path.Combine(Root, "reference.dll");
        internal string Profile => Path.Combine(Root, "profile.mibc");
        internal string Tool => Path.Combine(Root, OperatingSystem.IsWindows() ? "crossgen2.exe" : "crossgen2");
        internal string Jit => Path.Combine(Root, OperatingSystem.IsWindows() ? "clrjit_win_x64_x64.dll" : "libclrjit_unix_x64_x64.so");
        internal string Support => Path.Combine(Root, OperatingSystem.IsWindows() ? "jitinterface_x64.dll" : "libjitinterface_x64.so");
        internal string SymbolWriter => Path.Combine(Root, "Microsoft.DiaSymReader.Native.dll");
        internal string Output => Path.Combine(Root, "out", "input.dll");
        internal string Map => Path.ChangeExtension(Output, SymbolExtension);
        internal static string SymbolExtension => OperatingSystem.IsWindows() ? ".ni.pdb" : ".ni.r2rmap";

        internal Fixture()
        {
            Directory.CreateDirectory(Root);
            File.Copy(typeof(Crossgen2CacheTests).Assembly.Location, Input);
            File.Copy(Input, Reference);
            // Change the CodeView filename to match the isolated fixture, not the built test PDB.
            using (FileStream stream = File.Open(Input, FileMode.Open, FileAccess.ReadWrite))
            {
                using var pe = new PEReader(stream, PEStreamOptions.LeaveOpen);
                DebugDirectoryEntry entry = pe.ReadDebugDirectory().Single(e => e.Type == DebugDirectoryEntryType.CodeView);
                stream.Position = entry.DataPointer + 24;
                byte[] name = System.Text.Encoding.UTF8.GetBytes("input.pdb\0");
                stream.Write(name);
            }
            File.Copy(Path.ChangeExtension(typeof(Crossgen2CacheTests).Assembly.Location, ".pdb"), Path.ChangeExtension(Input, ".pdb"));
            if (OperatingSystem.IsWindows())
            {
                File.Copy(Environment.ProcessPath!, Tool);
            }
            else
            {
                File.WriteAllBytes(Tool, new byte[] { 0x7f, (byte)'E', (byte)'L', (byte)'F', 0 });
            }
            File.WriteAllText(Jit, "jit");
            File.WriteAllText(Support, "support");
            File.WriteAllText(SymbolWriter, "symbol writer");
            File.WriteAllText(Profile, "profile");
            Environment.SetEnvironmentVariable("CROSSGEN2_EXPERIMENTAL_CACHE", "true");
            Environment.SetEnvironmentVariable("CROSSGEN2_EXPERIMENTAL_CACHE_PATH", Cache);
        }

        internal MockCompiler Create(string name = "input.dll")
        {
            var tool = new TaskItem(Tool);
            tool.SetMetadata("TargetOS", OperatingSystem.IsWindows() ? "windows" : "linux");
            tool.SetMetadata("TargetArch", "x64");
            tool.SetMetadata("PerfmapFormatVersion", "1");
            var compilation = new TaskItem(Input);
            string output = Path.Combine(Root, "out", name);
            compilation.SetMetadata("OutputR2RImage", output);
            compilation.SetMetadata("OutputPDBImage", Path.ChangeExtension(output, SymbolExtension));
            compilation.SetMetadata("EmitSymbols", "true");
            var task = new MockCompiler
            {
                Crossgen2Tool = tool,
                UseCrossgen2 = true,
                CompilationEntry = compilation,
                Crossgen2ExtraCommandLineArgs = ";",
                ImplementationAssemblyReferences = new[] { new TaskItem(Input), new TaskItem(Reference) },
                Crossgen2PgoFiles = new[] { new TaskItem(Profile) }
            };
            task.BuildEngine = task.Engine;
            return task;
        }

        internal InvocationCache CreateCache(TaskLoggingHelper log)
            => InvocationCache.TryCreateFromEnvironment(log, new InvocationCacheConfiguration("Crossgen2",
                "CROSSGEN2_EXPERIMENTAL_CACHE", "CROSSGEN2_EXPERIMENTAL_CACHE_PATH"))!;

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }

    private sealed class MockCompiler : RunReadyToRunCompiler
    {
        internal Engine Engine { get; } = new();
        internal int Executions { get; private set; }
        internal int Result { get; set; }
        internal bool EmitWarning { get; set; }
        internal bool EmitError { get; set; }
        internal bool ProduceMap { get; set; } = true;
        internal bool RealTool { get; set; }
        internal Action? BeforeExecute { get; set; }

        internal override int ExecuteCompiler(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            Executions++;
            BeforeExecute?.Invoke();
            if (RealTool)
            {
                return base.ExecuteCompiler(pathToTool, responseFileCommands, commandLineCommands);
            }
            File.WriteAllText(CompilationEntry.GetMetadata("OutputR2RImage"), "image");
            if (ProduceMap && CompilationEntry.GetMetadata("EmitSymbols") == "true")
            {
                File.WriteAllText(CompilationEntry.GetMetadata("OutputPDBImage"), "map");
            }
            if (EmitWarning)
            {
                LogEventsFromTextOutput("warning: example compiler warning", MessageImportance.Normal);
            }
            if (EmitError)
            {
                Log.LogError("example compiler error");
            }
            return Result;
        }
    }

    private sealed class Engine : IBuildEngine
    {
        internal List<string> Messages { get; } = new();
        internal List<string> Warnings { get; } = new();
        internal List<string> Errors { get; } = new();
        public bool ContinueOnError => false;
        public int LineNumberOfTaskNode => 0;
        public int ColumnNumberOfTaskNode => 0;
        public string ProjectFileOfTaskNode => "";
        public void LogErrorEvent(BuildErrorEventArgs e)
        {
            Messages.Add(e.Message!);
            Errors.Add(e.Message!);
        }
        public void LogWarningEvent(BuildWarningEventArgs e) => Warnings.Add(e.Message!);
        public void LogMessageEvent(BuildMessageEventArgs e) => Messages.Add(e.Message!);
        public void LogCustomEvent(CustomBuildEventArgs e) { }
        public bool BuildProjectFile(string file, string[] targets, IDictionary properties, IDictionary outputs) => throw new NotSupportedException();
    }
}

public sealed class CachePlatformFactAttribute : FactAttribute
{
    public CachePlatformFactAttribute()
    {
        if ((!OperatingSystem.IsLinux() && !OperatingSystem.IsWindows()) || RuntimeInformation.ProcessArchitecture != Architecture.X64)
        {
            Skip = "The experimental cache supports Linux x64 and Windows x64 only.";
        }
    }
}

public class CachePlatformTheoryAttribute : TheoryAttribute
{
    public CachePlatformTheoryAttribute()
    {
        if ((!OperatingSystem.IsLinux() && !OperatingSystem.IsWindows()) || RuntimeInformation.ProcessArchitecture != Architecture.X64)
        {
            Skip = "The experimental cache supports Linux x64 and Windows x64 only.";
        }
    }
}

public sealed class RealCrossgen2TheoryAttribute : CachePlatformTheoryAttribute
{
    public RealCrossgen2TheoryAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CROSSGEN2_CACHE_TEST_TOOL")))
        {
            Skip = "Set CROSSGEN2_CACHE_TEST_TOOL to a directly executed crossgen2 deployment for this host.";
        }
    }
}

public sealed class WindowsCacheTheoryAttribute : CachePlatformTheoryAttribute
{
    public WindowsCacheTheoryAttribute()
    {
        if (!OperatingSystem.IsWindows())
        {
            Skip = "This test exercises Windows deployment or path conventions.";
        }
    }
}
