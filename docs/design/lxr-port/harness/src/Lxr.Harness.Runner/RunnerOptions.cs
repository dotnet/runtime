// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Lxr.Harness.Core;

namespace Lxr.Harness.Runner;

/// <summary>Command line for the orchestrator.</summary>
public sealed class RunnerOptions
{
    public string Command { get; private set; } = "matrix";

    public string? RepoRoot { get; private set; }

    public string? BuildRoot { get; private set; }

    public string? OutputRoot { get; private set; }

    public List<string> Scenarios { get; } = [];

    public List<string> Arms { get; } = [];

    public List<string> Hosts { get; } = [];

    public List<double> HeapFactors { get; } = [];

    public int Invocations { get; private set; } = 3;

    public int Seed { get; private set; } = 20221101;

    public int ServerHeapCount { get; private set; } = 8;

    public double WarmupSeconds { get; private set; } = 1.0;

    public double SteadyStateSeconds { get; private set; } = 3.0;

    public int Workers { get; private set; } = 2;

    public string Mode { get; private set; } = "throughput";

    public double ArrivalRatePerSecond { get; private set; } = 1000;

    public long DumpBudgetMb { get; private set; } = 512;

    public int TimeoutMultiplierPercent { get; private set; } = 100;

    public double NoiseThresholdPercent { get; private set; } = 70;

    public string? BaselineArm { get; private set; }

    public string RunId { get; private set; } = "smoke-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);

    public string? ConformanceInput { get; private set; }

    public string? Control { get; private set; }

    public bool KeepSamples { get; private set; } = true;

    // ---- P0.5: heap calibration ----

    /// <summary>
    /// Consecutive valid invocations a heap size must produce before it counts as viable. One success
    /// is not a measurement of a minimum heap: near the edge, whether a run survives depends on where
    /// allocation happened to land, so a single pass would find a heap that works sometimes.
    /// </summary>
    public int CalibrationSuccesses { get; private set; } = 2;

    public long CalibrationFloorMb { get; private set; } = 16;

    public long CalibrationCeilingMb { get; private set; } = 4096;

    /// <summary>
    /// The bracket width at which bisection stops. Finer than this is spurious precision: the minimum
    /// heap is not a sharp threshold, and pretending to resolve it to the megabyte would cost
    /// invocations to publish a digit that does not reproduce.
    /// </summary>
    public long CalibrationToleranceMb { get; private set; } = 8;

    /// <summary>Where the calibration file is read from and written to.</summary>
    public string? CalibrationPath { get; private set; }

    // ---- P0.5: per-scenario arrival rates ----

    /// <summary>
    /// Arrival rate per scenario for the open-loop pass, overriding <see cref="ArrivalRatePerSecond"/>.
    /// One global rate cannot suit both a scenario that sustains millions of operations a second and one
    /// that sustains hundreds; the fast scenario is then measured far below capacity while the slow one
    /// is measured in overload, where the number produced is the capacity limit rather than a latency.
    /// </summary>
    public Dictionary<string, double> ScenarioRates { get; } = new(StringComparer.Ordinal);

    /// <summary>A throughput results file the per-scenario rates are derived from.</summary>
    public string? RateSourcePath { get; private set; }

    /// <summary>
    /// Upper bound on any derived arrival rate, in operations per second. The open-loop dispatcher has
    /// a capacity of its own, below the closed-loop capacity of the fastest scenarios; asking for more
    /// produces a run whose latency is dispatcher backlog. Null leaves derived rates uncapped.
    /// </summary>
    public double? RateCapPerSecond { get; private set; }

    /// <summary>
    /// The declared fraction of measured capacity the open-loop pass offers. Fixed in advance rather
    /// than tuned until nothing reports <c>overloaded</c>, which would be choosing the offered load from
    /// the result it produces.
    /// </summary>
    public double RateFractionOfCapacity { get; private set; } = 0.5;

    // ---- P0.5: control 7 blind-band bound ----

    /// <summary>
    /// Upper bound on the half-width of control 7's fine (barrier-scale) arm, as a fraction of the point
    /// estimate.
    /// </summary>
    /// <remarks>
    /// <para>P0.4 measured the fine arm and asserted nothing about it, because asserting that a ~2%
    /// effect is <em>resolved</em> made the control's verdict track background load: the same binary
    /// reached the target at n=8, at n=15, and not at all. That decision was right and is unchanged -
    /// this bound is not on the effect, it is on the estimator's own precision.</para>
    ///
    /// <para>The default is deliberately generous against an observed half-width of roughly 1%: far
    /// enough above ordinary noise that a rerun cannot trip it, close enough that a catastrophic loss of
    /// resolution fails rather than being published as a number nobody reads. It is the same reasoning
    /// that set the coarse arm's injection at 16.7% instead of 2%. It leaves a real blind band - a
    /// resolution regression between roughly 2% and 10% still fails no assertion - and that band is
    /// published rather than closed, because closing it would need a quieter machine, not a tighter
    /// threshold.</para>
    /// </remarks>
    public double FineHalfWidthBound { get; private set; } = 0.10;

    public static readonly string[] Commands = ["matrix", "controls", "conformance", "hosts", "calibrate", "publish"];

    // ---- P0.5: publication ----

    /// <summary>Run directories merged into a published checkpoint by the <c>publish</c> verb.</summary>
    public List<string> PublishInputs { get; } = [];

    public string? CheckpointId { get; private set; }

    public string? CheckpointDate { get; private set; }

    public string StepId { get; private set; } = "P0.5";

    public string? CheckpointNotes { get; private set; }

    // ---- P0.5: machine facts the worker cannot observe in-process ----

    /// <summary>
    /// Facts about the host that <see cref="MachineInfo.Capture"/> takes as parameters because a worker
    /// process cannot read them: the power plan, the physical core count and the system model all need a
    /// WMI query or an OS-specific call that the harness deliberately does not take a dependency on.
    /// Supplied by the operator from the machine survey, and recorded as given - a machine field left
    /// null is honest, and a fabricated one is not.
    /// </summary>
    public string? MachinePowerPlan { get; private set; }

    public int? MachinePhysicalCores { get; private set; }

    public string? MachineModel { get; private set; }

    /// <summary>
    /// Physical memory of the host, measured in the runner process rather than in the worker.
    /// </summary>
    /// <remarks>
    /// <see cref="System.GC.GetGCMemoryInfo"/>'s <c>TotalAvailableMemoryBytes</c> reports the heap hard
    /// limit when one is set, so a worker pinned to 128 MiB reports a 128 MiB machine - and reports a
    /// different machine at every heap factor. The runner runs unpinned, so its own reading is the
    /// host's memory, and it is taken once at start-up rather than per cell.
    /// </remarks>
    public long RunnerTotalMemoryBytes { get; } = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;

    public static RunnerOptions Parse(string[] args)
    {
        var options = new RunnerOptions();
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            switch (arg)
            {
                case "matrix" or "controls" or "conformance" or "hosts" or "calibrate" or "publish" or "reaggregate":
                    options.Command = arg;
                    break;
                case "--repo-root":
                    options.RepoRoot = Next(args, ref i);
                    break;
                case "--build-root":
                    options.BuildRoot = Next(args, ref i);
                    break;
                case "--output":
                    options.OutputRoot = Next(args, ref i);
                    break;
                case "--scenario":
                    options.Scenarios.AddRange(Next(args, ref i).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                    break;
                case "--arm":
                    options.Arms.AddRange(Next(args, ref i).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                    break;
                case "--host":
                    options.Hosts.AddRange(Next(args, ref i).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                    break;
                case "--heap-factor":
                    foreach (string factor in Next(args, ref i).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        options.HeapFactors.Add(double.Parse(factor, CultureInfo.InvariantCulture));
                    }

                    break;
                case "--invocations":
                    options.Invocations = int.Parse(Next(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--seed":
                    options.Seed = int.Parse(Next(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--server-heap-count":
                    options.ServerHeapCount = int.Parse(Next(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--warmup-seconds":
                    options.WarmupSeconds = double.Parse(Next(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--duration-seconds":
                    options.SteadyStateSeconds = double.Parse(Next(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--workers":
                    options.Workers = int.Parse(Next(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--mode":
                    options.Mode = Next(args, ref i);
                    break;
                case "--rate":
                    options.ArrivalRatePerSecond = double.Parse(Next(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--dump-budget-mb":
                    options.DumpBudgetMb = long.Parse(Next(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--timeout-multiplier-percent":
                    options.TimeoutMultiplierPercent = int.Parse(Next(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--noise-threshold-percent":
                    options.NoiseThresholdPercent = double.Parse(Next(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--baseline-arm":
                    options.BaselineArm = Next(args, ref i);
                    break;
                case "--run-id":
                    options.RunId = Next(args, ref i);
                    break;
                case "--input":
                    options.ConformanceInput = Next(args, ref i);
                    break;
                case "--control":
                    options.Control = Next(args, ref i);
                    break;
                case "--no-samples":
                    options.KeepSamples = false;
                    break;
                case "--calibration-successes":
                    options.CalibrationSuccesses = int.Parse(Next(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--calibration-floor-mb":
                    options.CalibrationFloorMb = long.Parse(Next(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--calibration-ceiling-mb":
                    options.CalibrationCeilingMb = long.Parse(Next(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--calibration-tolerance-mb":
                    options.CalibrationToleranceMb = long.Parse(Next(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--calibration":
                    options.CalibrationPath = Next(args, ref i);
                    break;
                case "--scenario-rate":
                    foreach (string pair in Next(args, ref i).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        int equals = pair.IndexOf('=', StringComparison.Ordinal);
                        if (equals <= 0)
                        {
                            throw new ArgumentException($"--scenario-rate expects scenario=rate, got '{pair}'.");
                        }

                        options.ScenarioRates[pair[..equals]] = double.Parse(pair[(equals + 1)..], CultureInfo.InvariantCulture);
                    }

                    break;
                case "--rate-from":
                    options.RateSourcePath = Next(args, ref i);
                    break;
                case "--rate-fraction":
                    options.RateFractionOfCapacity = double.Parse(Next(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--rate-cap":
                    options.RateCapPerSecond = double.Parse(Next(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--fine-half-width-bound":
                    options.FineHalfWidthBound = double.Parse(Next(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--publish-input":
                    options.PublishInputs.AddRange(Next(args, ref i).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                    break;
                case "--checkpoint-id":
                    options.CheckpointId = Next(args, ref i);
                    break;
                case "--checkpoint-date":
                    options.CheckpointDate = Next(args, ref i);
                    break;
                case "--step-id":
                    options.StepId = Next(args, ref i);
                    break;
                case "--checkpoint-notes":
                    options.CheckpointNotes = Next(args, ref i);
                    break;
                case "--machine-power-plan":
                    options.MachinePowerPlan = Next(args, ref i);
                    break;
                case "--machine-physical-cores":
                    options.MachinePhysicalCores = int.Parse(Next(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--machine-model":
                    options.MachineModel = Next(args, ref i);
                    break;
                default:
                    throw new ArgumentException($"Unrecognised argument '{arg}'.");
            }
        }

        return options;
    }

    private static string Next(string[] args, ref int index)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Argument '{args[index]}' requires a value.");
        }

        return args[++index];
    }

    public int TimeoutFor(int baseSeconds) =>
        Math.Max(5, (int)(baseSeconds * (TimeoutMultiplierPercent / 100.0)));
}
