// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// Zip Spec here: http://www.pkware.com/documents/casestudies/APPNOTE.TXT

using System.Collections.ObjectModel;
using System.Text;

namespace System.IO.Compression
{
    public class ZipArchive : IDisposable
    {
        internal ZipArchiveStrategy Strategy { get; }

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
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            Strategy = ChooseStrategy(stream, mode, leaveOpen, entryNameEncoding);
        }

        /// <summary>
        /// The collection of entries that are currently in the ZipArchive. This may not accurately represent the actual entries that are present in the underlying file or stream.
        /// </summary>
        /// <exception cref="NotSupportedException">The ZipArchive does not support reading.</exception>
        /// <exception cref="ObjectDisposedException">The ZipArchive has already been closed.</exception>
        /// <exception cref="InvalidDataException">The Zip archive is corrupt and the entries cannot be retrieved.</exception>
        public ReadOnlyCollection<ZipArchiveEntry> Entries
            => Strategy.EntriesCollection;

        /// <summary>
        /// The ZipArchiveMode that the ZipArchive was initialized with.
        /// </summary>
        public ZipArchiveMode Mode
            => Strategy.Mode;

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
            => Strategy.CreateEntry(entryName, null);

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
            => Strategy.CreateEntry(entryName, compressionLevel);

        /// <summary>
        /// Releases the unmanaged resources used by ZipArchive and optionally finishes writing the archive and releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to finish writing the archive and release unmanaged and managed resources, false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
            => Strategy.Dispose(disposing);

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
            => Strategy.GetEntry(entryName);

        private ZipArchiveStrategy ChooseStrategy(Stream stream, ZipArchiveMode mode, bool leaveOpen, Encoding? entryNameEncoding)
        {
            return mode switch
            {
                ZipArchiveMode.Create => new ZipArchiveCreateStrategy(this, stream, leaveOpen, entryNameEncoding),
                ZipArchiveMode.Read => new ZipArchiveReadStrategy(this, stream, leaveOpen, entryNameEncoding),
                ZipArchiveMode.Update => new ZipArchiveUpdateStrategy(this, stream, leaveOpen, entryNameEncoding),
                _ => throw new ArgumentOutOfRangeException(nameof(mode)),
            };
        }
    }
}
