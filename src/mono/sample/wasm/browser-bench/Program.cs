// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Runtime.InteropServices.JavaScript;

namespace Sample
{
    public partial class Test
    {
        static bool JsonResults = false;

        List<BenchTask> tasks = new()
        {
            new AppStartTask(),
            new ExceptionsTask(),
            new JsonTask(),
            new VectorTask(),
            new WebSocketTask()
        };
        static Test instance = new Test();
        Formatter formatter = new HTMLFormatter();

        [MethodImpl(MethodImplOptions.NoInlining)]
        [JSExport]
        public static Task<string> RunBenchmark()
        {
            return instance.RunTasks();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        // the constructors of the task we care about are already used when createing tasks field
        [UnconditionalSuppressMessage("Trim analysis error", "IL2057")]
        [UnconditionalSuppressMessage("Trim analysis error", "IL2072")]
        [JSExport]
        public static void SetTasks(string taskNames)
        {
            Regex pattern;
            var names = taskNames.Split(',');
            var tasksList = new List<BenchTask>();

            for (int i = 0; i < names.Length; i++)
            {
                var idx = names[i].IndexOf(':');
                string name;

                if (idx == -1)
                {
                    name = names[i];
                    pattern = null;
                }
                else
                {
                    name = names[i].Substring(0, idx);
                    pattern = new Regex(names[i][(idx + 1)..]);
                }

                var taskType = Type.GetType($"Sample.{name}Task");
                if (taskType == null)
                    continue;

                var task = (BenchTask)Activator.CreateInstance(taskType);
                task.pattern = pattern;
                tasksList.Add(task);
            }

            instance.tasks = tasksList;
        }

        [JSExport]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetFullJsonResults()
        {
            return instance.GetJsonResults();
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
        Dictionary<string, double> minTimes = new();
        bool resultsReturned;

        bool NextTask()
        {
            bool hasMeasurement;
            do
            {
                if (taskCounter == tasks.Count)
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

        bool NextMeasurement()
        {
            runIdx = 0;

            while (measurementIdx < Task.Measurements.Length - 1)
            {
                measurementIdx++;

                if (Task.pattern == null || Task.pattern.IsMatch(Task.Measurements[measurementIdx].Name))
                    return true;
            }

            measurementIdx = -1;

            return false;
        }

        public async Task<string> RunTasks()
        {
            if (resultsReturned)
                return "";

            if (taskCounter == 0)
            {
                NextTask();
                return $"Benchmark started{formatter.NewLine}";
            }

            if (measurementIdx == -1)
                return ResultsSummary();

            if (runIdx >= Task.Measurements[measurementIdx].NumberOfRuns && !NextMeasurement() && !NextTask())
                return ResultsSummary();

            runIdx++;

            return $"{await Task.RunBatch(results, measurementIdx)}{formatter.NewLine}";
        }

        string ResultsSummary()
        {
            ProcessResults();
            if (JsonResults)
                PrintJsonResults();

            StringBuilder sb = new($"{formatter.NewLine}Summary{formatter.NewLine}");
            foreach (var key in minTimes.Keys)
            {
                sb.Append($"{key}: {minTimes[key]}ms{formatter.NewLine}");
            }

            sb.Append($"{formatter.NewLine}.md{formatter.NewLine}{formatter.CodeStart}| measurement | time |{formatter.NewLine}|-:|-:|{formatter.NewLine}");
            foreach (var key in minTimes.Keys)
            {
                var time = minTimes[key];
                var unit = "ms";
                if (time < 0.001)
                {
                    time *= 1000;
                    unit = "us";
                }
                sb.Append($"| {key.Replace('_', ' '),38} | {time,10:F4}{unit} |{formatter.NewLine}".Replace(" ", formatter.NonBreakingSpace));
            }
            sb.Append($"{formatter.CodeEnd}");

            resultsReturned = true;

            return sb.ToString();
        }

        private void ProcessResults()
        {
            minTimes.Clear();

            foreach (var result in results)
            {
                double t;
                var key = $"{result.taskName}, {result.measurementName}";
                t = result.span.TotalMilliseconds / result.steps;
                if (minTimes.ContainsKey(key))
                    t = Math.Min(minTimes[key], t);

                minTimes[key] = t;
            }
        }

        class JsonResultsData
        {
            public List<BenchTask.Result> results;
            public Dictionary<string, double> minTimes;
            public DateTime timeStamp;
        }

        string GetJsonResults ()
        {
            var options = new JsonSerializerOptions { IncludeFields = true, WriteIndented = true };
            var jsonObject = new JsonResultsData { results = results, minTimes = minTimes, timeStamp = DateTime.UtcNow };
            return JsonSerializer.Serialize(jsonObject, options);
        }

        private void PrintJsonResults()
        {
            Console.WriteLine("=== json results start ===");
            Console.WriteLine(GetJsonResults ());
            Console.WriteLine("=== json results end ===");
        }
    }

    public abstract class Formatter
    {
        public abstract string NewLine { get; }
        public abstract string NonBreakingSpace { get; }
        public abstract string CodeStart { get; }
        public abstract string CodeEnd { get; }
    }

    public class PlainFormatter : Formatter
    {
        override public string NewLine => "\n";
        override public string NonBreakingSpace => " ";
        override public string CodeStart => "";
        override public string CodeEnd => "";
    }

    public class HTMLFormatter : Formatter
    {
        override public string NewLine => "<br/>";
        override public string NonBreakingSpace => "&nbsp;";
        override public string CodeStart => "<code>";
        override public string CodeEnd => "</code>";
    }
}
