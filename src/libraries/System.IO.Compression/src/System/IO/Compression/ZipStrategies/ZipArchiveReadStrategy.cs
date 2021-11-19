// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Text;

namespace System.IO.Compression
{
    internal sealed class ZipArchiveReadStrategy : ZipArchiveStrategy
    {
        public override ZipArchiveMode Mode => ZipArchiveMode.Read;
        public override BinaryReader? ArchiveReader { get; }
        public override Stream ArchiveStream { get; }
        public override Stream? BackingStream { get; }
        public override ReadOnlyCollection<ZipArchiveEntry> EntriesCollection => GetEntriesInternal();

        internal ZipArchiveReadStrategy(ZipArchive archive, Stream stream, bool leaveOpen, Encoding? entryNameEncoding)
            : base(archive, leaveOpen, entryNameEncoding, readEntries: false)
        {
            if (!stream.CanRead)
            {
                throw new ArgumentException(SR.ReadModeCapabilities);
            }

            Stream? extraTempStream = null;
            try
            {
                if (!stream.CanSeek)
                {
                    BackingStream = stream;
                    extraTempStream = stream = new MemoryStream();
                    BackingStream.CopyTo(stream);
                    stream.Seek(0, SeekOrigin.Begin);
                }

                ArchiveStream = stream;
                ArchiveReader = new BinaryReader(stream);

                ReadEndOfCentralDirectory();
            }
            catch
            {
                if (extraTempStream != null)
                {
                    extraTempStream.Dispose();
                }
                throw;
            }

        }

        protected override void DisposeInternal() => CloseStreamsAndMarkAsDisposed();

        public override ZipArchiveEntry CreateEntry(string entryName, CompressionLevel? compressionLevel) => throw new NotSupportedException(SR.CreateInReadMode);

        public override ZipArchiveEntry? GetEntry(string entryName) => GetEntryInternal(entryName);

        public override void PerformModeSpecificEocdReadActions(ZipEndOfCentralDirectoryBlock eocd) { }

        public override void PerformModeSpecificWriteFileActions() { }
        public override void RemoveEntry(ZipArchiveEntry entry) => throw new NotSupportedException(SR.CreateInReadMode);
    }
}
