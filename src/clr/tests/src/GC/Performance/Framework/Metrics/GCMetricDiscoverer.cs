// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if WINDOWS

using GCPerfTestFramework.Metrics.Builders;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Xunit.Performance;
using Microsoft.Xunit.Performance.Sdk;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit.Abstractions;

namespace GCPerfTestFramework.Metrics
{
    /// <summary>
    /// GCMetricDiscoverer is one of two publicly-exposed classes from the library and is the
    /// portion of this library that speaks directly to xunit-performance. When a
    /// <see cref="CollectGCMetricsAttribute"/> is observed when xunit-performance is enumerating
    /// the attributes on a test method, it instantiates an instance of this class and calls
    /// GetMetrics on it, which yields the list of metrics that this library provides.
    /// 
    /// This class and <see cref="CollectGCMetricsAttribute"/> should be the *only* classes
    /// exposed by this namespace.
    /// </summary>
    public class GCMetricDiscoverer : IPerformanceMetricDiscoverer
    {
        /// <summary>
        /// Yields all current custom GC metrics.
        /// </summary>
        /// <param name="metricAttribute">Unused.</param>
        /// <returns>An enumerator yielding new instances of all of the existing custom GC metrics.</returns>
        public IEnumerable<PerformanceMetricInfo> GetMetrics(IAttributeInfo metricAttribute)
        {
            yield return new GCMaxPauseMetric();
            yield return new GCMeanPauseMetric();
            yield return new GCPeakVirtualMemoryMetric();
            yield return new GCPeakWorkingSetMetric();
            yield return new GCTotalPauseTimeMetric();
            yield return new GCCpuTimeInGCMetric();
            yield return new GCGenZeroMeanPauseDuration();
            yield return new GCGenOneMeanPauseDuration();
            yield return new GCGenTwoMeanPauseDuration();
            yield return new GCGenZeroCount();
            yield return new GCGenOneCount();
            yield return new GCGenTwoBGCCount();
            yield return new GCGenTwoGCCount();
        }
    }
    
    /// <summary>
    /// Base class for all GC-related metrics that handles provider registration for child metrics, since
    /// all GC-related metrics will be listening to the same trace providers.
    /// </summary>
    internal abstract class GCMetric : PerformanceMetric
    {
        /// <summary>
        /// Number of bytes in a megabyte, for convenience.
        /// </summary>
        public const int BytesInMegabyte = 1048576;

        /// <summary>
        /// Creates a new GCMetric with the given ID, display name, and unit.
        /// </summary>
        /// <param name="id">The ID of the metric</param>
        /// <param name="displayName">A human-friendly display name of the metric</param>
        /// <param name="unit">The unit of the metric</param>
        public GCMetric(string id, string displayName, string unit)
            : base(id, displayName, unit)
        {
        }

        /// <summary>
        /// Indicates to xunit-performance what trace providers that these metrics
        /// require.
        /// </summary>
        public override IEnumerable<ProviderInfo> ProviderInfo
        {
            get
            {
                yield return new KernelProviderInfo()
                {
                    Keywords = (ulong)(KernelTraceEventParser.Keywords.ContextSwitch
                        | KernelTraceEventParser.Keywords.Profile
                        | KernelTraceEventParser.Keywords.ProcessCounters)
                };
                yield return new UserProviderInfo()
                {
                    ProviderGuid = ClrTraceEventParser.ProviderGuid,
                    Level = TraceEventLevel.Verbose,
                    Keywords = (ulong)ClrTraceEventParser.Keywords.GC
                };
            }
        }

        /// <summary>
        /// Constructs a new PerformanceMetricEvaluator for this metric. Implementors of a custom metric must override
        /// this method and instruct it to instantiate the GCEvaluator for that custom metric.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public abstract override PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context);
    }

    /// <summary>
    /// Base class for all GC-related metric evaluators that handles the complexity of multiplexing possibly many
    /// GC metrics on top of a single "trace session" using a reference-counting strategy.
    /// </summary>
    internal abstract class GCEvaluator : PerformanceMetricEvaluator
    {
        /// <summary>
        /// The sample rate used by xunit-performance when collecting ETW traces. Used
        /// to infer the total time spent in GC based on CPU samples.
        /// </summary>
        const float SampleRate = 1.0f;

        // These three fields are part of a bit of a hack to avoid having to re-parse the ETL file
        // every time a new metric is evaluated.
        //
        // The idea here is that every class that derives from GCEvaluator increments the
        // reference count whenever an iteration begins and decrements it whenever an iteration ends.
        // When the reference count is zero, the session is nulled out for the next iteration.
        // If _session is null when an iteration begins, the first metric to reach it will set it up
        // to trace the session. In this way, the first metric in sets up the session and the last one
        // out tears it down in preparation for the next iteration.
        //
        // This scheme is not thread-safe and will break if xunit-performance ever runs benchmarks in
        // parallel, although that's pretty unlikely for a benchmarking framework.
        private static IDictionary<int, GCProcess> s_session;
        private static int s_sessionRefCount;
        private static bool s_hasComputedRollup;

        private readonly PerformanceMetricEvaluationContext _context;

        /// <summary>
        /// Property exposed to child metrics that automatically ensures that the session is valid and that
        /// rollup information has been calculated, calculating it if it has not happened already.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this property is unable to determine an
        /// appropriate process for analysis. Usually this occurs when
        /// the test framework itself crashes and fails to launch a test.
        /// </exception>
        protected GCProcess ProcessInfo
        {
            get
            {
                if (!s_hasComputedRollup)
                {
                    GCProcess.ComputeRollup(s_session);
                    s_hasComputedRollup = true;
                }

                // Since we are spawning this process with UseShellExecute set to false,
                // the spawned process itself spawns an instance of "conhost.exe" on Windows.
                // We want to be sure we don't pick out that one for analysis.
                foreach (var candidate in s_session.Values)
                {
                    if (candidate.CommandLine != null)
                    {
                        if (!candidate.CommandLine.Contains("conhost.exe"))
                        {
                            return candidate;
                        }
                    }
                }

                // This should never happen in GC-related tests, which are always required to spawn an additional process.
                throw new InvalidOperationException("Failed to find an appropriate target process for analysis!");
            }
        }

        /// <summary>
        /// Constructs a new GCEvaluator and sets its content to the given PerformanceMetricEvaluationContext.
        /// </summary>
        /// <param name="context">The context received from the test framework</param>
        public GCEvaluator(PerformanceMetricEvaluationContext context)
        {
            Debug.Assert(context.TraceEventSource is TraceEventDispatcher);
            _context = context;
        }

        /// <summary>
        /// Creates a session if it does not exist and increments the reference count on the session.
        /// </summary>
        /// <param name="beginEvent">Unused.</param>
        public override void BeginIteration(TraceEvent beginEvent)
        {
            if (s_session == null)
            {
                // The filter function here is to filter out events that we are not concerned with collecting, i.e. events from
                // processes not spawned by us.
                s_session = GCProcess.Collect(_context.TraceEventSource as TraceEventDispatcher, SampleRate, filterFunc: _context.IsTestEvent);
                s_hasComputedRollup = false;
            }

            s_sessionRefCount++;
        }

        /// <summary>
        /// Yields the metric and decrements the reference count on the session, disposing it
        /// if the reference count is zero.
        /// </summary>
        /// <param name="endEvent">Unused.</param>
        /// <returns>The value of the metric calculated by this class</returns>
        public override double EndIteration(TraceEvent endEvent)
        {
            var metric = YieldMetric();
            s_sessionRefCount--;
            if (s_sessionRefCount == 0)
            {
                s_session = null;

                // not doing this results in tremendous memory leaks!
                _context.TraceEventSource.Kernel.RemoveCallback<TraceEvent>(null);
                _context.TraceEventSource.Clr.RemoveCallback<TraceEvent>(null);
            }

            return metric;
        }

        /// <summary>
        /// Overriden by child metrics to determine how to yield the value of the metric
        /// that the child metric provides. In general, overriders of this method
        /// do something with the value of the <see cref="ProcessInfo"/> property.
        /// </summary>
        /// <returns>The value of this metric</returns>
        protected abstract double YieldMetric();
    }
}

#endif
