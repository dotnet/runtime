// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Diagnostics.Tracing
{
    [System.FlagsAttribute]
    public enum EventActivityOptions
    {
        None = 0,
        Disable = 2,
        Recursive = 4,
        Detachable = 8,
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method)]
    public sealed partial class EventAttribute : System.Attribute
    {
        public EventAttribute(int eventId) { }
        public System.Diagnostics.Tracing.EventActivityOptions ActivityOptions { get { throw null; } set { } }
        public System.Diagnostics.Tracing.EventChannel Channel { get { throw null; } set { } }
        public int EventId { get { throw null; } }
        public System.Diagnostics.Tracing.EventKeywords Keywords { get { throw null; } set { } }
        public System.Diagnostics.Tracing.EventLevel Level { get { throw null; } set { } }
        public string? Message { get { throw null; } set { } }
        public System.Diagnostics.Tracing.EventOpcode Opcode { get { throw null; } set { } }
        public System.Diagnostics.Tracing.EventTags Tags { get { throw null; } set { } }
        public System.Diagnostics.Tracing.EventTask Task { get { throw null; } set { } }
        public byte Version { get { throw null; } set { } }
    }
    public enum EventChannel : byte
    {
        None = (byte)0,
        Admin = (byte)16,
        Operational = (byte)17,
        Analytic = (byte)18,
        Debug = (byte)19,
    }
    public enum EventCommand
    {
        Disable = -3,
        Enable = -2,
        SendManifest = -1,
        Update = 0,
    }
    public partial class EventCommandEventArgs : System.EventArgs
    {
        internal EventCommandEventArgs() { }
        public System.Collections.Generic.IDictionary<string, string?>? Arguments { get { throw null; } }
        public System.Diagnostics.Tracing.EventCommand Command { get { throw null; } }
        public bool DisableEvent(int eventId) { throw null; }
        public bool EnableEvent(int eventId) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Struct, Inherited=false)]
    public partial class EventDataAttribute : System.Attribute
    {
        public EventDataAttribute() { }
        public string? Name { get { throw null; } set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property)]
    public partial class EventFieldAttribute : System.Attribute
    {
        public EventFieldAttribute() { }
        public System.Diagnostics.Tracing.EventFieldFormat Format { get { throw null; } set { } }
        public System.Diagnostics.Tracing.EventFieldTags Tags { get { throw null; } set { } }
    }
    public enum EventFieldFormat
    {
        Default = 0,
        String = 2,
        Boolean = 3,
        Hexadecimal = 4,
        Xml = 11,
        Json = 12,
        HResult = 15,
    }
    [System.FlagsAttribute]
    public enum EventFieldTags
    {
        None = 0,
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property)]
    public partial class EventIgnoreAttribute : System.Attribute
    {
        public EventIgnoreAttribute() { }
    }
    [System.FlagsAttribute]
    public enum EventKeywords : long
    {
        All = (long)-1,
        None = (long)0,
        MicrosoftTelemetry = (long)562949953421312,
        WdiContext = (long)562949953421312,
        WdiDiagnostic = (long)1125899906842624,
        Sqm = (long)2251799813685248,
        AuditFailure = (long)4503599627370496,
        CorrelationHint = (long)4503599627370496,
        AuditSuccess = (long)9007199254740992,
        EventLogClassic = (long)36028797018963968,
    }
    public enum EventLevel
    {
        LogAlways = 0,
        Critical = 1,
        Error = 2,
        Warning = 3,
        Informational = 4,
        Verbose = 5,
    }
    public abstract partial class EventListener : System.IDisposable
    {
        protected EventListener() { }
        public event System.EventHandler<System.Diagnostics.Tracing.EventSourceCreatedEventArgs>? EventSourceCreated { add { } remove { } }
        public event System.EventHandler<System.Diagnostics.Tracing.EventWrittenEventArgs>? EventWritten { add { } remove { } }
        public void DisableEvents(System.Diagnostics.Tracing.EventSource eventSource) { }
        public virtual void Dispose() { }
        public void EnableEvents(System.Diagnostics.Tracing.EventSource eventSource, System.Diagnostics.Tracing.EventLevel level) { }
        public void EnableEvents(System.Diagnostics.Tracing.EventSource eventSource, System.Diagnostics.Tracing.EventLevel level, System.Diagnostics.Tracing.EventKeywords matchAnyKeyword) { }
        public void EnableEvents(System.Diagnostics.Tracing.EventSource eventSource, System.Diagnostics.Tracing.EventLevel level, System.Diagnostics.Tracing.EventKeywords matchAnyKeyword, System.Collections.Generic.IDictionary<string, string?>? arguments) { }
        protected static int EventSourceIndex(System.Diagnostics.Tracing.EventSource eventSource) { throw null; }
        protected internal virtual void OnEventSourceCreated(System.Diagnostics.Tracing.EventSource eventSource) { }
        protected internal virtual void OnEventWritten(System.Diagnostics.Tracing.EventWrittenEventArgs eventData) { }
    }
    [System.FlagsAttribute]
    public enum EventManifestOptions
    {
        None = 0,
        Strict = 1,
        AllCultures = 2,
        OnlyIfNeededForRegistration = 4,
        AllowEventSourceOverride = 8,
    }
    public enum EventOpcode
    {
        Info = 0,
        Start = 1,
        Stop = 2,
        DataCollectionStart = 3,
        DataCollectionStop = 4,
        Extension = 5,
        Reply = 6,
        Resume = 7,
        Suspend = 8,
        Send = 9,
        Receive = 240,
    }
    [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]
    public partial class EventSource : System.IDisposable
    {
        protected EventSource() { }
        protected EventSource(bool throwOnEventWriteErrors) { }
        protected EventSource(System.Diagnostics.Tracing.EventSourceSettings settings) { }
        protected EventSource(System.Diagnostics.Tracing.EventSourceSettings settings, params string[]? traits) { }
        public EventSource(string eventSourceName) { }
        public EventSource(string eventSourceName, System.Diagnostics.Tracing.EventSourceSettings config) { }
        public EventSource(string eventSourceName, System.Diagnostics.Tracing.EventSourceSettings config, params string[]? traits) { }
        public System.Exception? ConstructionException { get { throw null; } }
        public static System.Guid CurrentThreadActivityId { get { throw null; } }
        public System.Guid Guid { get { throw null; } }
        public string Name { get { throw null; } }
        public System.Diagnostics.Tracing.EventSourceSettings Settings { get { throw null; } }
        public event System.EventHandler<System.Diagnostics.Tracing.EventCommandEventArgs>? EventCommandExecuted { add { } remove { } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        ~EventSource() { }
        public static string? GenerateManifest([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type eventSourceType, string? assemblyPathToIncludeInManifest) { throw null; }
        public static string? GenerateManifest([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type eventSourceType, string? assemblyPathToIncludeInManifest, System.Diagnostics.Tracing.EventManifestOptions flags) { throw null; }
        public static System.Guid GetGuid(System.Type eventSourceType) { throw null; }
        public static string GetName(System.Type eventSourceType) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Diagnostics.Tracing.EventSource> GetSources() { throw null; }
        public string? GetTrait(string key) { throw null; }
        public bool IsEnabled() { throw null; }
        public bool IsEnabled(System.Diagnostics.Tracing.EventLevel level, System.Diagnostics.Tracing.EventKeywords keywords) { throw null; }
        public bool IsEnabled(System.Diagnostics.Tracing.EventLevel level, System.Diagnostics.Tracing.EventKeywords keywords, System.Diagnostics.Tracing.EventChannel channel) { throw null; }
        protected virtual void OnEventCommand(System.Diagnostics.Tracing.EventCommandEventArgs command) { }
        public static void SendCommand(System.Diagnostics.Tracing.EventSource eventSource, System.Diagnostics.Tracing.EventCommand command, System.Collections.Generic.IDictionary<string, string?>? commandArguments) { }
        public static void SetCurrentThreadActivityId(System.Guid activityId) { }
        public static void SetCurrentThreadActivityId(System.Guid activityId, out System.Guid oldActivityThatWillContinue) { throw null; }
        public override string ToString() { throw null; }
        public void Write(string? eventName) { }
        public void Write(string? eventName, System.Diagnostics.Tracing.EventSourceOptions options) { }
        protected void WriteEvent(int eventId) { }
        protected void WriteEvent(int eventId, byte[]? arg1) { }
        protected void WriteEvent(int eventId, int arg1) { }
        protected void WriteEvent(int eventId, int arg1, int arg2) { }
        protected void WriteEvent(int eventId, int arg1, int arg2, int arg3) { }
        protected void WriteEvent(int eventId, int arg1, string? arg2) { }
        protected void WriteEvent(int eventId, long arg1) { }
        protected void WriteEvent(int eventId, long arg1, byte[]? arg2) { }
        protected void WriteEvent(int eventId, long arg1, long arg2) { }
        protected void WriteEvent(int eventId, long arg1, long arg2, long arg3) { }
        protected void WriteEvent(int eventId, long arg1, string? arg2) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("EventSource will serialize the whole object graph. Trimmer will not safely handle this case because properties may be trimmed. This can be suppressed if the object is a primitive type")]
        protected void WriteEvent(int eventId, params object?[] args) { }
        protected void WriteEvent(int eventId, string? arg1) { }
        protected void WriteEvent(int eventId, string? arg1, int arg2) { }
        protected void WriteEvent(int eventId, string? arg1, int arg2, int arg3) { }
        protected void WriteEvent(int eventId, string? arg1, long arg2) { }
        protected void WriteEvent(int eventId, string? arg1, string? arg2) { }
        protected void WriteEvent(int eventId, string? arg1, string? arg2, string? arg3) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("EventSource will serialize the whole object graph. Trimmer will not safely handle this case because properties may be trimmed. This can be suppressed if the object is a primitive type")]
        [System.CLSCompliantAttribute(false)]
        protected unsafe void WriteEventCore(int eventId, int eventDataCount, System.Diagnostics.Tracing.EventSource.EventData* data) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("EventSource will serialize the whole object graph. Trimmer will not safely handle this case because properties may be trimmed. This can be suppressed if the object is a primitive type")]
        protected void WriteEventWithRelatedActivityId(int eventId, System.Guid relatedActivityId, params object?[] args) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("EventSource will serialize the whole object graph. Trimmer will not safely handle this case because properties may be trimmed. This can be suppressed if the object is a primitive type")]
        [System.CLSCompliantAttribute(false)]
        protected unsafe void WriteEventWithRelatedActivityIdCore(int eventId, System.Guid* relatedActivityId, int eventDataCount, System.Diagnostics.Tracing.EventSource.EventData* data) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("EventSource will serialize the whole object graph. Trimmer will not safely handle this case because properties may be trimmed. This can be suppressed if the object is a primitive type")]
        public void Write<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] T>(string? eventName, System.Diagnostics.Tracing.EventSourceOptions options, T data) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("EventSource will serialize the whole object graph. Trimmer will not safely handle this case because properties may be trimmed. This can be suppressed if the object is a primitive type")]
        public void Write<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] T>(string? eventName, ref System.Diagnostics.Tracing.EventSourceOptions options, ref System.Guid activityId, ref System.Guid relatedActivityId, ref T data) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("EventSource will serialize the whole object graph. Trimmer will not safely handle this case because properties may be trimmed. This can be suppressed if the object is a primitive type")]
        public void Write<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] T>(string? eventName, ref System.Diagnostics.Tracing.EventSourceOptions options, ref T data) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("EventSource will serialize the whole object graph. Trimmer will not safely handle this case because properties may be trimmed. This can be suppressed if the object is a primitive type")]
        public void Write<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] T>(string? eventName, T data) { }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        protected internal partial struct EventData
        {
            private int _dummyPrimitive;
            public System.IntPtr DataPointer { get { throw null; } set { } }
            public int Size { get { throw null; } set { } }
        }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class)]
    public sealed partial class EventSourceAttribute : System.Attribute
    {
        public EventSourceAttribute() { }
        public string? Guid { get { throw null; } set { } }
        public string? LocalizationResources { get { throw null; } set { } }
        public string? Name { get { throw null; } set { } }
    }
    public partial class EventSourceCreatedEventArgs : System.EventArgs
    {
        public EventSourceCreatedEventArgs() { }
        public System.Diagnostics.Tracing.EventSource? EventSource { get { throw null; } }
    }
    public partial class EventSourceException : System.Exception
    {
        public EventSourceException() { }
        protected EventSourceException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public EventSourceException(string? message) { }
        public EventSourceException(string? message, System.Exception? innerException) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct EventSourceOptions
    {
        private int _dummyPrimitive;
        public System.Diagnostics.Tracing.EventActivityOptions ActivityOptions { get { throw null; } set { } }
        public System.Diagnostics.Tracing.EventKeywords Keywords { get { throw null; } set { } }
        public System.Diagnostics.Tracing.EventLevel Level { get { throw null; } set { } }
        public System.Diagnostics.Tracing.EventOpcode Opcode { get { throw null; } set { } }
        public System.Diagnostics.Tracing.EventTags Tags { get { throw null; } set { } }
    }
    [System.FlagsAttribute]
    public enum EventSourceSettings
    {
        Default = 0,
        ThrowOnEventWriteErrors = 1,
        EtwManifestEventFormat = 4,
        EtwSelfDescribingEventFormat = 8,
    }
    [System.FlagsAttribute]
    public enum EventTags
    {
        None = 0,
    }
    public enum EventTask
    {
        None = 0,
    }
    public partial class EventWrittenEventArgs : System.EventArgs
    {
        internal EventWrittenEventArgs() { }
        public System.Guid ActivityId { get { throw null; } }
        public System.Diagnostics.Tracing.EventChannel Channel { get { throw null; } }
        public int EventId { get { throw null; } }
        public string? EventName { get { throw null; } }
        public System.Diagnostics.Tracing.EventSource EventSource { get { throw null; } }
        public System.Diagnostics.Tracing.EventKeywords Keywords { get { throw null; } }
        public System.Diagnostics.Tracing.EventLevel Level { get { throw null; } }
        public string? Message { get { throw null; } }
        public System.Diagnostics.Tracing.EventOpcode Opcode { get { throw null; } }
        public long OSThreadId { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<object?>? Payload { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<string>? PayloadNames { get { throw null; } }
        public System.Guid RelatedActivityId { get { throw null; } }
        public System.Diagnostics.Tracing.EventTags Tags { get { throw null; } }
        public System.Diagnostics.Tracing.EventTask Task { get { throw null; } }
        public System.DateTime TimeStamp { get { throw null; } }
        public byte Version { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method)]
    public sealed partial class NonEventAttribute : System.Attribute
    {
        public NonEventAttribute() { }
    }
}
