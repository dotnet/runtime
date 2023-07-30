// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
            get { throw null; }
            set { }
        }
        public static event System.EventHandler<System.Diagnostics.ActivityChangedEventArgs>? CurrentChanged { add { } remove { } }
        public static System.Diagnostics.ActivityIdFormat DefaultIdFormat { get { throw null; } set { } }
        public System.TimeSpan Duration { get { throw null; } }
        public static bool ForceDefaultIdFormat { get { throw null; } set { } }
        public string? Id
        {
            get { throw null; }
        }

        public bool HasRemoteParent { get { throw null; } }
        public bool IsAllDataRequested { get { throw null; } set { throw null; } }
        public bool IsStopped { get { throw null; } }
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
        public System.Diagnostics.ActivityStatusCode Status { get { throw null; } }
        public string? StatusDescription  { get { throw null; } }
        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string?>> Tags { get { throw null; } }
        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>> TagObjects { get { throw null; } }
        public System.Collections.Generic.IEnumerable<System.Diagnostics.ActivityEvent> Events { get { throw null; } }
        public System.Collections.Generic.IEnumerable<System.Diagnostics.ActivityLink> Links { get { throw null; } }
        public System.Diagnostics.ActivityTraceId TraceId { get { throw null; } }
        public string? TraceStateString { get { throw null; } set { } }
        public System.Diagnostics.Activity AddBaggage(string key, string? value) { throw null; }
        public System.Diagnostics.Activity AddEvent(System.Diagnostics.ActivityEvent e) { throw null; }
        public System.Diagnostics.Activity AddTag(string key, string? value) { throw null; }
        public System.Diagnostics.Activity AddTag(string key, object? value) { throw null; }
        public System.Diagnostics.Activity SetTag(string key, object? value) { throw null; }
        public System.Diagnostics.Activity SetBaggage(string key, string? value) { throw null; }
        public string? GetBaggageItem(string key) { throw null; }
        public object? GetTagItem(string key) { throw null; }
        public System.Diagnostics.Activity SetEndTime(System.DateTime endTimeUtc) { throw null; }
        public static Func<System.Diagnostics.ActivityTraceId>? TraceIdGenerator { get { throw null; } set { throw null; } }
        public System.Diagnostics.Activity SetIdFormat(System.Diagnostics.ActivityIdFormat format) { throw null; }
        public System.Diagnostics.Activity SetParentId(System.Diagnostics.ActivityTraceId traceId, System.Diagnostics.ActivitySpanId spanId, System.Diagnostics.ActivityTraceFlags activityTraceFlags = System.Diagnostics.ActivityTraceFlags.None) { throw null; }
        public System.Diagnostics.Activity SetParentId(string parentId) { throw null; }
        public System.Diagnostics.Activity SetStartTime(System.DateTime startTimeUtc) { throw null; }
        public System.Diagnostics.Activity SetStatus(System.Diagnostics.ActivityStatusCode code, string? description = null) { throw null; }
        public System.Diagnostics.Activity Start() { throw null; }
        public void Stop() { throw null; }
        public void Dispose()  { throw null; }
        protected virtual void Dispose(bool disposing) { throw null; }
        public void SetCustomProperty(string propertyName, object? propertyValue) { throw null; }
        public object? GetCustomProperty(string propertyName) { throw null; }
        public System.Diagnostics.ActivityContext Context { get { throw null; } }
        public System.Diagnostics.Activity.Enumerator<System.Collections.Generic.KeyValuePair<string, object?>> EnumerateTagObjects() { throw null; }
        public System.Diagnostics.Activity.Enumerator<ActivityEvent> EnumerateEvents() { throw null; }
        public System.Diagnostics.Activity.Enumerator<ActivityLink> EnumerateLinks() { throw null; }

        public struct Enumerator<T>
        {
            [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
            public readonly System.Diagnostics.Activity.Enumerator<T> GetEnumerator() { throw null; }
            public readonly ref T Current { get { throw null; } }
            public bool MoveNext() { throw null; }
        }
    }
    public readonly struct ActivityChangedEventArgs
    {
        public System.Diagnostics.Activity? Previous { get { throw null; } init { throw null; } }
        public System.Diagnostics.Activity? Current { get { throw null; } init { throw null; } }
    }
    public class ActivityTagsCollection : System.Collections.Generic.IDictionary<string, object?>
    {
        public ActivityTagsCollection() { throw null; }
        public ActivityTagsCollection(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>> list) { throw null; }
        public object? this[string key] { get { throw null; } set { } }
        public System.Collections.Generic.ICollection<string> Keys { get { throw null; } }
        public System.Collections.Generic.ICollection<object?> Values { get { throw null; } }
        public int Count { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public void Add(string key, object? value) { throw null; }
        public void Add(System.Collections.Generic.KeyValuePair<string, object?> item) { throw null; }
        public void Clear() { throw null; }
        public bool Contains(System.Collections.Generic.KeyValuePair<string, object?> item) { throw null; }
        public bool ContainsKey(string key) { throw null; }
        public void CopyTo(System.Collections.Generic.KeyValuePair<string, object?>[] array, int arrayIndex) { throw null; }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object?>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>>.GetEnumerator() { throw null; }
        public bool Remove(string key) { throw null; }
        public bool Remove(System.Collections.Generic.KeyValuePair<string, object?> item) { throw null; }
        public bool TryGetValue(string key, out object? value) { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public Enumerator GetEnumerator()  { throw null; }

        public struct Enumerator : System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object?>>, System.Collections.IEnumerator
        {
            public System.Collections.Generic.KeyValuePair<string, object?> Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { throw null; }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { throw null; }
        }
    }
    public enum ActivityStatusCode
    {
        Unset = 0,
        Ok = 1,
        Error = 2
    }
    public enum ActivityIdFormat
    {
        Unknown = 0,
        Hierarchical = 1,
        W3C = 2,
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
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
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
        public System.Diagnostics.Activity? CreateActivity(string name, System.Diagnostics.ActivityKind kind) { throw null; }
        public System.Diagnostics.Activity? CreateActivity(string name, System.Diagnostics.ActivityKind kind, System.Diagnostics.ActivityContext parentContext, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>>? tags = null, System.Collections.Generic.IEnumerable<System.Diagnostics.ActivityLink>? links = null, System.Diagnostics.ActivityIdFormat idFormat = System.Diagnostics.ActivityIdFormat.Unknown) { throw null; }
        public System.Diagnostics.Activity? CreateActivity(string name, System.Diagnostics.ActivityKind kind, string? parentId, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>>? tags = null, System.Collections.Generic.IEnumerable<System.Diagnostics.ActivityLink>? links = null, System.Diagnostics.ActivityIdFormat idFormat = System.Diagnostics.ActivityIdFormat.Unknown) { throw null; }
        public System.Diagnostics.Activity? StartActivity([System.Runtime.CompilerServices.CallerMemberName] string name = "", System.Diagnostics.ActivityKind kind = ActivityKind.Internal)  { throw null; }
        public System.Diagnostics.Activity? StartActivity(string name, System.Diagnostics.ActivityKind kind, System.Diagnostics.ActivityContext parentContext, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>>? tags = null, System.Collections.Generic.IEnumerable<System.Diagnostics.ActivityLink>? links = null, System.DateTimeOffset startTime = default) { throw null; }
        public System.Diagnostics.Activity? StartActivity(string name, System.Diagnostics.ActivityKind kind, string? parentId, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>>? tags = null, System.Collections.Generic.IEnumerable<System.Diagnostics.ActivityLink>? links = null, System.DateTimeOffset startTime = default) { throw null; }
        public System.Diagnostics.Activity? StartActivity(System.Diagnostics.ActivityKind kind, System.Diagnostics.ActivityContext parentContext = default, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>>? tags = null, System.Collections.Generic.IEnumerable<System.Diagnostics.ActivityLink>? links = null, DateTimeOffset startTime = default, [System.Runtime.CompilerServices.CallerMemberName] string name = "") { throw null; }
        public static void AddActivityListener(System.Diagnostics.ActivityListener listener) { throw null; }
        public void Dispose() { throw null; }
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
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
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
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("The type of object being written to DiagnosticSource cannot be discovered statically.")]
        public System.Diagnostics.Activity StartActivity(System.Diagnostics.Activity activity, object? args) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Only the properties of the T type will be preserved. Properties of referenced types and properties of derived types may be trimmed.")]
        public System.Diagnostics.Activity StartActivity<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] T>(Activity activity, T args) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("The type of object being written to DiagnosticSource cannot be discovered statically.")]
        public void StopActivity(System.Diagnostics.Activity activity, object? args) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Only the properties of the T type will be preserved. Properties of referenced types and properties of derived types may be trimmed.")]
        public void StopActivity<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] T>(Activity activity, T args) { throw null; }
    }
    public enum ActivitySamplingResult
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
        public ActivityEvent(string name, System.DateTimeOffset timestamp = default, System.Diagnostics.ActivityTagsCollection? tags = null) { throw null; }
        public string Name { get { throw null; } }
        public System.DateTimeOffset Timestamp { get { throw null; } }
        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>> Tags { get { throw null; } }
        public System.Diagnostics.Activity.Enumerator<System.Collections.Generic.KeyValuePair<string, object?>> EnumerateTagObjects() { throw null; }
    }
    public readonly struct ActivityContext : System.IEquatable<System.Diagnostics.ActivityContext>
    {
        public ActivityContext(System.Diagnostics.ActivityTraceId traceId, System.Diagnostics.ActivitySpanId spanId, System.Diagnostics.ActivityTraceFlags traceFlags, string? traceState = null, bool isRemote = false) { throw null; }
        public System.Diagnostics.ActivityTraceId TraceId  { get { throw null; } }
        public System.Diagnostics.ActivitySpanId SpanId  { get { throw null; } }
        public System.Diagnostics.ActivityTraceFlags TraceFlags  { get { throw null; } }
        public string? TraceState  { get { throw null; } }
        public bool IsRemote { get { throw null; } }
        public static bool TryParse(string? traceParent, string? traceState, out System.Diagnostics.ActivityContext context) { throw null; }
        public static bool TryParse(string? traceParent, string? traceState, bool isRemote, out System.Diagnostics.ActivityContext context) { throw null; }
        public static System.Diagnostics.ActivityContext Parse(string traceParent, string? traceState) { throw null; }
        public static bool operator ==(System.Diagnostics.ActivityContext left, System.Diagnostics.ActivityContext right) { throw null; }
        public static bool operator !=(System.Diagnostics.ActivityContext left, System.Diagnostics.ActivityContext right) { throw null; }
        public bool Equals(System.Diagnostics.ActivityContext value) { throw null; }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public readonly struct ActivityLink : IEquatable<ActivityLink>
    {
        public ActivityLink(System.Diagnostics.ActivityContext context, System.Diagnostics.ActivityTagsCollection? tags = null) { throw null; }
        public System.Diagnostics.ActivityContext Context  { get { throw null; } }
        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>>? Tags  { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals(System.Diagnostics.ActivityLink value) { throw null; }
        public static bool operator ==(System.Diagnostics.ActivityLink left, System.Diagnostics.ActivityLink right) { throw null; }
        public static bool operator !=(System.Diagnostics.ActivityLink left, System.Diagnostics.ActivityLink right) { throw null; }
        public override int GetHashCode()  { throw null; }
        public System.Diagnostics.Activity.Enumerator<System.Collections.Generic.KeyValuePair<string, object?>> EnumerateTagObjects() { throw null; }
    }
    public readonly struct ActivityCreationOptions<T>
    {
        public System.Diagnostics.ActivitySource Source  { get { throw null; } }
        public string Name  { get { throw null; } }
        public System.Diagnostics.ActivityKind Kind  { get { throw null; } }
        public T Parent  { get { throw null; } }
        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>>? Tags  { get { throw null; } }
        public System.Collections.Generic.IEnumerable<System.Diagnostics.ActivityLink>? Links  { get { throw null; } }
        public System.Diagnostics.ActivityTagsCollection SamplingTags { get { throw null; } }
        public System.Diagnostics.ActivityTraceId TraceId { get { throw null; } }
        public string? TraceState { get { throw null; } init { throw null; } }
    }
    public delegate System.Diagnostics.ActivitySamplingResult SampleActivity<T>(ref System.Diagnostics.ActivityCreationOptions<T> options);
    public sealed class ActivityListener : IDisposable
    {
        public ActivityListener() { throw null; }
        public System.Action<System.Diagnostics.Activity>? ActivityStarted { get { throw null; } set { throw null; } }
        public System.Action<System.Diagnostics.Activity>? ActivityStopped { get { throw null; } set { throw null; } }
        public System.Func<System.Diagnostics.ActivitySource, bool>? ShouldListenTo { get { throw null; } set { throw null; } }
        public System.Diagnostics.SampleActivity<string>? SampleUsingParentId { get { throw null; } set { throw null; } }
        public System.Diagnostics.SampleActivity<ActivityContext>? Sample { get { throw null; } set { throw null; } }
        public void Dispose() { throw null; }
    }
    public abstract class DistributedContextPropagator
    {
      public delegate void PropagatorGetterCallback(object? carrier, string fieldName, out string? fieldValue, out System.Collections.Generic.IEnumerable<string>? fieldValues);
      public delegate void PropagatorSetterCallback(object? carrier, string fieldName, string fieldValue);
      public abstract System.Collections.Generic.IReadOnlyCollection<string> Fields { get; }
      public abstract void Inject(Activity? activity, object? carrier, PropagatorSetterCallback? setter);
      public abstract void ExtractTraceIdAndState(object? carrier, PropagatorGetterCallback? getter, out string? traceId, out string? traceState);
      public abstract System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string?>>? ExtractBaggage(object? carrier, PropagatorGetterCallback? getter);
      public static DistributedContextPropagator Current { get; set; }
      public static DistributedContextPropagator CreateDefaultPropagator() { throw null; }
      public static DistributedContextPropagator CreatePassThroughPropagator() { throw null; }
      public static DistributedContextPropagator CreateNoOutputPropagator() { throw null; }
    }
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct TagList : System.Collections.Generic.IList<System.Collections.Generic.KeyValuePair<string, object?>>, System.Collections.Generic.IReadOnlyList<System.Collections.Generic.KeyValuePair<string, object?>>
    {
        public TagList(System.ReadOnlySpan<System.Collections.Generic.KeyValuePair<string, object?>> tagList) : this() { throw null; }
        public readonly int Count => throw null;
        public readonly bool IsReadOnly => throw null;
        public System.Collections.Generic.KeyValuePair<string, object?> this[int index]
        {
            readonly get { { throw null; } }
            set { { throw null; } }
        }
        public void Add(string key, object? value) { throw null; }
        public void Add(System.Collections.Generic.KeyValuePair<string, object?> tag) { throw null; }
        public readonly void CopyTo(System.Span<System.Collections.Generic.KeyValuePair<string, object?>> tags) { throw null; }
        public void Insert(int index, System.Collections.Generic.KeyValuePair<string, object?> item) { throw null; }
        public void RemoveAt(int index) { throw null; }
        public void Clear() { throw null; }
        public readonly bool Contains(System.Collections.Generic.KeyValuePair<string, object?> item) { throw null; }
        public readonly void CopyTo(System.Collections.Generic.KeyValuePair<string, object?>[] array, int arrayIndex) { throw null; }
        public bool Remove(System.Collections.Generic.KeyValuePair<string, object?> item) { throw null; }
        public readonly System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object?>> GetEnumerator() { throw null; }
        readonly System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public readonly int IndexOf(System.Collections.Generic.KeyValuePair<string, object?> item) { throw null; }
        public struct Enumerator : System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object?>>, System.Collections.IEnumerator
        {
            public System.Collections.Generic.KeyValuePair<string, object?> Current => throw null;
            object System.Collections.IEnumerator.Current => throw null;
            public void Dispose() { throw null; }
            public bool MoveNext() { throw null; }
            public void Reset() { throw null; }
        }
    }
}

namespace System.Diagnostics.Metrics
{
    public sealed class Counter<T> : Instrument<T> where T : struct
    {
        public void Add(T delta) { throw null; }
        public void Add(T delta, System.Collections.Generic.KeyValuePair<string, object?> tag)  { throw null; }
        public void Add(T delta, System.Collections.Generic.KeyValuePair<string, object?> tag1, System.Collections.Generic.KeyValuePair<string, object?> tag2)  { throw null; }
        public void Add(T delta, System.Collections.Generic.KeyValuePair<string, object?> tag1, System.Collections.Generic.KeyValuePair<string, object?> tag2, System.Collections.Generic.KeyValuePair<string, object?> tag3)  { throw null; }
        public void Add(T delta, ReadOnlySpan<System.Collections.Generic.KeyValuePair<string, object?>> tags) { throw null; }
        public void Add(T delta, params System.Collections.Generic.KeyValuePair<string, object?>[] tags) { throw null; }
        public void Add(T delta, in TagList tagList) { throw null; }
        internal Counter(Meter meter, string name, string? unit, string? description) :
                        base(meter, name, unit, description) { throw null; }
    }
    public sealed class UpDownCounter<T> : Instrument<T> where T : struct
    {
        public void Add(T delta) { throw null; }
        public void Add(T delta, System.Collections.Generic.KeyValuePair<string, object?> tag)  { throw null; }
        public void Add(T delta, System.Collections.Generic.KeyValuePair<string, object?> tag1, System.Collections.Generic.KeyValuePair<string, object?> tag2)  { throw null; }
        public void Add(T delta, System.Collections.Generic.KeyValuePair<string, object?> tag1, System.Collections.Generic.KeyValuePair<string, object?> tag2, System.Collections.Generic.KeyValuePair<string, object?> tag3)  { throw null; }
        public void Add(T delta, ReadOnlySpan<System.Collections.Generic.KeyValuePair<string, object?>> tags) { throw null; }
        public void Add(T delta, params System.Collections.Generic.KeyValuePair<string, object?>[] tags) { throw null; }
        public void Add(T delta, in TagList tagList) { throw null; }
        internal UpDownCounter(Meter meter, string name, string? unit, string? description) :
                        base(meter, name, unit, description) { throw null; }
    }
    public sealed class Histogram<T> : Instrument<T> where T : struct
    {
        internal Histogram(Meter meter, string name, string? unit, string? description) : base(meter, name, unit, description) { throw null; }
        public void Record(T value) { throw null; }
        public void Record(T value, System.Collections.Generic.KeyValuePair<string, object?> tag) { throw null; }
        public void Record(T value, System.Collections.Generic.KeyValuePair<string, object?> tag1, System.Collections.Generic.KeyValuePair<string, object?> tag2) { throw null; }
        public void Record(T value, System.Collections.Generic.KeyValuePair<string, object?> tag1, System.Collections.Generic.KeyValuePair<string, object?> tag2, System.Collections.Generic.KeyValuePair<string, object?> tag3) { throw null; }
        public void Record(T value, in TagList tagList) { throw null; }
        public void Record(T value, ReadOnlySpan<System.Collections.Generic.KeyValuePair<string, object?>> tags) { throw null; }
        public void Record(T value, params System.Collections.Generic.KeyValuePair<string, object?>[] tags) { throw null; }
    }
    public abstract class Instrument
    {
        public string? Description { get {throw null;} }
        public bool Enabled { get  {throw null; } }
        protected Instrument(Meter meter, string name, string? unit, string? description) {throw null;}
        protected Instrument(Meter meter, string name, string? unit, string? description, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>>? tags) {throw null;}
        public virtual bool IsObservable { get  {throw null; } }
        public Meter Meter { get {throw null;} }
        public string Name { get {throw null;} }
        protected void Publish() {throw null;}
        public string? Unit { get {throw null; } }
        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>>? Tags { get; }
    }
    public abstract class Instrument<T> : Instrument where T : struct
    {
        protected Instrument(Meter meter, string name, string? unit, string? description) : base(meter, name, unit, description) { throw null; }
        protected Instrument(Meter meter, string name, string? unit, string? description, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>>? tags) : base(meter, name, unit, description, tags)  {throw null;}
        protected void RecordMeasurement(T measurement)  { throw null; }
        protected void RecordMeasurement(T measurement, System.Collections.Generic.KeyValuePair<string, object?> tag) { throw null; }
        protected void RecordMeasurement(T measurement, System.Collections.Generic.KeyValuePair<string, object?> tag1, System.Collections.Generic.KeyValuePair<string, object?> tag2)  { throw null; }
        protected void RecordMeasurement(T measurement, System.Collections.Generic.KeyValuePair<string, object?> tag1, System.Collections.Generic.KeyValuePair<string, object?> tag2, System.Collections.Generic.KeyValuePair<string, object?> tag3)  { throw null; }
        protected void RecordMeasurement(T measurement, in TagList tagList) { throw null; }
        protected void RecordMeasurement(T measurement, ReadOnlySpan<System.Collections.Generic.KeyValuePair<string, object?>> tags) { throw null; }
    }
    public sealed class InstrumentRecorder<T> : IDisposable where T : struct
    {
        public InstrumentRecorder(Instrument instrument) { throw null; }
        public InstrumentRecorder(object? scopeFilter, string meterName, string instrumentName) { throw null; }
        public InstrumentRecorder(Meter meter, string instrumentName) { throw null; }
        public Instrument? Instrument { get { throw null; }  }
        public System.Collections.Generic.IEnumerable<Measurement<T>> GetMeasurements(bool clear = false) { throw null; }
        public void Dispose() { throw null; }
    }
    public readonly struct Measurement<T> where T : struct
    {
        public Measurement(T value) { throw null; }
        public Measurement(T value, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>>? tags) { throw null; }
        public Measurement(T value, params System.Collections.Generic.KeyValuePair<string, object?>[]? tags) { throw null; }
        public Measurement(T value, ReadOnlySpan<System.Collections.Generic.KeyValuePair<string, object?>> tags) { throw null; }
        public ReadOnlySpan<System.Collections.Generic.KeyValuePair<string, object?>> Tags { get { throw null; } }
        public T Value { get { throw null; } }
    }
    public delegate void MeasurementCallback<T>(Instrument instrument, T measurement, ReadOnlySpan<System.Collections.Generic.KeyValuePair<string, object?>> tags, object? state);
    public class Meter : IDisposable
    {
        public Counter<T> CreateCounter<T>(string name, string? unit = null, string? description = null) where T : struct  { throw null; }
        public Counter<T> CreateCounter<T>(string name, string? unit, string? description, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>> tags) where T : struct  { throw null; }
        public UpDownCounter<T> CreateUpDownCounter<T>(string name, string? unit = null, string? description = null) where T : struct  { throw null; }
        public UpDownCounter<T> CreateUpDownCounter<T>(string name, string? unit, string? description, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>> tags) where T : struct  { throw null; }
        public Histogram<T> CreateHistogram<T>(string name, string? unit = null, string? description = null) where T : struct { throw null; }
        public Histogram<T> CreateHistogram<T>(string name, string? unit, string? description, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>> tags) where T : struct { throw null; }
        public ObservableCounter<T> CreateObservableCounter<T>(
                            string name,
                            Func<T> observeValue,
                            string? unit = null,
                            string? description = null) where T : struct { throw null; }
        public ObservableCounter<T> CreateObservableCounter<T>(
                            string name,
                            Func<T> observeValue,
                            string? unit,
                            string? description,
                            System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>> tags) where T : struct { throw null; }
        public ObservableCounter<T> CreateObservableCounter<T>(
                            string name,
                            Func<Measurement<T>> observeValue,
                            string? unit = null,
                            string? description = null) where T : struct { throw null; }
        public ObservableCounter<T> CreateObservableCounter<T>(
                            string name,
                            Func<Measurement<T>> observeValue,
                            string? unit,
                            string? description,
                            System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>> tags) where T : struct { throw null; }
        public ObservableCounter<T> CreateObservableCounter<T>(
                            string name,
                            Func<System.Collections.Generic.IEnumerable<Measurement<T>>> observeValues,
                            string? unit = null,
                            string? description = null) where T : struct { throw null; }
        public ObservableCounter<T> CreateObservableCounter<T>(
                            string name,
                            Func<System.Collections.Generic.IEnumerable<Measurement<T>>> observeValues,
                            string? unit,
                            string? description,
                            System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>> tags) where T : struct { throw null; }
        public ObservableUpDownCounter<T> CreateObservableUpDownCounter<T>(
                            string name,
                            Func<T> observeValue,
                            string? unit = null,
                            string? description = null) where T : struct { throw null; }
        public ObservableUpDownCounter<T> CreateObservableUpDownCounter<T>(
                            string name,
                            Func<T> observeValue,
                            string? unit,
                            string? description,
                            System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>> tags) where T : struct { throw null; }
        public ObservableUpDownCounter<T> CreateObservableUpDownCounter<T>(
                            string name,
                            Func<Measurement<T>> observeValue,
                            string? unit = null,
                            string? description = null) where T : struct { throw null; }
        public ObservableUpDownCounter<T> CreateObservableUpDownCounter<T>(
                            string name,
                            Func<Measurement<T>> observeValue,
                            string? unit,
                            string? description,
                            System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>> tags) where T : struct { throw null; }
        public ObservableUpDownCounter<T> CreateObservableUpDownCounter<T>(
                            string name,
                            Func<System.Collections.Generic.IEnumerable<Measurement<T>>> observeValues,
                            string? unit = null,
                            string? description = null) where T : struct { throw null; }
        public ObservableUpDownCounter<T> CreateObservableUpDownCounter<T>(
                            string name,
                            Func<System.Collections.Generic.IEnumerable<Measurement<T>>> observeValues,
                            string? unit,
                            string? description,
                            System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>> tags) where T : struct { throw null; }
        public ObservableGauge<T> CreateObservableGauge<T>(
                            string name,
                            Func<T> observeValue,
                            string? unit = null,
                            string? description = null) where T : struct { throw null; }
        public ObservableGauge<T> CreateObservableGauge<T>(
                            string name,
                            Func<T> observeValue,
                            string? unit,
                            string? description,
                            System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>> tags) where T : struct { throw null; }
        public ObservableGauge<T> CreateObservableGauge<T>(
                            string name,
                            Func<Measurement<T>> observeValue,
                            string? unit = null,
                            string? description = null) where T : struct { throw null; }
        public ObservableGauge<T> CreateObservableGauge<T>(
                            string name,
                            Func<Measurement<T>> observeValue,
                            string? unit,
                            string? description,
                            System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>> tags) where T : struct { throw null; }
        public ObservableGauge<T> CreateObservableGauge<T>(
                            string name,
                            Func<System.Collections.Generic.IEnumerable<Measurement<T>>> observeValues,
                            string? unit = null,
                            string? description = null) where T : struct { throw null; }
        public ObservableGauge<T> CreateObservableGauge<T>(
                            string name,
                            Func<System.Collections.Generic.IEnumerable<Measurement<T>>> observeValues,
                            string? unit,
                            string? description,
                            System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>> tags) where T : struct { throw null; }
        protected virtual void Dispose(bool disposing) { throw null; }
        public void Dispose() { throw null; }
        public Meter(MeterOptions options) { throw null; }
        public Meter(string name) { throw null; }
        public Meter(string name, string? version)  { throw null; }
        public Meter(string name, string? version, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>>? tags, object? scope = null) { throw null; }
        public string Name { get { throw null; }  }
        public string? Version { get { throw null; } }
        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>>? Tags { get { throw null; }  }
        public object? Scope { get { throw null; }  }
    }
    public sealed class MeterListener : IDisposable
    {
        public object? DisableMeasurementEvents(Instrument instrument) { throw null; }
        public void Dispose() { throw null; }
        public void EnableMeasurementEvents(Instrument instrument, object? state = null) { throw null; }
        public Action<Instrument, MeterListener>? InstrumentPublished { get { throw null; } set { throw null; } }
        public Action<Instrument, object?>? MeasurementsCompleted { get { throw null; } set { throw null; } }
        public MeterListener() { throw null; }
        public void RecordObservableInstruments() { throw null; }
        public void SetMeasurementEventCallback<T>(MeasurementCallback<T>? measurementCallback) where T : struct { throw null; }
        public void Start() { throw null; }
    }
    public class MeterOptions
    {
        public string Name { get { throw null;} set { throw null;} }
        public string? Version { get { throw null;} set { throw null;} }
        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string,object?>>? Tags { get { throw null;} set { throw null;} }
        public object? Scope { get { throw null;} set { throw null;} }
        public MeterOptions(string name) { throw null;}
    }    
    public sealed class ObservableCounter<T> : ObservableInstrument<T> where T : struct
    {
        internal ObservableCounter(Meter meter, string name, string? unit, string? description) : base(meter, name, unit, description) { throw null; }
        protected override System.Collections.Generic.IEnumerable<Measurement<T>> Observe() { throw null;}
    }
    public sealed class ObservableUpDownCounter<T> : ObservableInstrument<T> where T : struct
    {
        internal ObservableUpDownCounter(Meter meter, string name, string? unit, string? description) : base(meter, name, unit, description) { throw null; }
        protected override System.Collections.Generic.IEnumerable<Measurement<T>> Observe() { throw null;}
    }
    public sealed class ObservableGauge<T> : ObservableInstrument<T> where T : struct
    {
        internal ObservableGauge(Meter meter, string name, string? unit, string? description) : base(meter, name, unit, description) { throw null; }
        protected override System.Collections.Generic.IEnumerable<Measurement<T>> Observe() { throw null; }
    }
    public abstract class ObservableInstrument<T> : Instrument where T : struct
    {
        public override bool IsObservable { get { throw null; } }
        protected ObservableInstrument(Meter meter, string name, string? unit, string? description) : this(meter, name, unit, description, tags: null) { throw null; }
        protected ObservableInstrument(Meter meter, string name, string? unit, string? description, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>> tags) : base(meter, name, unit, description) { throw null; }
        protected abstract System.Collections.Generic.IEnumerable<Measurement<T>> Observe();
    }
}
