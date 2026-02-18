// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
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
public abstract class DumpTestBase : IAsyncLifetime, IDisposable
{
    private static readonly ConcurrentDictionary<string, string?> s_targetOSCache = new();

    private ClrMdDumpHost? _host;
    private ContractDescriptorTarget? _target;

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
    /// Loads the dump and creates the cDAC Target before each test.
    /// </summary>
    public Task InitializeAsync()
    {
        string dumpPath = GetDumpPath();
        if (!File.Exists(dumpPath))
            throw new FileNotFoundException($"Dump file not found: {dumpPath}. Run dump generation first.");

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

        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

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
    /// Uses the cDAC RuntimeInfo contract. Results are cached per dump path.
    /// Must be used with <c>[ConditionalFact]</c> for xunit to report the test as skipped.
    /// </summary>
    protected void SkipIfTargetOS(string operatingSystem, string reason)
    {
        string? targetOS = ResolveTargetOS();
        if (targetOS is not null && string.Equals(targetOS, operatingSystem, StringComparison.OrdinalIgnoreCase))
            throw new SkipTestException($"[TargetOS={targetOS}] {reason}");
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

    private string? ResolveTargetOS()
    {
        string dumpPath = GetDumpPath();
        return s_targetOSCache.GetOrAdd(dumpPath, static path =>
        {
            if (!File.Exists(path))
                return null;

            using ClrMdDumpHost host = ClrMdDumpHost.Open(path);
            ulong descriptor = host.FindContractDescriptorAddress();

            if (!ContractDescriptorTarget.TryCreate(
                    descriptor,
                    host.ReadFromTarget,
                    writeToTarget: null!,
                    host.GetThreadContext,
                    additionalFactories: [],
                    out ContractDescriptorTarget? target))
            {
                return null;
            }

            try
            {
                return target.Contracts.RuntimeInfo.GetTargetOperatingSystem().ToString();
            }
            catch
            {
                return null;
            }
        });
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

    public void Dispose()
    {
        _host?.Dispose();
        System.GC.SuppressFinalize(this);
    }
}
