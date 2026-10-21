// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Lxr.Harness.Core;

/// <summary>
/// One thing that must be true of the collector for a run to count. A run whose collector cannot be
/// confirmed is invalid, so these are checked from inside the process under test.
/// </summary>
public sealed class IdentityAssertion
{
    /// <summary>Key as it appears in <see cref="GC.GetConfigurationVariables"/> - the C++ identifier.</summary>
    public required string Key { get; init; }

    public required string ExpectedValue { get; init; }

    /// <summary>When set, the assertion only applies if this other key currently has
    /// <see cref="AppliesWhenValue"/>. Used so DATAS is only asserted where its readback is real.</summary>
    public string? AppliesWhenKey { get; init; }

    public string? AppliesWhenValue { get; init; }

    /// <summary>Why this assertion is sound, cited to runtime source.</summary>
    public required string Justification { get; init; }
}

/// <summary>
/// A named, fully specified collector configuration.
///
/// <para>Knobs are delivered as runtime host properties (<c>System.GC.*</c>) rather than as
/// <c>DOTNET_*</c> environment variables, and that choice is load-bearing: numeric GC configuration
/// read through the environment is parsed as <em>hexadecimal</em>
/// (<c>u16_strtoui64(out, &amp;end, 16)</c>, src/coreclr/vm/gcenv.ee.cpp line 1338), whereas the
/// public host-property channel parses with base 0, i.e. decimal
/// (src/coreclr/utilcode/configuration.cpp line 86). Delivering <c>DOTNET_GCHeapCount=16</c> would
/// silently request twenty-two heaps. The environment channel also takes precedence over the
/// property channel (gcenv.ee.cpp lines 1324 and 1351), which is what makes it useful for forcing a
/// deliberate mismatch in control 1.</para>
/// </summary>
public sealed class CollectorArm
{
    public required string Id { get; init; }

    public required string Description { get; init; }

    public required IReadOnlyDictionary<string, string> RuntimeProperties { get; init; }

    public IReadOnlyDictionary<string, string> EnvironmentOverrides { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    public required IReadOnlyList<IdentityAssertion> Identity { get; init; }
}

public static class CollectorArms
{
    public const string WorkstationId = "wks";
    public const string ServerId = "srv";
    public const string ServerDatasId = "srv-datas";
    public const string LxrId = "lxr";

    public static CollectorArm Workstation { get; } = new()
    {
        Id = WorkstationId,
        Description = "Workstation GC, background GC on, DATAS not applicable.",
        RuntimeProperties = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["System.GC.Server"] = "false",
            ["System.GC.Concurrent"] = "true",
        },
        Identity =
        [
            new IdentityAssertion
            {
                Key = GcIdentity.ServerGcKey,
                ExpectedValue = "false",
                Justification = "gcenv.ee.cpp:1249 answers the gcServer config from g_heap_type == GC_HEAP_SVR.",
            },
            new IdentityAssertion
            {
                Key = GcIdentity.ConcurrentGcKey,
                ExpectedValue = "true",
                Justification = "init.cpp:829 writes back gc_can_use_concurrent as the effective value.",
            },
        ],
    };

    /// <summary>
    /// Server GC with a pinned heap count. Pinning the heap count also provably disables DATAS:
    /// <c>init.cpp</c> lines 792-796 read the dynamic adaptation mode and then force it to zero when
    /// <c>GetHeapCount() != 0</c>. One knob therefore pins the heap count and disables the adaptive
    /// heap sizing that would otherwise confound every Server GC comparison, and both effects are
    /// separately observable.
    /// </summary>
    public static CollectorArm Server(int heapCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(heapCount);
        string heapCountText = heapCount.ToString(CultureInfo.InvariantCulture);

        return new CollectorArm
        {
            Id = ServerId,
            Description = $"Server GC, {heapCountText} heaps pinned, DATAS forced off by the pinned heap count.",
            RuntimeProperties = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["System.GC.Server"] = "true",
                ["System.GC.Concurrent"] = "true",
                ["System.GC.HeapCount"] = heapCountText,
            },
            Identity =
            [
                new IdentityAssertion
                {
                    Key = GcIdentity.ServerGcKey,
                    ExpectedValue = "true",
                    Justification = "gcenv.ee.cpp:1249 answers the gcServer config from g_heap_type == GC_HEAP_SVR.",
                },
                new IdentityAssertion
                {
                    Key = GcIdentity.HeapCountKey,
                    ExpectedValue = heapCountText,
                    Justification = "interface.cpp:431 SetHeapCount(nhp) records the heap count the GC actually created.",
                },
                new IdentityAssertion
                {
                    Key = GcIdentity.DynamicAdaptationModeKey,
                    ExpectedValue = "0",
                    AppliesWhenKey = GcIdentity.ServerGcKey,
                    AppliesWhenValue = "true",
                    Justification = "init.cpp:792-796 forces dynamic_adaptation_mode to 0 when GetHeapCount() != 0; " +
                        "interface.cpp:752 writes the effective value back, but only under DYNAMIC_HEAP_COUNT (gcpriv.h:158-162).",
                },
            ],
        };
    }

    /// <summary>
    /// Server GC with DATAS left at its default. Included deliberately: DATAS defaults to on
    /// (<c>gcconfig.h</c> line 145 gives <c>GCDynamicAdaptationMode</c> a default of 1) and varies the
    /// heap count during the run, so it is a different collector configuration rather than a variant
    /// of <see cref="Server"/>, and comparing against it unknowingly is the confound the brief warns
    /// about.
    /// </summary>
    public static CollectorArm ServerDatas { get; } = new()
    {
        Id = ServerDatasId,
        Description = "Server GC with DATAS at its default (on); heap count varies during the run.",
        RuntimeProperties = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["System.GC.Server"] = "true",
            ["System.GC.Concurrent"] = "true",
        },
        Identity =
        [
            new IdentityAssertion
            {
                Key = GcIdentity.ServerGcKey,
                ExpectedValue = "true",
                Justification = "gcenv.ee.cpp:1249 answers the gcServer config from g_heap_type == GC_HEAP_SVR.",
            },
            new IdentityAssertion
            {
                Key = GcIdentity.DynamicAdaptationModeKey,
                ExpectedValue = "1",
                AppliesWhenKey = GcIdentity.ServerGcKey,
                AppliesWhenValue = "true",
                Justification = "gcconfig.h:145 defaults GCDynamicAdaptationMode to 1; interface.cpp:752 writes the effective value back under DYNAMIC_HEAP_COUNT.",
            },
        ],
    };

    /// <summary>
    /// The future LXR arm. LXR does not exist in .NET yet, so this produces a configuration that
    /// cannot currently succeed - by design. It is present so that adding the collector later is a
    /// data change rather than a harness redesign, and so the shape of that change is reviewable now.
    ///
    /// <para>A standalone collector is selected by <c>System.GC.Name</c> and <c>System.GC.Path</c>
    /// (<c>gcconfig.h</c> lines 142-143), and because both are surfaced by
    /// <see cref="GC.GetConfigurationVariables"/> the harness can read back the name of the collector
    /// that actually loaded - so the LXR arm gets exactly the same identity guarantee as the
    /// in-tree ones, through the same mechanism.</para>
    /// </summary>
    public static CollectorArm Lxr(string libraryName, string? libraryPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(libraryName);

        var properties = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["System.GC.Name"] = libraryName,
        };

        var identity = new List<IdentityAssertion>
        {
            new()
            {
                Key = GcIdentity.GcNameKey,
                ExpectedValue = libraryName,
                Justification = "gcconfig.h:142 exposes GCName/System.GC.Name, and EnumerateConfigurationValues surfaces it.",
            },
        };

        if (!string.IsNullOrWhiteSpace(libraryPath))
        {
            properties["System.GC.Path"] = libraryPath;
            identity.Add(new IdentityAssertion
            {
                Key = GcIdentity.GcPathKey,
                ExpectedValue = libraryPath,
                Justification = "gcconfig.h:143 exposes GCPath/System.GC.Path.",
            });
        }

        return new CollectorArm
        {
            Id = LxrId,
            Description = $"Standalone LXR collector '{libraryName}' (not yet implemented in .NET).",
            RuntimeProperties = properties,
            Identity = identity,
        };
    }

    public static CollectorArm Resolve(string id, int serverHeapCount) =>
        id switch
        {
            WorkstationId => Workstation,
            ServerId => Server(serverHeapCount),
            ServerDatasId => ServerDatas,
            _ => throw new ArgumentException($"Unknown collector arm '{id}'. Known arms: {WorkstationId}, {ServerId}, {ServerDatasId}.", nameof(id)),
        };
}
