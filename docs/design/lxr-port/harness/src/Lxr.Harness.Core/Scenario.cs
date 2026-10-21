// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Lxr.Harness.Core;

/// <summary>Which measurement a scenario reports as its headline number.</summary>
public enum PrimaryMetric
{
    Throughput,
    Latency,
}

/// <summary>
/// Framework capabilities a scenario needs from the host it runs on. A host that cannot supply
/// them produces a <em>declared</em> skip rather than a silent absence.
/// </summary>
[Flags]
public enum HostCapabilities
{
    None = 0,
    AspNetCoreSharedFramework = 1 << 0,
}

/// <summary>Typed, defaulted access to scenario parameters, recording every value actually used.</summary>
public sealed class ScenarioParameters
{
    private readonly Dictionary<string, string> _supplied;
    private readonly Dictionary<string, string> _effective = new(StringComparer.OrdinalIgnoreCase);

    public ScenarioParameters(IReadOnlyDictionary<string, string>? supplied = null)
        => _supplied = supplied is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(supplied, StringComparer.OrdinalIgnoreCase);

    /// <summary>Every parameter value the scenario actually read, including defaults it fell back to.</summary>
    public IReadOnlyDictionary<string, string> Effective => _effective;

    public int GetInt32(string name, int defaultValue)
    {
        int result = _supplied.TryGetValue(name, out string? raw) && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
            ? parsed
            : defaultValue;
        _effective[name] = result.ToString(CultureInfo.InvariantCulture);
        return result;
    }

    public long GetInt64(string name, long defaultValue)
    {
        long result = _supplied.TryGetValue(name, out string? raw) && long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed)
            ? parsed
            : defaultValue;
        _effective[name] = result.ToString(CultureInfo.InvariantCulture);
        return result;
    }

    public double GetDouble(string name, double defaultValue)
    {
        double result = _supplied.TryGetValue(name, out string? raw) && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
            ? parsed
            : defaultValue;
        _effective[name] = result.ToString("R", CultureInfo.InvariantCulture);
        return result;
    }

    public bool GetBoolean(string name, bool defaultValue)
    {
        bool result = _supplied.TryGetValue(name, out string? raw) && bool.TryParse(raw, out bool parsed)
            ? parsed
            : defaultValue;
        _effective[name] = result ? "true" : "false";
        return result;
    }
}

/// <summary>Everything a scenario is given at setup time.</summary>
public sealed class ScenarioContext
{
    public required int Seed { get; init; }

    public required int WorkerCount { get; init; }

    public required ScenarioParameters Parameters { get; init; }

    /// <summary>
    /// Set when the run is a control demonstration that must not be treated as measurement data.
    /// Scenarios never read this; it exists so the result record can carry it.
    /// </summary>
    public string? ControlTag { get; init; }
}

/// <summary>
/// A scenario's own statement that it did the work it claims. Exit code zero is never sufficient:
/// see control 6 in P0.4-harness.md.
/// </summary>
public sealed class ScenarioVerification
{
    public required bool Success { get; init; }

    /// <summary>A stable, scenario-specific marker string, present only when the work was really done.</summary>
    public required string Marker { get; init; }

    public string? Detail { get; init; }

    /// <summary>Semantic invariants the scenario asserts. Any entry here fails the run.</summary>
    public IReadOnlyList<string> Violations { get; init; } = Array.Empty<string>();
}

/// <summary>Static description of a scenario: what it measures, why, and how it must be driven.</summary>
public sealed class ScenarioDescriptor
{
    /// <summary>Stable kebab-case identifier, used in result records and the matrix table.</summary>
    public required string Id { get; init; }

    /// <summary>
    /// Why this scenario exists, tied to a mechanism the port must get right. A scenario without a
    /// mechanism rationale is untethered from the parity contract; the gate asserts this is present.
    /// </summary>
    public required string Rationale { get; init; }

    public required PrimaryMetric Primary { get; init; }

    public HostCapabilities RequiredCapabilities { get; init; }

    public int DefaultTimeoutSeconds { get; init; } = 300;

    public int DefaultWorkerCount { get; init; } = 1;

    public int MaxWorkerCount { get; init; } = 1;

    public double DefaultArrivalRatePerSecond { get; init; } = 2000;

    /// <summary>Below this the run did not do enough work to be a result, regardless of exit code.</summary>
    public long MinimumOperations { get; init; } = 1;

    /// <summary>
    /// A declared, <em>unmeasured</em> heap size used to derive the heap axis until P0.5 calibrates
    /// it. Reported with <c>provisional: true</c> so it is never mistaken for a measured value.
    /// </summary>
    public long ProvisionalBaselineHeapBytes { get; init; } = 64L * 1024 * 1024;

    /// <summary>
    /// Whether this scenario legitimately induces collections. P0.3 traced the hsqldb p99 anomaly to
    /// induced-collection policy, so an unexpected induced collection invalidates a run.
    /// </summary>
    public bool AllowsInducedCollections { get; init; }

    public IReadOnlyList<string> Axes { get; init; } = Array.Empty<string>();
}

/// <summary>
/// One measurable workload. Implementations must make <see cref="RunOperation"/> safe to call
/// concurrently from <see cref="ScenarioContext.WorkerCount"/> threads when
/// <see cref="ScenarioDescriptor.MaxWorkerCount"/> is greater than one; the
/// <paramref name="workerIndex"/> is provided so per-worker state can be kept in pre-sized arrays
/// rather than allocated per operation.
/// </summary>
public interface IScenario
{
    ScenarioDescriptor Describe();

    void Setup(ScenarioContext context);

    /// <summary>
    /// Executes exactly one operation and returns a value folded into the run checksum. The return
    /// value must depend on the work performed so that skipped work is detectable.
    /// </summary>
    long RunOperation(int workerIndex);

    ScenarioVerification Verify();

    void Teardown();
}
