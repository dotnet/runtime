// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
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
        public FileSystemEventArgs(System.IO.WatcherChangeTypes changeType, string directory, string? name) { }
        public System.IO.WatcherChangeTypes ChangeType { get { throw null; } }
        public string FullPath { get { throw null; } }
        public string? Name { get { throw null; } }
    }
    public delegate void FileSystemEventHandler(object sender, System.IO.FileSystemEventArgs e);
    public partial class FileSystemWatcher : System.ComponentModel.Component, System.ComponentModel.ISupportInitialize
    {
        public FileSystemWatcher() { }
        public FileSystemWatcher(string path) { }
        public FileSystemWatcher(string path, string filter) { }
        public bool EnableRaisingEvents { get { throw null; } set { } }
        public string Filter { get { throw null; } set { } }
        public System.Collections.ObjectModel.Collection<string> Filters { get { throw null; } }
        public bool IncludeSubdirectories { get { throw null; } set { } }
        public int InternalBufferSize { get { throw null; } set { } }
        public System.IO.NotifyFilters NotifyFilter { get { throw null; } set { } }
        [System.ComponentModel.EditorAttribute("System.Diagnostics.Design.FSWPathEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public string Path { get { throw null; } set { } }
        public override System.ComponentModel.ISite? Site { get { throw null; } set { } }
        public System.ComponentModel.ISynchronizeInvoke? SynchronizingObject { get { throw null; } set { } }
        public event System.IO.FileSystemEventHandler? Changed { add { } remove { } }
        public event System.IO.FileSystemEventHandler? Created { add { } remove { } }
        public event System.IO.FileSystemEventHandler? Deleted { add { } remove { } }
        public event System.IO.ErrorEventHandler? Error { add { } remove { } }
        public event System.IO.RenamedEventHandler? Renamed { add { } remove { } }
        public void BeginInit() { }
        protected override void Dispose(bool disposing) { }
        public void EndInit() { }
        protected void OnChanged(System.IO.FileSystemEventArgs e) { }
        protected void OnCreated(System.IO.FileSystemEventArgs e) { }
        protected void OnDeleted(System.IO.FileSystemEventArgs e) { }
        protected void OnError(System.IO.ErrorEventArgs e) { }
        protected void OnRenamed(System.IO.RenamedEventArgs e) { }
        public System.IO.WaitForChangedResult WaitForChanged(System.IO.WatcherChangeTypes changeType) { throw null; }
        public System.IO.WaitForChangedResult WaitForChanged(System.IO.WatcherChangeTypes changeType, int timeout) { throw null; }
        public System.IO.WaitForChangedResult WaitForChanged(System.IO.WatcherChangeTypes changeType, System.TimeSpan timeout) { throw null; }
    }
    public partial class InternalBufferOverflowException : System.SystemException
    {
        public InternalBufferOverflowException() { }
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        protected InternalBufferOverflowException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public InternalBufferOverflowException(string? message) { }
        public InternalBufferOverflowException(string? message, System.Exception? inner) { }
    }
    [System.FlagsAttribute]
    public enum NotifyFilters
    {
        FileName = 1,
        DirectoryName = 2,
        Attributes = 4,
        Size = 8,
        LastWrite = 16,
        LastAccess = 32,
        CreationTime = 64,
        Security = 256,
    }
    public partial class RenamedEventArgs : System.IO.FileSystemEventArgs
    {
        public RenamedEventArgs(System.IO.WatcherChangeTypes changeType, string directory, string? name, string? oldName) : base (default(System.IO.WatcherChangeTypes), default(string), default(string)) { }
        public string OldFullPath { get { throw null; } }
        public string? OldName { get { throw null; } }
    }
    public delegate void RenamedEventHandler(object sender, System.IO.RenamedEventArgs e);
    public partial struct WaitForChangedResult
    {
        private object _dummy;
        private int _dummyPrimitive;
        public System.IO.WatcherChangeTypes ChangeType { readonly get { throw null; } set { } }
        public string? Name { readonly get { throw null; } set { } }
        public string? OldName { readonly get { throw null; } set { } }
        public bool TimedOut { readonly get { throw null; } set { } }
    }
    [System.FlagsAttribute]
    public enum WatcherChangeTypes
    {
        Created = 1,
        Deleted = 2,
        Changed = 4,
        Renamed = 8,
        All = 15,
    }
}
