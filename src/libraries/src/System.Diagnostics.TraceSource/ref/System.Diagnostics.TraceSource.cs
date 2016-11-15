// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Diagnostics
{
    public partial class BooleanSwitch : System.Diagnostics.Switch
    {
        public BooleanSwitch(string displayName, string description) : base(default(string), default(string)) { }
        public BooleanSwitch(string displayName, string description, string defaultSwitchValue) : base(default(string), default(string)) { }
        public bool Enabled { get { throw null; } set { } }
        protected override void OnValueChanged() { }
    }
    public partial class CorrelationManager
    {
        internal CorrelationManager() { }
        public System.Guid ActivityId { get { throw null; } set { } }
        public System.Collections.Stack LogicalOperationStack { get { throw null; } }
        public void StartLogicalOperation() { }
        public void StartLogicalOperation(object operationId) { }
        public void StopLogicalOperation() { }
    }
    public partial class DefaultTraceListener : System.Diagnostics.TraceListener
    {
        public DefaultTraceListener() { }
        public override void Fail(string message) { }
        public override void Fail(string message, string detailMessage) { }
        public override void Write(string message) { }
        public override void WriteLine(string message) { }
        public bool AssertUiEnabled { get { throw null; } set { } }
        public string LogFileName { get { throw null; } set { } }
    }
    public partial class EventTypeFilter : System.Diagnostics.TraceFilter
    {
        public EventTypeFilter(System.Diagnostics.SourceLevels level) { }
        public System.Diagnostics.SourceLevels EventType { get { throw null; } set { } }
        public override bool ShouldTrace(System.Diagnostics.TraceEventCache cache, string source, System.Diagnostics.TraceEventType eventType, int id, string formatOrMessage, object[] args, object data1, object[] data) { throw null; }
    }
    public partial class SourceFilter : System.Diagnostics.TraceFilter
    {
        public SourceFilter(string source) { }
        public string Source { get { throw null; } set { } }
        public override bool ShouldTrace(System.Diagnostics.TraceEventCache cache, string source, System.Diagnostics.TraceEventType eventType, int id, string formatOrMessage, object[] args, object data1, object[] data) { throw null; }
    }
    [System.FlagsAttribute]
    public enum SourceLevels
    {
        All = -1,
        Critical = 1,
        Error = 3,
        Information = 15,
        Off = 0,
        Verbose = 31,
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
        ActivityTracing = 0xFF00,
        Warning = 7,
    }
    public partial class SourceSwitch : System.Diagnostics.Switch
    {
        public SourceSwitch(string name) : base(default(string), default(string)) { }
        public SourceSwitch(string displayName, string defaultSwitchValue) : base(default(string), default(string)) { } 
        public System.Diagnostics.SourceLevels Level { get { throw null; } set { } }
        protected override void OnValueChanged() { }
        public bool ShouldTrace(System.Diagnostics.TraceEventType eventType) { throw null; }
    }
    public abstract partial class Switch
    {
        protected Switch(string displayName, string description) { }
        protected Switch(string displayName, string description, string defaultSwitchValue) { }
        public string Description { get { throw null; } }
        public string DisplayName { get { throw null; } }
        protected int SwitchSetting { get { throw null; } set { } }
        protected string Value { get { throw null; } set { } }
        protected virtual void OnSwitchSettingChanged() { }
        protected virtual void OnValueChanged() { }
        public System.Collections.Specialized.StringDictionary Attributes { get { throw null; } }
        protected internal virtual string[] GetSupportedAttributes() { throw null; }
    }
    public sealed partial class Trace
    {
        public static CorrelationManager CorrelationManager { get { throw null; } }
        internal Trace() { }
        public static bool AutoFlush { get { throw null; } set { } }
        public static int IndentLevel { get { throw null; } set { } }
        public static int IndentSize { get { throw null; } set { } }
        public static System.Diagnostics.TraceListenerCollection Listeners { get { throw null; } }
        public static bool UseGlobalLock { get { throw null; } set { } }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public static void Assert(bool condition) { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public static void Assert(bool condition, string message) { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public static void Assert(bool condition, string message, string detailMessage) { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public static void Close() { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public static void Fail(string message) { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public static void Fail(string message, string detailMessage) { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public static void Flush() { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public static void Indent() { }
        public static void Refresh() { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public static void TraceError(string message) { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public static void TraceError(string format, params object[] args) { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public static void TraceInformation(string message) { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public static void TraceInformation(string format, params object[] args) { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public static void TraceWarning(string message) { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public static void TraceWarning(string format, params object[] args) { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public static void Unindent() { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public static void Write(object value) { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public static void Write(object value, string category) { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public static void Write(string message) { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public static void Write(string message, string category) { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public static void WriteIf(bool condition, object value) { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public static void WriteIf(bool condition, object value, string category) { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public static void WriteIf(bool condition, string message) { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public static void WriteIf(bool condition, string message, string category) { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public static void WriteLine(object value) { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public static void WriteLine(object value, string category) { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public static void WriteLine(string message) { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public static void WriteLine(string message, string category) { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public static void WriteLineIf(bool condition, object value) { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public static void WriteLineIf(bool condition, object value, string category) { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public static void WriteLineIf(bool condition, string message) { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public static void WriteLineIf(bool condition, string message, string category) { }
    }
    public partial class TraceEventCache
    {
        public TraceEventCache() { }
        public System.DateTime DateTime { get { throw null; } }
        public int ProcessId { get { throw null; } }
        public string ThreadId { get { throw null; } }
        public long Timestamp { get { throw null; } }
        public string Callstack { get { throw null; } }
        public System.Collections.Stack LogicalOperationStack { get { throw null; } }
    }
    public enum TraceEventType
    {
        Critical = 1,
        Error = 2,
        Information = 8,
        Verbose = 16,
        Warning = 4,
        Resume = 2048,
        Start = 256,
        Stop = 512,
        Suspend = 1024,
        Transfer = 4096,
    }
    public abstract partial class TraceFilter
    {
        protected TraceFilter() { }
        public abstract bool ShouldTrace(System.Diagnostics.TraceEventCache cache, string source, System.Diagnostics.TraceEventType eventType, int id, string formatOrMessage, object[] args, object data1, object[] data);
    }
    public enum TraceLevel
    {
        Error = 1,
        Info = 3,
        Off = 0,
        Verbose = 4,
        Warning = 2,
    }
    public abstract partial class TraceListener : System.MarshalByRefObject, System.IDisposable
    {
        protected TraceListener() { }
        protected TraceListener(string name) { }
        public System.Diagnostics.TraceFilter Filter { get { throw null; } set { } }
        public int IndentLevel { get { throw null; } set { } }
        public int IndentSize { get { throw null; } set { } }
        public virtual bool IsThreadSafe { get { throw null; } }
        public virtual string Name { get { throw null; } set { } }
        protected bool NeedIndent { get { throw null; } set { } }
        public System.Diagnostics.TraceOptions TraceOutputOptions { get { throw null; } set { } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public virtual void Fail(string message) { }
        public virtual void Fail(string message, string detailMessage) { }
        public virtual void Flush() { }
        public virtual void TraceData(System.Diagnostics.TraceEventCache eventCache, string source, System.Diagnostics.TraceEventType eventType, int id, object data) { }
        public virtual void TraceData(System.Diagnostics.TraceEventCache eventCache, string source, System.Diagnostics.TraceEventType eventType, int id, params object[] data) { }
        public virtual void TraceEvent(System.Diagnostics.TraceEventCache eventCache, string source, System.Diagnostics.TraceEventType eventType, int id) { }
        public virtual void TraceEvent(System.Diagnostics.TraceEventCache eventCache, string source, System.Diagnostics.TraceEventType eventType, int id, string message) { }
        public virtual void TraceEvent(System.Diagnostics.TraceEventCache eventCache, string source, System.Diagnostics.TraceEventType eventType, int id, string format, params object[] args) { }
        public virtual void Write(object o) { }
        public virtual void Write(object o, string category) { }
        public abstract void Write(string message);
        public virtual void Write(string message, string category) { }
        protected virtual void WriteIndent() { }
        public virtual void WriteLine(object o) { }
        public virtual void WriteLine(object o, string category) { }
        public abstract void WriteLine(string message);
        public virtual void WriteLine(string message, string category) { }
        public System.Collections.Specialized.StringDictionary Attributes { get { throw null; } }
        public virtual void Close() { }
        protected internal virtual string[] GetSupportedAttributes() { throw null; }
        public virtual void TraceTransfer(TraceEventCache eventCache, string source, int id, string message, Guid relatedActivityId) { throw null; }
    }
    public partial class TraceListenerCollection : System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        internal TraceListenerCollection() { }
        public int Count { get { throw null; } }
        public System.Diagnostics.TraceListener this[int i] { get { throw null; } set { } }
        public System.Diagnostics.TraceListener this[string name] { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        bool System.Collections.IList.IsReadOnly { get { throw null; } }
        object System.Collections.IList.this[int index] { get { throw null; } set { } }
        public int Add(System.Diagnostics.TraceListener listener) { throw null; }
        public void AddRange(System.Diagnostics.TraceListener[] value) { }
        public void AddRange(System.Diagnostics.TraceListenerCollection value) { }
        public void Clear() { }
        public bool Contains(System.Diagnostics.TraceListener listener) { throw null; }
        public void CopyTo(System.Diagnostics.TraceListener[] listeners, int index) { }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        public int IndexOf(System.Diagnostics.TraceListener listener) { throw null; }
        public void Insert(int index, System.Diagnostics.TraceListener listener) { }
        public void Remove(System.Diagnostics.TraceListener listener) { }
        public void Remove(string name) { }
        public void RemoveAt(int index) { }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        int System.Collections.IList.Add(object value) { throw null; }
        bool System.Collections.IList.Contains(object value) { throw null; }
        int System.Collections.IList.IndexOf(object value) { throw null; }
        void System.Collections.IList.Insert(int index, object value) { }
        void System.Collections.IList.Remove(object value) { }
    }
    [System.FlagsAttribute]
    public enum TraceOptions
    {
        DateTime = 2,
        None = 0,
        LogicalOperationStack = 1,
        ProcessId = 8,
        ThreadId = 16,
        Timestamp = 4,
        Callstack = 32,
    }
    public partial class TraceSource
    {
        public TraceSource(string name) { }
        public TraceSource(string name, System.Diagnostics.SourceLevels defaultLevel) { }
        public System.Diagnostics.TraceListenerCollection Listeners { get { throw null; } }
        public string Name { get { throw null; } }
        public System.Diagnostics.SourceSwitch Switch { get { throw null; } set { } }
        public void Close() { }
        public void Flush() { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public void TraceData(System.Diagnostics.TraceEventType eventType, int id, object data) { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public void TraceData(System.Diagnostics.TraceEventType eventType, int id, params object[] data) { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public void TraceEvent(System.Diagnostics.TraceEventType eventType, int id) { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public void TraceEvent(System.Diagnostics.TraceEventType eventType, int id, string message) { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public void TraceEvent(System.Diagnostics.TraceEventType eventType, int id, string format, params object[] args) { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public void TraceInformation(string message) { }
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public void TraceInformation(string format, params object[] args) { }
        public System.Collections.Specialized.StringDictionary Attributes { get { throw null; } }
        protected internal virtual string[] GetSupportedAttributes() { throw null; }
        public void TraceTransfer(int id, string message, System.Guid relatedActivityId) { }
    }
    public partial class TraceSwitch : System.Diagnostics.Switch
    {
        public TraceSwitch(string displayName, string description) : base(default(string), default(string)) { }
        public TraceSwitch(string displayName, string description, string defaultSwitchValue) : base(default(string), default(string)) { }
        public System.Diagnostics.TraceLevel Level { get { throw null; } set { } }
        public bool TraceError { get { throw null; } }
        public bool TraceInfo { get { throw null; } }
        public bool TraceVerbose { get { throw null; } }
        public bool TraceWarning { get { throw null; } }
        protected override void OnSwitchSettingChanged() { }
        protected override void OnValueChanged() { }
    }
    public sealed class SwitchAttribute : System.Attribute 
    {
        public SwitchAttribute(string switchName, Type switchType) { throw null; }
        public string SwitchDescription { get { throw null; } set { } }
        public string SwitchName { get { throw null; } set { } }
        public System.Type SwitchType { get { throw null; } set { } }
        public static SwitchAttribute[] GetAll(System.Reflection.Assembly assembly) { throw null; }
    }
    public sealed class SwitchLevelAttribute : System.Attribute 
    {
        public SwitchLevelAttribute(Type switchLevelType) { throw null; }
        public  System.Type SwitchLevelType { get { throw null; } set { } }
    }
}
