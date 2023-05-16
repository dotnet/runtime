// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// Zip Spec here: http://www.pkware.com/documents/casestudies/APPNOTE.TXT

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.IO.Compression
{
    public class ZipArchive : IDisposable
    {
        private readonly Stream _archiveStream;
        private ZipArchiveEntry? _archiveStreamOwner;
        private readonly BinaryReader? _archiveReader;
        private readonly ZipArchiveMode _mode;
        private readonly List<ZipArchiveEntry> _entries;
        private readonly ReadOnlyCollection<ZipArchiveEntry> _entriesCollection;
        private readonly Dictionary<string, ZipArchiveEntry> _entriesDictionary;
        private bool _readEntries;
        private readonly bool _leaveOpen;
        private long _centralDirectoryStart; //only valid after ReadCentralDirectory
        private bool _isDisposed;
        private uint _numberOfThisDisk; //only valid after ReadCentralDirectory
        private long _expectedNumberOfEntries;
        private readonly Stream? _backingStream;
        private byte[] _archiveComment;
        private Encoding? _entryNameAndCommentEncoding;

#if DEBUG_FORCE_ZIP64
        public bool _forceZip64;
#endif

        /// <summary>
        /// Initializes a new instance of ZipArchive on the given stream for reading.
        /// </summary>
        /// <exception cref="ArgumentException">The stream is already closed or does not support reading.</exception>
        /// <exception cref="ArgumentNullException">The stream is null.</exception>
        /// <exception cref="InvalidDataException">The contents of the stream could not be interpreted as a Zip archive.</exception>
        /// <param name="stream">The stream containing the archive to be read.</param>
        public ZipArchive(Stream stream) : this(stream, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: null) { }

        /// <summary>
        /// Initializes a new instance of ZipArchive on the given stream in the specified mode.
        /// </summary>
        /// <exception cref="ArgumentException">The stream is already closed. -or- mode is incompatible with the capabilities of the stream.</exception>
        /// <exception cref="ArgumentNullException">The stream is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">mode specified an invalid value.</exception>
        /// <exception cref="InvalidDataException">The contents of the stream could not be interpreted as a Zip file. -or- mode is Update and an entry is missing from the archive or is corrupt and cannot be read. -or- mode is Update and an entry is too large to fit into memory.</exception>
        /// <param name="stream">The input or output stream.</param>
        /// <param name="mode">See the description of the ZipArchiveMode enum. Read requires the stream to support reading, Create requires the stream to support writing, and Update requires the stream to support reading, writing, and seeking.</param>
        public ZipArchive(Stream stream, ZipArchiveMode mode) : this(stream, mode, leaveOpen: false, entryNameEncoding: null) { }

        /// <summary>
        /// Initializes a new instance of ZipArchive on the given stream in the specified mode, specifying whether to leave the stream open.
        /// </summary>
        /// <exception cref="ArgumentException">The stream is already closed. -or- mode is incompatible with the capabilities of the stream.</exception>
        /// <exception cref="ArgumentNullException">The stream is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">mode specified an invalid value.</exception>
        /// <exception cref="InvalidDataException">The contents of the stream could not be interpreted as a Zip file. -or- mode is Update and an entry is missing from the archive or is corrupt and cannot be read. -or- mode is Update and an entry is too large to fit into memory.</exception>
        /// <param name="stream">The input or output stream.</param>
        /// <param name="mode">See the description of the ZipArchiveMode enum. Read requires the stream to support reading, Create requires the stream to support writing, and Update requires the stream to support reading, writing, and seeking.</param>
        /// <param name="leaveOpen">true to leave the stream open upon disposing the ZipArchive, otherwise false.</param>
        public ZipArchive(Stream stream, ZipArchiveMode mode, bool leaveOpen) : this(stream, mode, leaveOpen, entryNameEncoding: null) { }

        /// <summary>
        /// Initializes a new instance of ZipArchive on the given stream in the specified mode, specifying whether to leave the stream open.
        /// </summary>
        /// <exception cref="ArgumentException">The stream is already closed. -or- mode is incompatible with the capabilities of the stream.</exception>
        /// <exception cref="ArgumentNullException">The stream is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">mode specified an invalid value.</exception>
        /// <exception cref="InvalidDataException">The contents of the stream could not be interpreted as a Zip file. -or- mode is Update and an entry is missing from the archive or is corrupt and cannot be read. -or- mode is Update and an entry is too large to fit into memory.</exception>
        /// <param name="stream">The input or output stream.</param>
        /// <param name="mode">See the description of the ZipArchiveMode enum. Read requires the stream to support reading, Create requires the stream to support writing, and Update requires the stream to support reading, writing, and seeking.</param>
        /// <param name="leaveOpen">true to leave the stream open upon disposing the ZipArchive, otherwise false.</param>
        /// <param name="entryNameEncoding">The encoding to use when reading or writing entry names in this ZipArchive.
        ///         ///     <para>NOTE: Specifying this parameter to values other than <c>null</c> is discouraged.
        ///         However, this may be necessary for interoperability with ZIP archive tools and libraries that do not correctly support
        ///         UTF-8 encoding for entry names.<br />
        ///         This value is used as follows:</para>
        ///     <para><strong>Reading (opening) ZIP archive files:</strong></para>
        ///     <para>If <c>entryNameEncoding</c> is not specified (<c>== null</c>):</para>
        ///     <list>
        ///         <item>For entries where the language encoding flag (EFS) in the general purpose bit flag of the local file header is <em>not</em> set,
        ///         use the current system default code page (<c>Encoding.Default</c>) in order to decode the entry name.</item>
        ///         <item>For entries where the language encoding flag (EFS) in the general purpose bit flag of the local file header <em>is</em> set,
        ///         use UTF-8 (<c>Encoding.UTF8</c>) in order to decode the entry name.</item>
        ///     </list>
        ///     <para>If <c>entryNameEncoding</c> is specified (<c>!= null</c>):</para>
        ///     <list>
        ///         <item>For entries where the language encoding flag (EFS) in the general purpose bit flag of the local file header is <em>not</em> set,
        ///         use the specified <c>entryNameEncoding</c> in order to decode the entry name.</item>
        ///         <item>For entries where the language encoding flag (EFS) in the general purpose bit flag of the local file header <em>is</em> set,
        ///         use UTF-8 (<c>Encoding.UTF8</c>) in order to decode the entry name.</item>
        ///     </list>
        ///     <para><strong>Writing (saving) ZIP archive files:</strong></para>
        ///     <para>If <c>entryNameEncoding</c> is not specified (<c>== null</c>):</para>
        ///     <list>
        ///         <item>For entry names that contain characters outside the ASCII range,
        ///         the language encoding flag (EFS) will be set in the general purpose bit flag of the local file header,
        ///         and UTF-8 (<c>Encoding.UTF8</c>) will be used in order to encode the entry name into bytes.</item>
        ///         <item>For entry names that do not contain characters outside the ASCII range,
        ///         the language encoding flag (EFS) will not be set in the general purpose bit flag of the local file header,
        ///         and the current system default code page (<c>Encoding.Default</c>) will be used to encode the entry names into bytes.</item>
        ///     </list>
        ///     <para>If <c>entryNameEncoding</c> is specified (<c>!= null</c>):</para>
        ///     <list>
        ///         <item>The specified <c>entryNameEncoding</c> will always be used to encode the entry names into bytes.
        ///         The language encoding flag (EFS) in the general purpose bit flag of the local file header will be set if and only
        ///         if the specified <c>entryNameEncoding</c> is a UTF-8 encoding.</item>
        ///     </list>
        ///     <para>Note that Unicode encodings other than UTF-8 may not be currently used for the <c>entryNameEncoding</c>,
        ///     otherwise an <see cref="ArgumentException"/> is thrown.</para>
        /// </param>
        /// <exception cref="ArgumentException">If a Unicode encoding other than UTF-8 is specified for the <code>entryNameEncoding</code>.</exception>
        public ZipArchive(Stream stream, ZipArchiveMode mode, bool leaveOpen, Encoding? entryNameEncoding)
        {
            ArgumentNullException.ThrowIfNull(stream);

            EntryNameAndCommentEncoding = entryNameEncoding;
            Stream? extraTempStream = null;

            try
            {
                _backingStream = null;

                // check stream against mode
                switch (mode)
                {
                    case ZipArchiveMode.Create:
                        if (!stream.CanWrite)
                            throw new ArgumentException(SR.CreateModeCapabilities);
                        break;
                    case ZipArchiveMode.Read:
                        if (!stream.CanRead)
                            throw new ArgumentException(SR.ReadModeCapabilities);
                        if (!stream.CanSeek)
                        {
                            _backingStream = stream;
                            extraTempStream = stream = new MemoryStream();
                            _backingStream.CopyTo(stream);
                            stream.Seek(0, SeekOrigin.Begin);
                        }
                        break;
                    case ZipArchiveMode.Update:
                        if (!stream.CanRead || !stream.CanWrite || !stream.CanSeek)
                            throw new ArgumentException(SR.UpdateModeCapabilities);
                        break;
                    default:
                        // still have to throw this, because stream constructor doesn't do mode argument checks
                        throw new ArgumentOutOfRangeException(nameof(mode));
                }

                _mode = mode;
                if (mode == ZipArchiveMode.Create && !stream.CanSeek)
                    _archiveStream = new PositionPreservingWriteOnlyStreamWrapper(stream);
                else
                    _archiveStream = stream;
                _archiveStreamOwner = null;
                if (mode == ZipArchiveMode.Create)
                    _archiveReader = null;
                else
                    _archiveReader = new BinaryReader(_archiveStream);
                _entries = new List<ZipArchiveEntry>();
                _entriesCollection = new ReadOnlyCollection<ZipArchiveEntry>(_entries);
                _entriesDictionary = new Dictionary<string, ZipArchiveEntry>();
                _readEntries = false;
                _leaveOpen = leaveOpen;
                _centralDirectoryStart = 0; // invalid until ReadCentralDirectory
                _isDisposed = false;
                _numberOfThisDisk = 0; // invalid until ReadCentralDirectory
                _archiveComment = Array.Empty<byte>();

                switch (mode)
                {
                    case ZipArchiveMode.Create:
                        _readEntries = true;
                        break;
                    case ZipArchiveMode.Read:
                        ReadEndOfCentralDirectory();
                        break;
                    case ZipArchiveMode.Update:
                    default:
                        Debug.Assert(mode == ZipArchiveMode.Update);
                        if (_archiveStream.Length == 0)
                        {
                            _readEntries = true;
                        }
                        else
                        {
                            ReadEndOfCentralDirectory();
                            EnsureCentralDirectoryRead();
                            foreach (ZipArchiveEntry entry in _entries)
                            {
                                entry.ThrowIfNotOpenable(needToUncompress: false, needToLoadIntoMemory: true);
                            }
                        }
                        break;
                }
            }
            catch
            {
                extraTempStream?.Dispose();

                throw;
            }
        }

        /// <summary>
        /// Gets or sets the optional archive comment.
        /// </summary>
        /// <remarks>
        /// The comment encoding is determined by the <c>entryNameEncoding</c> parameter of the <see cref="ZipArchive(Stream,ZipArchiveMode,bool,Encoding?)"/> constructor.
        /// If the comment byte length is larger than <see cref="ushort.MaxValue"/>, it will be truncated when disposing the archive.
        /// </remarks>
        [AllowNull]
        public string Comment
        {
            get => (EntryNameAndCommentEncoding ?? Encoding.UTF8).GetString(_archiveComment);
            set => _archiveComment = ZipHelper.GetEncodedTruncatedBytesFromString(value, EntryNameAndCommentEncoding, ZipEndOfCentralDirectoryBlock.ZipFileCommentMaxLength, out _);
        }

        /// <summary>
        /// The collection of entries that are currently in the ZipArchive. This may not accurately represent the actual entries that are present in the underlying file or stream.
        /// </summary>
        /// <exception cref="NotSupportedException">The ZipArchive does not support reading.</exception>
        /// <exception cref="ObjectDisposedException">The ZipArchive has already been closed.</exception>
        /// <exception cref="InvalidDataException">The Zip archive is corrupt and the entries cannot be retrieved.</exception>
        public ReadOnlyCollection<ZipArchiveEntry> Entries
        {
            get
            {
                if (_mode == ZipArchiveMode.Create)
                    throw new NotSupportedException(SR.EntriesInCreateMode);

                ThrowIfDisposed();

                EnsureCentralDirectoryRead();
                return _entriesCollection;
            }
        }

        /// <summary>
        /// The ZipArchiveMode that the ZipArchive was initialized with.
        /// </summary>
        public ZipArchiveMode Mode
        {
            get
            {
                return _mode;
            }
        }

        /// <summary>
        /// Creates an empty entry in the Zip archive with the specified entry name.
        /// There are no restrictions on the names of entries.
        /// The last write time of the entry is set to the current time.
        /// If an entry with the specified name already exists in the archive, a second entry will be created that has an identical name.
        /// Since no <code>CompressionLevel</code> is specified, the default provided by the implementation of the underlying compression
        /// algorithm will be used; the <code>ZipArchive</code> will not impose its own default.
        /// (Currently, the underlying compression algorithm is provided by the <code>System.IO.Compression.DeflateStream</code> class.)
        /// </summary>
        /// <exception cref="ArgumentException">entryName is a zero-length string.</exception>
        /// <exception cref="ArgumentNullException">entryName is null.</exception>
        /// <exception cref="NotSupportedException">The ZipArchive does not support writing.</exception>
        /// <exception cref="ObjectDisposedException">The ZipArchive has already been closed.</exception>
        /// <param name="entryName">A path relative to the root of the archive, indicating the name of the entry to be created.</param>
        /// <returns>A wrapper for the newly created file entry in the archive.</returns>
        public ZipArchiveEntry CreateEntry(string entryName)
        {
            return DoCreateEntry(entryName, null);
        }

        /// <summary>
        /// Creates an empty entry in the Zip archive with the specified entry name. There are no restrictions on the names of entries. The last write time of the entry is set to the current time. If an entry with the specified name already exists in the archive, a second entry will be created that has an identical name.
        /// </summary>
        /// <exception cref="ArgumentException">entryName is a zero-length string.</exception>
        /// <exception cref="ArgumentNullException">entryName is null.</exception>
        /// <exception cref="NotSupportedException">The ZipArchive does not support writing.</exception>
        /// <exception cref="ObjectDisposedException">The ZipArchive has already been closed.</exception>
        /// <param name="entryName">A path relative to the root of the archive, indicating the name of the entry to be created.</param>
        /// <param name="compressionLevel">The level of the compression (speed/memory vs. compressed size trade-off).</param>
        /// <returns>A wrapper for the newly created file entry in the archive.</returns>
        public ZipArchiveEntry CreateEntry(string entryName, CompressionLevel compressionLevel)
        {
            return DoCreateEntry(entryName, compressionLevel);
        }

        /// <summary>
        /// Releases the unmanaged resources used by ZipArchive and optionally finishes writing the archive and releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to finish writing the archive and release unmanaged and managed resources, false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed)
            {
                try
                {
                    switch (_mode)
                    {
                        case ZipArchiveMode.Read:
                            break;
                        case ZipArchiveMode.Create:
                        case ZipArchiveMode.Update:
                        default:
                            Debug.Assert(_mode == ZipArchiveMode.Update || _mode == ZipArchiveMode.Create);
                            WriteFile();
                            break;
                    }
                }
                finally
                {
                    CloseStreams();
                    _isDisposed = true;
                }
            }
        }

        /// <summary>
        /// Finishes writing the archive and releases all resources used by the ZipArchive object, unless the object was constructed with leaveOpen as true. Any streams from opened entries in the ZipArchive still open will throw exceptions on subsequent writes, as the underlying streams will have been closed.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Retrieves a wrapper for the file entry in the archive with the specified name. Names are compared using ordinal comparison. If there are multiple entries in the archive with the specified name, the first one found will be returned.
        /// </summary>
        /// <exception cref="ArgumentException">entryName is a zero-length string.</exception>
        /// <exception cref="ArgumentNullException">entryName is null.</exception>
        /// <exception cref="NotSupportedException">The ZipArchive does not support reading.</exception>
        /// <exception cref="ObjectDisposedException">The ZipArchive has already been closed.</exception>
        /// <exception cref="InvalidDataException">The Zip archive is corrupt and the entries cannot be retrieved.</exception>
        /// <param name="entryName">A path relative to the root of the archive, identifying the desired entry.</param>
        /// <returns>A wrapper for the file entry in the archive. If no entry in the archive exists with the specified name, null will be returned.</returns>
        public ZipArchiveEntry? GetEntry(string entryName)
        {
            ArgumentNullException.ThrowIfNull(entryName);

            if (_mode == ZipArchiveMode.Create)
                throw new NotSupportedException(SR.EntriesInCreateMode);

            EnsureCentralDirectoryRead();
            _entriesDictionary.TryGetValue(entryName, out ZipArchiveEntry? result);
            return result;
        }

        internal BinaryReader? ArchiveReader => _archiveReader;

        internal Stream ArchiveStream => _archiveStream;

        internal uint NumberOfThisDisk => _numberOfThisDisk;

        internal Encoding? EntryNameAndCommentEncoding
        {
            get => _entryNameAndCommentEncoding;

            private set
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
                        (value.Equals(Encoding.BigEndianUnicode)
                        || value.Equals(Encoding.Unicode)))
                {
                    throw new ArgumentException(SR.EntryNameAndCommentEncodingNotSupported, nameof(EntryNameAndCommentEncoding));
                }

                _entryNameAndCommentEncoding = value;
            }
        }

        private ZipArchiveEntry DoCreateEntry(string entryName, CompressionLevel? compressionLevel)
        {
            ArgumentException.ThrowIfNullOrEmpty(entryName);

            if (_mode == ZipArchiveMode.Read)
                throw new NotSupportedException(SR.CreateInReadMode);

            ThrowIfDisposed();


            ZipArchiveEntry entry = compressionLevel.HasValue ?
                new ZipArchiveEntry(this, entryName, compressionLevel.Value) :
                new ZipArchiveEntry(this, entryName);

            AddEntry(entry);

            return entry;
        }

        internal void AcquireArchiveStream(ZipArchiveEntry entry)
        {
            // if a previous entry had held the stream but never wrote anything, we write their local header for them
            if (_archiveStreamOwner != null)
            {
                if (!_archiveStreamOwner.EverOpenedForWrite)
                {
                    _archiveStreamOwner.WriteAndFinishLocalEntry();
                }
                else
                {
                    throw new IOException(SR.CreateModeCreateEntryWhileOpen);
                }
            }

            _archiveStreamOwner = entry;
        }

        private void AddEntry(ZipArchiveEntry entry)
        {
            _entries.Add(entry);
            _entriesDictionary.TryAdd(entry.FullName, entry);
        }

        [Conditional("DEBUG")]
        internal void DebugAssertIsStillArchiveStreamOwner(ZipArchiveEntry entry) => Debug.Assert(_archiveStreamOwner == entry);

        internal void ReleaseArchiveStream(ZipArchiveEntry entry)
        {
            Debug.Assert(_archiveStreamOwner == entry);

            _archiveStreamOwner = null;
        }

        internal void RemoveEntry(ZipArchiveEntry entry)
        {
            _entries.Remove(entry);

            _entriesDictionary.Remove(entry.FullName);
        }

        internal void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
        }

        private void CloseStreams()
        {
            if (!_leaveOpen)
            {
                _archiveStream.Dispose();
                _backingStream?.Dispose();
                _archiveReader?.Dispose();
            }
            else
            {
                // if _backingStream isn't null, that means we assigned the original stream they passed
                // us to _backingStream (which they requested we leave open), and _archiveStream was
                // the temporary copy that we needed
                if (_backingStream != null)
                    _archiveStream.Dispose();
            }
        }

        private void EnsureCentralDirectoryRead()
        {
            if (!_readEntries)
            {
                ReadCentralDirectory();
                _readEntries = true;
            }
        }

        private void ReadCentralDirectory()
        {
            try
            {
                // assume ReadEndOfCentralDirectory has been called and has populated _centralDirectoryStart

                _archiveStream.Seek(_centralDirectoryStart, SeekOrigin.Begin);

                long numberOfEntries = 0;

                Debug.Assert(_archiveReader != null);
                //read the central directory
                ZipCentralDirectoryFileHeader currentHeader;
                bool saveExtraFieldsAndComments = Mode == ZipArchiveMode.Update;
                while (ZipCentralDirectoryFileHeader.TryReadBlock(_archiveReader,
                                                        saveExtraFieldsAndComments, out currentHeader))
                {
                    AddEntry(new ZipArchiveEntry(this, currentHeader));
                    numberOfEntries++;
                }

                if (numberOfEntries != _expectedNumberOfEntries)
                    throw new InvalidDataException(SR.NumEntriesWrong);
            }
            catch (EndOfStreamException ex)
            {
                throw new InvalidDataException(SR.Format(SR.CentralDirectoryInvalid, ex));
            }
        }

        // This function reads all the EOCD stuff it needs to find the offset to the start of the central directory
        // This offset gets put in _centralDirectoryStart and the number of this disk gets put in _numberOfThisDisk
        // Also does some verification that this isn't a split/spanned archive
        // Also checks that offset to CD isn't out of bounds
        private void ReadEndOfCentralDirectory()
        {
            try
            {
                // This seeks backwards almost to the beginning of the EOCD, one byte after where the signature would be
                // located if the EOCD had the minimum possible size (no file zip comment)
                _archiveStream.Seek(-ZipEndOfCentralDirectoryBlock.SizeOfBlockWithoutSignature, SeekOrigin.End);

                // If the EOCD has the minimum possible size (no zip file comment), then exactly the previous 4 bytes will contain the signature
                // But if the EOCD has max possible size, the signature should be found somewhere in the previous 64K + 4 bytes
                if (!ZipHelper.SeekBackwardsToSignature(_archiveStream,
                        ZipEndOfCentralDirectoryBlock.SignatureConstant,
                        ZipEndOfCentralDirectoryBlock.ZipFileCommentMaxLength + ZipEndOfCentralDirectoryBlock.SignatureSize))
                    throw new InvalidDataException(SR.EOCDNotFound);

                long eocdStart = _archiveStream.Position;

                Debug.Assert(_archiveReader != null);
                // read the EOCD
                ZipEndOfCentralDirectoryBlock eocd;
                bool eocdProper = ZipEndOfCentralDirectoryBlock.TryReadBlock(_archiveReader, out eocd);
                Debug.Assert(eocdProper); // we just found this using the signature finder, so it should be okay

                if (eocd.NumberOfThisDisk != eocd.NumberOfTheDiskWithTheStartOfTheCentralDirectory)
                    throw new InvalidDataException(SR.SplitSpanned);

                _numberOfThisDisk = eocd.NumberOfThisDisk;
                _centralDirectoryStart = eocd.OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber;

                if (eocd.NumberOfEntriesInTheCentralDirectory != eocd.NumberOfEntriesInTheCentralDirectoryOnThisDisk)
                    throw new InvalidDataException(SR.SplitSpanned);

                _expectedNumberOfEntries = eocd.NumberOfEntriesInTheCentralDirectory;

                _archiveComment = eocd.ArchiveComment;

                TryReadZip64EndOfCentralDirectory(eocd, eocdStart);

                if (_centralDirectoryStart > _archiveStream.Length)
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
                _archiveStream.Seek(eocdStart - Zip64EndOfCentralDirectoryLocator.SizeOfBlockWithoutSignature, SeekOrigin.Begin);

                // Exactly the previous 4 bytes should contain the Zip64-EOCDL signature
                // if we don't find it, assume it doesn't exist and use data from normal EOCD
                if (ZipHelper.SeekBackwardsToSignature(_archiveStream,
                        Zip64EndOfCentralDirectoryLocator.SignatureConstant,
                        Zip64EndOfCentralDirectoryLocator.SignatureSize))
                {
                    Debug.Assert(_archiveReader != null);

                    // use locator to get to Zip64-EOCD
                    Zip64EndOfCentralDirectoryLocator locator;
                    bool zip64eocdLocatorProper = Zip64EndOfCentralDirectoryLocator.TryReadBlock(_archiveReader, out locator);
                    Debug.Assert(zip64eocdLocatorProper); // we just found this using the signature finder, so it should be okay

                    if (locator.OffsetOfZip64EOCD > long.MaxValue)
                        throw new InvalidDataException(SR.FieldTooBigOffsetToZip64EOCD);

                    long zip64EOCDOffset = (long)locator.OffsetOfZip64EOCD;

                    _archiveStream.Seek(zip64EOCDOffset, SeekOrigin.Begin);

                    // Read Zip64 End of Central Directory Record

                    Zip64EndOfCentralDirectoryRecord record;
                    if (!Zip64EndOfCentralDirectoryRecord.TryReadBlock(_archiveReader, out record))
                        throw new InvalidDataException(SR.Zip64EOCDNotWhereExpected);

                    _numberOfThisDisk = record.NumberOfThisDisk;

                    if (record.NumberOfEntriesTotal > long.MaxValue)
                        throw new InvalidDataException(SR.FieldTooBigNumEntries);

                    if (record.OffsetOfCentralDirectory > long.MaxValue)
                        throw new InvalidDataException(SR.FieldTooBigOffsetToCD);

                    if (record.NumberOfEntriesTotal != record.NumberOfEntriesOnThisDisk)
                        throw new InvalidDataException(SR.SplitSpanned);

                    _expectedNumberOfEntries = (long)record.NumberOfEntriesTotal;
                    _centralDirectoryStart = (long)record.OffsetOfCentralDirectory;
                }
            }
        }

        private void WriteFile()
        {
            // if we are in create mode, we always set readEntries to true in Init
            // if we are in update mode, we call EnsureCentralDirectoryRead, which sets readEntries to true
            Debug.Assert(_readEntries);

            if (_mode == ZipArchiveMode.Update)
            {
                List<ZipArchiveEntry> markedForDelete = new List<ZipArchiveEntry>();
                foreach (ZipArchiveEntry entry in _entries)
                {
                    if (!entry.LoadLocalHeaderExtraFieldAndCompressedBytesIfNeeded())
                        markedForDelete.Add(entry);
                }
                foreach (ZipArchiveEntry entry in markedForDelete)
                    entry.Delete();

                _archiveStream.Seek(0, SeekOrigin.Begin);
                _archiveStream.SetLength(0);
            }

            foreach (ZipArchiveEntry entry in _entries)
            {
                entry.WriteAndFinishLocalEntry();
            }

            long startOfCentralDirectory = _archiveStream.Position;

            foreach (ZipArchiveEntry entry in _entries)
            {
                entry.WriteCentralDirectoryFileHeader();
            }

            long sizeOfCentralDirectory = _archiveStream.Position - startOfCentralDirectory;

            WriteArchiveEpilogue(startOfCentralDirectory, sizeOfCentralDirectory);
        }

        // writes eocd, and if needed, zip 64 eocd, zip64 eocd locator
        // should only throw an exception in extremely exceptional cases because it is called from dispose
        private void WriteArchiveEpilogue(long startOfCentralDirectory, long sizeOfCentralDirectory)
        {
            // determine if we need Zip 64
            if (startOfCentralDirectory >= uint.MaxValue
                || sizeOfCentralDirectory >= uint.MaxValue
                || _entries.Count >= ZipHelper.Mask16Bit
#if DEBUG_FORCE_ZIP64
                || _forceZip64
#endif
                )
            {
                // if we need zip 64, write zip 64 eocd and locator
                long zip64EOCDRecordStart = _archiveStream.Position;
                Zip64EndOfCentralDirectoryRecord.WriteBlock(_archiveStream, _entries.Count, startOfCentralDirectory, sizeOfCentralDirectory);
                Zip64EndOfCentralDirectoryLocator.WriteBlock(_archiveStream, zip64EOCDRecordStart);
            }

            // write normal eocd
            ZipEndOfCentralDirectoryBlock.WriteBlock(_archiveStream, _entries.Count, startOfCentralDirectory, sizeOfCentralDirectory, _archiveComment);
        }
    }
}
