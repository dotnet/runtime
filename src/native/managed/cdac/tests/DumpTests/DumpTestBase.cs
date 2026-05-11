// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Base class for dump-based cDAC integration tests.
/// Each test is a <c>[ConditionalTheory]</c> parameterized by <see cref="TestConfiguration"/>.
/// Call <see cref="InitializeDumpTest"/> at the start of every test method to load
/// the dump and evaluate skip attributes such as <see cref="SkipOnVersionAttribute"/>.
/// </summary>
public abstract class DumpTestBase : IDisposable
{
    private ClrMdDumpHost? _host;
    private ContractDescriptorTarget? _target;
    private DumpInfo? _dumpInfo;

    /// <summary>
    /// The runtime version identifiers tested by <see cref="TestConfigurations"/> and
    /// searched by <see cref="GetDumpSource"/>. Centralised here so both share the same list.
    /// </summary>
    private static readonly string[] RuntimeVersions = ["local", "net10.0"];

    /// <summary>
    /// The set of runtime versions and R2R modes to test against.
    /// Each entry produces a separate test invocation via <c>[MemberData]</c>.
    /// R2R modes are provided by <see cref="GetR2RModes"/>, which currently yields
    /// both <c>"r2r"</c> and <c>"jit"</c> for each runtime version.
    /// </summary>
    public static IEnumerable<object[]> TestConfigurations
    {
        get
        {
            string? dumpSource = GetDumpSource();
            foreach (string r2rMode in GetR2RModes())
            {
                foreach (string version in RuntimeVersions)
                {
                    if (!IsVersionSkipped(version))
                        yield return [new TestConfiguration(version, r2rMode, dumpSource)];
                }
            }
        }
    }

    /// <summary>
    /// The name of the debuggee that produced the dump (e.g., "BasicThreads").
    /// </summary>
    protected abstract string DebuggeeName { get; }

    /// <summary>
    /// The dump type to load (e.g., "heap", "full"). Determines the subdirectory
    /// under the dump root where the dump file is located. Override in test classes
    /// to select a different dump type.
    /// </summary>
    protected virtual string DumpType => "heap";

    /// <summary>
    /// The cDAC Target created from the dump.
    /// </summary>
    protected ContractDescriptorTarget Target => _target ?? throw new InvalidOperationException("Dump not loaded.");

    /// <summary>
    /// Metadata about the environment in which the dump was generated.
    /// Available after <see cref="InitializeDumpTest"/> has been called.
    /// </summary>
    protected DumpInfo? DumpMetadata => _dumpInfo;

    /// <summary>
    /// Loads the dump for the given <paramref name="config"/> and evaluates skip
    /// attributes on the calling test method. Call this as the first line of every test.
    /// </summary>
    protected void InitializeDumpTest(TestConfiguration config, [CallerMemberName] string callerName = "")
        => InitializeDumpTest(config, DebuggeeName, DumpType, callerName);

    /// <summary>
    /// Loads the dump for the given <paramref name="config"/> using an explicit
    /// <paramref name="debuggeeName"/> and <paramref name="dumpType"/>.
    /// Use this overload when individual test methods need different debuggees.
    /// </summary>
    protected void InitializeDumpTest(TestConfiguration config, string debuggeeName, string dumpType, [CallerMemberName] string callerName = "")
    {
        string dumpRoot = GetDumpRoot();
        string versionDir = Path.Combine(dumpRoot, config.RuntimeVersion);
        _dumpInfo = DumpInfo.TryLoad(versionDir);

        EvaluateSkipAttributes(config, callerName, dumpType);

        string dumpPath = Path.Combine(versionDir, dumpType, config.R2RMode, debuggeeName, $"{debuggeeName}.dmp");

        if (!File.Exists(dumpPath))
        {
            if (_dumpInfo is not null && _dumpInfo.IsDumpExpected(debuggeeName, dumpType, config.R2RMode))
                Assert.Fail($"Expected {config.R2RMode}/{dumpType} dump for {debuggeeName} but not found: {dumpPath}");

            throw new SkipTestException($"No {config.R2RMode} dump for {debuggeeName}: {dumpPath}");
        }

        _host = ClrMdDumpHost.Open(dumpPath, GetSymbolPaths(debuggeeName, versionDir));
        ulong contractDescriptor = _host.FindContractDescriptorAddress();

        bool created = ContractDescriptorTarget.TryCreate(
            contractDescriptor,
            _host.ReadFromTarget,
            writeToTarget: static (_, _) => -1,
            _host.GetThreadContext,
            allocVirtual: static (ulong _, out ulong _) => throw new NotImplementedException("Dump tests do not provide AllocVirtual"),
            [Contracts.CoreCLRContracts.Register],
            out _target);

        Assert.True(created, $"Failed to create ContractDescriptorTarget from dump: {dumpPath}");
    }

    public void Dispose()
    {
        _host?.Dispose();
        System.GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Checks the calling test method for skip attributes and throws
    /// <see cref="SkipTestException"/> if the current configuration matches.
    /// </summary>
    private void EvaluateSkipAttributes(TestConfiguration config, string callerName, string? dumpType = null)
    {
        if (config.RuntimeVersion is "net10.0" && (dumpType ?? DumpType) == "heap")
        {
            throw new SkipTestException($"[net10.0] Skipping heap dump tests due to outdated dump generation.");
        }

        MethodInfo? method = GetType().GetMethod(callerName, BindingFlags.Public | BindingFlags.Instance);
        if (method is null)
            return;

        foreach (SkipOnVersionAttribute attr in method.GetCustomAttributes<SkipOnVersionAttribute>())
        {
            if (string.Equals(attr.Version, config.RuntimeVersion, StringComparison.OrdinalIgnoreCase))
                throw new SkipTestException($"[{config.RuntimeVersion}] {attr.Reason}");
        }

        if (_dumpInfo is not null)
        {
            foreach (SkipOnOSAttribute attr in method.GetCustomAttributes<SkipOnOSAttribute>())
            {
                if (attr.IncludeOnly is not null)
                {
                    if (!string.Equals(attr.IncludeOnly, _dumpInfo.Os, StringComparison.OrdinalIgnoreCase))
                        throw new SkipTestException($"[{_dumpInfo.Os}] {attr.Reason}");
                }
                else if (attr.Os is not null)
                {
                    if (string.Equals(attr.Os, _dumpInfo.Os, StringComparison.OrdinalIgnoreCase))
                        throw new SkipTestException($"[{_dumpInfo.Os}] {attr.Reason}");
                }
            }

            foreach (SkipOnArchAttribute attr in method.GetCustomAttributes<SkipOnArchAttribute>())
            {
                if (string.Equals(attr.Arch, _dumpInfo.Arch, StringComparison.OrdinalIgnoreCase))
                    throw new SkipTestException($"[{_dumpInfo.Arch}] {attr.Reason}");
            }
        }
    }

    private static string GetDumpRoot()
    {
        string? dumpRoot = Environment.GetEnvironmentVariable("CDAC_DUMP_ROOT");
        if (!string.IsNullOrEmpty(dumpRoot))
            return dumpRoot;

        string? repoRoot = FindRepoRoot();
        if (repoRoot is null)
            throw new InvalidOperationException("Could not locate the repository root.");

        return Path.Combine(repoRoot, "artifacts", "dumps", "cdac");
    }

    /// <summary>
    /// Returns the dump source platform from dump-info.json (e.g., "windows_x64"),
    /// or null for local runs where CDAC_DUMP_ROOT is not set.
    /// </summary>
    private static string? GetDumpSource()
    {
        string? dumpRoot = Environment.GetEnvironmentVariable("CDAC_DUMP_ROOT");
        if (string.IsNullOrEmpty(dumpRoot))
            return null;

        // Try loading dump-info.json from any version directory to get OS/Arch
        foreach (string versionDir in RuntimeVersions)
        {
            DumpInfo? info = DumpInfo.TryLoad(Path.Combine(dumpRoot, versionDir));
            if (info is not null)
                return $"{info.Os}_{info.Arch}";
        }

        // Fall back to the directory name if dump-info.json isn't available
        return Path.GetFileName(dumpRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }

    /// <summary>
    /// Returns the R2R modes to test against. Both modes are always tested;
    /// dumps that don't exist for a given mode are skipped via <see cref="SkipTestException"/>.
    /// </summary>
    private static IEnumerable<string> GetR2RModes()
    {
        yield return "r2r";
        yield return "jit";
    }

    /// <summary>
    /// Checks if the given version is in the <c>SkipDumpVersions</c> MSBuild property
    /// (baked into runtimeconfig.json as <c>CDAC_SKIP_VERSIONS</c>).
    /// </summary>
    private static bool IsVersionSkipped(string version)
    {
        if (AppContext.GetData("CDAC_SKIP_VERSIONS") is not string skipVersions)
            return false;

        foreach (string entry in skipVersions.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (string.Equals(entry, version, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Collects local symbol paths for ClrMD to resolve modules in the dump.
    /// Checks <c>symbols/</c> directories in the helix payload (Helix and xplat dumps)
    /// </summary>
    private static List<string> GetSymbolPaths(string debuggeeName, string versionDir)
    {
        List<string> paths = [];

        // Symbols directory in the dump tree (populated by Helix commands before tarring)
        string symbolsDir = Path.Combine(versionDir, "symbols");
        if (Directory.Exists(symbolsDir))
        {
            string runtimeSymbols = Path.Combine(symbolsDir, "runtime");
            if (Directory.Exists(runtimeSymbols))
                paths.Add(runtimeSymbols);

            string debuggeeSymbols = Path.Combine(symbolsDir, "debuggees", debuggeeName);
            if (Directory.Exists(debuggeeSymbols))
                paths.Add(debuggeeSymbols);
        }

        return paths;
    }

    private static string? FindRepoRoot()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir, "global.json")))
                return dir;
            dir = Path.GetDirectoryName(dir);
        }

        return null;
    }
}
