// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Base class for dump-based cDAC integration tests.
/// Loads a crash dump via <see cref="IAsyncLifetime"/>, creates a
/// <see cref="ContractDescriptorTarget"/>, and provides helpers for
/// version-aware and OS-aware test skipping.
/// </summary>
/// <remarks>
/// Tests that need conditional skipping should:
/// <list type="number">
///   <item>Use <c>[ConditionalFact]</c> instead of <c>[Fact]</c></item>
///   <item>Call <see cref="SkipIfVersion"/> or <see cref="SkipIfTargetOS"/>
///         at the start of the test method</item>
/// </list>
/// </remarks>
public abstract class DumpTestBase : IAsyncLifetime
{
    private ClrMdDumpHost? _host;
    private ContractDescriptorTarget? _target;
    private string? _targetOS;

    /// <summary>
    /// The name of the debuggee that produced the dump (e.g., "BasicThreads").
    /// </summary>
    protected abstract string DebuggeeName { get; }

    /// <summary>
    /// The runtime version identifier (e.g., "local", "net10.0").
    /// </summary>
    protected abstract string RuntimeVersion { get; }

    /// <summary>
    /// The cDAC Target created from the dump.
    /// </summary>
    protected ContractDescriptorTarget Target => _target ?? throw new InvalidOperationException("Dump not loaded.");

    /// <summary>
    /// The target operating system of the dump, resolved from the RuntimeInfo contract.
    /// May be <c>null</c> if the contract is unavailable.
    /// </summary>
    protected string TargetOS => _targetOS ?? string.Empty;

    /// <summary>
    /// Loads the dump and creates the cDAC Target before each test.
    /// Also resolves the target OS from the RuntimeInfo contract.
    /// </summary>
    public Task InitializeAsync()
    {
        if (IsVersionSkipped(RuntimeVersion))
            throw new SkipTestException($"RuntimeVersion '{RuntimeVersion}' is in SkipDumpVersions list.");

        string dumpPath = GetDumpPath();

        if (!File.Exists(dumpPath))
            throw new SkipTestException($"Dump file not found: {dumpPath}");
        _host = ClrMdDumpHost.Open(dumpPath);
        ulong contractDescriptor = _host.FindContractDescriptorAddress();

        bool created = ContractDescriptorTarget.TryCreate(
            contractDescriptor,
            _host.ReadFromTarget,
            writeToTarget: null!,
            _host.GetThreadContext,
            additionalFactories: [],
            out _target);

        Assert.True(created, $"Failed to create ContractDescriptorTarget from dump: {dumpPath}");

        try
        {
            _targetOS = _target!.Contracts.RuntimeInfo.GetTargetOperatingSystem().ToString();
        }
        catch
        {
            // Resolving the target OS is best-effort. The RuntimeInfo contract may be
            // unavailable in older dumps or throw for unexpected reasons. Treat the OS
            // as unknown and allow tests to continue — they can handle a missing TargetOS.
            _targetOS = null;
        }

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _host?.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Skips the current test if <see cref="RuntimeVersion"/> matches <paramref name="version"/>.
    /// Must be used with <c>[ConditionalFact]</c> for xunit to report the test as skipped.
    /// </summary>
    protected void SkipIfVersion(string version, string reason)
    {
        if (string.Equals(RuntimeVersion, version, StringComparison.OrdinalIgnoreCase))
            throw new SkipTestException($"[{version}] {reason}");
    }

    /// <summary>
    /// Skips the current test if the dump's target OS matches <paramref name="operatingSystem"/>.
    /// Must be used with <c>[ConditionalFact]</c> for xunit to report the test as skipped.
    /// </summary>
    protected void SkipIfTargetOS(string operatingSystem, string reason)
    {
        if (string.Equals(TargetOS, operatingSystem, StringComparison.OrdinalIgnoreCase))
            throw new SkipTestException($"[TargetOS={TargetOS}] {reason}");
    }

    private string GetDumpPath()
    {
        string? dumpRoot = Environment.GetEnvironmentVariable("CDAC_DUMP_ROOT");
        if (string.IsNullOrEmpty(dumpRoot))
        {
            string? repoRoot = FindRepoRoot();
            if (repoRoot is null)
                throw new InvalidOperationException("Could not locate the repository root.");

            dumpRoot = Path.Combine(repoRoot, "artifacts", "dumps", "cdac");
        }

        return Path.Combine(dumpRoot, RuntimeVersion, DebuggeeName, $"{DebuggeeName}.dmp");
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
