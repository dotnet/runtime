// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if WINDOWS

using System.Linq;
using Microsoft.Xunit.Performance.Sdk;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

/// <summary>
/// This file contains a number of GC-related metrics that are provided to xunit-performance.
/// Each one of these derives from GCMetric, which manages the creation of the GC object model
/// from an ETL trace - these classes are only responsible for using it to produce a meaningful
/// metric.
/// 
/// Each one of these metrics should be fairly self-explanatory.
/// </summary>
namespace GCPerfTestFramework.Metrics
{
    #region Maximum Pause Duration
    internal class GCMaxPauseMetric : GCMetric
    {
        public GCMaxPauseMetric()
            : base("GCMaxPause", "Maximum GC Pause Duraction", PerformanceMetricUnits.Milliseconds)
        {

        }

        public override PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context)
        {
            return new GCMaxPauseEvaluator(context);
        }
    }

    internal class GCMaxPauseEvaluator : GCEvaluator
    {
        public GCMaxPauseEvaluator(PerformanceMetricEvaluationContext context)
            : base(context)
        {

        }

        protected override double YieldMetric()
        {
            return ProcessInfo.Total.MaxPauseDurationMSec;
        }
    }
    #endregion

    #region Mean Pause Duration
    internal class GCMeanPauseMetric : GCMetric
    {
        public GCMeanPauseMetric()
            : base("GCMeanPause", "Mean GC Pause Duraction", PerformanceMetricUnits.Milliseconds)
        {

        }

        public override PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context)
        {
            return new GCMeanPauseEvaluator(context);
        }
    }

    internal class GCMeanPauseEvaluator : GCEvaluator
    {
        public GCMeanPauseEvaluator(PerformanceMetricEvaluationContext context)
            : base(context)
        {

        }

        protected override double YieldMetric()
        {
            return ProcessInfo.Total.MeanPauseDurationMSec;
        }
    }
    #endregion

    #region Peak Virtual Memory Size
    internal class GCPeakVirtualMemoryMetric : GCMetric
    {
        public GCPeakVirtualMemoryMetric()
            : base("GCPeakVirtualMemory", "Process Peak Virtual Memory", PerformanceMetricUnits.Bytes)
        {

        }

        public override PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context)
        {
            return new GCPeakVirtualMemoryMetricEvaluator(context);
        }
    }

    internal class GCPeakVirtualMemoryMetricEvaluator : GCEvaluator
    {
        public GCPeakVirtualMemoryMetricEvaluator(PerformanceMetricEvaluationContext context)
            : base(context)
        {

        }

        protected override double YieldMetric()
        {
            return ProcessInfo.PeakVirtualMB * GCMetric.BytesInMegabyte;
        }
    }
    #endregion

    #region Peak Working Set Size
    internal class GCPeakWorkingSetMetric : GCMetric
    {
        public GCPeakWorkingSetMetric()
            : base("GCPeakWorkingSet", "Process Peak Working Set", PerformanceMetricUnits.Bytes)
        {

        }

        public override PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context)
        {
            return new GCPeakWorkingSetMetricEvaluator(context);
        }
    }

    internal class GCPeakWorkingSetMetricEvaluator : GCEvaluator
    {
        public GCPeakWorkingSetMetricEvaluator(PerformanceMetricEvaluationContext context)
            : base(context)
        {

        }

        protected override double YieldMetric()
        {
            return ProcessInfo.PeakWorkingSetMB * GCMetric.BytesInMegabyte;
        }
    }
    #endregion

    #region Total Pause Time
    internal class GCTotalPauseTimeMetric : GCMetric
    {
        public GCTotalPauseTimeMetric()
            : base("GCTotalPauseTime", "Total time spent paused due to GC activity", PerformanceMetricUnits.Milliseconds)
        {

        }

        public override PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context)
        {
            return new GCTotalPauseTimeMetricEvaluator(context);
        }
    }

    internal class GCTotalPauseTimeMetricEvaluator : GCEvaluator
    {
        public GCTotalPauseTimeMetricEvaluator(PerformanceMetricEvaluationContext context)
            : base(context)
        {

        }

        protected override double YieldMetric()
        {
            return ProcessInfo.Total.TotalPauseTimeMSec;
        }
    }
    #endregion

    #region CPU time in GC
    internal class GCCpuTimeInGCMetric : GCMetric
    {
        public GCCpuTimeInGCMetric()
            : base("GCCpuTimeInGC", "Total CPU time spent in GC activity", PerformanceMetricUnits.Milliseconds)
        {

        }

        public override PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context)
        {
            return new GCCpuTimeInGCMetricEvaluator(context);
        }
    }

    internal class GCCpuTimeInGCMetricEvaluator : GCEvaluator
    {
        public GCCpuTimeInGCMetricEvaluator(PerformanceMetricEvaluationContext context)
            : base(context)
        {

        }

        protected override double YieldMetric()
        {
            return ProcessInfo.Total.TotalGCCpuMSec;
        }
    }
    #endregion

    #region Generation Zero Mean Pause Duration
    internal class GCGenZeroMeanPauseDuration : GCMetric
    {
        public GCGenZeroMeanPauseDuration()
            : base("GCGenZeroMeanPauseDuration", "Mean pause duration for Gen0 collections", PerformanceMetricUnits.Count)
        {

        }

        public override PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context)
        {
            return new GCGenZeroMeanPauseDurationEvaluator(context);
        }
    }

    internal class GCGenZeroMeanPauseDurationEvaluator : GCEvaluator
    {
        public GCGenZeroMeanPauseDurationEvaluator(PerformanceMetricEvaluationContext context)
            : base(context)
        {

        }

        protected override double YieldMetric()
        {
            return ProcessInfo.Generations[0].MeanPauseDurationMSec;
        }
    }
    #endregion

    #region Generation One Mean Pause Duration
    internal class GCGenOneMeanPauseDuration : GCMetric
    {
        public GCGenOneMeanPauseDuration()
            : base("GCGenOneMeanPauseDuration", "Mean pause duration for Gen1 collections", PerformanceMetricUnits.Count)
        {

        }

        public override PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context)
        {
            return new GCGenOneMeanPauseDurationEvaluator(context);
        }
    }

    internal class GCGenOneMeanPauseDurationEvaluator : GCEvaluator
    {
        public GCGenOneMeanPauseDurationEvaluator(PerformanceMetricEvaluationContext context)
            : base(context)
        {

        }

        protected override double YieldMetric()
        {
            return ProcessInfo.Generations[1].MeanPauseDurationMSec;
        }
    }
    #endregion

    #region Generation Two Mean Pause Duration
    internal class GCGenTwoMeanPauseDuration : GCMetric
    {
        public GCGenTwoMeanPauseDuration()
            : base("GCGenTwoMeanPauseDuration", "Mean pause duration for Gen2 collections", PerformanceMetricUnits.Count)
        {

        }

        public override PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context)
        {
            return new GCGenTwoMeanPauseDurationEvaluator(context);
        }
    }

    internal class GCGenTwoMeanPauseDurationEvaluator : GCEvaluator
    {
        public GCGenTwoMeanPauseDurationEvaluator(PerformanceMetricEvaluationContext context)
            : base(context)
        {

        }

        protected override double YieldMetric()
        {
            return ProcessInfo.Generations[2].MeanPauseDurationMSec;
        }
    }
    #endregion

    #region Generation Zero GC Count
    internal class GCGenZeroCount : GCMetric
    {
        public GCGenZeroCount()
            : base("GCGenZeroCount", "Number of Generation 0 GCs", PerformanceMetricUnits.Count)
        {

        }

        public override PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context)
        {
            return new GCGenZeroCountEvaluator(context);
        }
    }

    internal class GCGenZeroCountEvaluator : GCEvaluator
    {
        public GCGenZeroCountEvaluator(PerformanceMetricEvaluationContext context)
            : base(context)
        {

        }

        protected override double YieldMetric()
        {
            return ProcessInfo.Generations[0].GCCount;
        }
    }
    #endregion

    #region Generation One GC Count
    internal class GCGenOneCount : GCMetric
    {
        public GCGenOneCount()
            : base("GCGenOneCount", "Number of Generation 1 GCs", PerformanceMetricUnits.Count)
        {

        }

        public override PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context)
        {
            return new GCGenOneCountEvaluator(context);
        }
    }

    internal class GCGenOneCountEvaluator : GCEvaluator
    {
        public GCGenOneCountEvaluator(PerformanceMetricEvaluationContext context)
            : base(context)
        {

        }

        protected override double YieldMetric()
        {
            return ProcessInfo.Generations[1].GCCount;
        }
    }
    #endregion

    #region Generation 2 Background GC Count
    internal class GCGenTwoBGCCount : GCMetric
    {
        public GCGenTwoBGCCount()
            : base("GCGenTwoBGCCount", "Number of Generation 2 background GCs", PerformanceMetricUnits.Count)
        {

        }

        public override PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context)
        {
            return new GCGenTwoBGCCountEvaluator(context);
        }
    }

    internal class GCGenTwoBGCCountEvaluator : GCEvaluator
    {
        public GCGenTwoBGCCountEvaluator(PerformanceMetricEvaluationContext context)
            : base(context)
        {

        }

        protected override double YieldMetric()
        {
            return ProcessInfo.Events.Count(e => e.Generation == 2 && e.Type == GCType.BackgroundGC);
        }
    }
    #endregion

    #region Generation 2 Blocking GC Count
    internal class GCGenTwoGCCount : GCMetric
    {
        public GCGenTwoGCCount()
            : base("GCGenTwoGCCount", "Number of Generation 2 blocking GCs", PerformanceMetricUnits.Count)
        {

        }

        public override PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context)
        {
            return new GCGenTwoGCCountEvaluator(context);
        }
    }

    internal class GCGenTwoGCCountEvaluator : GCEvaluator
    {
        public GCGenTwoGCCountEvaluator(PerformanceMetricEvaluationContext context)
            : base(context)
        {

        }

        protected override double YieldMetric()
        {
            return ProcessInfo.Events.Count(e => e.Generation == 2 && e.Type == GCType.NonConcurrentGC);
        }
    }
    #endregion
}

#endif
