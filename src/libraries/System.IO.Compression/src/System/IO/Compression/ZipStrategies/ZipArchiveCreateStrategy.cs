// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Text;

namespace System.IO.Compression
{
    internal sealed class ZipArchiveCreateStrategy : ZipArchiveStrategy
    {
        public override ZipArchiveMode Mode => ZipArchiveMode.Create;
        public override BinaryReader? ArchiveReader { get; }
        public override Stream ArchiveStream { get; }
        public override Stream? BackingStream => null;
        public override ReadOnlyCollection<ZipArchiveEntry> EntriesCollection => throw new NotSupportedException(SR.EntriesInCreateMode);

        internal ZipArchiveCreateStrategy(ZipArchive archive, Stream stream, bool leaveOpen, Encoding? entryNameEncoding)
            : base(archive, leaveOpen, entryNameEncoding, readEntries: true)
        {
            if (!stream.CanWrite)
            {
                throw new ArgumentException(SR.CreateModeCapabilities);
            }
            ArchiveStream = WrapStreamIfNeeded(stream);
            ArchiveReader = null;
        }

        protected override void DisposeInternal() => WriteAndDispose();

        public override ZipArchiveEntry CreateEntry(string entryName, CompressionLevel? compressionLevel) => CreateEntryInternal(entryName, compressionLevel);

        public override ZipArchiveEntry? GetEntry(string entryName) => throw new NotSupportedException(SR.EntriesInCreateMode);

        public override void PerformModeSpecificEocdReadActions(ZipEndOfCentralDirectoryBlock eocd) { }

        public override void PerformModeSpecificWriteFileActions() { }
        public override void RemoveEntry(ZipArchiveEntry entry) => throw new NotSupportedException(SR.EntriesInCreateMode);

        private static Stream WrapStreamIfNeeded(Stream stream) => stream.CanSeek ? stream : new PositionPreservingWriteOnlyStreamWrapper(stream);
    }
}
