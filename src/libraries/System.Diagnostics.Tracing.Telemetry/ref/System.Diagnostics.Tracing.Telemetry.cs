namespace System.Diagnostics.Tracing {
  public partial class TelemetryListener : System.Diagnostics.Tracing.TelemetrySource, System.IDisposable, System.IObservable<System.Collections.Generic.KeyValuePair<string, object>> {
    public TelemetryListener(string name) { }
    public static System.Diagnostics.Tracing.TelemetryListener DefaultListener { get { return default(System.Diagnostics.Tracing.TelemetryListener); } }
    public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
    public static IObservable<TelemetryListener> AllListeners { get; } 
    public virtual void Dispose() { }
    public override bool IsEnabled(string telemetryName) { return default(bool); }
    public System.IDisposable Subscribe(System.IObserver<System.Collections.Generic.KeyValuePair<string, object>> observer) { return default(System.IDisposable); }
    public virtual System.IDisposable Subscribe(System.IObserver<System.Collections.Generic.KeyValuePair<string, object>> observer, System.Predicate<string> isEnabled) { return default(System.IDisposable); }
    public override void WriteTelemetry(string telemetryName, object parameters) { }
  }
  public abstract partial class TelemetrySource {
    protected TelemetrySource() { }
    public static System.Diagnostics.Tracing.TelemetrySource DefaultSource { get { return default(System.Diagnostics.Tracing.TelemetrySource); } }
    public abstract bool IsEnabled(string telemetryName);
    public abstract void WriteTelemetry(string telemetryName, object parameters);
  }
}

