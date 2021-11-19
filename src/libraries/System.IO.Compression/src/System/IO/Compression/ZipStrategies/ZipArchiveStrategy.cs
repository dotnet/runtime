// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace System.IO.Compression
{
    internal abstract class ZipArchiveStrategy : IDisposable
    {
        private readonly ReadOnlyCollection<ZipArchiveEntry> _entriesCollection;
        private Encoding? _entryNameEncoding;

        public ZipArchive Archive { get; }
        public byte[]? ArchiveComment { get; set; }
        public abstract BinaryReader? ArchiveReader { get; }
        public abstract Stream ArchiveStream { get; }
        public ZipArchiveEntry? ArchiveStreamOwner { get; set; }
        public abstract Stream? BackingStream { get; }
        public long CentralDirectoryStart { get; set; } //only valid after ReadCentralDirectory
        public List<ZipArchiveEntry> Entries { get; }
        public abstract ReadOnlyCollection<ZipArchiveEntry> EntriesCollection { get; }
        public Dictionary<string, ZipArchiveEntry> EntriesDictionary { get; }
        public Encoding? EntryNameEncoding
        {
            get => _entryNameEncoding;
            set
            {
                // value == null is fine. This means the user does not want to overwrite default encoding picking logic.

                // The Zip file spec [http://www.pkware.com/documents/casestudies/APPNOTE.TXT] specifies a bit in the entry header
                // (specifically: the language encoding flag (EFS) in the general purpose bit flag of the local file header) that
                // basically says: UTF8 (1) or CP437 (0). But in reality, tools replace CP437 with "something else that is not UTF8".
                // For instance, the Windows Shell Zip tool takes "something else" to mean "the local system codepage".
                // We default to the same behaviour, but we let the user explicitly specify the encoding to use for cases where they
                // understand their use case well enough.
                // Since the definition of acceptable encodings for the "something else" case is in reality by convention, it is not
                // immediately clear, whether non-UTF8 Unicode encodings are acceptable. To determine that we would need to survey
                // what is currently being done in the field, but we do not have the time for it right now.
                // So, we artificially disallow non-UTF8 Unicode encodings for now to make sure we are not creating a compat burden
                // for something other tools do not support. If we realise in future that "something else" should include non-UTF8
                // Unicode encodings, we can remove this restriction.

                if (value != null &&
                    (value.Equals(Encoding.BigEndianUnicode) || value.Equals(Encoding.Unicode)))
                {
                    throw new ArgumentException(SR.EntryNameEncodingNotSupported, nameof(EntryNameEncoding));
                }

                _entryNameEncoding = value;
            }
        }
        public long ExpectedNumberOfEntries { get; set; }
        public bool IsDisposed { get; set; }
        public bool LeaveOpen { get; }
        public abstract ZipArchiveMode Mode { get; }
        public uint NumberOfThisDisk { get; set; } //only valid after ReadCentralDirectory
        public bool ReadEntries { get; set; }
        public virtual bool ShouldSaveExtraFieldsAndComments => false;

        internal ZipArchiveStrategy(ZipArchive archive, bool leaveOpen, Encoding? entryNameEncoding, bool readEntries)
        {
            Archive = archive;
            EntryNameEncoding = entryNameEncoding;
            LeaveOpen = leaveOpen;
            ReadEntries = readEntries;

            ArchiveComment = null;
            ArchiveStreamOwner = null;
            CentralDirectoryStart = 0; // invalid until ReadCentralDirectory
            Entries = new List<ZipArchiveEntry>();
            EntriesDictionary = new Dictionary<string, ZipArchiveEntry>();
            ExpectedNumberOfEntries = 0;
            IsDisposed = false;
            NumberOfThisDisk = 0; // invalid until ReadCentralDirectory

            _entriesCollection = new ReadOnlyCollection<ZipArchiveEntry>(Entries);
        }

        public abstract ZipArchiveEntry CreateEntry(string entryName, CompressionLevel? compressionLevel);
        protected abstract void DisposeInternal();
        public abstract ZipArchiveEntry? GetEntry(string entryName);
        public abstract void PerformModeSpecificEocdReadActions(ZipEndOfCentralDirectoryBlock eocd);
        public abstract void PerformModeSpecificWriteFileActions();
        public abstract void RemoveEntry(ZipArchiveEntry entry);

        public void AcquireArchiveStream(ZipArchiveEntry entry)
        {
            // if a previous entry had held the stream but never wrote anything, we write their local header for them
            if (ArchiveStreamOwner != null)
            {
                if (!ArchiveStreamOwner.EverOpenedForWrite)
                {
                    ArchiveStreamOwner.WriteAndFinishLocalEntry();
                }
                else
                {
                    throw new IOException(SR.CreateModeCreateEntryWhileOpen);
                }
            }

            ArchiveStreamOwner = entry;
        }

        public void Dispose(bool disposing)
        {
            if (disposing && !IsDisposed)
            {
                DisposeInternal();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void ReleaseArchiveStream(ZipArchiveEntry entry)
        {
            Debug.Assert(ArchiveStreamOwner == entry);
            ArchiveStreamOwner = null;
        }

        public void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().ToString());
            }
        }

        protected void CloseStreamsAndMarkAsDisposed()
        {
            CloseStreams();
            IsDisposed = true;
        }

        protected ZipArchiveEntry CreateEntryInternal(string entryName, CompressionLevel? compressionLevel)
        {
            Debug.Assert(Mode != ZipArchiveMode.Read);

            if (entryName == null)
            {
                throw new ArgumentNullException(nameof(entryName));
            }

            if (string.IsNullOrEmpty(entryName))
            {
                throw new ArgumentException(SR.CannotBeEmpty, nameof(entryName));
            }

            ThrowIfDisposed();

            ZipArchiveEntry entry = compressionLevel.HasValue ?
                new ZipArchiveEntry(this, entryName, compressionLevel.Value) :
                new ZipArchiveEntry(this, entryName);

            AddEntry(entry);

            return entry;
        }

        protected ZipArchiveEntry? GetEntryInternal(string entryName)
        {
            Debug.Assert(Mode != ZipArchiveMode.Create);

            if (entryName == null)
            {
                throw new ArgumentNullException(nameof(entryName));
            }

            EnsureCentralDirectoryRead();
            EntriesDictionary.TryGetValue(entryName, out ZipArchiveEntry? result);
            return result;
        }

        protected ReadOnlyCollection<ZipArchiveEntry> GetEntriesInternal()
        {
            Debug.Assert(Mode != ZipArchiveMode.Create);

            ThrowIfDisposed();
            EnsureCentralDirectoryRead();

            return _entriesCollection;
        }

        // This function reads all the EOCD stuff it needs to find the offset to the start of the central directory
        // This offset gets put in _centralDirectoryStart and the number of this disk gets put in _numberOfThisDisk
        // Also does some verification that this isn't a split/spanned archive
        // Also checks that offset to CD isn't out of bounds
        protected void ReadEndOfCentralDirectory()
        {
            try
            {
                // This seeks backwards almost to the beginning of the EOCD, one byte after where the signature would be
                // located if the EOCD had the minimum possible size (no file zip comment)
                ArchiveStream.Seek(-ZipEndOfCentralDirectoryBlock.SizeOfBlockWithoutSignature, SeekOrigin.End);

                // If the EOCD has the minimum possible size (no zip file comment), then exactly the previous 4 bytes will contain the signature
                // But if the EOCD has max possible size, the signature should be found somewhere in the previous 64K + 4 bytes
                if (!ZipHelper.SeekBackwardsToSignature(ArchiveStream,
                        ZipEndOfCentralDirectoryBlock.SignatureConstant,
                        ZipEndOfCentralDirectoryBlock.ZipFileCommentMaxLength + ZipEndOfCentralDirectoryBlock.SignatureSize))
                {
                    throw new InvalidDataException(SR.EOCDNotFound);
                }

                long eocdStart = ArchiveStream.Position;

                Debug.Assert(ArchiveReader != null);
                // read the EOCD
                bool eocdProper = ZipEndOfCentralDirectoryBlock.TryReadBlock(ArchiveReader, out ZipEndOfCentralDirectoryBlock eocd);
                Debug.Assert(eocdProper); // we just found this using the signature finder, so it should be okay

                if (eocd.NumberOfThisDisk != eocd.NumberOfTheDiskWithTheStartOfTheCentralDirectory)
                {
                    throw new InvalidDataException(SR.SplitSpanned);
                }

                NumberOfThisDisk = eocd.NumberOfThisDisk;
                CentralDirectoryStart = eocd.OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber;

                if (eocd.NumberOfEntriesInTheCentralDirectory != eocd.NumberOfEntriesInTheCentralDirectoryOnThisDisk)
                {
                    throw new InvalidDataException(SR.SplitSpanned);
                }

                ExpectedNumberOfEntries = eocd.NumberOfEntriesInTheCentralDirectory;

                PerformModeSpecificEocdReadActions(eocd);

                TryReadZip64EndOfCentralDirectory(eocd, eocdStart);

                if (CentralDirectoryStart > ArchiveStream.Length)
                {
                    throw new InvalidDataException(SR.FieldTooBigOffsetToCD);
                }
            }
            catch (EndOfStreamException ex)
            {
                throw new InvalidDataException(SR.CDCorrupt, ex);
            }
            catch (IOException ex)
            {
                throw new InvalidDataException(SR.CDCorrupt, ex);
            }
        }

        protected void WriteAndDispose()
        {
            try
            {
                WriteFile();
            }
            finally
            {
                CloseStreamsAndMarkAsDisposed();
            }
        }

        protected void EnsureCentralDirectoryRead()
        {
            if (!ReadEntries)
            {
                ReadCentralDirectory();
                ReadEntries = true;
            }
        }

        private void AddEntry(ZipArchiveEntry entry)
        {
            Entries.Add(entry);

            string entryName = entry.FullName;
            if (!EntriesDictionary.ContainsKey(entryName))
            {
                EntriesDictionary.Add(entryName, entry);
            }
        }

        private void CloseStreams()
        {
            if (!LeaveOpen)
            {
                ArchiveStream.Dispose();
                BackingStream?.Dispose();
                ArchiveReader?.Dispose();
            }
            else
            {
                // if BackingStream isn't null, that means we assigned the original stream they passed
                // us to BackingStream (which they requested we leave open), and ArchiveStream was
                // the temporary copy that we needed
                if (BackingStream != null)
                {
                    ArchiveStream.Dispose();
                }
            }
        }

        private void ReadCentralDirectory()
        {
            Debug.Assert(ArchiveReader != null);
            try
            {
                // assume ReadEndOfCentralDirectory has been called and has populated _centralDirectoryStart

                ArchiveStream.Seek(CentralDirectoryStart, SeekOrigin.Begin);

                long numberOfEntries = 0;

                //read the central directory
                while (ZipCentralDirectoryFileHeader.TryReadBlock(ArchiveReader, ShouldSaveExtraFieldsAndComments, out ZipCentralDirectoryFileHeader currentHeader))
                {
                    AddEntry(new ZipArchiveEntry(this, currentHeader));
                    numberOfEntries++;
                }

                if (numberOfEntries != ExpectedNumberOfEntries)
                {
                    throw new InvalidDataException(SR.NumEntriesWrong);
                }
            }
            catch (EndOfStreamException ex)
            {
                throw new InvalidDataException(SR.Format(SR.CentralDirectoryInvalid, ex));
            }
        }

        // Tries to find the Zip64 End of Central Directory Locator, then the Zip64 End of Central Directory, assuming the
        // End of Central Directory block has already been found, as well as the location in the stream where the EOCD starts.
        private void TryReadZip64EndOfCentralDirectory(ZipEndOfCentralDirectoryBlock eocd, long eocdStart)
        {
            // Only bother looking for the Zip64-EOCD stuff if we suspect it is needed because some value is FFFFFFFFF
            // because these are the only two values we need, we only worry about these
            // if we don't find the Zip64-EOCD, we just give up and try to use the original values
            if (eocd.NumberOfThisDisk == ZipHelper.Mask16Bit ||
                eocd.OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber == ZipHelper.Mask32Bit ||
                eocd.NumberOfEntriesInTheCentralDirectory == ZipHelper.Mask16Bit)
            {
                // Read Zip64 End of Central Directory Locator

                // This seeks forwards almost to the beginning of the Zip64-EOCDL, one byte after where the signature would be located
                ArchiveStream.Seek(eocdStart - Zip64EndOfCentralDirectoryLocator.SizeOfBlockWithoutSignature, SeekOrigin.Begin);

                // Exactly the previous 4 bytes should contain the Zip64-EOCDL signature
                // if we don't find it, assume it doesn't exist and use data from normal EOCD
                if (ZipHelper.SeekBackwardsToSignature(
                        ArchiveStream,
                        Zip64EndOfCentralDirectoryLocator.SignatureConstant,
                        Zip64EndOfCentralDirectoryLocator.SignatureSize))
                {
                    Debug.Assert(ArchiveReader != null);

                    // use locator to get to Zip64-EOCD
                    bool zip64eocdLocatorProper = Zip64EndOfCentralDirectoryLocator.TryReadBlock(ArchiveReader, out Zip64EndOfCentralDirectoryLocator locator);
                    Debug.Assert(zip64eocdLocatorProper); // we just found this using the signature finder, so it should be okay

                    if (locator.OffsetOfZip64EOCD > long.MaxValue)
                    {
                        throw new InvalidDataException(SR.FieldTooBigOffsetToZip64EOCD);
                    }

                    long zip64EOCDOffset = (long)locator.OffsetOfZip64EOCD;

                    ArchiveStream.Seek(zip64EOCDOffset, SeekOrigin.Begin);

                    // Read Zip64 End of Central Directory Record

                    if (!Zip64EndOfCentralDirectoryRecord.TryReadBlock(ArchiveReader, out Zip64EndOfCentralDirectoryRecord record))
                    {
                        throw new InvalidDataException(SR.Zip64EOCDNotWhereExpected);
                    }

                    NumberOfThisDisk = record.NumberOfThisDisk;

                    if (record.NumberOfEntriesTotal > long.MaxValue)
                    {
                        throw new InvalidDataException(SR.FieldTooBigNumEntries);
                    }

                    if (record.OffsetOfCentralDirectory > long.MaxValue)
                    {
                        throw new InvalidDataException(SR.FieldTooBigOffsetToCD);
                    }

                    if (record.NumberOfEntriesTotal != record.NumberOfEntriesOnThisDisk)
                    {
                        throw new InvalidDataException(SR.SplitSpanned);
                    }

                    ExpectedNumberOfEntries = (long)record.NumberOfEntriesTotal;
                    CentralDirectoryStart = (long)record.OffsetOfCentralDirectory;
                }
            }
        }

        private void WriteFile()
        {
            // if we are in create mode, we always set readEntries to true in Init
            // if we are in update mode, we call EnsureCentralDirectoryRead, which sets readEntries to true
            Debug.Assert(ReadEntries);

            PerformModeSpecificWriteFileActions();

            foreach (ZipArchiveEntry entry in Entries)
            {
                entry.WriteAndFinishLocalEntry();
            }

            long startOfCentralDirectory = ArchiveStream.Position;

            foreach (ZipArchiveEntry entry in Entries)
            {
                entry.WriteCentralDirectoryFileHeader();
            }

            long sizeOfCentralDirectory = ArchiveStream.Position - startOfCentralDirectory;

            WriteArchiveEpilogue(startOfCentralDirectory, sizeOfCentralDirectory);
        }

        // writes eocd, and if needed, zip 64 eocd, zip64 eocd locator
        // should only throw an exception in extremely exceptional cases because it is called from dispose
        private void WriteArchiveEpilogue(long startOfCentralDirectory, long sizeOfCentralDirectory)
        {
            // determine if we need Zip 64
            if (startOfCentralDirectory >= uint.MaxValue
                || sizeOfCentralDirectory >= uint.MaxValue
                || Entries.Count >= ZipHelper.Mask16Bit
#if DEBUG_FORCE_ZIP64
                || _forceZip64
#endif
                )
            {
                // if we need zip 64, write zip 64 eocd and locator
                long zip64EOCDRecordStart = ArchiveStream.Position;
                Zip64EndOfCentralDirectoryRecord.WriteBlock(ArchiveStream, Entries.Count, startOfCentralDirectory, sizeOfCentralDirectory);
                Zip64EndOfCentralDirectoryLocator.WriteBlock(ArchiveStream, zip64EOCDRecordStart);
            }

            // write normal eocd
            ZipEndOfCentralDirectoryBlock.WriteBlock(ArchiveStream, Entries.Count, startOfCentralDirectory, sizeOfCentralDirectory, ArchiveComment);
        }
    }
}
