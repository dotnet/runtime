// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Lxr.Harness.Core;

namespace Lxr.Harness.Runner;

/// <summary>
/// Where a worker process runs, and - the part that actually differs - how collector configuration
/// reaches the runtime.
///
/// <para>The two delivery mechanisms are not interchangeable. <c>dotnet</c> reads
/// <c>&lt;app&gt;.runtimeconfig.json</c>, so properties are injected there or by
/// <c>DOTNET_*</c> environment variables. CoreRun does not read <c>runtimeconfig.json</c> at all -
/// <c>corerun.cpp</c> lines 538-612 build the property list itself and line 726 documents
/// <c>-p System.GC.Concurrent=true</c> - so properties must be passed on its command line. Making
/// this an explicit abstraction rather than an accident is what lets the same matrix run on both.</para>
/// </summary>
public sealed class HostDescriptor
{
    public required string Id { get; init; }

    public required string Executable { get; init; }

    public required bool UsesCoreRunProperties { get; init; }

    public required HostCapabilities Capabilities { get; init; }

    public required string Description { get; init; }

    /// <summary>
    /// The directory CoreRun should treat as <c>core_root</c>, i.e. the one holding
    /// <c>coreclr.dll</c> together with the framework assemblies. Null for hosts that resolve the
    /// framework themselves. See <c>WorkerLauncher</c> for why CoreRun needs this.
    /// </summary>
    public string? FrameworkDirectory { get; init; }

    public static HostDescriptor Sdk(string dotnetPath, bool aspNetAvailable) => new()
    {
        Id = "sdk",
        Executable = dotnetPath,
        UsesCoreRunProperties = false,
        Capabilities = aspNetAvailable ? HostCapabilities.AspNetCoreSharedFramework : HostCapabilities.None,
        Description = "The repository's bootstrapped SDK host.",
    };

    public static HostDescriptor TestHost(string dotnetPath, bool aspNetAvailable) => new()
    {
        Id = "testhost",
        Executable = dotnetPath,
        UsesCoreRunProperties = false,
        Capabilities = aspNetAvailable ? HostCapabilities.AspNetCoreSharedFramework : HostCapabilities.None,
        Description = "The locally built runtime, laid out as a shared framework under artifacts/bin/testhost.",
    };

    public static HostDescriptor CoreRun(string coreRunPath, string? frameworkDirectory = null) => new()
    {
        Id = "corerun",
        Executable = coreRunPath,
        UsesCoreRunProperties = true,
        FrameworkDirectory = frameworkDirectory,

        // CoreRun has no shared-framework concept, so it can never supply ASP.NET Core. The flagship
        // scenario reaches the locally built runtime through the testhost host instead.
        Capabilities = HostCapabilities.None,
        Description = "The locally built CoreRun host, artifacts/bin/coreclr/<config>/corerun.exe.",
    };

    /// <summary>
    /// Detects which hosts are usable, rather than assuming. A host that is not present produces a
    /// declared skip, never a silent absence.
    /// </summary>
    public static List<HostDescriptor> Discover(string repoRoot, string configuration = "Release")
    {
        var hosts = new List<HostDescriptor>();

        string sdkDotnet = Path.Combine(repoRoot, ".dotnet", "dotnet.exe");
        if (File.Exists(sdkDotnet))
        {
            hosts.Add(Sdk(sdkDotnet, HasAspNetCore(Path.Combine(repoRoot, ".dotnet"))));
        }

        string? testHostRoot = FindTestHostRoot(repoRoot, configuration);
        if (testHostRoot is not null)
        {
            string testHostDotnet = Path.Combine(testHostRoot, "dotnet.exe");
            if (File.Exists(testHostDotnet))
            {
                hosts.Add(TestHost(testHostDotnet, HasAspNetCore(testHostRoot)));
            }
        }

        string coreRun = Path.Combine(repoRoot, "artifacts", "bin", "coreclr", $"windows.x64.{configuration}", "corerun.exe");
        if (File.Exists(coreRun))
        {
            hosts.Add(CoreRun(coreRun, FindSharedFrameworkDirectory(testHostRoot)));
        }

        return hosts;
    }

    /// <summary>
    /// Finds the directory that holds the built framework assemblies alongside coreclr.dll. The
    /// testhost's shared-framework directory is exactly that layout, and its coreclr.dll is
    /// byte-identical to the one beside corerun.exe.
    /// </summary>
    internal static string? FindSharedFrameworkDirectory(string? testHostRoot)
    {
        if (testHostRoot is null)
        {
            return null;
        }

        string shared = Path.Combine(testHostRoot, "shared", "Microsoft.NETCore.App");
        if (!Directory.Exists(shared))
        {
            return null;
        }

        foreach (string candidate in Directory.GetDirectories(shared))
        {
            if (File.Exists(Path.Combine(candidate, "coreclr.dll")) &&
                File.Exists(Path.Combine(candidate, "System.Runtime.dll")))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string? FindTestHostRoot(string repoRoot, string configuration)
    {
        string testHost = Path.Combine(repoRoot, "artifacts", "bin", "testhost");
        if (!Directory.Exists(testHost))
        {
            return null;
        }

        foreach (string candidate in Directory.GetDirectories(testHost))
        {
            if (Path.GetFileName(candidate).Contains(configuration, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// Whether an ASP.NET Core shared framework is laid out under this host root. The testhost built by
    /// this repository contains only <c>Microsoft.NETCore.App</c>; composing an ASP.NET Core framework
    /// beside it is what lets the flagship scenario run against a locally built runtime, and
    /// <c>scripts/compose-testhost-aspnet.ps1</c> performs that composition. Capability is
    /// <em>detected</em> here rather than created, so a host that was never composed reports a declared
    /// skip instead of silently appearing to work.
    /// </summary>
    public static bool HasAspNetCore(string hostRoot)
    {
        string shared = Path.Combine(hostRoot, "shared", "Microsoft.AspNetCore.App");
        return Directory.Exists(shared) && Directory.GetDirectories(shared).Length > 0;
    }
}
