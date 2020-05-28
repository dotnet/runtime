// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Diagnostics
{
    public partial class Activity : IDisposable
    {
        public Activity(string operationName) { }
        public System.Diagnostics.ActivityTraceFlags ActivityTraceFlags { get { throw null; } set { } }
        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string?>> Baggage { get { throw null; } }
        public static System.Diagnostics.Activity? Current
        {
#if ALLOW_PARTIALLY_TRUSTED_CALLERS
            [System.Security.SecuritySafeCriticalAttribute]
#endif
            get { throw null; }
#if ALLOW_PARTIALLY_TRUSTED_CALLERS
            [System.Security.SecuritySafeCriticalAttribute]
#endif
            set { }
        }
        public static System.Diagnostics.ActivityIdFormat DefaultIdFormat { get { throw null; } set { } }
        public System.TimeSpan Duration { get { throw null; } }
        public static bool ForceDefaultIdFormat { get { throw null; } set { } }
        public string? Id
        {
#if ALLOW_PARTIALLY_TRUSTED_CALLERS
        [System.Security.SecuritySafeCriticalAttribute]
#endif
            get { throw null; }
        }

        public bool IsAllDataRequested { get { throw null; } set { throw null; }}
        public System.Diagnostics.ActivityIdFormat IdFormat { get { throw null; } }
        public System.Diagnostics.ActivityKind Kind  { get { throw null; } }
        public string OperationName { get { throw null; } }
        public string DisplayName { get { throw null; } set { throw null; } }
        public System.Diagnostics.ActivitySource Source { get { throw null; } }
        public System.Diagnostics.Activity? Parent { get { throw null; } }
        public string? ParentId { get { throw null; } }
        public System.Diagnostics.ActivitySpanId ParentSpanId { get { throw null; } }
        public bool Recorded { get { throw null; } }
        public string? RootId { get { throw null; } }
        public System.Diagnostics.ActivitySpanId SpanId { get { throw null; } }
        public System.DateTime StartTimeUtc { get { throw null; } }
        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string?>> Tags { get { throw null; } }
        public System.Collections.Generic.IEnumerable<System.Diagnostics.ActivityEvent> Events { get { throw null; } }
        public System.Collections.Generic.IEnumerable<System.Diagnostics.ActivityLink> Links { get { throw null; } }
        public System.Diagnostics.ActivityTraceId TraceId { get { throw null; } }
        public string? TraceStateString { get { throw null; } set { } }
        public System.Diagnostics.Activity AddBaggage(string key, string? value) { throw null; }
        public System.Diagnostics.Activity AddEvent(System.Diagnostics.ActivityEvent e) { throw null; }
        public System.Diagnostics.Activity AddTag(string key, string? value) { throw null; }
        public string? GetBaggageItem(string key) { throw null; }
        public System.Diagnostics.Activity SetEndTime(System.DateTime endTimeUtc) { throw null; }
        public System.Diagnostics.Activity SetIdFormat(System.Diagnostics.ActivityIdFormat format) { throw null; }
        public System.Diagnostics.Activity SetParentId(System.Diagnostics.ActivityTraceId traceId, System.Diagnostics.ActivitySpanId spanId, System.Diagnostics.ActivityTraceFlags activityTraceFlags = System.Diagnostics.ActivityTraceFlags.None) { throw null; }
        public System.Diagnostics.Activity SetParentId(string parentId) { throw null; }
        public System.Diagnostics.Activity SetStartTime(System.DateTime startTimeUtc) { throw null; }
        public System.Diagnostics.Activity Start() { throw null; }
        public void Stop() { throw null; }
        public void Dispose()  { throw null; }
        protected virtual void Dispose(bool disposing) { throw null; }
        public void SetCustomProperty(string propertyName, object? propertyValue) { throw null; }
        public object? GetCustomProperty(string propertyName) { throw null; }
        public ActivityContext Context { get { throw null; } }
    }
    public enum ActivityIdFormat
    {
        Unknown = 0,
        Hierarchical = 1,
        W3C = 2,
    }
#if ALLOW_PARTIALLY_TRUSTED_CALLERS
    [System.Security.SecuritySafeCriticalAttribute]
#endif
    public readonly partial struct ActivitySpanId : System.IEquatable<System.Diagnostics.ActivitySpanId>
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public void CopyTo(System.Span<byte> destination) { }
        public static System.Diagnostics.ActivitySpanId CreateFromBytes(System.ReadOnlySpan<byte> idData) { throw null; }
        public static System.Diagnostics.ActivitySpanId CreateFromString(System.ReadOnlySpan<char> idData) { throw null; }
        public static System.Diagnostics.ActivitySpanId CreateFromUtf8String(System.ReadOnlySpan<byte> idData) { throw null; }
        public static System.Diagnostics.ActivitySpanId CreateRandom() { throw null; }
        public bool Equals(System.Diagnostics.ActivitySpanId spanId) { throw null; }
        public override bool Equals(object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Diagnostics.ActivitySpanId spanId1, System.Diagnostics.ActivitySpanId spandId2) { throw null; }
        public static bool operator !=(System.Diagnostics.ActivitySpanId spanId1, System.Diagnostics.ActivitySpanId spandId2) { throw null; }
        public string ToHexString() { throw null; }
        public override string ToString() { throw null; }
    }
    public sealed class ActivitySource : IDisposable
    {
        public ActivitySource(string name, string? version = "") { throw null; }
        public string Name { get { throw null; } }
        public string? Version { get { throw null; } }
        public bool HasListeners() { throw null; }
        public System.Diagnostics.Activity? StartActivity(string name, System.Diagnostics.ActivityKind kind = ActivityKind.Internal)  { throw null; }
        public System.Diagnostics.Activity? StartActivity(string name, System.Diagnostics.ActivityKind kind, System.Diagnostics.ActivityContext parentContext, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string?>>? tags = null, System.Collections.Generic.IEnumerable<System.Diagnostics.ActivityLink>? links = null, System.DateTimeOffset startTime = default) { throw null; }
        public System.Diagnostics.Activity? StartActivity(string name, System.Diagnostics.ActivityKind kind, string parentId, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string?>>? tags = null, System.Collections.Generic.IEnumerable<System.Diagnostics.ActivityLink>? links = null, System.DateTimeOffset startTime = default) { throw null; }
        public static void AddActivityListener(System.Diagnostics.ActivityListener listener) { throw null; }
        public void Dispose() { throw null; }
    }
    [System.FlagsAttribute]
    public enum ActivityTraceFlags
    {
        None = 0,
        Recorded = 1,
    }
#if ALLOW_PARTIALLY_TRUSTED_CALLERS
    [System.Security.SecuritySafeCriticalAttribute]
#endif
    public readonly partial struct ActivityTraceId : System.IEquatable<System.Diagnostics.ActivityTraceId>
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public void CopyTo(System.Span<byte> destination) { }
        public static System.Diagnostics.ActivityTraceId CreateFromBytes(System.ReadOnlySpan<byte> idData) { throw null; }
        public static System.Diagnostics.ActivityTraceId CreateFromString(System.ReadOnlySpan<char> idData) { throw null; }
        public static System.Diagnostics.ActivityTraceId CreateFromUtf8String(System.ReadOnlySpan<byte> idData) { throw null; }
        public static System.Diagnostics.ActivityTraceId CreateRandom() { throw null; }
        public bool Equals(System.Diagnostics.ActivityTraceId traceId) { throw null; }
        public override bool Equals(object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Diagnostics.ActivityTraceId traceId1, System.Diagnostics.ActivityTraceId traceId2) { throw null; }
        public static bool operator !=(System.Diagnostics.ActivityTraceId traceId1, System.Diagnostics.ActivityTraceId traceId2) { throw null; }
        public string ToHexString() { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class DiagnosticListener
    {
        public override void OnActivityExport(System.Diagnostics.Activity activity, object? payload) { }
        public override void OnActivityImport(System.Diagnostics.Activity activity, object? payload) { }
        public virtual System.IDisposable Subscribe(System.IObserver<System.Collections.Generic.KeyValuePair<string, object?>> observer, System.Func<string, object?, object?, bool>? isEnabled, System.Action<System.Diagnostics.Activity, object?>? onActivityImport = null, System.Action<System.Diagnostics.Activity, object?>? onActivityExport = null) { throw null; }
    }
    public abstract partial class DiagnosticSource
    {
        public virtual void OnActivityExport(System.Diagnostics.Activity activity, object? payload) { }
        public virtual void OnActivityImport(System.Diagnostics.Activity activity, object? payload) { }
        public System.Diagnostics.Activity StartActivity(System.Diagnostics.Activity activity, object? args) { throw null; }
        public void StopActivity(System.Diagnostics.Activity activity, object? args) { }
    }
    public enum ActivityDataRequest
    {
        None,
        PropagationData,
        AllData,
        AllDataAndRecorded
    }
    public enum ActivityKind
    {
        Internal = 0,
        Server = 1,
        Client = 2,
        Producer = 3,
        Consumer = 4,
    }
    public readonly struct ActivityEvent
    {
        public ActivityEvent(string name) {throw null; }
        public ActivityEvent(string name, System.DateTimeOffset timestamp) { throw null; }
        public ActivityEvent(string name, System.DateTimeOffset timestamp, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>>? attributes) { throw null; }
        public ActivityEvent(string name, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>>? attributes) { throw null; }
        public string Name { get { throw null; } }
        public System.DateTimeOffset Timestamp { get { throw null; } }
        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>> Attributes { get { throw null; } }
    }
    public readonly struct ActivityContext : System.IEquatable<System.Diagnostics.ActivityContext>
    {
        public ActivityContext(System.Diagnostics.ActivityTraceId traceId, System.Diagnostics.ActivitySpanId spanId, System.Diagnostics.ActivityTraceFlags traceOptions, string? traceState = null) { throw null; }
        public System.Diagnostics.ActivityTraceId TraceId  { get { throw null; } }
        public System.Diagnostics.ActivitySpanId SpanId  { get { throw null; } }
        public System.Diagnostics.ActivityTraceFlags TraceFlags  { get { throw null; } }
        public string? TraceState  { get { throw null; } }
        public static bool operator ==(System.Diagnostics.ActivityContext left, System.Diagnostics.ActivityContext right) { throw null; }
        public static bool operator !=(System.Diagnostics.ActivityContext left, System.Diagnostics.ActivityContext right) { throw null; }
        public bool Equals(System.Diagnostics.ActivityContext value) { throw null; }
        public override bool Equals(object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public readonly struct ActivityLink : IEquatable<ActivityLink>
    {
        public ActivityLink(System.Diagnostics.ActivityContext context) { throw null; }
        public ActivityLink(System.Diagnostics.ActivityContext context, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>>? attributes) { throw null; }
        public System.Diagnostics.ActivityContext Context  { get { throw null; } }
        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>>? Attributes  { get { throw null; } }

        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Diagnostics.ActivityLink value) { throw null; }
        public static bool operator ==(System.Diagnostics.ActivityLink left, System.Diagnostics.ActivityLink right) { throw null; }
        public static bool operator !=(System.Diagnostics.ActivityLink left, System.Diagnostics.ActivityLink right) { throw null; }
        public override int GetHashCode()  { throw null; }
    }
    public readonly struct ActivityCreationOptions<T>
    {
        public System.Diagnostics.ActivitySource Source  { get { throw null; } }
        public string Name  { get { throw null; } }
        public System.Diagnostics.ActivityKind Kind  { get { throw null; } }
        public T Parent  { get { throw null; } }
        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string?>> Tags  { get { throw null; } }
        public System.Collections.Generic.IEnumerable<System.Diagnostics.ActivityLink> Links  { get { throw null; } }
    }
    public delegate System.Diagnostics.ActivityDataRequest GetRequestedData<T>(ref System.Diagnostics.ActivityCreationOptions<T> options);
    public sealed class ActivityListener : IDisposable
    {
        public ActivityListener() { throw null; }
        public System.Action<System.Diagnostics.Activity>? ActivityStarted { get { throw null; } set { } }
        public System.Action<System.Diagnostics.Activity>? ActivityStopped { get { throw null; } set { } }
        public System.Func<System.Diagnostics.ActivitySource, bool>? ShouldListenTo { get { throw null; } set { } }
        public System.Diagnostics.GetRequestedData<string>? GetRequestedDataUsingParentId { get { throw null; } set { } }
        public System.Diagnostics.GetRequestedData<ActivityContext>? GetRequestedDataUsingContext { get { throw null; } set { } }
        public void Dispose() { throw null; }
   }
}

