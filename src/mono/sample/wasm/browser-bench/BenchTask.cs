// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public abstract class BenchTask
{
    public abstract string Name { get; }
    readonly List<Result> results = new();
    public Regex pattern;

    public virtual bool BrowserOnly => false;

    public async Task<string> RunBatch(List<Result> results, int measurementIdx, int milliseconds = -1)
    {
        var measurement = Measurements[measurementIdx];
        if (milliseconds == -1)
            milliseconds = measurement.RunLength;
        await measurement.BeforeBatch();
        var result = await measurement.RunBatch(this, milliseconds);
        results.Add(result);
        await measurement.AfterBatch();

        return result.ToString();
    }

    public virtual void Initialize() { }

    public abstract Measurement[] Measurements { get; }

    public class Result
    {
        public TimeSpan span;
        public int steps;
        public string taskName;
        public string measurementName;

        public override string ToString() => $"{taskName}, {measurementName} count: {steps}, per call: {span.TotalMilliseconds / steps}ms, total: {span.TotalSeconds}s";
    }

    public abstract class Measurement
    {
        protected int currentStep = 0;
        public abstract string Name { get; }

        public virtual int InitialSamples => 10;
        public virtual int NumberOfRuns => 5;
        public virtual int RunLength => 5000;
        public virtual Task<bool> IsEnabled() => Task.FromResult(true);

        public virtual Task BeforeBatch() { return Task.CompletedTask; }

        public virtual Task AfterBatch() { return Task.CompletedTask; }

        public virtual void RunStep() { }
        public virtual async Task RunStepAsync() { await Task.CompletedTask; }

        public virtual bool HasRunStepAsync => false;

        protected virtual int CalculateSteps(int milliseconds, TimeSpan initTs, int initialSamples)
        {
            return (int)(milliseconds * initialSamples / Math.Max(1.0, initTs.TotalMilliseconds));
        }

        public async Task<Result> RunBatch(BenchTask task, int milliseconds)
        {
            DateTime start = DateTime.Now;
            DateTime end;
            int initialSamples = InitialSamples;
            try
            {
                // run one to eliminate possible startup overhead and do GC collection
                if (HasRunStepAsync)
                    await RunStepAsync();
                else
                    RunStep();

                GC.Collect();

                start = DateTime.Now;
                if (HasRunStepAsync)
                    await RunStepAsync();
                else
                    RunStep();
                end = DateTime.Now;

                // try to limit initial samples to 1s
                var oneTs = end - start;
                var maxInitMs = 1000;
                if (oneTs.TotalMilliseconds > 0 && oneTs.TotalMilliseconds*InitialSamples > maxInitMs)
                    initialSamples = (int)(maxInitMs/oneTs.TotalMilliseconds);

                if (initialSamples > 1) {
                    GC.Collect();

                    start = DateTime.Now;
                    for (currentStep = 0; currentStep < initialSamples; currentStep++)
                        if (HasRunStepAsync)
                            await RunStepAsync();
                        else
                            RunStep();
                    end = DateTime.Now;
                } else {
                    // we already have the 1st measurement
                    initialSamples = 1;
                }

                var initTs = end - start;
                int steps = CalculateSteps(milliseconds, initTs, initialSamples);

                start = DateTime.Now;
                for (currentStep = 0; currentStep < steps; currentStep++)
                {
                    if (HasRunStepAsync)
                        await RunStepAsync();
                    else
                        RunStep();
                }
                end = DateTime.Now;

                var ts = end - start;

                return new Result { span = ts + initTs, steps = steps + initialSamples, taskName = task.Name, measurementName = Name };
            }
            catch (Exception ex)
            {
                end = DateTime.Now;
                var ts = end - start;
                Console.WriteLine(ex);
                return new Result { span = ts, steps = currentStep + initialSamples, taskName = task.Name, measurementName = Name + " " + ex.Message };
            }
        }
    }
}
