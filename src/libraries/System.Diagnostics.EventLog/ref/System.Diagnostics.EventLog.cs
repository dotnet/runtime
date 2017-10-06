// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    [System.Diagnostics.MonitoringDescriptionAttribute("Provides interaction with Windows event logs.")]
    public partial class EventLog : System.ComponentModel.Component, System.ComponentModel.ISupportInitialize
    {
        public EventLog() { }
        public EventLog(string logName) { }
        public EventLog(string logName, string machineName) { }
        public EventLog(string logName, string machineName, string source) { }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DefaultValueAttribute(false)]
        [System.Diagnostics.MonitoringDescriptionAttribute("Indicates if the component monitors the event log for changes.")]
        public bool EnableRaisingEvents { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute((System.ComponentModel.DesignerSerializationVisibility)(0))]
        [System.Diagnostics.MonitoringDescriptionAttribute("The contents of the log.")]
        public System.Diagnostics.EventLogEntryCollection Entries { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.ComponentModel.ReadOnlyAttribute(true)]
        [System.ComponentModel.SettingsBindableAttribute(true)]
        [System.Diagnostics.MonitoringDescriptionAttribute("Gets or sets the name of the log to read from and write to.")]
        public string Log { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public string LogDisplayName { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(".")]
        [System.ComponentModel.ReadOnlyAttribute(true)]
        [System.ComponentModel.SettingsBindableAttribute(true)]
        [System.Diagnostics.MonitoringDescriptionAttribute("The machine on which this event log resides.")]
        public string MachineName { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute((System.ComponentModel.DesignerSerializationVisibility)(0))]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public long MaximumKilobytes { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public int MinimumRetentionDays { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public System.Diagnostics.OverflowAction OverflowAction { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.ComponentModel.ReadOnlyAttribute(true)]
        [System.ComponentModel.SettingsBindableAttribute(true)]
        [System.Diagnostics.MonitoringDescriptionAttribute("The application name (source name) to use when writing to the event log.")]
        public string Source { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DefaultValueAttribute(null)]
        [System.Diagnostics.MonitoringDescriptionAttribute("The object used to marshal the event handler calls issued as a result of an EventLog change.")]
        public System.ComponentModel.ISynchronizeInvoke SynchronizingObject { [System.Security.Permissions.HostProtectionAttribute(System.Security.Permissions.SecurityAction.LinkDemand, Synchronization=true)]get { throw null; } set { } }
        [System.Diagnostics.MonitoringDescriptionAttribute("Raised each time any application writes an entry to the event log.")]
        public event System.Diagnostics.EntryWrittenEventHandler EntryWritten { add { } remove { } }
        public void BeginInit() { }
        public void Clear() { }
        public void Close() { }
        public static void CreateEventSource(System.Diagnostics.EventSourceCreationData sourceData) { }
        public static void CreateEventSource(string source, string logName) { }
        [System.ObsoleteAttribute("This method has been deprecated.  Please use System.Diagnostics.EventLog.CreateEventSource(EventSourceCreationData sourceData) instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
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
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public void ModifyOverflowPolicy(System.Diagnostics.OverflowAction action, int retentionDays) { }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
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
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public void WriteEvent(System.Diagnostics.EventInstance instance, byte[] data, params object[] values) { }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public void WriteEvent(System.Diagnostics.EventInstance instance, params object[] values) { }
        public static void WriteEvent(string source, System.Diagnostics.EventInstance instance, byte[] data, params object[] values) { }
        public static void WriteEvent(string source, System.Diagnostics.EventInstance instance, params object[] values) { }
    }
    [System.ComponentModel.DesignTimeVisibleAttribute(false)]
    [System.ComponentModel.ToolboxItemAttribute(false)]
    public sealed partial class EventLogEntry : System.ComponentModel.Component, System.Runtime.Serialization.ISerializable
    {
        internal EventLogEntry() { }
        [System.Diagnostics.MonitoringDescriptionAttribute("The category for this message.")]
        public string Category { get { throw null; } }
        [System.Diagnostics.MonitoringDescriptionAttribute("An application-specific category number assigned to this entry.")]
        public short CategoryNumber { get { throw null; } }
        [System.Diagnostics.MonitoringDescriptionAttribute("The binary data associated with this entry in the event log.")]
        public byte[] Data { get { throw null; } }
        [System.Diagnostics.MonitoringDescriptionAttribute("The type of entry - Information, Warning, etc.")]
        public System.Diagnostics.EventLogEntryType EntryType { get { throw null; } }
        [System.Diagnostics.MonitoringDescriptionAttribute("The number identifying the message for this source.")]
        [System.ObsoleteAttribute("This property has been deprecated.  Please use System.Diagnostics.EventLogEntry.InstanceId instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public int EventID { get { throw null; } }
        [System.Diagnostics.MonitoringDescriptionAttribute("The sequence of this entry in the event log.")]
        public int Index { get { throw null; } }
        [System.Diagnostics.MonitoringDescriptionAttribute("The full number identifying the message in the event message dll.")]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public long InstanceId { get { throw null; } }
        [System.Diagnostics.MonitoringDescriptionAttribute("The machine on which this event log resides.")]
        public string MachineName { get { throw null; } }
        [System.Diagnostics.MonitoringDescriptionAttribute("The text of the message for this entry.")]
        public string Message { get { throw null; } }
        [System.Diagnostics.MonitoringDescriptionAttribute("The application-supplied strings used in the message.")]
        public string[] ReplacementStrings { get { throw null; } }
        [System.Diagnostics.MonitoringDescriptionAttribute("The name of the application that wrote this entry.")]
        public string Source { get { throw null; } }
        [System.Diagnostics.MonitoringDescriptionAttribute("The time at which the application logged this entry.")]
        public System.DateTime TimeGenerated { get { throw null; } }
        [System.Diagnostics.MonitoringDescriptionAttribute("The time at which the system logged this entry to the event log.")]
        public System.DateTime TimeWritten { get { throw null; } }
        [System.Diagnostics.MonitoringDescriptionAttribute("The username of the account associated with this entry by the writing application.")]
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
        FailureAudit = 16,
        Information = 4,
        SuccessAudit = 8,
        Warning = 2,
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
