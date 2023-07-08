// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Diagnostics
{
    public partial class EntryWrittenEventArgs : System.EventArgs
    {
        public EntryWrittenEventArgs() { }
        public EntryWrittenEventArgs(System.Diagnostics.EventLogEntry entry) { }
        public System.Diagnostics.EventLogEntry Entry { get { throw null; } }
    }
    public delegate void EntryWrittenEventHandler(object sender, System.Diagnostics.EntryWrittenEventArgs e);
    public partial class EventInstance
    {
        public EventInstance(long instanceId, int categoryId) { }
        public EventInstance(long instanceId, int categoryId, System.Diagnostics.EventLogEntryType entryType) { }
        public int CategoryId { get { throw null; } set { } }
        public System.Diagnostics.EventLogEntryType EntryType { get { throw null; } set { } }
        public long InstanceId { get { throw null; } set { } }
    }
    [System.ComponentModel.DefaultEventAttribute("EntryWritten")]
    public partial class EventLog : System.ComponentModel.Component, System.ComponentModel.ISupportInitialize
    {
        public EventLog() { }
        public EventLog(string logName) { }
        public EventLog(string logName, string machineName) { }
        public EventLog(string logName, string machineName, string source) { }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool EnableRaisingEvents { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Diagnostics.EventLogEntryCollection Entries { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.ComponentModel.ReadOnlyAttribute(true)]
        [System.ComponentModel.SettingsBindableAttribute(true)]
        public string Log { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public string LogDisplayName { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(".")]
        [System.ComponentModel.ReadOnlyAttribute(true)]
        [System.ComponentModel.SettingsBindableAttribute(true)]
        public string MachineName { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public long MaximumKilobytes { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public int MinimumRetentionDays { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public System.Diagnostics.OverflowAction OverflowAction { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.ComponentModel.ReadOnlyAttribute(true)]
        [System.ComponentModel.SettingsBindableAttribute(true)]
        public string Source { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DefaultValueAttribute(null)]
        public System.ComponentModel.ISynchronizeInvoke SynchronizingObject { get { throw null; } set { } }
        public event System.Diagnostics.EntryWrittenEventHandler EntryWritten { add { } remove { } }
        public void BeginInit() { }
        public void Clear() { }
        public void Close() { }
        public static void CreateEventSource(System.Diagnostics.EventSourceCreationData sourceData) { }
        public static void CreateEventSource(string source, string logName) { }
        [System.ObsoleteAttribute("EventLog.CreateEventSource has been deprecated. Use System.Diagnostics.EventLog.CreateEventSource(EventSourceCreationData sourceData) instead.")]
        public static void CreateEventSource(string source, string logName, string machineName) { }
        public static void Delete(string logName) { }
        public static void Delete(string logName, string machineName) { }
        public static void DeleteEventSource(string source) { }
        public static void DeleteEventSource(string source, string machineName) { }
        protected override void Dispose(bool disposing) { }
        public void EndInit() { }
        public static bool Exists(string logName) { throw null; }
        public static bool Exists(string logName, string machineName) { throw null; }
        public static System.Diagnostics.EventLog[] GetEventLogs() { throw null; }
        public static System.Diagnostics.EventLog[] GetEventLogs(string machineName) { throw null; }
        public static string LogNameFromSourceName(string source, string machineName) { throw null; }
        public void ModifyOverflowPolicy(System.Diagnostics.OverflowAction action, int retentionDays) { }
        public void RegisterDisplayName(string resourceFile, long resourceId) { }
        public static bool SourceExists(string source) { throw null; }
        public static bool SourceExists(string source, string machineName) { throw null; }
        public void WriteEntry(string message) { }
        public void WriteEntry(string message, System.Diagnostics.EventLogEntryType type) { }
        public void WriteEntry(string message, System.Diagnostics.EventLogEntryType type, int eventID) { }
        public void WriteEntry(string message, System.Diagnostics.EventLogEntryType type, int eventID, short category) { }
        public void WriteEntry(string message, System.Diagnostics.EventLogEntryType type, int eventID, short category, byte[] rawData) { }
        public static void WriteEntry(string source, string message) { }
        public static void WriteEntry(string source, string message, System.Diagnostics.EventLogEntryType type) { }
        public static void WriteEntry(string source, string message, System.Diagnostics.EventLogEntryType type, int eventID) { }
        public static void WriteEntry(string source, string message, System.Diagnostics.EventLogEntryType type, int eventID, short category) { }
        public static void WriteEntry(string source, string message, System.Diagnostics.EventLogEntryType type, int eventID, short category, byte[] rawData) { }
        public void WriteEvent(System.Diagnostics.EventInstance instance, byte[] data, params object[] values) { }
        public void WriteEvent(System.Diagnostics.EventInstance instance, params object[] values) { }
        public static void WriteEvent(string source, System.Diagnostics.EventInstance instance, byte[] data, params object[] values) { }
        public static void WriteEvent(string source, System.Diagnostics.EventInstance instance, params object[] values) { }
    }
    [System.ComponentModel.DesignTimeVisibleAttribute(false)]
    [System.ComponentModel.ToolboxItemAttribute(false)]
    public sealed partial class EventLogEntry : System.ComponentModel.Component, System.Runtime.Serialization.ISerializable
    {
        internal EventLogEntry() { }
        public string Category { get { throw null; } }
        public short CategoryNumber { get { throw null; } }
        public byte[] Data { get { throw null; } }
        public System.Diagnostics.EventLogEntryType EntryType { get { throw null; } }
        [System.ObsoleteAttribute("EventLogEntry.EventID has been deprecated. Use System.Diagnostics.EventLogEntry.InstanceId instead.")]
        public int EventID { get { throw null; } }
        public int Index { get { throw null; } }
        public long InstanceId { get { throw null; } }
        public string MachineName { get { throw null; } }
        [System.ComponentModel.EditorAttribute("System.ComponentModel.Design.BinaryEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public string Message { get { throw null; } }
        public string[] ReplacementStrings { get { throw null; } }
        public string Source { get { throw null; } }
        public System.DateTime TimeGenerated { get { throw null; } }
        public System.DateTime TimeWritten { get { throw null; } }
        public string UserName { get { throw null; } }
        public bool Equals(System.Diagnostics.EventLogEntry otherEntry) { throw null; }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class EventLogEntryCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        internal EventLogEntryCollection() { }
        public int Count { get { throw null; } }
        public virtual System.Diagnostics.EventLogEntry this[int index] { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        public void CopyTo(System.Diagnostics.EventLogEntry[] entries, int index) { }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
    }
    public enum EventLogEntryType
    {
        Error = 1,
        Warning = 2,
        Information = 4,
        SuccessAudit = 8,
        FailureAudit = 16,
    }
    public sealed partial class EventLogTraceListener : System.Diagnostics.TraceListener
    {
        public EventLogTraceListener() { }
        public EventLogTraceListener(System.Diagnostics.EventLog eventLog) { }
        public EventLogTraceListener(string source) { }
        public System.Diagnostics.EventLog EventLog { get { throw null; } set { } }
        public override string Name { get { throw null; } set { } }
        public override void Close() { }
        protected override void Dispose(bool disposing) { }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override void TraceData(System.Diagnostics.TraceEventCache eventCache, string source, System.Diagnostics.TraceEventType severity, int id, object data) { }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override void TraceData(System.Diagnostics.TraceEventCache eventCache, string source, System.Diagnostics.TraceEventType severity, int id, params object[] data) { }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override void TraceEvent(System.Diagnostics.TraceEventCache eventCache, string source, System.Diagnostics.TraceEventType severity, int id, string message) { }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override void TraceEvent(System.Diagnostics.TraceEventCache eventCache, string source, System.Diagnostics.TraceEventType severity, int id, string format, params object[] args) { }
        public override void Write(string message) { }
        public override void WriteLine(string message) { }
    }
    public partial class EventSourceCreationData
    {
        public EventSourceCreationData(string source, string logName) { }
        public int CategoryCount { get { throw null; } set { } }
        public string CategoryResourceFile { get { throw null; } set { } }
        public string LogName { get { throw null; } set { } }
        public string MachineName { get { throw null; } set { } }
        public string MessageResourceFile { get { throw null; } set { } }
        public string ParameterResourceFile { get { throw null; } set { } }
        public string Source { get { throw null; } set { } }
    }
    public enum OverflowAction
    {
        DoNotOverwrite = -1,
        OverwriteAsNeeded = 0,
        OverwriteOlder = 1,
    }
}
namespace System.Diagnostics.Eventing.Reader
{
    public partial class EventBookmark
    {
        internal EventBookmark() { }
    }
    public sealed partial class EventKeyword
    {
        internal EventKeyword() { }
        public string DisplayName { get { throw null; } }
        public string Name { get { throw null; } }
        public long Value { get { throw null; } }
    }
    public sealed partial class EventLevel
    {
        internal EventLevel() { }
        public string DisplayName { get { throw null; } }
        public string Name { get { throw null; } }
        public int Value { get { throw null; } }
    }
    public partial class EventLogConfiguration : System.IDisposable
    {
        public EventLogConfiguration(string logName) { }
        public EventLogConfiguration(string logName, System.Diagnostics.Eventing.Reader.EventLogSession session) { }
        public bool IsClassicLog { get { throw null; } }
        public bool IsEnabled { get { throw null; } set { } }
        public string LogFilePath { get { throw null; } set { } }
        public System.Diagnostics.Eventing.Reader.EventLogIsolation LogIsolation { get { throw null; } }
        public System.Diagnostics.Eventing.Reader.EventLogMode LogMode { get { throw null; } set { } }
        public string LogName { get { throw null; } }
        public System.Diagnostics.Eventing.Reader.EventLogType LogType { get { throw null; } }
        public long MaximumSizeInBytes { get { throw null; } set { } }
        public string OwningProviderName { get { throw null; } }
        public int? ProviderBufferSize { get { throw null; } }
        public System.Guid? ProviderControlGuid { get { throw null; } }
        public long? ProviderKeywords { get { throw null; } set { } }
        public int? ProviderLatency { get { throw null; } }
        public int? ProviderLevel { get { throw null; } set { } }
        public int? ProviderMaximumNumberOfBuffers { get { throw null; } }
        public int? ProviderMinimumNumberOfBuffers { get { throw null; } }
        public System.Collections.Generic.IEnumerable<string> ProviderNames { get { throw null; } }
        public string SecurityDescriptor { get { throw null; } set { } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public void SaveChanges() { }
    }
    public partial class EventLogException : System.Exception
    {
        public EventLogException() { }
        protected EventLogException(int errorCode) { }
#if NET8_0_OR_GREATER
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        protected EventLogException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public EventLogException(string message) { }
        public EventLogException(string message, System.Exception innerException) { }
        public override string Message { get { throw null; } }
#if NET8_0_OR_GREATER
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public sealed partial class EventLogInformation
    {
        internal EventLogInformation() { }
        public int? Attributes { get { throw null; } }
        public System.DateTime? CreationTime { get { throw null; } }
        public long? FileSize { get { throw null; } }
        public bool? IsLogFull { get { throw null; } }
        public System.DateTime? LastAccessTime { get { throw null; } }
        public System.DateTime? LastWriteTime { get { throw null; } }
        public long? OldestRecordNumber { get { throw null; } }
        public long? RecordCount { get { throw null; } }
    }
    public partial class EventLogInvalidDataException : System.Diagnostics.Eventing.Reader.EventLogException
    {
        public EventLogInvalidDataException() { }
#if NET8_0_OR_GREATER
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        protected EventLogInvalidDataException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public EventLogInvalidDataException(string message) { }
        public EventLogInvalidDataException(string message, System.Exception innerException) { }
    }
    public enum EventLogIsolation
    {
        Application = 0,
        System = 1,
        Custom = 2,
    }
    public sealed partial class EventLogLink
    {
        internal EventLogLink() { }
        public string DisplayName { get { throw null; } }
        public bool IsImported { get { throw null; } }
        public string LogName { get { throw null; } }
    }
    public enum EventLogMode
    {
        Circular = 0,
        AutoBackup = 1,
        Retain = 2,
    }
    public partial class EventLogNotFoundException : System.Diagnostics.Eventing.Reader.EventLogException
    {
        public EventLogNotFoundException() { }
#if NET8_0_OR_GREATER
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        protected EventLogNotFoundException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public EventLogNotFoundException(string message) { }
        public EventLogNotFoundException(string message, System.Exception innerException) { }
    }
    public partial class EventLogPropertySelector : System.IDisposable
    {
        public EventLogPropertySelector(System.Collections.Generic.IEnumerable<string> propertyQueries) { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
    }
    public partial class EventLogProviderDisabledException : System.Diagnostics.Eventing.Reader.EventLogException
    {
        public EventLogProviderDisabledException() { }
#if NET8_0_OR_GREATER
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        protected EventLogProviderDisabledException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public EventLogProviderDisabledException(string message) { }
        public EventLogProviderDisabledException(string message, System.Exception innerException) { }
    }
    public partial class EventLogQuery
    {
        public EventLogQuery(string path, System.Diagnostics.Eventing.Reader.PathType pathType) { }
        public EventLogQuery(string path, System.Diagnostics.Eventing.Reader.PathType pathType, string query) { }
        public bool ReverseDirection { get { throw null; } set { } }
        public System.Diagnostics.Eventing.Reader.EventLogSession Session { get { throw null; } set { } }
        public bool TolerateQueryErrors { get { throw null; } set { } }
    }
    public partial class EventLogReader : System.IDisposable
    {
        public EventLogReader(System.Diagnostics.Eventing.Reader.EventLogQuery eventQuery) { }
        public EventLogReader(System.Diagnostics.Eventing.Reader.EventLogQuery eventQuery, System.Diagnostics.Eventing.Reader.EventBookmark bookmark) { }
        public EventLogReader(string path) { }
        public EventLogReader(string path, System.Diagnostics.Eventing.Reader.PathType pathType) { }
        public int BatchSize { get { throw null; } set { } }
        public System.Collections.Generic.IList<System.Diagnostics.Eventing.Reader.EventLogStatus> LogStatus { get { throw null; } }
        public void CancelReading() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public System.Diagnostics.Eventing.Reader.EventRecord ReadEvent() { throw null; }
        public System.Diagnostics.Eventing.Reader.EventRecord ReadEvent(System.TimeSpan timeout) { throw null; }
        public void Seek(System.Diagnostics.Eventing.Reader.EventBookmark bookmark) { }
        public void Seek(System.Diagnostics.Eventing.Reader.EventBookmark bookmark, long offset) { }
        public void Seek(System.IO.SeekOrigin origin, long offset) { }
    }
    public partial class EventLogReadingException : System.Diagnostics.Eventing.Reader.EventLogException
    {
        public EventLogReadingException() { }
#if NET8_0_OR_GREATER
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        protected EventLogReadingException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public EventLogReadingException(string message) { }
        public EventLogReadingException(string message, System.Exception innerException) { }
    }
    public partial class EventLogRecord : System.Diagnostics.Eventing.Reader.EventRecord
    {
        internal EventLogRecord() { }
        public override System.Guid? ActivityId { get { throw null; } }
        public override System.Diagnostics.Eventing.Reader.EventBookmark Bookmark { get { throw null; } }
        public string ContainerLog { get { throw null; } }
        public override int Id { get { throw null; } }
        public override long? Keywords { get { throw null; } }
        public override System.Collections.Generic.IEnumerable<string> KeywordsDisplayNames { get { throw null; } }
        public override byte? Level { get { throw null; } }
        public override string LevelDisplayName { get { throw null; } }
        public override string LogName { get { throw null; } }
        public override string MachineName { get { throw null; } }
        public System.Collections.Generic.IEnumerable<int> MatchedQueryIds { get { throw null; } }
        public override short? Opcode { get { throw null; } }
        public override string OpcodeDisplayName { get { throw null; } }
        public override int? ProcessId { get { throw null; } }
        public override System.Collections.Generic.IList<System.Diagnostics.Eventing.Reader.EventProperty> Properties { get { throw null; } }
        public override System.Guid? ProviderId { get { throw null; } }
        public override string ProviderName { get { throw null; } }
        public override int? Qualifiers { get { throw null; } }
        public override long? RecordId { get { throw null; } }
        public override System.Guid? RelatedActivityId { get { throw null; } }
        public override int? Task { get { throw null; } }
        public override string TaskDisplayName { get { throw null; } }
        public override int? ThreadId { get { throw null; } }
        public override System.DateTime? TimeCreated { get { throw null; } }
        public override System.Security.Principal.SecurityIdentifier UserId { get { throw null; } }
        public override byte? Version { get { throw null; } }
        protected override void Dispose(bool disposing) { }
        public override string FormatDescription() { throw null; }
        public override string FormatDescription(System.Collections.Generic.IEnumerable<object> values) { throw null; }
        public System.Collections.Generic.IList<object> GetPropertyValues(System.Diagnostics.Eventing.Reader.EventLogPropertySelector propertySelector) { throw null; }
        public override string ToXml() { throw null; }
    }
    public partial class EventLogSession : System.IDisposable
    {
        public EventLogSession() { }
        public EventLogSession(string server) { }
        public EventLogSession(string server, string domain, string user, System.Security.SecureString password, System.Diagnostics.Eventing.Reader.SessionAuthentication logOnType) { }
        public static System.Diagnostics.Eventing.Reader.EventLogSession GlobalSession { get { throw null; } }
        public void CancelCurrentOperations() { }
        public void ClearLog(string logName) { }
        public void ClearLog(string logName, string backupPath) { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public void ExportLog(string path, System.Diagnostics.Eventing.Reader.PathType pathType, string query, string targetFilePath) { }
        public void ExportLog(string path, System.Diagnostics.Eventing.Reader.PathType pathType, string query, string targetFilePath, bool tolerateQueryErrors) { }
        public void ExportLogAndMessages(string path, System.Diagnostics.Eventing.Reader.PathType pathType, string query, string targetFilePath) { }
        public void ExportLogAndMessages(string path, System.Diagnostics.Eventing.Reader.PathType pathType, string query, string targetFilePath, bool tolerateQueryErrors, System.Globalization.CultureInfo targetCultureInfo) { }
        public System.Diagnostics.Eventing.Reader.EventLogInformation GetLogInformation(string logName, System.Diagnostics.Eventing.Reader.PathType pathType) { throw null; }
        public System.Collections.Generic.IEnumerable<string> GetLogNames() { throw null; }
        public System.Collections.Generic.IEnumerable<string> GetProviderNames() { throw null; }
    }
    public sealed partial class EventLogStatus
    {
        internal EventLogStatus() { }
        public string LogName { get { throw null; } }
        public int StatusCode { get { throw null; } }
    }
    public enum EventLogType
    {
        Administrative = 0,
        Operational = 1,
        Analytical = 2,
        Debug = 3,
    }
    public partial class EventLogWatcher : System.IDisposable
    {
        public EventLogWatcher(System.Diagnostics.Eventing.Reader.EventLogQuery eventQuery) { }
        public EventLogWatcher(System.Diagnostics.Eventing.Reader.EventLogQuery eventQuery, System.Diagnostics.Eventing.Reader.EventBookmark bookmark) { }
        public EventLogWatcher(System.Diagnostics.Eventing.Reader.EventLogQuery eventQuery, System.Diagnostics.Eventing.Reader.EventBookmark bookmark, bool readExistingEvents) { }
        public EventLogWatcher(string path) { }
        public bool Enabled { get { throw null; } set { } }
        public event System.EventHandler<System.Diagnostics.Eventing.Reader.EventRecordWrittenEventArgs> EventRecordWritten { add { } remove { } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
    }
    public sealed partial class EventMetadata
    {
        internal EventMetadata() { }
        public string Description { get { throw null; } }
        public long Id { get { throw null; } }
        public System.Collections.Generic.IEnumerable<System.Diagnostics.Eventing.Reader.EventKeyword> Keywords { get { throw null; } }
        public System.Diagnostics.Eventing.Reader.EventLevel Level { get { throw null; } }
        public System.Diagnostics.Eventing.Reader.EventLogLink LogLink { get { throw null; } }
        public System.Diagnostics.Eventing.Reader.EventOpcode Opcode { get { throw null; } }
        public System.Diagnostics.Eventing.Reader.EventTask Task { get { throw null; } }
        public string Template { get { throw null; } }
        public byte Version { get { throw null; } }
    }
    public sealed partial class EventOpcode
    {
        internal EventOpcode() { }
        public string DisplayName { get { throw null; } }
        public string Name { get { throw null; } }
        public int Value { get { throw null; } }
    }
    public sealed partial class EventProperty
    {
        internal EventProperty() { }
        public object Value { get { throw null; } }
    }
    public abstract partial class EventRecord : System.IDisposable
    {
        protected EventRecord() { }
        public abstract System.Guid? ActivityId { get; }
        public abstract System.Diagnostics.Eventing.Reader.EventBookmark Bookmark { get; }
        public abstract int Id { get; }
        public abstract long? Keywords { get; }
        public abstract System.Collections.Generic.IEnumerable<string> KeywordsDisplayNames { get; }
        public abstract byte? Level { get; }
        public abstract string LevelDisplayName { get; }
        public abstract string LogName { get; }
        public abstract string MachineName { get; }
        public abstract short? Opcode { get; }
        public abstract string OpcodeDisplayName { get; }
        public abstract int? ProcessId { get; }
        public abstract System.Collections.Generic.IList<System.Diagnostics.Eventing.Reader.EventProperty> Properties { get; }
        public abstract System.Guid? ProviderId { get; }
        public abstract string ProviderName { get; }
        public abstract int? Qualifiers { get; }
        public abstract long? RecordId { get; }
        public abstract System.Guid? RelatedActivityId { get; }
        public abstract int? Task { get; }
        public abstract string TaskDisplayName { get; }
        public abstract int? ThreadId { get; }
        public abstract System.DateTime? TimeCreated { get; }
        public abstract System.Security.Principal.SecurityIdentifier UserId { get; }
        public abstract byte? Version { get; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public abstract string FormatDescription();
        public abstract string FormatDescription(System.Collections.Generic.IEnumerable<object> values);
        public abstract string ToXml();
    }
    public sealed partial class EventRecordWrittenEventArgs : System.EventArgs
    {
        internal EventRecordWrittenEventArgs() { }
        public System.Exception EventException { get { throw null; } }
        public System.Diagnostics.Eventing.Reader.EventRecord EventRecord { get { throw null; } }
    }
    public sealed partial class EventTask
    {
        internal EventTask() { }
        public string DisplayName { get { throw null; } }
        public System.Guid EventGuid { get { throw null; } }
        public string Name { get { throw null; } }
        public int Value { get { throw null; } }
    }
    public enum PathType
    {
        LogName = 1,
        FilePath = 2,
    }
    public partial class ProviderMetadata : System.IDisposable
    {
        public ProviderMetadata(string providerName) { }
        public ProviderMetadata(string providerName, System.Diagnostics.Eventing.Reader.EventLogSession session, System.Globalization.CultureInfo targetCultureInfo) { }
        public string DisplayName { get { throw null; } }
        public System.Collections.Generic.IEnumerable<System.Diagnostics.Eventing.Reader.EventMetadata> Events { get { throw null; } }
        public System.Uri HelpLink { get { throw null; } }
        public System.Guid Id { get { throw null; } }
        public System.Collections.Generic.IList<System.Diagnostics.Eventing.Reader.EventKeyword> Keywords { get { throw null; } }
        public System.Collections.Generic.IList<System.Diagnostics.Eventing.Reader.EventLevel> Levels { get { throw null; } }
        public System.Collections.Generic.IList<System.Diagnostics.Eventing.Reader.EventLogLink> LogLinks { get { throw null; } }
        public string MessageFilePath { get { throw null; } }
        public string Name { get { throw null; } }
        public System.Collections.Generic.IList<System.Diagnostics.Eventing.Reader.EventOpcode> Opcodes { get { throw null; } }
        public string ParameterFilePath { get { throw null; } }
        public string ResourceFilePath { get { throw null; } }
        public System.Collections.Generic.IList<System.Diagnostics.Eventing.Reader.EventTask> Tasks { get { throw null; } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
    }
    public enum SessionAuthentication
    {
        Default = 0,
        Negotiate = 1,
        Kerberos = 2,
        Ntlm = 3,
    }
    [System.FlagsAttribute]
    public enum StandardEventKeywords : long
    {
        None = (long)0,
        ResponseTime = (long)281474976710656,
        WdiContext = (long)562949953421312,
        WdiDiagnostic = (long)1125899906842624,
        Sqm = (long)2251799813685248,
        AuditFailure = (long)4503599627370496,
        [System.ObsoleteAttribute("StandardEventKeywords.CorrelationHint has an incorrect value and has been deprecated. Use CorrelationHint2 instead.")]
        CorrelationHint = (long)4503599627370496,
        AuditSuccess = (long)9007199254740992,
        CorrelationHint2 = (long)18014398509481984,
        EventLogClassic = (long)36028797018963968,
    }
    public enum StandardEventLevel
    {
        LogAlways = 0,
        Critical = 1,
        Error = 2,
        Warning = 3,
        Informational = 4,
        Verbose = 5,
    }
    public enum StandardEventOpcode
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
    public enum StandardEventTask
    {
        None = 0,
    }
}
