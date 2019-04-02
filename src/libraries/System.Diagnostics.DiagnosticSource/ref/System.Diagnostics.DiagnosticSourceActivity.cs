// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Diagnostics
{
    public partial class Activity
    {
        public Activity(string operationName) { }
        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>> Baggage { get { throw null; } }
        public static System.Diagnostics.Activity Current
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
        public string Id {
#if ALLOW_PARTIALLY_TRUSTED_CALLERS
        [System.Security.SecuritySafeCriticalAttribute]
#endif
            get { throw null; } }
        public System.Diagnostics.ActivityIdFormat IdFormat { get { throw null; } }
        public string OperationName { get { throw null; } }
        public System.Diagnostics.Activity Parent { get { throw null; } }
        public string ParentId { get { throw null; } }
        public ref readonly System.Diagnostics.ActivitySpanId ParentSpanId { get { throw null; } }
        public bool Recorded { get { throw null; } }
        public string RootId { get { throw null; } }
        public ref readonly System.Diagnostics.ActivitySpanId SpanId { get { throw null; } }
        public System.DateTime StartTimeUtc { get { throw null; } }
        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>> Tags { get { throw null; } }
        public ref readonly System.Diagnostics.ActivityTraceId TraceId { get { throw null; } }
        public string TraceStateString { get { throw null; } set { } }
        public System.Diagnostics.ActivityTraceFlags ActivityTraceFlags { get { throw null; } set { } }
        public System.Diagnostics.Activity AddBaggage(string key, string value) { throw null; }
        public System.Diagnostics.Activity AddTag(string key, string value) { throw null; }
        public string GetBaggageItem(string key) { throw null; }
        public System.Diagnostics.Activity SetEndTime(System.DateTime endTimeUtc) { throw null; }
        public System.Diagnostics.Activity SetParentId(in System.Diagnostics.ActivityTraceId traceId, in System.Diagnostics.ActivitySpanId spanId, ActivityTraceFlags activityTraceFlags = ActivityTraceFlags.None) { throw null; }
        public System.Diagnostics.Activity SetParentId(string parentId) { throw null; }
        public System.Diagnostics.Activity SetStartTime(System.DateTime startTimeUtc) { throw null; }
        public System.Diagnostics.Activity Start() { throw null; }
        public void Stop() { }
    }
    public enum ActivityIdFormat
    {
        Hierarchical = 1,
        Unknown = 0,
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
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(in System.Diagnostics.ActivitySpanId spanId1, in System.Diagnostics.ActivitySpanId spandId2) { throw null; }
        public static bool operator !=(in System.Diagnostics.ActivitySpanId spanId1, in System.Diagnostics.ActivitySpanId spandId2) { throw null; }
        public string ToHexString() { throw null; }
        public override string ToString() { throw null; }
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
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(in System.Diagnostics.ActivityTraceId traceId1, in System.Diagnostics.ActivityTraceId traceId2) { throw null; }
        public static bool operator !=(in System.Diagnostics.ActivityTraceId traceId1, in System.Diagnostics.ActivityTraceId traceId2) { throw null; }
        public string ToHexString() { throw null; }
        public override string ToString() { throw null; }
    }
    [System.FlagsAttribute]
    public enum ActivityTraceFlags
    {
        None = 0,
        Recorded = 1,
    }
    public partial class DiagnosticListener
    {
        public override void OnActivityExport(System.Diagnostics.Activity activity, object payload) { }
        public override void OnActivityImport(System.Diagnostics.Activity activity, object payload) { }
        public virtual System.IDisposable Subscribe(System.IObserver<System.Collections.Generic.KeyValuePair<string, object>> observer, System.Func<string, object, object, bool> isEnabled, System.Action<System.Diagnostics.Activity, object> onActivityImport = null, System.Action<System.Diagnostics.Activity, object> onActivityExport = null) { throw null; }
    }
    public abstract partial class DiagnosticSource
    {
        public virtual void OnActivityExport(System.Diagnostics.Activity activity, object payload) { }
        public virtual void OnActivityImport(System.Diagnostics.Activity activity, object payload) { }
        public System.Diagnostics.Activity StartActivity(System.Diagnostics.Activity activity, object args) { throw null; }
        public void StopActivity(System.Diagnostics.Activity activity, object args) { }
    }
}
