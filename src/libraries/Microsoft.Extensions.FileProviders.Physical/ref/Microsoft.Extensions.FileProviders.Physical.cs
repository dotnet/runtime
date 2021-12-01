// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Extensions.FileProviders
{
    public partial class PhysicalFileProvider : Microsoft.Extensions.FileProviders.IFileProvider, System.IDisposable
    {
        public PhysicalFileProvider(string root) { }
        public PhysicalFileProvider(string root, Microsoft.Extensions.FileProviders.Physical.ExclusionFilters filters) { }
        public string Root { get { throw null; } }
        public bool UseActivePolling { get { throw null; } set { } }
        public bool UsePollingFileWatcher { get { throw null; } set { } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        ~PhysicalFileProvider() { }
        public Microsoft.Extensions.FileProviders.IDirectoryContents GetDirectoryContents(string subpath) { throw null; }
        public Microsoft.Extensions.FileProviders.IFileInfo GetFileInfo(string subpath) { throw null; }
        public Microsoft.Extensions.Primitives.IChangeToken Watch(string filter) { throw null; }
    }
}
namespace Microsoft.Extensions.FileProviders.Internal
{
    public partial class PhysicalDirectoryContents : Microsoft.Extensions.FileProviders.IDirectoryContents, System.Collections.Generic.IEnumerable<Microsoft.Extensions.FileProviders.IFileInfo>, System.Collections.IEnumerable
    {
        public PhysicalDirectoryContents(string directory) { }
        public PhysicalDirectoryContents(string directory, Microsoft.Extensions.FileProviders.Physical.ExclusionFilters filters) { }
        public bool Exists { get { throw null; } }
        public System.Collections.Generic.IEnumerator<Microsoft.Extensions.FileProviders.IFileInfo> GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
}
namespace Microsoft.Extensions.FileProviders.Physical
{
    [System.FlagsAttribute]
    public enum ExclusionFilters
    {
        None = 0,
        DotPrefixed = 1,
        Hidden = 2,
        System = 4,
        Sensitive = 7,
    }
    public partial class PhysicalDirectoryInfo : Microsoft.Extensions.FileProviders.IFileInfo
    {
        public PhysicalDirectoryInfo(System.IO.DirectoryInfo info) { }
        public bool Exists { get { throw null; } }
        public bool IsDirectory { get { throw null; } }
        public System.DateTimeOffset LastModified { get { throw null; } }
        public long Length { get { throw null; } }
        public string Name { get { throw null; } }
        public string PhysicalPath { get { throw null; } }
        public System.IO.Stream CreateReadStream() { throw null; }
    }
    public partial class PhysicalFileInfo : Microsoft.Extensions.FileProviders.IFileInfo
    {
        public PhysicalFileInfo(System.IO.FileInfo info) { }
        public bool Exists { get { throw null; } }
        public bool IsDirectory { get { throw null; } }
        public System.DateTimeOffset LastModified { get { throw null; } }
        public long Length { get { throw null; } }
        public string Name { get { throw null; } }
        public string PhysicalPath { get { throw null; } }
        public System.IO.Stream CreateReadStream() { throw null; }
    }
    public partial class PhysicalFilesWatcher : System.IDisposable
    {
        public PhysicalFilesWatcher(string root, System.IO.FileSystemWatcher? fileSystemWatcher, bool pollForChanges) { }
        public PhysicalFilesWatcher(string root, System.IO.FileSystemWatcher? fileSystemWatcher, bool pollForChanges, Microsoft.Extensions.FileProviders.Physical.ExclusionFilters filters) { }
        public Microsoft.Extensions.Primitives.IChangeToken CreateFileChangeToken(string filter) { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        ~PhysicalFilesWatcher() { }
    }
    public partial class PollingFileChangeToken : Microsoft.Extensions.Primitives.IChangeToken
    {
        public PollingFileChangeToken(System.IO.FileInfo fileInfo) { }
        public bool ActiveChangeCallbacks { get { throw null; } }
        public bool HasChanged { get { throw null; } }
        public System.IDisposable RegisterChangeCallback(System.Action<object?> callback, object? state) { throw null; }
    }
    public partial class PollingWildCardChangeToken : Microsoft.Extensions.Primitives.IChangeToken
    {
        public PollingWildCardChangeToken(string root, string pattern) { }
        public bool ActiveChangeCallbacks { get { throw null; } }
        public bool HasChanged { get { throw null; } }
        protected virtual System.DateTime GetLastWriteUtc(string path) { throw null; }
        System.IDisposable Microsoft.Extensions.Primitives.IChangeToken.RegisterChangeCallback(System.Action<object?> callback, object? state) { throw null; }
    }
}
