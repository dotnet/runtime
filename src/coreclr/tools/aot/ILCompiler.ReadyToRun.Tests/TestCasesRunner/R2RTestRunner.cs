// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ILCompiler.Reflection.ReadyToRun;
using Xunit;
using Xunit.Abstractions;

namespace ILCompiler.ReadyToRun.Tests.TestCasesRunner;

/// <summary>
/// Describes an assembly compiled by Roslyn as part of a test case.
/// </summary>
internal sealed class CompiledAssembly
{
    public required string AssemblyName { get; init; }

    /// <summary>
    /// The name of the string resources that contain the source code for this assembly.
    /// </summary>
    public required string[] SourceResourceNames { get; init; }

    /// <summary>
    /// Roslyn feature flags for this assembly (e.g. runtime-async=on).
    /// </summary>
    public List<KeyValuePair<string, string>> Features { get; init; } = new();

    /// <summary>
    /// References to other assemblies that this assembly depends on.
    /// </summary>
    public List<CompiledAssembly> References { get; init; } = new();

    private string? _outputDir = null;
    public string FilePath => _outputDir != null ? Path.Combine(_outputDir, "IL", AssemblyName + ".dll")
        : throw new InvalidOperationException("Output directory not set");

    public void SetOutputDir(string outputDir)
    {
        _outputDir = outputDir;
    }
}

/// <summary>
/// References a <see cref="CompiledAssembly"/> within a <see cref="CrossgenCompilation"/>,
/// specifying its role and per-assembly options.
/// </summary>
internal sealed class CrossgenAssembly(CompiledAssembly ilAssembly)
{
    public CompiledAssembly ILAssembly => ilAssembly;
    /// <summary>
    /// How this assembly is passed to crossgen2.
    /// Defaults to <see cref="Crossgen2InputKind.InputAssembly"/>.
    /// </summary>
    public Crossgen2InputKind Kind { get; init; } = Crossgen2InputKind.InputAssembly;
    /// <summary>
    /// Per-assembly crossgen2 options (e.g. cross-module optimization targets).
    /// </summary>
    public List<Crossgen2AssemblyOption> Options { get; init; } = new();

    public void SetOutputDir(string outputDir)
    {
        ILAssembly.SetOutputDir(outputDir);
    }
}

/// <summary>
/// A single crossgen2 compilation step.
/// </summary>
internal sealed class CrossgenCompilation(string name, List<CrossgenAssembly> assemblies)
{
    /// <summary>
    /// Assemblies involved in this compilation. Each specifies its role
    /// (<see cref="Crossgen2InputKind"/>) and per-assembly options.
    /// All other Roslyn-compiled assemblies are automatically available as refs.
    /// </summary>
    public List<CrossgenAssembly> Assemblies => assemblies;

    /// <summary>
    /// Image-level crossgen2 options (e.g. Composite, InputBubble, Optimize).
    /// </summary>
    public List<Crossgen2Option> Options { get; init; } = new();

    /// <summary>
    /// Optional validator for this compilation's R2R output image.
    /// </summary>
    public Action<ReadyToRunReader>? Validate { get; init; }

    public string Name => name;

    public bool IsComposite => Options.Contains(Crossgen2Option.Composite);

    private string? _outputDir = null;

    /// <summary>
    /// The output path for this compilation. In composite mode, uses a "-composite" suffix
    /// to avoid colliding with component stubs that crossgen2 creates alongside the composite image.
    /// </summary>
    public string FilePath => _outputDir != null
        ? Path.Combine(_outputDir, "CG2", Name + (IsComposite ? "-composite" : "") + ".dll")
        : throw new InvalidOperationException("Output directory not set");

    public void SetOutputDir(string outputDir)
    {
        _outputDir = outputDir;
        foreach (var assembly in assemblies)
        {
            assembly.SetOutputDir(outputDir);
        }
    }
}

/// <summary>
/// Describes a test case: assemblies compiled with Roslyn, then crossgen2'd in one or more
/// compilation steps, each optionally validated.
/// </summary>
internal sealed class R2RTestCase(string name, List<CrossgenCompilation> compilations)
{
    public string Name => name;

    /// <summary>
    /// One or more crossgen2 compilation steps, executed after Roslyn compilation.
    /// Each step can independently produce and validate an R2R image.
    /// </summary>
    public List<CrossgenCompilation> Compilations => compilations;

    public IEnumerable<CompiledAssembly> GetAssemblies()
    {
        // Should be a small number of assemblies, so a simple list is fine as an insertion-ordered set
        List<CompiledAssembly> seen = new();
        foreach (var cg2Compilation in compilations)
        {
            foreach(var assembly in cg2Compilation.Assemblies)
            {
                GetDependencies(assembly.ILAssembly, seen);
            }
        }
        return seen;

        IEnumerable<CompiledAssembly> GetDependencies(CompiledAssembly assembly, List<CompiledAssembly> seen)
        {
            foreach(var reference in assembly.References)
            {
                GetDependencies(reference, seen);
            }
            if (!seen.Contains(assembly))
            {
                seen.Add(assembly);
            }
            return seen;
        }
    }

    public void SetOutputDir(string outputDir)
    {
        Compilations.ForEach(c => c.SetOutputDir(outputDir));
    }
}

/// <summary>
/// Orchestrates the full R2R test pipeline: Roslyn compile → crossgen2 → validate.
/// </summary>
internal sealed class R2RTestRunner
{
    private readonly ITestOutputHelper _output;
    private readonly TestPaths _paths;

    public R2RTestRunner(ITestOutputHelper output)
    {
        _output = output;
        _paths = new TestPaths(output);
    }

    /// <summary>
    /// Runs a test case end-to-end.
    /// </summary>
    public void Run(R2RTestCase testCase)
    {
        var assembliesToCompile = testCase.GetAssemblies();
        Assert.True(assembliesToCompile.Count() > 0, "Test case must have at least one assembly.");
        Assert.True(testCase.Compilations.Count > 0, "Test case must have at least one compilation.");

        string baseOutputDir = Path.Combine(AppContext.BaseDirectory, "R2RTestCases", testCase.Name, Guid.NewGuid().ToString("N")[..8]);
        testCase.SetOutputDir(baseOutputDir);

        _output.WriteLine($"Test '{testCase.Name}': baseOutputDir = {baseOutputDir}");

        try
        {
            // Step 1: Compile all assemblies with Roslyn in order
            var assemblyPaths = CompileAllAssemblies(assembliesToCompile);

            // Step 2: Run each crossgen2 compilation and validate
            var driver = new R2RDriver(_output, _paths);
            var refPaths = BuildReferencePaths();

            foreach(var compilation in testCase.Compilations)
            {
                string outputPath = RunCrossgenCompilation(
                    testCase.Name, compilation, driver, compilation.FilePath, refPaths, assemblyPaths);

                if (compilation.Validate is not null)
                {
                    Assert.True(File.Exists(outputPath), $"R2R image not found: {outputPath}");
                    _output.WriteLine($"  Validating R2R image: {outputPath}");
                    var reader = new ReadyToRunReader(new SimpleAssemblyResolver(_paths), outputPath);
                    compilation.Validate(reader);
                }
            }
        }
        finally
        {
            if (Environment.GetEnvironmentVariable("KEEP_R2R_TESTS") is null)
            {
                try { Directory.Delete(baseOutputDir, true); }
                catch { /* best effort */ }
            }
        }
    }

    private Dictionary<string, string> CompileAllAssemblies(
        IEnumerable<CompiledAssembly> assemblies)
    {
        var compiler = new R2RTestCaseCompiler(_paths);
        var paths = new Dictionary<string, string>();

        foreach (var asm in assemblies)
        {
            var sources = asm.SourceResourceNames
                .Select(R2RTestCaseCompiler.ReadEmbeddedSource)
                .ToList();

            EnsureDirectoryExists(Path.GetDirectoryName(asm.FilePath));

            string ilPath = compiler.CompileAssembly(
                asm.AssemblyName,
                sources,
                asm.FilePath,
                additionalReferences: asm.References.Select(r => r.FilePath).ToList(),
                features: asm.Features.Count > 0 ? asm.Features : null);
            paths[asm.AssemblyName] = ilPath;
            _output.WriteLine($"  Roslyn compiled '{asm.AssemblyName}' -> {ilPath}");
        }

        return paths;
    }

    private static void EnsureDirectoryExists(string? v)
    {
        if (v is not null && !Directory.Exists(v))
        {
            Directory.CreateDirectory(v);
        }
    }

    private static string RunCrossgenCompilation(
        string testName,
        CrossgenCompilation compilation,
        R2RDriver driver,
        string outputFile,
        List<string> refPaths,
        Dictionary<string, string> assemblyPaths)
    {
        var args = new List<string>();

        var inputFiles = new List<string>();
        // Per-assembly inputs and options
        foreach (var asm in compilation.Assemblies)
        {
            var ilAssemblyName = asm.ILAssembly.AssemblyName;
            Assert.True(assemblyPaths.ContainsKey(ilAssemblyName),
                $"Assembly '{ilAssemblyName}' not found in compiled assemblies.");

            string ilPath = asm.ILAssembly.FilePath;

            if (asm.Kind == Crossgen2InputKind.InputAssembly)
            {
                inputFiles.Add(ilPath);
            }
            else
            {
                args.Add(asm.Kind.ToArg());
                args.Add(ilPath);
            }

            foreach (var option in asm.Options)
            {
                args.Add(option.ToArg());
                args.Add(ilAssemblyName);
            }
        }

        // Image-level options
        foreach (var option in compilation.Options)
            args.Add(option.ToArg());

        // Global refs (runtime pack + System.Private.CoreLib)
        AddRefArgs(args, refPaths);

        EnsureDirectoryExists(Path.GetDirectoryName(outputFile));

        inputFiles.AddRange(args);
        args = inputFiles;
        args.Add($"--out");
        args.Add($"{outputFile}");
        var result = driver.Compile(args);
        Assert.True(result.Success,
            $"crossgen2 failed for '{testName}':\n{result.StandardError}\n{result.StandardOutput}");

        return outputFile;
    }

    private static void AddRefArgs(List<string> args, List<string> refPaths)
    {
        foreach (string refPath in refPaths)
        {
            args.Add("-r");
            args.Add(refPath);
        }
    }

    private List<string> BuildReferencePaths()
    {
        var paths = new List<string>();

        paths.Add(Path.Combine(_paths.RuntimePackDir, "*.dll"));

        // SPCL lives in the runtime pack native/ dir in full builds (placed by
        // externals.csproj BinPlace during libs.pretest).  In partial CI builds
        // that skip libs.pretest, the runtime pack layout may not exist, but the
        // CoreCLR artifacts directory always has SPCL after clr.nativecorelib.
        string spcl = Path.Combine(_paths.RuntimePackNativeDir, "System.Private.CoreLib.dll");
        if (!File.Exists(spcl))
        {
            string fallback = Path.Combine(_paths.CoreCLRArtifactsDir, "System.Private.CoreLib.dll");
            if (File.Exists(fallback))
            {
                _output.WriteLine($"[R2RTestRunner] SPCL not found at '{spcl}'; using CoreCLR artifacts fallback '{fallback}'");
                spcl = fallback;
            }
        }

        Assert.True(File.Exists(spcl),
            $"System.Private.CoreLib.dll not found at '{spcl}'. " +
            $"Searched RuntimePackNativeDir='{_paths.RuntimePackNativeDir}' and " +
            $"CoreCLRArtifactsDir='{_paths.CoreCLRArtifactsDir}'");
        paths.Add(spcl);

        return paths;
    }
}
