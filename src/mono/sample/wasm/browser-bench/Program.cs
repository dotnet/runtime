// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;

namespace Sample
{
    public class Test
    {
        public static void Main(string[] args)
        {
        }

        BenchTask[] tasks =
        {
            new ExceptionsTask(),
            new JsonTask ()
        };

        static Test instance;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string RunBenchmark()
        {
            if (instance == null)
                instance = new Test();

            return instance.RunTasks ();
        }

        int taskCounter = 0;
        int measurementIdx = 0;
        int runIdx = 0;
        BenchTask task;
        BenchTask Task
        {
            get { return task; }
            set { task = value; measurementIdx = 0; task.Initialize(); }
        }
        List<BenchTask.Result> results = new List<BenchTask.Result> ();
        bool resultsReturned;

        public string RunTasks()
        {
            if (resultsReturned)
                return "";

            if (taskCounter == 0) {
                taskCounter++;
                Task = tasks[0];
                return "Benchmark started<br>";
            }

            if (runIdx >= Task.Measurements [measurementIdx].NumberOfRuns)
            {
                runIdx = 0;

                measurementIdx++;
                if (measurementIdx >= Task.Measurements.Length)
                {
                    taskCounter++;

                    if (taskCounter > tasks.Length)
                    {
                        resultsReturned = true;

                        return ResultsSummary();
                    }

                    Task = tasks[taskCounter - 1];
                }

            }

            runIdx++;

            return Task.RunBatch(results, measurementIdx);
        }

        string ResultsSummary ()
        {
            Dictionary<string, double> minTimes = new Dictionary<string, double> ();
            StringBuilder sb = new StringBuilder();

            foreach (var result in results)
            {
                double t;
                var key = $"{result.taskName}, {result.measurementName}";
                t = result.span.TotalMilliseconds/result.steps;
                if (minTimes.ContainsKey(key))
                    t = Math.Min (minTimes[key], t);

                minTimes[key] = t;
            }

            sb.Append("<h4>Summary</h4>");
            foreach (var key in minTimes.Keys)
            {
                sb.Append($"{key}: {minTimes [key]}ms<br>");
            }

            sb.Append("<h4>.md</h4><tt>| measurement | time |<br>|-:|-:|<br>");
            foreach (var key in minTimes.Keys)
            {
                var time = minTimes[key];
                var unit = "ms";
                if (time < 0.001)
                {
                    time *= 1000;
                    unit = "us";
                }
                sb.Append($"| {key,32} | {time,10:F4}{unit} |<br>".Replace (" ", "&nbsp;"));
            }
            sb.Append("</tt>");

            return sb.ToString();
        }
    }
}
