// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace System.IO.Compression
{
    internal interface IZipArchiveStrategy
    {
        public ZipArchive Archive { get; }
        public byte[]? ArchiveComment { get; set; }
        public BinaryReader? ArchiveReader { get; }
        public Stream ArchiveStream { get; }
        public ZipArchiveEntry? ArchiveStreamOwner { get; set; }
        public Stream? BackingStream { get; }
        public long CentralDirectoryStart { get; set; }
        public List<ZipArchiveEntry> Entries { get; }
        public ReadOnlyCollection<ZipArchiveEntry> EntriesCollection { get; }
        public Dictionary<string, ZipArchiveEntry> EntriesDictionary { get; }
        public Encoding? EntryNameEncoding { get; set; }
        public long ExpectedNumberOfEntries { get; set; }
        public bool IsDisposed { get; set; }
        public bool LeaveOpen { get; }
        public ZipArchiveMode Mode { get; }
        public uint NumberOfThisDisk { get; set; }
        public bool ReadEntries { get; set; }
        public bool ShouldSaveExtraFieldsAndComments { get; }

        public void AcquireArchiveStream(ZipArchiveEntry entry);
        public ZipArchiveEntry CreateEntry(string entryName, CompressionLevel? compressionLevel);
        public void Dispose(bool disposing);
        public ZipArchiveEntry? GetEntry(string entryName);
        public void PerformModeSpecificEocdReadActions(ZipEndOfCentralDirectoryBlock eocd);
        public void PerformModeSpecificWriteFileActions();
        public void ReleaseArchiveStream(ZipArchiveEntry entry);
        public void RemoveEntry(ZipArchiveEntry entry);
        public void ThrowIfDisposed();
    }
}
