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
using Xunit.Sdk;

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
    /// Metadata about the environment in which the dump was generated.
    /// Available after <see cref="InitializeDumpTest"/> has been called.
    /// </summary>
    protected DumpInfo? DumpMetadata => _dumpInfo;

    /// <summary>
    /// Loads the dump for the given <paramref name="config"/> and evaluates skip
    /// attributes on the calling test method. Call this as the first line of every test.
    /// </summary>
    protected void InitializeDumpTest(TestConfiguration config, [CallerMemberName] string callerName = "")
    {
        string dumpRoot = GetDumpRoot();
        string versionDir = Path.Combine(dumpRoot, config.RuntimeVersion);
        _dumpInfo = DumpInfo.TryLoad(versionDir);

        EvaluateSkipAttributes(config, callerName);

        string dumpPath = Path.Combine(versionDir, DumpType, DebuggeeName, $"{DebuggeeName}.dmp");

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
    /// Checks the calling test method for skip attributes and throws
    /// <see cref="SkipException"/> if the current configuration matches.
    /// </summary>
    private void EvaluateSkipAttributes(TestConfiguration config, string callerName)
    {
        if (config.RuntimeVersion is "net10.0" && DumpType == "heap")
        {
            throw SkipException.ForSkip($"[net10.0] Skipping heap dump tests due to outdated dump generation.");
        }

        MethodInfo? method = GetType().GetMethod(callerName, BindingFlags.Public | BindingFlags.Instance);
        if (method is null)
            return;

        foreach (SkipOnVersionAttribute attr in method.GetCustomAttributes<SkipOnVersionAttribute>())
        {
            if (string.Equals(attr.Version, config.RuntimeVersion, StringComparison.OrdinalIgnoreCase))
                throw SkipException.ForSkip($"[{config.RuntimeVersion}] {attr.Reason}");
        }

        if (_dumpInfo is not null)
        {
            foreach (SkipOnOSAttribute attr in method.GetCustomAttributes<SkipOnOSAttribute>())
            {
                if (attr.IncludeOnly is not null)
                {
                    if (!string.Equals(attr.IncludeOnly, _dumpInfo.Os, StringComparison.OrdinalIgnoreCase))
                        throw SkipException.ForSkip($"[{_dumpInfo.Os}] {attr.Reason}");
                }
                else if (attr.Os is not null)
                {
                    if (string.Equals(attr.Os, _dumpInfo.Os, StringComparison.OrdinalIgnoreCase))
                        throw SkipException.ForSkip($"[{_dumpInfo.Os}] {attr.Reason}");
                }
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
