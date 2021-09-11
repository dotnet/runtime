// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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
        static Test instance = new Test ();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Task<string> RunBenchmark()
        {
            return instance.RunTasks ();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void SetTasks(string taskNames)
        {
            Regex pattern;
            var names = taskNames.Split(',');
            var tasksList = new List<BenchTask>();

            for (int i = 0; i < names.Length; i++)
            {
                var idx = names[i].IndexOf(':');
                string name;

                if (idx == -1) {
                    name = names[i];
                    pattern = null;
                }
                else
                {
                    name = names[i].Substring(0, idx);
                    pattern = new Regex (names[i][(idx + 1)..]);
                }

                var taskType = Type.GetType($"Sample.{name}Task");
                if (taskType == null)
                    continue;

                var task = (BenchTask)Activator.CreateInstance(taskType);
                task.pattern = pattern;
                tasksList.Add (task);
            }

            instance.tasks = tasksList.ToArray ();
        }

        int taskCounter = 0;
        int measurementIdx = 0;
        int runIdx = 0;
        BenchTask task;
        BenchTask Task
        {
            get { return task; }
            set { task = value; }
        }
        List<BenchTask.Result> results = new();
        bool resultsReturned;

        bool NextTask ()
        {
            bool hasMeasurement;
            do {
                if (taskCounter == tasks.Length)
                    return false;

                Task = tasks[taskCounter];
                measurementIdx = -1;
                hasMeasurement = NextMeasurement();

                if (hasMeasurement)
                    task.Initialize();

                taskCounter++;
            } while (!hasMeasurement);

            return true;
        }

        bool NextMeasurement ()
        {
            runIdx = 0;

            while (measurementIdx < Task.Measurements.Length - 1)
            {
                measurementIdx++;

                if (Task.pattern == null || Task.pattern.Match(Task.Measurements[measurementIdx].Name).Success)
                    return true;
            }

            measurementIdx = -1;

            return false;
        }

        public async Task<string> RunTasks()
        {
            if (resultsReturned)
                return "";

            if (taskCounter == 0) {
                NextTask ();
                return "Benchmark started<br>";
            }

            if (measurementIdx == -1)
                return ResultsSummary();

            if (runIdx >= Task.Measurements [measurementIdx].NumberOfRuns && !NextMeasurement() && !NextTask ())
                    return ResultsSummary();

            runIdx++;

            return await Task.RunBatch(results, measurementIdx);
        }

        string ResultsSummary ()
        {
            Dictionary<string, double> minTimes = new Dictionary<string, double> ();
            StringBuilder sb = new();

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

            resultsReturned = true;

            return sb.ToString();
        }
    }
}
