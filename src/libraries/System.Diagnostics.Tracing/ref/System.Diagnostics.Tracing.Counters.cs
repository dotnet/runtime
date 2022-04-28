// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.Tracing
{
#if !FEATURE_WASM_PERFTRACING
    [System.Runtime.Versioning.UnsupportedOSPlatform("browser")]
#endif
    public abstract partial class DiagnosticCounter : System.IDisposable
    {
        internal DiagnosticCounter() { }
        public string DisplayName { get { throw null; } set { } }
        public string DisplayUnits { get { throw null; } set { } }
        public System.Diagnostics.Tracing.EventSource EventSource { get { throw null; } }
        public string Name { get { throw null; } }
        public void AddMetadata(string key, string? value) { }
        public void Dispose() { }
    }
#if !FEATURE_WASM_PERFTRACING
    [System.Runtime.Versioning.UnsupportedOSPlatform("browser")]
#endif
    public partial class PollingCounter : System.Diagnostics.Tracing.DiagnosticCounter
    {
        public PollingCounter(string name, System.Diagnostics.Tracing.EventSource eventSource, System.Func<double> metricProvider) { }
        public override string ToString() { throw null; }
    }
#if !FEATURE_WASM_PERFTRACING
    [System.Runtime.Versioning.UnsupportedOSPlatform("browser")]
#endif
    public partial class IncrementingEventCounter : System.Diagnostics.Tracing.DiagnosticCounter
    {
        public IncrementingEventCounter(string name, System.Diagnostics.Tracing.EventSource eventSource) { }
        public System.TimeSpan DisplayRateTimeScale { get { throw null; } set { } }
        public void Increment(double increment = 1) { }
        public override string ToString() { throw null; }
    }
#if !FEATURE_WASM_PERFTRACING
    [System.Runtime.Versioning.UnsupportedOSPlatform("browser")]
#endif
    public partial class IncrementingPollingCounter : System.Diagnostics.Tracing.DiagnosticCounter
    {
        public IncrementingPollingCounter(string name, System.Diagnostics.Tracing.EventSource eventSource, System.Func<double> totalValueProvider) { }
        public System.TimeSpan DisplayRateTimeScale { get { throw null; } set { } }
        public override string ToString() { throw null; }
    }
#if !FEATURE_WASM_PERFTRACING
    [System.Runtime.Versioning.UnsupportedOSPlatform("browser")]
#endif
    public partial class EventCounter : System.Diagnostics.Tracing.DiagnosticCounter
    {
        public EventCounter(string name, System.Diagnostics.Tracing.EventSource eventSource) { }
        public override string ToString() { throw null; }
        public void WriteMetric(double value) { }
        public void WriteMetric(float value) { }
    }
}
