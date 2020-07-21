// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Diagnostics
{
    public partial class Activity : System.IDisposable
    {
        public Activity(string operationName) { }
        public System.Diagnostics.ActivityTraceFlags ActivityTraceFlags { get { throw null; } set { } }
        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string?>> Baggage { get { throw null; } }
        public System.Diagnostics.ActivityContext Context { get { throw null; } }
        public static System.Diagnostics.Activity? Current { get { throw null; } set { } }
        public static System.Diagnostics.ActivityIdFormat DefaultIdFormat { get { throw null; } set { } }
        public string DisplayName { get { throw null; } set { } }
        public System.TimeSpan Duration { get { throw null; } }
        public System.Collections.Generic.IEnumerable<System.Diagnostics.ActivityEvent> Events { get { throw null; } }
        public static bool ForceDefaultIdFormat { get { throw null; } set { } }
        public string? Id { get { throw null; } }
        public System.Diagnostics.ActivityIdFormat IdFormat { get { throw null; } }
        public bool IsAllDataRequested { get { throw null; } set { } }
        public System.Diagnostics.ActivityKind Kind { get { throw null; } }
        public System.Collections.Generic.IEnumerable<System.Diagnostics.ActivityLink> Links { get { throw null; } }
        public string OperationName { get { throw null; } }
        public System.Diagnostics.Activity? Parent { get { throw null; } }
        public string? ParentId { get { throw null; } }
        public System.Diagnostics.ActivitySpanId ParentSpanId { get { throw null; } }
        public bool Recorded { get { throw null; } }
        public string? RootId { get { throw null; } }
        public System.Diagnostics.ActivitySource Source { get { throw null; } }
        public System.Diagnostics.ActivitySpanId SpanId { get { throw null; } }
        public System.DateTime StartTimeUtc { get { throw null; } }
        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>> TagObjects { get { throw null; } }
        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string?>> Tags { get { throw null; } }
        public System.Diagnostics.ActivityTraceId TraceId { get { throw null; } }
        public string? TraceStateString { get { throw null; } set { } }
        public System.Diagnostics.Activity AddBaggage(string key, string? value) { throw null; }
        public System.Diagnostics.Activity AddEvent(System.Diagnostics.ActivityEvent e) { throw null; }
        public System.Diagnostics.Activity AddTag(string key, object? value) { throw null; }
        public System.Diagnostics.Activity AddTag(string key, string? value) { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public string? GetBaggageItem(string key) { throw null; }
        public object? GetCustomProperty(string propertyName) { throw null; }
        public void SetCustomProperty(string propertyName, object? propertyValue) { }
        public System.Diagnostics.Activity SetEndTime(System.DateTime endTimeUtc) { throw null; }
        public System.Diagnostics.Activity SetIdFormat(System.Diagnostics.ActivityIdFormat format) { throw null; }
        public System.Diagnostics.Activity SetParentId(System.Diagnostics.ActivityTraceId traceId, System.Diagnostics.ActivitySpanId spanId, System.Diagnostics.ActivityTraceFlags activityTraceFlags = System.Diagnostics.ActivityTraceFlags.None) { throw null; }
        public System.Diagnostics.Activity SetParentId(string parentId) { throw null; }
        public System.Diagnostics.Activity SetStartTime(System.DateTime startTimeUtc) { throw null; }
        public System.Diagnostics.Activity SetTag(string key, object value) { throw null; }
        public System.Diagnostics.Activity Start() { throw null; }
        public void Stop() { }
    }
    public readonly partial struct ActivityContext : System.IEquatable<System.Diagnostics.ActivityContext>
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public ActivityContext(System.Diagnostics.ActivityTraceId traceId, System.Diagnostics.ActivitySpanId spanId, System.Diagnostics.ActivityTraceFlags traceFlags, string? traceState = null, bool isRemote = false) { throw null; }
        public bool IsRemote { get { throw null; } }
        public System.Diagnostics.ActivitySpanId SpanId { get { throw null; } }
        public System.Diagnostics.ActivityTraceFlags TraceFlags { get { throw null; } }
        public System.Diagnostics.ActivityTraceId TraceId { get { throw null; } }
        public string? TraceState { get { throw null; } }
        public bool Equals(System.Diagnostics.ActivityContext value) { throw null; }
        public override bool Equals(object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Diagnostics.ActivityContext left, System.Diagnostics.ActivityContext right) { throw null; }
        public static bool operator !=(System.Diagnostics.ActivityContext left, System.Diagnostics.ActivityContext right) { throw null; }
    }
    public readonly partial struct ActivityCreationOptions<T>
    {
        private readonly T _Parent_k__BackingField;
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Diagnostics.ActivityKind Kind { get { throw null; } }
        public System.Collections.Generic.IEnumerable<System.Diagnostics.ActivityLink>? Links { get { throw null; } }
        public string Name { get { throw null; } }
        public T Parent { get { throw null; } }
        public System.Diagnostics.ActivitySource Source { get { throw null; } }
        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>>? Tags { get { throw null; } }
    }
    public enum ActivityDataRequest
    {
        None = 0,
        PropagationData = 1,
        AllData = 2,
        AllDataAndRecorded = 3,
    }
    public readonly partial struct ActivityEvent
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public ActivityEvent(string name) { throw null; }
        public ActivityEvent(string name, System.DateTimeOffset timestamp = default(System.DateTimeOffset), System.Diagnostics.ActivityTagsCollection? tags = null) { throw null; }
        public string Name { get { throw null; } }
        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>> Tags { get { throw null; } }
        public System.DateTimeOffset Timestamp { get { throw null; } }
    }
    public enum ActivityIdFormat
    {
        Unknown = 0,
        Hierarchical = 1,
        W3C = 2,
    }
    public enum ActivityKind
    {
        Internal = 0,
        Server = 1,
        Client = 2,
        Producer = 3,
        Consumer = 4,
    }
    public readonly partial struct ActivityLink : System.IEquatable<System.Diagnostics.ActivityLink>
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public ActivityLink(System.Diagnostics.ActivityContext context, System.Diagnostics.ActivityTagsCollection? tags = null) { throw null; }
        public System.Diagnostics.ActivityContext Context { get { throw null; } }
        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>>? Tags { get { throw null; } }
        public bool Equals(System.Diagnostics.ActivityLink value) { throw null; }
        public override bool Equals(object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Diagnostics.ActivityLink left, System.Diagnostics.ActivityLink right) { throw null; }
        public static bool operator !=(System.Diagnostics.ActivityLink left, System.Diagnostics.ActivityLink right) { throw null; }
    }
    public sealed partial class ActivityListener : System.IDisposable
    {
        public ActivityListener() { }
        public System.Action<System.Diagnostics.Activity>? ActivityStarted { get { throw null; } set { } }
        public System.Action<System.Diagnostics.Activity>? ActivityStopped { get { throw null; } set { } }
        public bool AutoGenerateRootContextTraceId { get { throw null; } set { } }
        public System.Diagnostics.GetRequestedData<System.Diagnostics.ActivityContext>? GetRequestedDataUsingContext { get { throw null; } set { } }
        public System.Diagnostics.GetRequestedData<string>? GetRequestedDataUsingParentId { get { throw null; } set { } }
        public System.Func<System.Diagnostics.ActivitySource, bool>? ShouldListenTo { get { throw null; } set { } }
        public void Dispose() { }
    }
    public sealed partial class ActivitySource : System.IDisposable
    {
        public ActivitySource(string name, string? version = "") { }
        public string Name { get { throw null; } }
        public string? Version { get { throw null; } }
        public static void AddActivityListener(System.Diagnostics.ActivityListener listener) { }
        public void Dispose() { }
        public bool HasListeners() { throw null; }
        public System.Diagnostics.Activity? StartActivity(string name, System.Diagnostics.ActivityKind kind = System.Diagnostics.ActivityKind.Internal) { throw null; }
        public System.Diagnostics.Activity? StartActivity(string name, System.Diagnostics.ActivityKind kind, System.Diagnostics.ActivityContext parentContext, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>>? tags = null, System.Collections.Generic.IEnumerable<System.Diagnostics.ActivityLink>? links = null, System.DateTimeOffset startTime = default(System.DateTimeOffset)) { throw null; }
        public System.Diagnostics.Activity? StartActivity(string name, System.Diagnostics.ActivityKind kind, string parentId, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>>? tags = null, System.Collections.Generic.IEnumerable<System.Diagnostics.ActivityLink>? links = null, System.DateTimeOffset startTime = default(System.DateTimeOffset)) { throw null; }
    }
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
    public partial class ActivityTagsCollection : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.Generic.IDictionary<string, object>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.IEnumerable
    {
        public ActivityTagsCollection() { }
        public ActivityTagsCollection(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>> list) { }
        public int Count { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public object this[string key] { get { throw null; } set { } }
        public System.Collections.Generic.ICollection<string> Keys { get { throw null; } }
        public System.Collections.Generic.ICollection<object> Values { get { throw null; } }
        public void Add(System.Collections.Generic.KeyValuePair<string, object> item) { }
        public void Add(string key, object value) { }
        public void Clear() { }
        public bool Contains(System.Collections.Generic.KeyValuePair<string, object> item) { throw null; }
        public bool ContainsKey(string key) { throw null; }
        public void CopyTo(System.Collections.Generic.KeyValuePair<string, object>[] array, int arrayIndex) { }
        public System.Diagnostics.ActivityTagsCollection.Enumerator GetEnumerator() { throw null; }
        public bool Remove(System.Collections.Generic.KeyValuePair<string, object> item) { throw null; }
        public bool Remove(string key) { throw null; }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public bool TryGetValue(string key, [System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute(false)] out object value) { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Collections.Generic.KeyValuePair<string, object> Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
        }
    }
    [System.FlagsAttribute]
    public enum ActivityTraceFlags
    {
        None = 0,
        Recorded = 1,
    }
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
    public partial class DiagnosticListener : System.Diagnostics.DiagnosticSource, System.IDisposable, System.IObservable<System.Collections.Generic.KeyValuePair<string, object?>>
    {
        public DiagnosticListener(string name) { }
        public static System.IObservable<System.Diagnostics.DiagnosticListener> AllListeners { get { throw null; } }
        public string Name { get { throw null; } }
        public virtual void Dispose() { }
        public bool IsEnabled() { throw null; }
        public override bool IsEnabled(string name) { throw null; }
        public override bool IsEnabled(string name, object? arg1, object? arg2 = null) { throw null; }
        public override void OnActivityExport(System.Diagnostics.Activity activity, object? payload) { }
        public override void OnActivityImport(System.Diagnostics.Activity activity, object? payload) { }
        public virtual System.IDisposable Subscribe(System.IObserver<System.Collections.Generic.KeyValuePair<string, object?>> observer) { throw null; }
        public virtual System.IDisposable Subscribe(System.IObserver<System.Collections.Generic.KeyValuePair<string, object?>> observer, System.Func<string, object?, object?, bool>? isEnabled) { throw null; }
        public virtual System.IDisposable Subscribe(System.IObserver<System.Collections.Generic.KeyValuePair<string, object?>> observer, System.Func<string, object?, object?, bool>? isEnabled, System.Action<System.Diagnostics.Activity, object?>? onActivityImport = null, System.Action<System.Diagnostics.Activity, object?>? onActivityExport = null) { throw null; }
        public virtual System.IDisposable Subscribe(System.IObserver<System.Collections.Generic.KeyValuePair<string, object?>> observer, System.Predicate<string>? isEnabled) { throw null; }
        public override string ToString() { throw null; }
        public override void Write(string name, object? value) { }
    }
    public abstract partial class DiagnosticSource
    {
        protected DiagnosticSource() { }
        public abstract bool IsEnabled(string name);
        public virtual bool IsEnabled(string name, object? arg1, object? arg2 = null) { throw null; }
        public virtual void OnActivityExport(System.Diagnostics.Activity activity, object? payload) { }
        public virtual void OnActivityImport(System.Diagnostics.Activity activity, object? payload) { }
        public System.Diagnostics.Activity StartActivity(System.Diagnostics.Activity activity, object? args) { throw null; }
        public void StopActivity(System.Diagnostics.Activity activity, object? args) { }
        public abstract void Write(string name, object? value);
    }
    public delegate System.Diagnostics.ActivityDataRequest GetRequestedData<T>(ref System.Diagnostics.ActivityCreationOptions<T> options);
}
