namespace System.Diagnostics.Tracing
{
    public partial class EventCounter : System.Diagnostics.Tracing.IDisposable
    {
        public EventCounter(string name, System.Diagnostics.Tracing.EventSource eventSource) { }
        public void WriteMetric(float value) { }
    }
}