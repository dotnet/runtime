// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Lxr.Harness.Core;

/// <summary>
/// The canonical list of scenarios in the matrix, independent of which assembly can construct them.
///
/// <para>This exists so there is a single machine-readable statement of what the matrix contains. The
/// verification gate derives the expected scenario count from the document, cross-checks it against
/// this catalogue and against the scenario ids present in the smoke results, and requires all three
/// to agree - rather than repeating the number ten at three assertion sites, where a stale literal
/// would agree with itself forever.</para>
/// </summary>
public static class ScenarioCatalog
{
    public sealed class Entry
    {
        public required string Id { get; init; }

        public required PrimaryMetric Primary { get; init; }

        public HostCapabilities RequiredCapabilities { get; init; }

        /// <summary>
        /// Every scenario can be driven open-loop, so every scenario can produce coordinated-omission
        /// free latency; <see cref="Primary"/> only says which number the matrix headlines. This
        /// matters because the flagship latency scenario needs the ASP.NET Core shared framework and
        /// therefore cannot reach a bare CoreRun host, and a host with no latency evidence at all
        /// would be useless for the signal P0.2 established as the acceptance criterion.
        /// </summary>
        public bool SupportsLatency { get; init; } = true;

        /// <summary>Wall-clock limit for one invocation at default settings. Enforced by the runner;
        /// exceeding it records a failed run, never an absent one.</summary>
        public required int DefaultTimeoutSeconds { get; init; }
    }

    // Every field is spelled out on every row, including the ones whose value is the default. A
    // requirement that is declared by omission is indistinguishable from one nobody thought about,
    // and the gate cannot tell them apart either.
    public static IReadOnlyList<Entry> All { get; } =
    [
        new()
        {
            Id = "low-allocation-compute",
            Primary = PrimaryMetric.Throughput,
            RequiredCapabilities = HostCapabilities.None,
            SupportsLatency = true,
            DefaultTimeoutSeconds = 300,
        },
        new()
        {
            Id = "allocation-churn",
            Primary = PrimaryMetric.Throughput,
            RequiredCapabilities = HostCapabilities.None,
            SupportsLatency = true,
            DefaultTimeoutSeconds = 300,
        },
        new()
        {
            Id = "long-lived-cache",
            Primary = PrimaryMetric.Throughput,
            RequiredCapabilities = HostCapabilities.None,
            SupportsLatency = true,
            DefaultTimeoutSeconds = 420,
        },
        new()
        {
            Id = "cyclic-garbage",
            Primary = PrimaryMetric.Throughput,
            RequiredCapabilities = HostCapabilities.None,
            SupportsLatency = true,
            DefaultTimeoutSeconds = 420,
        },
        new()
        {
            Id = "pointer-chasing",
            Primary = PrimaryMetric.Throughput,
            RequiredCapabilities = HostCapabilities.None,
            SupportsLatency = true,
            DefaultTimeoutSeconds = 300,
        },
        new()
        {
            Id = "multi-thread-throughput",
            Primary = PrimaryMetric.Throughput,
            RequiredCapabilities = HostCapabilities.None,
            SupportsLatency = true,
            DefaultTimeoutSeconds = 300,
        },
        new()
        {
            Id = "aspnet-request-load",
            Primary = PrimaryMetric.Latency,
            RequiredCapabilities = HostCapabilities.AspNetCoreSharedFramework,
            SupportsLatency = true,
            DefaultTimeoutSeconds = 420,
        },
        new()
        {
            Id = "pinning-heavy-io",
            Primary = PrimaryMetric.Throughput,
            RequiredCapabilities = HostCapabilities.None,
            SupportsLatency = true,
            DefaultTimeoutSeconds = 420,
        },
        new()
        {
            Id = "lifecycle-semantics",
            Primary = PrimaryMetric.Throughput,
            RequiredCapabilities = HostCapabilities.None,
            SupportsLatency = true,
            DefaultTimeoutSeconds = 420,
        },
        new()
        {
            Id = "large-object-pressure",
            Primary = PrimaryMetric.Throughput,
            RequiredCapabilities = HostCapabilities.None,
            SupportsLatency = true,
            DefaultTimeoutSeconds = 600,
        },
    ];

    public static IReadOnlyList<string> Ids
    {
        get
        {
            var ids = new List<string>(All.Count);
            foreach (Entry entry in All)
            {
                ids.Add(entry.Id);
            }

            return ids;
        }
    }

    public static Entry Get(string id) =>
        Find(id) ?? throw new ArgumentException($"Unknown scenario '{id}'.", nameof(id));

    public static Entry? Find(string id)
    {
        foreach (Entry entry in All)
        {
            if (string.Equals(entry.Id, id, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    /// <summary>
    /// Scenarios that run on a host without the ASP.NET Core shared framework. The gate requires this
    /// to be non-empty for every host, so a future LXR arm can never be left with no evidence at all
    /// just because the flagship scenario cannot reach it.
    /// </summary>
    public static IReadOnlyList<string> RunnableOn(HostCapabilities capabilities)
    {
        var ids = new List<string>();
        foreach (Entry entry in All)
        {
            if ((entry.RequiredCapabilities & ~capabilities) == 0)
            {
                ids.Add(entry.Id);
            }
        }

        return ids;
    }
}
