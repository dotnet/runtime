namespace System.Diagnostics.Tracing
{
    public abstract partial class DiagnosticCounter : System.IDisposable
    {
        public DiagnosticCounter(string name, System.Diagnostics.Tracing.EventSource eventSource) { }
        public void AddMetadata(string key, string value) { }
        public void Dispose() { }
        public string DisplayName { get { throw null; } set { } }
        public string Name { get { throw null; } }
        public System.Diagnostics.Tracing.EventSource EventSource { get { throw null; } }
    }
    public partial class PollingCounter : System.Diagnostics.Tracing.DiagnosticCounter
    {
        public PollingCounter(string name, System.Diagnostics.Tracing.EventSource eventSource, Func<double> metricProvider) : base(name, eventSource) { }
    }
    public partial class IncrementingEventCounter : System.Diagnostics.Tracing.DiagnosticCounter
    {
        public IncrementingEventCounter(string name, System.Diagnostics.Tracing.EventSource eventSource) : base(name, eventSource) { }
        public void Increment(double increment = 1) { }
        public TimeSpan DisplayRateTimeScale { get { throw null; } set { } }
    }
    public partial class IncrementingPollingCounter : System.Diagnostics.Tracing.DiagnosticCounter
    {
        public IncrementingPollingCounter(string name, System.Diagnostics.Tracing.EventSource eventSource, Func<double> totalValueProvider) : base(name, eventSource) { }
        public TimeSpan DisplayRateTimeScale { get { throw null; } set { } }
    }    
    public partial class EventCounter : System.Diagnostics.Tracing.DiagnosticCounter
    {
        public EventCounter(string name, System.Diagnostics.Tracing.EventSource eventSource) : base(name, eventSource) { }
        public void WriteMetric(float value) { }
        public void WriteMetric(double value) { }
    }
}