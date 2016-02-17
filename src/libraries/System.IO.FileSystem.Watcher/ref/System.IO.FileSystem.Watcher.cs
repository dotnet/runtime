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
        public virtual System.Exception GetException() { return default(System.Exception); }
    }
    public delegate void ErrorEventHandler(object sender, System.IO.ErrorEventArgs e);
    public partial class FileSystemEventArgs : System.EventArgs
    {
        public FileSystemEventArgs(System.IO.WatcherChangeTypes changeType, string directory, string name) { }
        public System.IO.WatcherChangeTypes ChangeType { get { return default(System.IO.WatcherChangeTypes); } }
        public string FullPath { get { return default(string); } }
        public string Name { get { return default(string); } }
    }
    public delegate void FileSystemEventHandler(object sender, System.IO.FileSystemEventArgs e);
    public partial class FileSystemWatcher
    {
        public FileSystemWatcher() { }
        public FileSystemWatcher(string path) { }
        public FileSystemWatcher(string path, string filter) { }
        public bool EnableRaisingEvents { get { return default(bool); } set { } }
        public string Filter { get { return default(string); } set { } }
        public bool IncludeSubdirectories { get { return default(bool); } set { } }
        public int InternalBufferSize { get { return default(int); } set { } }
        public System.IO.NotifyFilters NotifyFilter { get { return default(System.IO.NotifyFilters); } set { } }
        public string Path { get { return default(string); } set { } }
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
        public string OldFullPath { get { return default(string); } }
        public string OldName { get { return default(string); } }
    }
    public delegate void RenamedEventHandler(object sender, System.IO.RenamedEventArgs e);
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
