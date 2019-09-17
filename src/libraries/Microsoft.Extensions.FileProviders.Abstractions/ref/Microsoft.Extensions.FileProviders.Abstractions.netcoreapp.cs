// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.FileProviders
{
    public partial interface IDirectoryContents : System.Collections.Generic.IEnumerable<Microsoft.Extensions.FileProviders.IFileInfo>, System.Collections.IEnumerable
    {
        bool Exists { get; }
    }
    public partial interface IFileInfo
    {
        bool Exists { get; }
        bool IsDirectory { get; }
        System.DateTimeOffset LastModified { get; }
        long Length { get; }
        string Name { get; }
        string PhysicalPath { get; }
        System.IO.Stream CreateReadStream();
    }
    public partial interface IFileProvider
    {
        Microsoft.Extensions.FileProviders.IDirectoryContents GetDirectoryContents(string subpath);
        Microsoft.Extensions.FileProviders.IFileInfo GetFileInfo(string subpath);
        Microsoft.Extensions.Primitives.IChangeToken Watch(string filter);
    }
    public partial class NotFoundDirectoryContents : Microsoft.Extensions.FileProviders.IDirectoryContents, System.Collections.Generic.IEnumerable<Microsoft.Extensions.FileProviders.IFileInfo>, System.Collections.IEnumerable
    {
        public NotFoundDirectoryContents() { }
        public bool Exists { get { throw null; } }
        public static Microsoft.Extensions.FileProviders.NotFoundDirectoryContents Singleton { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Collections.Generic.IEnumerator<Microsoft.Extensions.FileProviders.IFileInfo> GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public partial class NotFoundFileInfo : Microsoft.Extensions.FileProviders.IFileInfo
    {
        public NotFoundFileInfo(string name) { }
        public bool Exists { get { throw null; } }
        public bool IsDirectory { get { throw null; } }
        public System.DateTimeOffset LastModified { get { throw null; } }
        public long Length { get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string PhysicalPath { get { throw null; } }
        public System.IO.Stream CreateReadStream() { throw null; }
    }
    public partial class NullChangeToken : Microsoft.Extensions.Primitives.IChangeToken
    {
        internal NullChangeToken() { }
        public bool ActiveChangeCallbacks { get { throw null; } }
        public bool HasChanged { get { throw null; } }
        public static Microsoft.Extensions.FileProviders.NullChangeToken Singleton { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.IDisposable RegisterChangeCallback(System.Action<object> callback, object state) { throw null; }
    }
    public partial class NullFileProvider : Microsoft.Extensions.FileProviders.IFileProvider
    {
        public NullFileProvider() { }
        public Microsoft.Extensions.FileProviders.IDirectoryContents GetDirectoryContents(string subpath) { throw null; }
        public Microsoft.Extensions.FileProviders.IFileInfo GetFileInfo(string subpath) { throw null; }
        public Microsoft.Extensions.Primitives.IChangeToken Watch(string filter) { throw null; }
    }
}
