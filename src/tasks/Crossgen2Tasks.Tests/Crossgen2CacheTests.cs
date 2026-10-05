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
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.NET.Build.Tasks;
using Xunit;

namespace Crossgen2Tasks.Tests;

public class Crossgen2CacheTests
{
    [LinuxX64Theory]
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
            Assert.Contains(second.Engine.Messages, m => m.Contains("must be true or false", StringComparison.Ordinal));
        }
    }, setting).Dispose();

    [LinuxX64Theory]
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

    [LinuxX64Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RoundTripPreservesOutputsAndDiagnostics(bool showWarnings) => RemoteExecutor.Invoke(value =>
    {
        using var fixture = new Fixture();
        string neighbor = Path.Combine(fixture.Root, "out", "unrelated.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(neighbor)!);
        File.WriteAllText(neighbor, "do not delete");
        MockCompiler miss = fixture.Create();
        miss.ShowCompilerWarnings = bool.Parse(value);
        miss.EmitWarning = true;
        Assert.True(miss.Execute());
        byte[] image = File.ReadAllBytes(fixture.Output);
        byte[] map = File.ReadAllBytes(fixture.Map);
        File.Delete(fixture.Output);
        File.WriteAllText(fixture.Map, "stale");
        DateTime before = DateTime.UtcNow;
        MockCompiler hit = fixture.Create();
        hit.ShowCompilerWarnings = miss.ShowCompilerWarnings;
        Assert.True(hit.Execute());
        Assert.Equal(0, hit.Executions);
        Assert.Equal(0, hit.ExitCode);
        Assert.Equal(image, File.ReadAllBytes(fixture.Output));
        Assert.Equal(map, File.ReadAllBytes(fixture.Map));
        Assert.Equal(miss.WarningsDetected, hit.WarningsDetected);
        Assert.Equal(miss.Engine.Warnings, hit.Engine.Warnings);
        Assert.Equal(miss.Engine.Messages.Where(m => m.Contains("warning:", StringComparison.Ordinal)),
            hit.Engine.Messages.Where(m => m.Contains("warning:", StringComparison.Ordinal)));
        Assert.InRange(File.GetLastWriteTimeUtc(fixture.Output), before.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
        Assert.Equal("do not delete", File.ReadAllText(neighbor));
    }, showWarnings.ToString()).Dispose();

    [LinuxX64Theory]
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

    [LinuxX64Theory]
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
                File.WriteAllText(pdb, "new symbols");
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

    [LinuxX64Theory]
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
    [InlineData("missing-profile")]
    [InlineData("missing-jit")]
    [InlineData("unreadable-profile")]
    [InlineData("bad-input")]
    [InlineData("invalid-root")]
    [InlineData("outputs-in-cache")]
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
            case "symbols": task.Crossgen2Tool.SetMetadata("PerfmapFormatVersion", "0"); break;
            case "symbol-path": task.CompilationEntry.SetMetadata("OutputPDBImage", fixture.Map + ".wrong"); break;
            case "arch": task.Crossgen2Tool.SetMetadata("TargetArch", "arm64"); break;
            case "missing-profile": File.Delete(fixture.Profile); break;
            case "missing-jit": File.Delete(fixture.Jit); break;
            case "unreadable-profile": File.SetUnixFileMode(fixture.Profile, UnixFileMode.None); break;
            case "bad-input": File.WriteAllText(fixture.Input, "not a PE"); break;
            case "invalid-root": Environment.SetEnvironmentVariable("CROSSGEN2_EXPERIMENTAL_CACHE_PATH", fixture.Profile); break;
            case "outputs-in-cache": Environment.SetEnvironmentVariable("CROSSGEN2_EXPERIMENTAL_CACHE_PATH", fixture.Root + Path.DirectorySeparatorChar); break;
            case "relative-reference": task.ImplementationAssemblyReferences = new[] { new TaskItem("relative.dll") }; break;
            case "path-injection": task.ImplementationAssemblyReferences = new[] { new TaskItem(fixture.Reference + "\"\n--map") }; break;
        }
        Assert.True(task.Execute());
        Assert.Equal(1, task.Executions);
        Assert.Contains(task.Engine.Messages, m => m.Contains("cache bypass:", StringComparison.Ordinal));
    }, shape).Dispose();

    [LinuxX64Theory]
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

    [LinuxX64Theory]
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
            string entry = Directory.GetDirectories(Path.Combine(fixture.Cache, "v1")).Single();
            File.WriteAllText(Path.Combine(entry, "0"), "corrupt");
        }
        if (value == "manifest-corruption")
        {
            string entry = Directory.GetDirectories(Path.Combine(fixture.Cache, "v1")).Single();
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

    [LinuxX64Fact]
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
        Crossgen2Cache cache = Crossgen2Cache.FromEnvironment(owner.Log)!;
        File.WriteAllText(fixture.Output, "image");
        Parallel.For(0, 8, _ => cache.Store("publication-test", new[] { fixture.Output, fixture.Map }, new()));
        Assert.True(cache.TryRestore("publication-test", new[] { fixture.Output, fixture.Map }, DateTime.UtcNow, out _));
        Assert.Empty(Directory.GetDirectories(Path.Combine(fixture.Cache, "v1"), "*.tmp"));
    }).Dispose();

    [LinuxX64Fact]
    public void DefaultCacheLocation() => RemoteExecutor.Invoke(() =>
    {
        using var fixture = new Fixture();
        Environment.SetEnvironmentVariable("CROSSGEN2_EXPERIMENTAL_CACHE_PATH", null);
        Environment.SetEnvironmentVariable("XDG_CACHE_HOME", fixture.Cache);
        Assert.True(fixture.Create().Execute());
        Assert.True(Directory.Exists(Path.Combine(fixture.Cache, "crossgen2", "v1")));
    }).Dispose();

    [RealCrossgen2Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RealCompilerMissAndHitMatchUncachedExecution(bool symbols) => RemoteExecutor.Invoke(value =>
    {
        using var fixture = new Fixture();
        string tool = Environment.GetEnvironmentVariable("CROSSGEN2_CACHE_TEST_TOOL")!;
        string runtime = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        MockCompiler Create()
        {
            MockCompiler task = fixture.Create();
            task.RealTool = true;
            task.Crossgen2Tool.ItemSpec = tool;
            task.CompilationEntry.ItemSpec = Path.Combine(runtime, "System.Linq.dll");
            task.CompilationEntry.SetMetadata("EmitSymbols", value);
            task.ImplementationAssemblyReferences = Directory.GetFiles(runtime, "*.dll").Select(p => new TaskItem(p)).ToArray();
            task.Crossgen2PgoFiles = Array.Empty<ITaskItem>();
            return task;
        }

        Environment.SetEnvironmentVariable("CROSSGEN2_EXPERIMENTAL_CACHE", "false");
        MockCompiler uncached = Create();
        Assert.True(uncached.Execute(), string.Join(Environment.NewLine, uncached.Engine.Messages));
        Assert.Equal(1, uncached.Executions);
        byte[] image = File.ReadAllBytes(fixture.Output);
        byte[]? map = bool.Parse(value) ? File.ReadAllBytes(fixture.Map) : null;
        Environment.SetEnvironmentVariable("CROSSGEN2_EXPERIMENTAL_CACHE", "true");
        MockCompiler miss = Create();
        Assert.True(miss.Execute(), string.Join(Environment.NewLine, miss.Engine.Messages));
        Assert.Equal(1, miss.Executions);
        Assert.Contains(miss.Engine.Messages, m => m.Contains("cache stored:", StringComparison.Ordinal));
        Assert.Equal(image, File.ReadAllBytes(fixture.Output));
        File.Delete(fixture.Output);
        File.Delete(fixture.Map);
        MockCompiler hit = Create();
        Assert.True(hit.Execute(), string.Join(Environment.NewLine, hit.Engine.Messages));
        Assert.Equal(0, hit.Executions);
        Assert.Equal(image, File.ReadAllBytes(fixture.Output));
        if (map is not null)
        {
            Assert.Equal(map, File.ReadAllBytes(fixture.Map));
        }
    }, symbols.ToString()).Dispose();

    private static Guid ReadMvid(string path)
    {
        using FileStream stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        MetadataReader reader = pe.GetMetadataReader();
        return reader.GetGuid(reader.GetModuleDefinition().Mvid);
    }

    private sealed class Fixture : IDisposable
    {
        internal string Root { get; } = Path.Combine(Path.GetTempPath(), "crossgen-cache-tests-" + Guid.NewGuid().ToString("N"));
        internal string Cache => Path.Combine(Root, "cache");
        internal string Input => Path.Combine(Root, "input.dll");
        internal string Reference => Path.Combine(Root, "reference.dll");
        internal string Profile => Path.Combine(Root, "profile.mibc");
        internal string Tool => Path.Combine(Root, "crossgen2");
        internal string Jit => Path.Combine(Root, "libclrjit_unix_x64_x64.so");
        internal string Support => Path.Combine(Root, "libjitinterface_x64.so");
        internal string Output => Path.Combine(Root, "out", "input.dll");
        internal string Map => Path.ChangeExtension(Output, ".ni.r2rmap");

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
            File.WriteAllText(Path.ChangeExtension(Input, ".pdb"), "symbols");
            File.WriteAllBytes(Tool, new byte[] { 0x7f, (byte)'E', (byte)'L', (byte)'F', 0 });
            File.WriteAllText(Jit, "jit");
            File.WriteAllText(Support, "support");
            File.WriteAllText(Profile, "profile");
            Environment.SetEnvironmentVariable("CROSSGEN2_EXPERIMENTAL_CACHE", "true");
            Environment.SetEnvironmentVariable("CROSSGEN2_EXPERIMENTAL_CACHE_PATH", Cache);
        }

        internal MockCompiler Create(string name = "input.dll")
        {
            var tool = new TaskItem(Tool);
            tool.SetMetadata("TargetOS", "linux");
            tool.SetMetadata("TargetArch", "x64");
            tool.SetMetadata("PerfmapFormatVersion", "1");
            var compilation = new TaskItem(Input);
            string output = Path.Combine(Root, "out", name);
            compilation.SetMetadata("OutputR2RImage", output);
            compilation.SetMetadata("OutputPDBImage", Path.ChangeExtension(output, ".ni.r2rmap"));
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

        internal override int ExecuteCompiler(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            Executions++;
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

public sealed class LinuxX64FactAttribute : FactAttribute
{
    public LinuxX64FactAttribute()
    {
        if (!OperatingSystem.IsLinux() || RuntimeInformation.ProcessArchitecture != Architecture.X64)
        {
            Skip = "The experimental cache supports Linux x64 only.";
        }
    }
}

public class LinuxX64TheoryAttribute : TheoryAttribute
{
    public LinuxX64TheoryAttribute()
    {
        if (!OperatingSystem.IsLinux() || RuntimeInformation.ProcessArchitecture != Architecture.X64)
        {
            Skip = "The experimental cache supports Linux x64 only.";
        }
    }
}

public sealed class RealCrossgen2TheoryAttribute : LinuxX64TheoryAttribute
{
    public RealCrossgen2TheoryAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CROSSGEN2_CACHE_TEST_TOOL")))
        {
            Skip = "Set CROSSGEN2_CACHE_TEST_TOOL to a native Linux-x64 crossgen2 deployment.";
        }
    }
}
