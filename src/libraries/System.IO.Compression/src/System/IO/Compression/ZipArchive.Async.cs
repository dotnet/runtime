// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression;

public partial class ZipArchive : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Asynchronously initializes and returns a new instance of <see cref="ZipArchive"/> on the given stream in the specified mode, specifying whether to leave the stream open, with an optional encoding and an optional cancellation token.
    /// </summary>
    /// <param name="stream">The input or output stream.</param>
    /// <param name="mode">See the description of the ZipArchiveMode enum. Read requires the stream to support reading, Create requires the stream to support writing, and Update requires the stream to support reading, writing, and seeking.</param>
    /// <param name="leaveOpen">true to leave the stream open upon disposing the ZipArchive, otherwise false.</param>
    /// <param name="entryNameEncoding">The encoding to use when reading or writing entry names and comments in this ZipArchive.
    ///         ///     <para>NOTE: Specifying this parameter to values other than <c>null</c> is discouraged.
    ///         However, this may be necessary for interoperability with ZIP archive tools and libraries that do not correctly support
    ///         UTF-8 encoding for entry names.<br />
    ///         This value is used as follows:</para>
    ///     <para><strong>Reading (opening) ZIP archive files:</strong></para>
    ///     <para>If <c>entryNameEncoding</c> is not specified (<c>== null</c>):</para>
    ///     <list>
    ///         <item>For entries where the language encoding flag (EFS) in the general purpose bit flag of the local file header is <em>not</em> set,
    ///         use the current system default code page (<c>Encoding.Default</c>) in order to decode the entry name and comment.</item>
    ///         <item>For entries where the language encoding flag (EFS) in the general purpose bit flag of the local file header <em>is</em> set,
    ///         use UTF-8 (<c>Encoding.UTF8</c>) in order to decode the entry name and comment.</item>
    ///     </list>
    ///     <para>If <c>entryNameEncoding</c> is specified (<c>!= null</c>):</para>
    ///     <list>
    ///         <item>For entries where the language encoding flag (EFS) in the general purpose bit flag of the local file header is <em>not</em> set,
    ///         use the specified <c>entryNameEncoding</c> in order to decode the entry name and comment.</item>
    ///         <item>For entries where the language encoding flag (EFS) in the general purpose bit flag of the local file header <em>is</em> set,
    ///         use UTF-8 (<c>Encoding.UTF8</c>) in order to decode the entry name and comment.</item>
    ///     </list>
    ///     <para><strong>Writing (saving) ZIP archive files:</strong></para>
    ///     <para>If <c>entryNameEncoding</c> is not specified (<c>== null</c>):</para>
    ///     <list>
    ///         <item>For entry names and comments that contain characters outside the ASCII range,
    ///         the language encoding flag (EFS) will be set in the general purpose bit flag of the local file header,
    ///         and UTF-8 (<c>Encoding.UTF8</c>) will be used in order to encode the entry name and comment into bytes.</item>
    ///         <item>For entry names and comments that do not contain characters outside the ASCII range,
    ///         the language encoding flag (EFS) will not be set in the general purpose bit flag of the local file header,
    ///         and the current system default code page (<c>Encoding.Default</c>) will be used to encode the entry names and comments into bytes.</item>
    ///     </list>
    ///     <para>If <c>entryNameEncoding</c> is specified (<c>!= null</c>):</para>
    ///     <list>
    ///         <item>The specified <c>entryNameEncoding</c> will always be used to encode the entry names and comments into bytes.
    ///         The language encoding flag (EFS) in the general purpose bit flag of the local file header will be set if and only
    ///         if the specified <c>entryNameEncoding</c> is a UTF-8 encoding.</item>
    ///     </list>
    ///     <para>Note that Unicode encodings other than UTF-8 may not be currently used for the <c>entryNameEncoding</c>,
    ///     otherwise an <see cref="ArgumentException"/> is thrown.</para>
    /// </param>
    /// <param name="cancellationToken">The optional cancellation token to monitor.</param>
    /// <exception cref="ArgumentException">The stream is already closed. -or- mode is incompatible with the capabilities of the stream.</exception>
    /// <exception cref="ArgumentNullException">The stream is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">mode specified an invalid value.</exception>
    /// <exception cref="InvalidDataException">The contents of the stream could not be interpreted as a Zip file. -or- mode is Update and an entry is missing from the archive or is corrupt and cannot be read. -or- mode is Update and an entry is too large to fit into memory.</exception>
    /// <exception cref="ArgumentException">If a Unicode encoding other than UTF-8 is specified for the <code>entryNameEncoding</code>.</exception>
    /// <returns>A task that represents the asynchronous initialization. The task result is a <see cref="ZipArchive"/> instance opened on the provided stream.</returns>
    public static async Task<ZipArchive> CreateAsync(Stream stream, ZipArchiveMode mode, bool leaveOpen, Encoding? entryNameEncoding, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(stream);

        Stream? extraTempStream = null;

        try
        {
            Stream? backingStream = null;

            if (ValidateMode(mode, stream))
            {
                backingStream = stream;
                extraTempStream = stream = new MemoryStream();
                await backingStream.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
                stream.Seek(0, SeekOrigin.Begin);
            }

            ZipArchive zipArchive = new(mode, leaveOpen, entryNameEncoding, backingStream, DecideArchiveStream(mode, stream));

            switch (mode)
            {
                case ZipArchiveMode.Create:
                    zipArchive._readEntries = true;
                    break;
                case ZipArchiveMode.Read:
                    await zipArchive.ReadEndOfCentralDirectoryAsync(cancellationToken).ConfigureAwait(false);

                    // As there is no API for accessing .Entries asynchronously, we are expected to read the central
                    // directory up-front
                    await zipArchive.EnsureCentralDirectoryReadAsync(cancellationToken).ConfigureAwait(false);
                    break;
                case ZipArchiveMode.Update:
                default:
                    Debug.Assert(mode == ZipArchiveMode.Update);
                    if (zipArchive._archiveStream.Length == 0)
                    {
                        zipArchive._readEntries = true;
                    }
                    else
                    {
                        await zipArchive.ReadEndOfCentralDirectoryAsync(cancellationToken).ConfigureAwait(false);
                        await zipArchive.EnsureCentralDirectoryReadAsync(cancellationToken).ConfigureAwait(false);

                        foreach (ZipArchiveEntry entry in zipArchive._entries)
                        {
                            await entry.ThrowIfNotOpenableAsync(needToUncompress: false, needToLoadIntoMemory: true, cancellationToken).ConfigureAwait(false);
                        }
                    }
                    break;
            }

            return zipArchive;
        }
        catch (Exception)
        {
            if (extraTempStream != null)
            {
                await extraTempStream.DisposeAsync().ConfigureAwait(false);
            }

            throw;
        }
    }

    /// <summary>
    /// Asynchronously releases the resources used by the <see cref="ZipArchive"/>.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync() => await DisposeAsyncCore().ConfigureAwait(false);

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (!_isDisposed)
        {
            try
            {
                switch (_mode)
                {
                    case ZipArchiveMode.Read:
                        break;
                    case ZipArchiveMode.Create:
                        await WriteFileAsync().ConfigureAwait(false);
                        break;
                    case ZipArchiveMode.Update:
                    default:
                        Debug.Assert(_mode == ZipArchiveMode.Update);
                        // Only write if the archive has been modified
                        if (IsModified)
                        {
                            await WriteFileAsync().ConfigureAwait(false);
                        }
                        else
                        {
                            // Even if we didn't write, unload any entry buffers that may have been loaded
                            foreach (ZipArchiveEntry entry in _entries)
                            {
                                await entry.UnloadStreamsAsync().ConfigureAwait(false);
                            }
                        }
                        break;
                }
            }
            finally
            {
                await CloseStreamsAsync().ConfigureAwait(false);
                _isDisposed = true;
            }
        }
    }
}
