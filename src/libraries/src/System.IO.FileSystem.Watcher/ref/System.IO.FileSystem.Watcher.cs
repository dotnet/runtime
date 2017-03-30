// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.IO
{
    public partial class ErrorEventArgs : System.EventArgs
    {
        public ErrorEventArgs(System.Exception exception) { }
        public virtual System.Exception GetException() { throw null; }
    }
    public delegate void ErrorEventHandler(object sender, System.IO.ErrorEventArgs e);
    public partial class FileSystemEventArgs : System.EventArgs
    {
        public FileSystemEventArgs(System.IO.WatcherChangeTypes changeType, string directory, string name) { }
        public System.IO.WatcherChangeTypes ChangeType { get { throw null; } }
        public string FullPath { get { throw null; } }
        public string Name { get { throw null; } }
    }
    public delegate void FileSystemEventHandler(object sender, System.IO.FileSystemEventArgs e);
    public partial class FileSystemWatcher : System.ComponentModel.Component, System.ComponentModel.ISupportInitialize
    {
        public FileSystemWatcher() { }
        public FileSystemWatcher(string path) { }
        public FileSystemWatcher(string path, string filter) { }
        public bool EnableRaisingEvents { get { throw null; } set { } }
        public string Filter { get { throw null; } set { } }
        public bool IncludeSubdirectories { get { throw null; } set { } }
        public int InternalBufferSize { get { throw null; } set { } }
        public System.IO.NotifyFilters NotifyFilter { get { throw null; } set { } }
        public string Path { get { throw null; } set { } }
        public event System.IO.FileSystemEventHandler Changed { add { } remove { } }
        public event System.IO.FileSystemEventHandler Created { add { } remove { } }
        public event System.IO.FileSystemEventHandler Deleted { add { } remove { } }
        public event System.IO.ErrorEventHandler Error { add { } remove { } }
        public event System.IO.RenamedEventHandler Renamed { add { } remove { } }
        protected void OnChanged(System.IO.FileSystemEventArgs e) { }
        protected void OnCreated(System.IO.FileSystemEventArgs e) { }
        protected void OnDeleted(System.IO.FileSystemEventArgs e) { }
        protected void OnError(System.IO.ErrorEventArgs e) { }
        protected void OnRenamed(System.IO.RenamedEventArgs e) { }
        public System.IO.WaitForChangedResult WaitForChanged(System.IO.WatcherChangeTypes changeType) { throw null; }
        public System.IO.WaitForChangedResult WaitForChanged(System.IO.WatcherChangeTypes changeType, int timeout) { throw null; }
        public override System.ComponentModel.ISite Site { get { throw null; } set { } }
        public System.ComponentModel.ISynchronizeInvoke SynchronizingObject { get { throw null; } set { } }
        public void BeginInit() { }
        protected override void Dispose(bool disposing) { }
        public void EndInit() { }
    }
    [Serializable]
    public partial class InternalBufferOverflowException : System.SystemException
    {
        public InternalBufferOverflowException() { }
        public InternalBufferOverflowException(string message) { }
        public InternalBufferOverflowException(string message, System.Exception inner) { }
        protected InternalBufferOverflowException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    [System.FlagsAttribute]
    public enum NotifyFilters
    {
        Attributes = 4,
        CreationTime = 64,
        DirectoryName = 2,
        FileName = 1,
        LastAccess = 32,
        LastWrite = 16,
        Security = 256,
        Size = 8,
    }
    public partial class RenamedEventArgs : System.IO.FileSystemEventArgs
    {
        public RenamedEventArgs(System.IO.WatcherChangeTypes changeType, string directory, string name, string oldName) : base(default(System.IO.WatcherChangeTypes), default(string), default(string)) { }
        public string OldFullPath { get { throw null; } }
        public string OldName { get { throw null; } }
    }
    public delegate void RenamedEventHandler(object sender, System.IO.RenamedEventArgs e);
    public struct WaitForChangedResult
    {
        public System.IO.WatcherChangeTypes ChangeType { get { throw null; } set { } }
        public string Name { get { throw null; } set { } }
        public string OldName { get { throw null; } set { } }
        public bool TimedOut { get { throw null; } set { } }
    }
    [System.FlagsAttribute]
    public enum WatcherChangeTypes
    {
        All = 15,
        Changed = 4,
        Created = 1,
        Deleted = 2,
        Renamed = 8,
    }
}
