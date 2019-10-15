namespace System.Diagnostics.Tracing
{
    public partial class EventCounter : System.IDisposable
    {
        public EventCounter(string name, System.Diagnostics.Tracing.EventSource eventSource) { }
        public void Dispose() { }
        public void WriteMetric(float value) { }
    }
}