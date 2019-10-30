using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xunit.Performance.Api;

namespace JitBench
{
    public static class BenchviewResultExporter
    {
        public static void ConvertRunResult(ScenarioBenchmark scenario, BenchmarkRunResult runResult)
        {
            scenario.Tests = new List<ScenarioTestModel>();
            scenario.Tests.AddRange(ConvertRunResult(runResult));
        }

        static ScenarioTestModel[] ConvertRunResult(BenchmarkRunResult runResult)
        {
            List<ScenarioTestModel> testModels = new List<ScenarioTestModel>();
            string name = runResult.Benchmark.Name;
            List<Metric> metrics = CollectMetrics(runResult);
            foreach (Metric m in metrics.ToArray())
            {
                if(runResult.Benchmark.TryGetBenchviewCustomMetricReporting(m, out Metric newMetric, out string newScenarioModelName))
                {
                    metrics.Remove(m);
                    testModels.Add(ConvertRunResult(runResult, new Metric[] { newMetric }, oldMetric => m.Equals(oldMetric) ? newMetric : default(Metric), name, newScenarioModelName));
                }
            }
            testModels.Insert(0, ConvertRunResult(runResult, metrics, oldMetric => metrics.Contains(oldMetric) ? oldMetric : default(Metric), null, name));
            return testModels.ToArray();
        }

        static ScenarioTestModel ConvertRunResult(BenchmarkRunResult runResult, IEnumerable<Metric> metrics, Func<Metric,Metric> metricMapping, string scenarioModelNamespace, string scenarioModelName)
        {
            var testModel = new ScenarioTestModel(scenarioModelName);
            testModel.Namespace = scenarioModelNamespace;
            testModel.Performance = new PerformanceModel();
            testModel.Performance.Metrics = new List<MetricModel>();
            testModel.Performance.IterationModels = new List<IterationModel>();
            foreach (var iterationResult in runResult.IterationResults)
            {
             testModel.Performance.IterationModels.Add(ConvertIterationResult(iterationResult, metricMapping));
            }
            foreach (var metric in metrics)
            {
                testModel.Performance.Metrics.Add(new MetricModel()
                {
                    DisplayName = metric.Name,
                    Name = metric.Name,
                    Unit = metric.Unit
                });
            }
            return testModel;
        }

        static List<Metric> CollectMetrics(BenchmarkRunResult runResult)
        {
            List<Metric> metrics = new List<Metric>();
            foreach(IterationResult iterationResult in runResult.IterationResults)
            {
                foreach (KeyValuePair<Metric, double> measurement in iterationResult.Measurements)
                {
                    if (!metrics.Contains(measurement.Key))
                    {
                        metrics.Add(measurement.Key);
                    }
                }
            }
            return metrics;
        }

        /// <summary>
        /// Converts IterationResult into Benchview's IterationModel, remaping and filtering the metrics reported
        /// </summary>
        static IterationModel ConvertIterationResult(IterationResult iterationResult, Func<Metric, Metric> metricMapping)
        {
            IterationModel iterationModel = new IterationModel();
            iterationModel.Iteration = new Dictionary<string, double>();
            foreach(KeyValuePair<Metric,double> measurement in iterationResult.Measurements)
            {
                Metric finalMetric = metricMapping(measurement.Key);
                if(!finalMetric.Equals(default(Metric)))
                {
                    iterationModel.Iteration.Add(finalMetric.Name, measurement.Value);
                }
            }
            return iterationModel;
        }

        private static string GetFilePathWithoutExtension(string outputDir, string runId, ScenarioBenchmark benchmark)
        {
            return Path.Combine(outputDir, $"{runId}-{benchmark.Name}");
        }
    }
}
