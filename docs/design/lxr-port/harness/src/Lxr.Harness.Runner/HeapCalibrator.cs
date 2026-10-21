// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Lxr.Harness.Core;

namespace Lxr.Harness.Runner;

/// <summary>
/// Measures each scenario's minimum viable heap by bisection, per collector arm.
/// </summary>
/// <remarks>
/// <para>P0.4-harness.md section 2.1 and section 11 both state that this mechanism ships. It did not:
/// the verb did not exist and no code implemented it, which P0.5 found on first use. The heap axis
/// depends on it entirely - a heap <em>factor</em> is a multiple of a minimum heap, so without a
/// measured minimum the factor multiplies a guess and the resulting number cannot be compared with the
/// paper's, whose whole point is that the comparison inverts across that axis.</para>
///
/// <para><strong>The viability rule is declared, not inferred.</strong> A heap is viable for an arm when
/// <see cref="CalibrationOptions.ConsecutiveSuccesses"/> consecutive invocations, each at the same
/// warmup and steady state the measurement itself will use, all produce a valid record: the success
/// marker, the operation-count floor, the checksum, a confirmed collector and no out-of-memory failure.
/// Calibrating at a shorter duration than the measurement would find a heap that survives the probe and
/// not the run - the live set of several of these scenarios is still growing at three seconds - so the
/// duration is deliberately not a separate knob.</para>
///
/// <para>Bisection assumes monotonicity: that if a heap of <c>n</c> MiB works then <c>n+1</c> does too.
/// That assumption is false in general for a generational collector, and the trace is written precisely
/// so the assumption is checkable rather than trusted. When the upper bracket itself fails the scenario
/// is reported as <em>not calibrated</em>, with its probes, rather than being assigned the largest value
/// tried - an uncalibrated scenario must be visible as such.</para>
/// </remarks>
public sealed class HeapCalibrator
{
    private readonly RunnerOptions _options;
    private readonly MatrixRunner _runner;
    private readonly HostDescriptor _host;

    public HeapCalibrator(RunnerOptions options, MatrixRunner runner, HostDescriptor host)
    {
        _options = options;
        _runner = runner;
        _host = host;
    }

    public CalibrationTrace Run(IReadOnlyList<string> scenarios, IReadOnlyList<CollectorArm> arms)
    {
        ArgumentNullException.ThrowIfNull(scenarios);
        ArgumentNullException.ThrowIfNull(arms);

        var trace = new CalibrationTrace
        {
            RunId = _options.RunId,
            Host = _host.Id,
            ViabilityRule = ViabilityRule,
            WarmupSeconds = _options.WarmupSeconds,
            SteadyStateSeconds = _options.SteadyStateSeconds,
        };

        foreach (string scenario in scenarios)
        {
            ScenarioCatalog.Entry entry = ScenarioCatalog.Get(scenario);
            if ((entry.RequiredCapabilities & ~_host.Capabilities) != 0)
            {
                trace.Baselines.Add(new HeapBaseline
                {
                    Scenario = scenario,
                    WorkstationMinimumMb = HeapBaselines.Fallback.WorkstationMinimumMb,
                    ServerMinimumMb = HeapBaselines.Fallback.ServerMinimumMb,
                    Provisional = true,
                    Source = $"not calibrated on host '{_host.Id}'",
                    Note = "declared skip: the host lacks a capability this scenario requires, so no invocation could run at any heap size",
                });
                Console.WriteLine($"{scenario}: SKIPPED - host lacks a required capability");
                continue;
            }

            long? workstation = null;
            long? server = null;
            var notes = new List<string>();

            foreach (CollectorArm arm in arms)
            {
                (long? minimum, string note) = Bisect(scenario, arm, trace);
                if (string.Equals(arm.Id, CollectorArms.ServerId, StringComparison.Ordinal) ||
                    arm.Id.StartsWith("srv", StringComparison.Ordinal))
                {
                    server = minimum;
                }
                else
                {
                    workstation = minimum;
                }

                notes.Add($"{arm.Id}: {note}");
            }

            bool complete = workstation is not null && server is not null;
            trace.Baselines.Add(new HeapBaseline
            {
                Scenario = scenario,
                WorkstationMinimumMb = workstation ?? HeapBaselines.Fallback.WorkstationMinimumMb,
                ServerMinimumMb = server ?? HeapBaselines.Fallback.ServerMinimumMb,

                // An arm that did not converge leaves the whole scenario provisional. Publishing a
                // measured value for one arm and a fallback for the other under a single "measured"
                // flag would be the more comfortable choice and the wrong one: the shared minimum is
                // the maximum of the two, so one unmeasured arm makes the shared value unmeasured.
                Provisional = !complete,
                Source = complete
                    ? $"calibrated by run {_options.RunId} on host {_host.Id}"
                    : $"INCOMPLETE calibration in run {_options.RunId} on host {_host.Id}",
                Note = string.Join("; ", notes),
            });

            Console.WriteLine($"{scenario}: {HeapBaselines.Describe(trace.Baselines[^1])}");
        }

        return trace;
    }

    private string ViabilityRule =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{_options.CalibrationSuccesses} consecutive invocations at {_options.WarmupSeconds:F1} s warmup and " +
            $"{_options.SteadyStateSeconds:F1} s steady state - the measurement's own durations - each producing a valid " +
            $"record (success marker, operation floor, checksum, confirmed collector, no OOM). Bisection over " +
            $"[{_options.CalibrationFloorMb}, {_options.CalibrationCeilingMb}] MiB, stopping when the bracket is within " +
            $"{_options.CalibrationToleranceMb} MiB.");

    private (long? Minimum, string Note) Bisect(string scenario, CollectorArm arm, CalibrationTrace trace)
    {
        long low = _options.CalibrationFloorMb;
        long high = _options.CalibrationCeilingMb;

        // The ceiling must be shown to work before anything below it means anything. A bisection whose
        // upper bracket was never tested can only ever return the ceiling, and would report a heap that
        // does not work as the minimum that does.
        (bool ceilingViable, string ceilingDetail) = Probe(scenario, arm, high, trace);
        if (!ceilingViable)
        {
            Console.WriteLine($"  {scenario}/{arm.Id}: ceiling {high} MiB is NOT viable ({ceilingDetail}); not calibrated");
            return (null, $"did not converge - the {high} MiB ceiling itself failed ({ceilingDetail})");
        }

        // If the floor works there is nothing to bisect: the minimum is at or below the smallest size
        // this calibration is willing to try, and saying so is more honest than reporting the floor as
        // if it had been bracketed.
        (bool floorViable, string floorDetail) = Probe(scenario, arm, low, trace);
        if (floorViable)
        {
            Console.WriteLine($"  {scenario}/{arm.Id}: floor {low} MiB is already viable; minimum is at or below it");
            return (low, $"minimum is at or below the {low} MiB floor ({floorDetail})");
        }

        long viable = high;
        long infeasible = low;
        while (viable - infeasible > _options.CalibrationToleranceMb)
        {
            long midpoint = infeasible + ((viable - infeasible) / 2);
            if (midpoint <= infeasible || midpoint >= viable)
            {
                break;
            }

            (bool ok, _) = Probe(scenario, arm, midpoint, trace);
            if (ok)
            {
                viable = midpoint;
            }
            else
            {
                infeasible = midpoint;
            }
        }

        Console.WriteLine($"  {scenario}/{arm.Id}: minimum {viable} MiB (largest failure {infeasible} MiB)");
        return (viable, $"bisected to {viable} MiB with the largest observed failure at {infeasible} MiB");
    }

    private (bool Viable, string Detail) Probe(string scenario, CollectorArm arm, long limitMb, CalibrationTrace trace)
    {
        var cell = new MatrixCell
        {
            Scenario = scenario,
            Arm = arm,
            Host = _host,
            Primary = ScenarioCatalog.Get(scenario).Primary,
            HeapLimitMb = limitMb,
            Invocations = _options.CalibrationSuccesses,
            TimeoutSeconds = _options.TimeoutFor(ScenarioCatalog.Get(scenario).DefaultTimeoutSeconds),
            Tag = "cal",
        };

        int succeeded = 0;
        double wall = 0;
        string detail = "all invocations valid";
        for (int attempt = 0; attempt < _options.CalibrationSuccesses; attempt++)
        {
            InvocationOutcome outcome = _runner.LaunchCell(cell, attempt);
            wall += outcome.WallSeconds;
            if (outcome.Valid)
            {
                succeeded++;
                continue;
            }

            // Stopping at the first failure is deliberate. The rule is "k consecutive successes", so one
            // failure has already decided the answer, and continuing would spend minutes per probe to
            // reach a conclusion that is already known.
            detail = $"invocation {attempt} {outcome.Status}" +
                (ReadInvalidReason(outcome) is string reason ? $" ({reason})" : string.Empty) +
                (outcome.MarkerSeen ? string.Empty : ", no success marker") +
                SummariseFailure(outcome);
            break;
        }

        bool viable = succeeded == _options.CalibrationSuccesses;
        trace.Probes.Add(new CalibrationProbe
        {
            Scenario = scenario,
            Arm = arm.Id,
            LimitMb = limitMb,
            Viable = viable,
            AttemptsRequired = _options.CalibrationSuccesses,
            AttemptsSucceeded = succeeded,
            Detail = detail,
            WallSeconds = wall,
        });

        Console.WriteLine($"    probe {scenario}/{arm.Id} at {limitMb} MiB: {(viable ? "viable" : "NOT viable")} ({detail})");
        return (viable, detail);
    }

    /// <summary>
    /// Distinguishes the two ways a too-small heap presents, because they mean different things and a
    /// calibration that conflates them would silently treat a broken host as a tight heap.
    /// </summary>
    private static string SummariseFailure(InvocationOutcome outcome)
    {
        string text = outcome.StdErr;
        if (text.Length == 0)
        {
            return string.Empty;
        }

        if (text.Contains("OutOfMemoryException", StringComparison.Ordinal))
        {
            return ", managed OutOfMemoryException - the heap limit was reached by the scenario";
        }

        if (text.Contains("Failed to create CoreCLR", StringComparison.Ordinal) ||
            text.Contains("HRESULT: 0x8007000E", StringComparison.Ordinal) ||
            text.Contains("Failed to initialize", StringComparison.Ordinal))
        {
            return ", the runtime refused to start at this limit - a different failure from the scenario running out of room";
        }

        return string.Empty;
    }

    private static string? ReadInvalidReason(InvocationOutcome outcome) =>
        outcome.Report is JsonElement report &&
        report.TryGetProperty("invalidReason", out JsonElement reason) &&
        reason.ValueKind is JsonValueKind.String
            ? reason.GetString()
            : null;
}
