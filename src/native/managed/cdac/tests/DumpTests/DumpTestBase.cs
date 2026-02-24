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

    /// <summary>
    /// The set of runtime versions to test against.
    /// Each entry produces a separate test invocation via <c>[MemberData]</c>.
    /// </summary>
    public static IEnumerable<object[]> TestConfigurations
    {
        get
        {
            if (!IsVersionSkipped("local"))
                yield return [new TestConfiguration("local")];

            if (!IsVersionSkipped("net10.0"))
                yield return [new TestConfiguration("net10.0")];
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
    /// Loads the dump for the given <paramref name="config"/> and evaluates skip
    /// attributes on the calling test method. Call this as the first line of every test.
    /// </summary>
    protected void InitializeDumpTest(TestConfiguration config, [CallerMemberName] string callerName = "")
    {
        EvaluateVersionSkipAttributes(config, callerName);

        string dumpPath = GetDumpPath(config.RuntimeVersion);

        Assert.True(File.Exists(dumpPath), $"Dump file not found: {dumpPath}");


        _host = ClrMdDumpHost.Open(dumpPath);
        ulong contractDescriptor = _host.FindContractDescriptorAddress();

        bool created = ContractDescriptorTarget.TryCreate(
            contractDescriptor,
            _host.ReadFromTarget,
            writeToTarget: static (_, _) => -1,
            _host.GetThreadContext,
            additionalFactories: [],
            out _target);

        Assert.True(created, $"Failed to create ContractDescriptorTarget from dump: {dumpPath}");

    }

    public void Dispose()
    {
        _host?.Dispose();
        System.GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Checks the calling test method for <see cref="SkipOnVersionAttribute"/> and
    /// throws <see cref="SkipTestException"/> if the current configuration matches.
    /// </summary>
    private void EvaluateVersionSkipAttributes(TestConfiguration config, string callerName)
    {
        if (config.RuntimeVersion is "net10.0" && DumpType == "heap")
        {
            // Skip heap dumps on net10.0 for now, as they are currently generated with an older cDAC version that doesn't populate all fields
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
    }

    private string GetDumpPath(string runtimeVersion)
    {
        string? dumpRoot = Environment.GetEnvironmentVariable("CDAC_DUMP_ROOT");
        if (string.IsNullOrEmpty(dumpRoot))
        {
            string? repoRoot = FindRepoRoot();
            if (repoRoot is null)
                throw new InvalidOperationException("Could not locate the repository root.");

            dumpRoot = Path.Combine(repoRoot, "artifacts", "dumps", "cdac");
        }

        return Path.Combine(dumpRoot, runtimeVersion, DumpType, DebuggeeName, $"{DebuggeeName}.dmp");
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
