// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.IO.Compression.CompressionMode))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.IO.Compression.DeflateStream))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.IO.Compression.GZipStream))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.IO.Compression.CompressionLevel))]

namespace System.IO.Compression
{
    public partial class ZipArchive : System.IDisposable
    {
        public ZipArchive(System.IO.Stream stream) { }
        public ZipArchive(System.IO.Stream stream, System.IO.Compression.ZipArchiveMode mode) { }
        public ZipArchive(System.IO.Stream stream, System.IO.Compression.ZipArchiveMode mode, bool leaveOpen) { }
        public ZipArchive(System.IO.Stream stream, System.IO.Compression.ZipArchiveMode mode, bool leaveOpen, System.Text.Encoding entryNameEncoding) { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.IO.Compression.ZipArchiveEntry> Entries { get { throw null; } }
        public System.IO.Compression.ZipArchiveMode Mode { get { throw null; } }
        public System.IO.Compression.ZipArchiveEntry CreateEntry(string entryName) { throw null; }
        public System.IO.Compression.ZipArchiveEntry CreateEntry(string entryName, System.IO.Compression.CompressionLevel compressionLevel) { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public System.IO.Compression.ZipArchiveEntry GetEntry(string entryName) { throw null; }
    }
    public partial class ZipArchiveEntry
    {
        internal ZipArchiveEntry() { }
        public System.IO.Compression.ZipArchive Archive { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public long CompressedLength { get { throw null; } }
        public string FullName { get { throw null; } }
        public System.DateTimeOffset LastWriteTime { get { throw null; } set { } }
        public long Length { get { throw null; } }
        public string Name { get { throw null; } }
        public void Delete() { }
        public System.IO.Stream Open() { throw null; }
    }
    public enum ZipArchiveMode
    {
        Create = 1,
        Read = 0,
        Update = 2,
    }
}
