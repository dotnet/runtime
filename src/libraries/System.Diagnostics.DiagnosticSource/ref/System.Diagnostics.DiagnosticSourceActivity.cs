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
        public string Id { get { throw null; } }
        public System.Diagnostics.ActivityIdFormat IdFormat { get { throw null; } }
        public string OperationName { get { throw null; } }
        public System.Diagnostics.Activity Parent { get { throw null; } }
        public string ParentId { get { throw null; } }
        public ref System.Diagnostics.ActivitySpanId ParentSpanId { get { throw null; } }
        public string RootId { get { throw null; } }
        public ref System.Diagnostics.ActivitySpanId SpanId { get { throw null; } }
        public System.DateTime StartTimeUtc { get { throw null; } }
        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>> Tags { get { throw null; } }
        public ref System.Diagnostics.ActivityTraceId TraceId { get { throw null; } }
        public string TraceStateString { get { throw null; } set { } }
        public System.Diagnostics.Activity AddBaggage(string key, string value) { throw null; }
        public System.Diagnostics.Activity AddTag(string key, string value) { throw null; }
        public string GetBaggageItem(string key) { throw null; }
        public System.Diagnostics.Activity SetEndTime(System.DateTime endTimeUtc) { throw null; }
        public System.Diagnostics.Activity SetParentId(in System.Diagnostics.ActivityTraceId traceId, in System.Diagnostics.ActivitySpanId spanId) { throw null; }
        public System.Diagnostics.Activity SetParentId(string parentId) { throw null; }
        public System.Diagnostics.Activity SetStartTime(System.DateTime startTimeUtc) { throw null; }
        public System.Diagnostics.Activity Start() { throw null; }
        public void Stop() { }
    }
    public enum ActivityIdFormat : byte
    {
        Hierarchical = (byte)1,
        Unknown = (byte)0,
        W3C = (byte)2,
    }
#if ALLOW_PARTIALLY_TRUSTED_CALLERS
    [System.Security.SecuritySafeCriticalAttribute]
#endif
    public partial struct ActivitySpanId : System.IEquatable<System.Diagnostics.ActivitySpanId>
    {
        private object _dummy;
        private int _dummyPrimitive;
        public ActivitySpanId(System.ReadOnlySpan<byte> idData, bool isUtf8Chars = false) { throw null; }
        public ActivitySpanId(System.ReadOnlySpan<char> idData) { throw null; }
        public string AsHexString { get { throw null; } }
        public void CopyTo(System.Span<byte> destination) { }
        public bool Equals(System.Diagnostics.ActivitySpanId spanId) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Diagnostics.ActivitySpanId NewSpanId() { throw null; }
        public static bool operator ==(in System.Diagnostics.ActivitySpanId spanId1, in System.Diagnostics.ActivitySpanId spandId2) { throw null; }
        public static bool operator !=(in System.Diagnostics.ActivitySpanId spanId1, in System.Diagnostics.ActivitySpanId spandId2) { throw null; }
        public override string ToString() { throw null; }
    }
#if ALLOW_PARTIALLY_TRUSTED_CALLERS
    [System.Security.SecuritySafeCriticalAttribute]
#endif
    public partial struct ActivityTraceId : System.IEquatable<System.Diagnostics.ActivityTraceId>
    {
        private object _dummy;
        private int _dummyPrimitive;
        public ActivityTraceId(System.ReadOnlySpan<byte> idData, bool isUtf8Chars = false) { throw null; }
        public ActivityTraceId(System.ReadOnlySpan<char> idData) { throw null; }
        public string AsHexString { get { throw null; } }
        public void CopyTo(System.Span<byte> destination) { }
        public bool Equals(System.Diagnostics.ActivityTraceId traceId) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Diagnostics.ActivityTraceId NewTraceId() { throw null; }
        public static bool operator ==(in System.Diagnostics.ActivityTraceId traceId1, in System.Diagnostics.ActivityTraceId traceId2) { throw null; }
        public static bool operator !=(in System.Diagnostics.ActivityTraceId traceId1, in System.Diagnostics.ActivityTraceId traceId2) { throw null; }
        public override string ToString() { throw null; }
    }
    public abstract partial class DiagnosticSource
    {
        public System.Diagnostics.Activity StartActivity(System.Diagnostics.Activity activity, object args) { throw null; }
        public void StopActivity(System.Diagnostics.Activity activity, object args) { }
    }
}
