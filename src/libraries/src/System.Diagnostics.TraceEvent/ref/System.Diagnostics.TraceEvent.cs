// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Diagnostics
{
    [System.FlagsAttribute]
    public enum SourceLevels
    {
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(2))]
        ActivityTracing = 65280,
        All = -1,
        Critical = 1,
        Error = 3,
        Information = 15,
        Off = 0,
        Verbose = 31,
        Warning = 7,
    }
    public partial class SourceSwitch : System.Diagnostics.Switch
    {
        public SourceSwitch(string name) : base(default(string), default(string)) { }
        public SourceSwitch(string displayName, string defaultSwitchValue) : base(default(string), default(string)) { }
        public System.Diagnostics.SourceLevels Level { get { return default(System.Diagnostics.SourceLevels); } set { } }
        protected override void OnValueChanged() { }
        public bool ShouldTrace(System.Diagnostics.TraceEventType eventType) { return default(bool); }
    }
    public abstract partial class Switch
    {
        protected Switch(string displayName, string description) { }
        protected Switch(string displayName, string description, string defaultSwitchValue) { }
        public System.Collections.Specialized.StringDictionary Attributes { get { return default(System.Collections.Specialized.StringDictionary); } }
        public string Description { get { return default(string); } }
        public string DisplayName { get { return default(string); } }
        protected int SwitchSetting { get { return default(int); } set { } }
        protected string Value { get { return default(string); } set { } }
        protected internal virtual string[] GetSupportedAttributes() { return default(string[]); }
        protected virtual void OnSwitchSettingChanged() { }
        protected virtual void OnValueChanged() { }
    }
    public partial class TraceEventCache
    {
        public TraceEventCache() { }
        public string Callstack { get { return default(string); } }
        public System.DateTime DateTime { get { return default(System.DateTime); } }
        public System.Collections.Stack LogicalOperationStack { get { return default(System.Collections.Stack); } }
        public int ProcessId { get { return default(int); } }
        public string ThreadId { get { return default(string); } }
        public long Timestamp { get { return default(long); } }
    }
    public enum TraceEventType
    {
        Critical = 1,
        Error = 2,
        Information = 8,
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(2))]
        Resume = 2048,
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(2))]
        Start = 256,
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(2))]
        Stop = 512,
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(2))]
        Suspend = 1024,
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(2))]
        Transfer = 4096,
        Verbose = 16,
        Warning = 4,
    }
    public abstract partial class TraceFilter
    {
        protected TraceFilter() { }
        public abstract bool ShouldTrace(System.Diagnostics.TraceEventCache cache, string source, System.Diagnostics.TraceEventType eventType, int id, string formatOrMessage, object[] args, object data1, object[] data);
    }
    public abstract partial class TraceListener : System.IDisposable
    {
        protected TraceListener() { }
        protected TraceListener(string name) { }
        public System.Collections.Specialized.StringDictionary Attributes { get { return default(System.Collections.Specialized.StringDictionary); } }
        public System.Diagnostics.TraceFilter Filter { get { return default(System.Diagnostics.TraceFilter); } set { } }
        public int IndentLevel { get { return default(int); } set { } }
        public int IndentSize { get { return default(int); } set { } }
        public virtual bool IsThreadSafe { get { return default(bool); } }
        public virtual string Name { get { return default(string); } set { } }
        protected bool NeedIndent { get { return default(bool); } set { } }
        public System.Diagnostics.TraceOptions TraceOutputOptions { get { return default(System.Diagnostics.TraceOptions); } set { } }
        public virtual void Close() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public virtual void Fail(string message) { }
        public virtual void Fail(string message, string detailMessage) { }
        public virtual void Flush() { }
        protected internal virtual string[] GetSupportedAttributes() { return default(string[]); }
        public virtual void TraceData(System.Diagnostics.TraceEventCache eventCache, string source, System.Diagnostics.TraceEventType eventType, int id, object data) { }
        public virtual void TraceData(System.Diagnostics.TraceEventCache eventCache, string source, System.Diagnostics.TraceEventType eventType, int id, params object[] data) { }
        public virtual void TraceEvent(System.Diagnostics.TraceEventCache eventCache, string source, System.Diagnostics.TraceEventType eventType, int id) { }
        public virtual void TraceEvent(System.Diagnostics.TraceEventCache eventCache, string source, System.Diagnostics.TraceEventType eventType, int id, string message) { }
        public virtual void TraceEvent(System.Diagnostics.TraceEventCache eventCache, string source, System.Diagnostics.TraceEventType eventType, int id, string format, params object[] args) { }
        public virtual void TraceTransfer(System.Diagnostics.TraceEventCache eventCache, string source, int id, string message, System.Guid relatedActivityId) { }
        public virtual void Write(object o) { }
        public virtual void Write(object o, string category) { }
        public abstract void Write(string message);
        public virtual void Write(string message, string category) { }
        protected virtual void WriteIndent() { }
        public virtual void WriteLine(object o) { }
        public virtual void WriteLine(object o, string category) { }
        public abstract void WriteLine(string message);
        public virtual void WriteLine(string message, string category) { }
    }
    public partial class TraceListenerCollection : System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        internal TraceListenerCollection() { }
        public int Count { get { return default(int); } }
        public System.Diagnostics.TraceListener this[int i] { get { return default(System.Diagnostics.TraceListener); } set { } }
        public System.Diagnostics.TraceListener this[string name] { get { return default(System.Diagnostics.TraceListener); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        bool System.Collections.IList.IsFixedSize { get { return default(bool); } }
        bool System.Collections.IList.IsReadOnly { get { return default(bool); } }
        object System.Collections.IList.this[int index] { get { return default(object); } set { } }
        public int Add(System.Diagnostics.TraceListener listener) { return default(int); }
        public void AddRange(System.Diagnostics.TraceListener[] value) { }
        public void AddRange(System.Diagnostics.TraceListenerCollection value) { }
        public void Clear() { }
        public bool Contains(System.Diagnostics.TraceListener listener) { return default(bool); }
        public void CopyTo(System.Diagnostics.TraceListener[] listeners, int index) { }
        public System.Collections.IEnumerator GetEnumerator() { return default(System.Collections.IEnumerator); }
        public int IndexOf(System.Diagnostics.TraceListener listener) { return default(int); }
        public void Insert(int index, System.Diagnostics.TraceListener listener) { }
        public void Remove(System.Diagnostics.TraceListener listener) { }
        public void Remove(string name) { }
        public void RemoveAt(int index) { }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        int System.Collections.IList.Add(object value) { return default(int); }
        bool System.Collections.IList.Contains(object value) { return default(bool); }
        int System.Collections.IList.IndexOf(object value) { return default(int); }
        void System.Collections.IList.Insert(int index, object value) { }
        void System.Collections.IList.Remove(object value) { }
    }
    [System.FlagsAttribute]
    public enum TraceOptions
    {
        Callstack = 32,
        DateTime = 2,
        LogicalOperationStack = 1,
        None = 0,
        ProcessId = 8,
        ThreadId = 16,
        Timestamp = 4,
    }
    public partial class TraceSource
    {
        public TraceSource(string name) { }
        public TraceSource(string name, System.Diagnostics.SourceLevels defaultLevel) { }
        public System.Collections.Specialized.StringDictionary Attributes { get { return default(System.Collections.Specialized.StringDictionary); } }
        public System.Diagnostics.TraceListenerCollection Listeners { get { return default(System.Diagnostics.TraceListenerCollection); } }
        public string Name { get { return default(string); } }
        public System.Diagnostics.SourceSwitch Switch { get { return default(System.Diagnostics.SourceSwitch); } set { } }
        public void Close() { }
        public void Flush() { }
        protected internal virtual string[] GetSupportedAttributes() { return default(string[]); }
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
        [System.Diagnostics.ConditionalAttribute("TRACE")]
        public void TraceTransfer(int id, string message, System.Guid relatedActivityId) { }
    }
}
