// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Automatically checks <see cref="SkipOnRuntimeVersionAttribute"/> on test methods
/// before they execute. Applied at the class level on <see cref="DumpTestBase"/>,
/// so all derived test classes get automatic version-aware skipping.
/// </summary>
public sealed class CheckSkipOnRuntimeVersionAttribute : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest)
    {
        string? version = DumpTestBase.CurrentRuntimeVersion.Value;
        if (version is null)
            return;

        foreach (SkipOnRuntimeVersionAttribute attr in methodUnderTest.GetCustomAttributes<SkipOnRuntimeVersionAttribute>())
        {
            if (string.Equals(attr.Version, version, StringComparison.OrdinalIgnoreCase))
            {
                throw new SkipTestException($"[{version}] {attr.Reason}");
            }
        }
    }
}

/// <summary>
/// Base class for dump-based cDAC integration tests.
/// Loads a crash dump, creates a <see cref="ContractDescriptorTarget"/>, and provides
/// shared helpers for assertions.
/// </summary>
/// <remarks>
/// Tests that need version-aware skipping should:
/// <list type="number">
///   <item>Use <c>[ConditionalFact]</c> instead of <c>[Fact]</c></item>
///   <item>Apply <c>[SkipOnRuntimeVersion("version", "reason")]</c> to the method</item>
/// </list>
/// The <see cref="CheckSkipOnRuntimeVersionAttribute"/> on this class automatically
/// evaluates skip conditions — no manual <c>SkipIfVersionExcluded()</c> call required.
/// </remarks>
[CheckSkipOnRuntimeVersion]
public abstract class DumpTestBase : IDisposable
{
    internal static readonly AsyncLocal<string?> CurrentRuntimeVersion = new();

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
    protected ContractDescriptorTarget Target => _target ?? throw new InvalidOperationException("Dump not loaded. Call LoadDump() first.");

    /// <summary>
    /// Resolves the dump file path for the current debuggee and runtime version.
    /// Convention: {RepoRoot}/artifacts/dumps/cdac/{RuntimeVersion}/{DebuggeeName}/{DebuggeeName}.dmp
    /// </summary>
    protected string GetDumpPath()
    {
        string? repoRoot = FindRepoRoot();
        if (repoRoot is null)
            throw new InvalidOperationException("Could not locate the repository root.");

        return Path.Combine(repoRoot, "artifacts", "dumps", "cdac", RuntimeVersion, DebuggeeName, $"{DebuggeeName}.dmp");
    }

    /// <summary>
    /// Load the dump and create the cDAC Target.
    /// Throws if the dump file does not exist.
    /// </summary>
    protected void LoadDump()
    {
        CurrentRuntimeVersion.Value = RuntimeVersion;

        string dumpPath = GetDumpPath();
        if (!File.Exists(dumpPath))
            throw new FileNotFoundException($"Dump file not found: {dumpPath}. Run dump generation first.");

        _host = ClrMdDumpHost.Open(dumpPath);
        ulong contractDescriptor = _host.FindContractDescriptorAddress();

        bool created = ContractDescriptorTarget.TryCreate(
            contractDescriptor,
            _host.ReadFromTarget,
            writeToTarget: null!,
            getThreadContext: null!,
            additionalFactories: [],
            out _target);

        Assert.True(created, $"Failed to create ContractDescriptorTarget from dump: {dumpPath}");
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
        GC.SuppressFinalize(this);
    }
}
