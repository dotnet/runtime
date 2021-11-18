// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace System.IO.Compression
{
    internal class ZipArchiveUpdateStrategy : ZipArchiveStrategy
    {
        public override ZipArchiveMode Mode => ZipArchiveMode.Update;
        public override BinaryReader? ArchiveReader { get; }
        public override Stream ArchiveStream { get; }
        public override Stream? BackingStream => null;
        public override bool ShouldSaveExtraFieldsAndComments => true;
        public override ReadOnlyCollection<ZipArchiveEntry> EntriesCollection => GetEntriesInternal();

        internal ZipArchiveUpdateStrategy(ZipArchive archive, Stream stream, bool leaveOpen, Encoding? entryNameEncoding)
            : base(archive, leaveOpen, entryNameEncoding, readEntries: false)
        {
            if (!stream.CanRead || !stream.CanWrite || !stream.CanSeek)
            {
                throw new ArgumentException(SR.UpdateModeCapabilities);
            }

            ArchiveStream = stream;
            ArchiveReader = new BinaryReader(ArchiveStream);

            if (ArchiveStream.Length == 0)
            {
                ReadEntries = true;
            }
            else
            {
                ReadEndOfCentralDirectory();
                EnsureCentralDirectoryRead();
                foreach (ZipArchiveEntry entry in Entries)
                {
                    entry.ThrowIfNotOpenable(needToUncompress: false, needToLoadIntoMemory: true);
                }
            }
        }

        protected override void DisposeInternal() => WriteAndDispose();

        public override ZipArchiveEntry CreateEntry(string entryName, CompressionLevel? compressionLevel) => CreateEntryInternal(entryName, compressionLevel);

        public override ZipArchiveEntry? GetEntry(string entryName) => GetEntryInternal(entryName);

        public override void PerformModeSpecificEocdReadActions(ZipEndOfCentralDirectoryBlock eocd) => ArchiveComment = eocd.ArchiveComment;

        public override void PerformModeSpecificWriteFileActions()
        {
            List<ZipArchiveEntry> markedForDelete = new List<ZipArchiveEntry>();

            foreach (ZipArchiveEntry entry in Entries)
            {
                if (!entry.LoadLocalHeaderExtraFieldAndCompressedBytesIfNeeded())
                {
                    markedForDelete.Add(entry);
                }
            }

            foreach (ZipArchiveEntry entry in markedForDelete)
            {
                entry.Delete();
            }

            ArchiveStream.Seek(0, SeekOrigin.Begin);
            ArchiveStream.SetLength(0);
        }

        public override void RemoveEntry(ZipArchiveEntry entry)
        {
            Entries.Remove(entry);
            EntriesDictionary.Remove(entry.FullName);
        }
    }
}
