// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Lxr.Harness.Core;

/// <summary>
/// Command line accepted by the worker processes. Kept in one place because the runner constructs it
/// and both workers parse it, and a divergence between the two would be invisible.
/// </summary>
public sealed class WorkerOptions
{
    public string Scenario { get; private set; } = string.Empty;

    public string Arm { get; private set; } = CollectorArms.WorkstationId;

    public int ServerHeapCount { get; private set; } = 8;

    public int Seed { get; private set; } = 20221101;

    public int WorkerCount { get; private set; } = 1;

    public double WarmupSeconds { get; private set; } = 2.0;

    public double SteadyStateSeconds { get; private set; } = 5.0;

    public string Mode { get; private set; } = "throughput";

    public double ArrivalRatePerSecond { get; private set; } = 1000;

    public ArrivalDistribution Distribution { get; private set; } = ArrivalDistribution.Poisson;

    public string? OutputPath { get; private set; }

    public string? SamplesPath { get; private set; }

    public double? HeapFactor { get; private set; }

    public long? HeapLimitMb { get; private set; }

    public Dictionary<string, string> Parameters { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The configuration the runner asked for, so the worker can compare it with what it observes.</summary>
    public Dictionary<string, string> RequestedConfig { get; } = new(StringComparer.Ordinal);

    // ---- control injections; each exists to make a specific control demonstrably fire ----

    /// <summary>Control 2: stall duration, in milliseconds.</summary>
    public double InjectStallMs { get; private set; }

    /// <summary>Control 2: stall period, in seconds.</summary>
    public double InjectStallEverySeconds { get; private set; } = 5.0;

    /// <summary>Control 7: every Nth operation is done twice, a slowdown of exactly 1/N.</summary>
    public int InjectExtraWorkEveryNth { get; private set; }

    /// <summary>Control 4: sleep past the timeout instead of running.</summary>
    public bool Hang { get; private set; }

    /// <summary>Control 5: fail fatally so a dump is produced.</summary>
    public bool Crash { get; private set; }

    /// <summary>Control 6: exit zero having done nothing.</summary>
    public bool FakeSuccess { get; private set; }

    /// <summary>Control 6: do only this fraction of the work and still exit zero.</summary>
    public double PartialWorkFraction { get; private set; } = 1.0;

    /// <summary>Marks the run as a control demonstration rather than measurement data.</summary>
    public string? ControlTag { get; private set; }

    public static WorkerOptions Parse(string[] args)
    {
        var options = new WorkerOptions();
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            switch (arg)
            {
                case "--scenario":
                    options.Scenario = Next(args, ref i);
                    break;
                case "--arm":
                    options.Arm = Next(args, ref i);
                    break;
                case "--server-heap-count":
                    options.ServerHeapCount = int.Parse(Next(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--seed":
                    options.Seed = int.Parse(Next(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--workers":
                    options.WorkerCount = int.Parse(Next(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--warmup-seconds":
                    options.WarmupSeconds = double.Parse(Next(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--duration-seconds":
                    options.SteadyStateSeconds = double.Parse(Next(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--mode":
                    options.Mode = Next(args, ref i);
                    break;
                case "--rate":
                    options.ArrivalRatePerSecond = double.Parse(Next(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--arrival":
                    options.Distribution = Next(args, ref i).Equals("uniform", StringComparison.OrdinalIgnoreCase)
                        ? ArrivalDistribution.Uniform
                        : ArrivalDistribution.Poisson;
                    break;
                case "--output":
                    options.OutputPath = Next(args, ref i);
                    break;
                case "--samples":
                    options.SamplesPath = Next(args, ref i);
                    break;
                case "--heap-factor":
                    options.HeapFactor = double.Parse(Next(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--heap-limit-mb":
                    options.HeapLimitMb = long.Parse(Next(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--param":
                    AddPair(options.Parameters, Next(args, ref i));
                    break;
                case "--requested":
                    AddPair(options.RequestedConfig, Next(args, ref i));
                    break;
                case "--inject-stall-ms":
                    options.InjectStallMs = double.Parse(Next(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--inject-stall-every-seconds":
                    options.InjectStallEverySeconds = double.Parse(Next(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--inject-extra-work-every":
                    options.InjectExtraWorkEveryNth = int.Parse(Next(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--hang":
                    options.Hang = true;
                    break;
                case "--crash":
                    options.Crash = true;
                    break;
                case "--fake-success":
                    options.FakeSuccess = true;
                    break;
                case "--partial-work":
                    options.PartialWorkFraction = double.Parse(Next(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--control-tag":
                    options.ControlTag = Next(args, ref i);
                    break;
                default:
                    throw new ArgumentException($"Unrecognised argument '{arg}'.");
            }
        }

        if (string.IsNullOrEmpty(options.Scenario))
        {
            throw new ArgumentException("--scenario is required.");
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

    private static void AddPair(Dictionary<string, string> target, string pair)
    {
        int equals = pair.IndexOf('=', StringComparison.Ordinal);
        if (equals <= 0)
        {
            throw new ArgumentException($"Expected key=value, got '{pair}'.");
        }

        target[pair[..equals]] = pair[(equals + 1)..];
    }
}
