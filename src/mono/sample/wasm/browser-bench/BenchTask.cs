// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

abstract class BenchTask
{
    public abstract string Name { get; }
    readonly List<Result> results = new();
    public Regex pattern;

    public string RunBatch(List<Result> results, int measurementIdx, int milliseconds = 5000)
    {
        var result = Measurements[measurementIdx].RunBatch(this, milliseconds);
        results.Add(result);

        return result.ToString ();
    }

    public virtual void Initialize() { }

    public abstract Measurement[] Measurements { get; }

    public class Result
    {
        public TimeSpan span;
        public int steps;
        public string taskName;
        public string measurementName;

        public override string ToString() => $"{taskName}, {measurementName} count: {steps} per call: {span.TotalMilliseconds/steps}ms total: {span.TotalSeconds}s";
    }

    public abstract class Measurement {
        public abstract string Name { get; }

        public virtual int InitialSamples { get { return 10; } }
        public virtual int NumberOfRuns { get { return 5; } }

        public abstract void RunStep();

        public Result RunBatch(BenchTask task, int milliseconds)
        {
            DateTime start;
            DateTime end;

            // run one to eliminate possible startup overhead and do GC collection
            RunStep();
            GC.Collect();

            start = DateTime.Now;
            for (int i = 0; i < InitialSamples; i++)
                RunStep();
            end = DateTime.Now;

            var initTs = end - start;
            int steps = (int)(milliseconds * InitialSamples / Math.Max(1.0, initTs.TotalMilliseconds));

            start = DateTime.Now;
            for (int i = 0; i < steps; i++)
            {
                RunStep();
            }
            end = DateTime.Now;

            var ts = end - start;

            return new Result { span = ts + initTs, steps = steps + InitialSamples, taskName = task.Name, measurementName = Name };
        }
    }
}
