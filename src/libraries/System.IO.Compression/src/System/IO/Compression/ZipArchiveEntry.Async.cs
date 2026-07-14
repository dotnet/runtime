// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression;

// The disposable fields that this class owns get disposed when the ZipArchive it belongs to gets disposed
public partial class ZipArchiveEntry
{
    /// <summary>
    /// Asynchronously opens the entry. If the archive that the entry belongs to was opened in Read mode, the returned stream will be readable, and it may or may not be seekable. If Create mode, the returned stream will be writable and not seekable. If Update mode, the returned stream will be readable, writable, seekable, and support SetLength.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A Stream that represents the contents of the entry.</returns>
    /// <exception cref="IOException">The entry is already currently open for writing. -or- The entry has been deleted from the archive. -or- The archive that this entry belongs to was opened in ZipArchiveMode.Create, and this entry has already been written to once.</exception>
    /// <exception cref="InvalidDataException">The entry is missing from the archive or is corrupt and cannot be read. -or- The entry has been compressed using a compression method that is not supported.</exception>
    /// <exception cref="ObjectDisposedException">The ZipArchive that this entry belongs to has been disposed.</exception>
    public Task<Stream> OpenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfInvalidArchive();
        return OpenAsyncCore(InferAccessFromMode(), default, cancellationToken);
    }

    /// <summary>
    /// Asynchronously opens the entry with the specified access mode. This allows for more granular control over the returned stream's capabilities.
    /// </summary>
    /// <param name="access">The file access mode for the returned stream.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task{Stream}"/> that represents the asynchronous open operation.</returns>
    /// <remarks>
    /// <para>The allowed <paramref name="access"/> values depend on the <see cref="ZipArchiveMode"/>:</para>
    /// <list type="bullet">
    /// <item><description><see cref="ZipArchiveMode.Read"/>: Only <see cref="FileAccess.Read"/> is allowed.</description></item>
    /// <item><description><see cref="ZipArchiveMode.Create"/>: <see cref="FileAccess.Write"/> and <see cref="FileAccess.ReadWrite"/> are allowed (both write-only).</description></item>
    /// <item><description><see cref="ZipArchiveMode.Update"/>: All values are allowed. <see cref="FileAccess.Read"/> reads directly from the archive. <see cref="FileAccess.Write"/> discards existing content and provides an empty writable stream. <see cref="FileAccess.ReadWrite"/> loads existing content into memory (equivalent to <see cref="OpenAsync(CancellationToken)"/>).</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="access"/> is not a valid <see cref="FileAccess"/> value.</exception>
    /// <exception cref="InvalidOperationException">The requested access is not compatible with the archive's open mode.</exception>
    /// <exception cref="IOException">The entry is already currently open for writing. -or- The entry has been deleted from the archive. -or- The archive that this entry belongs to was opened in ZipArchiveMode.Create, and this entry has already been written to once.</exception>
    /// <exception cref="InvalidDataException">The entry is missing from the archive or is corrupt and cannot be read. -or- The entry has been compressed using a compression method that is not supported.</exception>
    /// <exception cref="ObjectDisposedException">The ZipArchive that this entry belongs to has been disposed.</exception>
    public Task<Stream> OpenAsync(FileAccess access, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfInvalidArchive();
        ValidateAccessForMode(access);
        return OpenAsyncCore(access, default, cancellationToken);
    }

    /// <summary>
    /// Asynchronously opens the entry with the specified access mode and password for decrypting encrypted entries.
    /// </summary>
    /// <param name="access">The file access mode for the returned stream.</param>
    /// <param name="password">The password used to decrypt the encrypted entry.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task{Stream}"/> that represents the asynchronous open operation.</returns>
    /// <remarks>
    /// <para>The allowed <paramref name="access"/> values depend on the <see cref="ZipArchiveMode"/>:</para>
    /// <list type="bullet">
    /// <item><description><see cref="ZipArchiveMode.Read"/>: Only <see cref="FileAccess.Read"/> is allowed.</description></item>
    /// <item><description><see cref="ZipArchiveMode.Create"/>: <see cref="FileAccess.Write"/> and <see cref="FileAccess.ReadWrite"/> are allowed; <see cref="FileAccess.Read"/> is not allowed. The <paramref name="password"/> is only used when decrypting existing encrypted entries and is not used when opening a newly created entry for writing.</description></item>
    /// <item><description><see cref="ZipArchiveMode.Update"/>: All values are allowed for encrypted entries.</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="access"/> is not a valid <see cref="FileAccess"/> value.</exception>
    /// <exception cref="InvalidOperationException">The requested access is not compatible with the archive's open mode.</exception>
    /// <exception cref="IOException">The entry is already currently open for writing. -or- The entry has been deleted from the archive.</exception>
    /// <exception cref="ObjectDisposedException">The ZipArchive that this entry belongs to has been disposed.</exception>
    public Task<Stream> OpenAsync(FileAccess access, ReadOnlySpan<char> password, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfInvalidArchive();
        ValidateAccessForMode(access);

        if (IsEncrypted && password.IsEmpty)
        {
            throw new ArgumentException(SR.PasswordRequired, nameof(password));
        }

        return OpenAsyncCore(access, password, cancellationToken);
    }

    /// <summary>
    /// Asynchronously opens the entry and uses the specified password to decrypt it if it is encrypted.
    /// If the archive that the entry belongs to was opened in Read mode, the returned stream will be readable, and it may or may not be seekable. If Create mode, the returned stream will be writable and not seekable. If Update mode, the returned stream will be readable, writable, seekable, and support <see cref="Stream.SetLength(long)" />.
    /// </summary>
    /// <param name="password">The password used to decrypt the encrypted entry.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task whose result is a stream that represents the contents of the entry.</returns>
    /// <remarks>
    /// <para>If the entry is not encrypted, <paramref name="password" /> is ignored.</para>
    /// </remarks>
    /// <exception cref="ArgumentException">The entry is encrypted and <paramref name="password" /> is empty.</exception>
    /// <exception cref="IOException">The entry is already currently open for writing. -or- The entry has been deleted from the archive. -or- The archive that this entry belongs to was opened in <see cref="ZipArchiveMode.Create" />, and this entry has already been written to once.</exception>
    /// <exception cref="InvalidDataException">The entry is missing from the archive or is corrupt and cannot be read. -or- The entry has been compressed using a compression method that is not supported.</exception>
    /// <exception cref="ObjectDisposedException">The <see cref="ZipArchive" /> that this entry belongs to has been disposed.</exception>
    public Task<Stream> OpenAsync(ReadOnlySpan<char> password, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfInvalidArchive();

        if (IsEncrypted && password.IsEmpty)
        {
            throw new ArgumentException(SR.PasswordRequired, nameof(password));
        }

        return OpenAsyncCore(InferAccessFromMode(), password, cancellationToken);
    }

    private Task<Stream> OpenAsyncCore(FileAccess access, ReadOnlySpan<char> password, CancellationToken cancellationToken)
    {
        bool usePassword = IsEncrypted && !password.IsEmpty;

        switch (_archive.Mode)
        {
            case ZipArchiveMode.Read:
                return OpenInReadModeAsync(checkOpenable: true, password, cancellationToken);
            case ZipArchiveMode.Create:
                return Task.FromResult<Stream>(OpenInWriteMode());
            case ZipArchiveMode.Update:
            default:
                Debug.Assert(_archive.Mode == ZipArchiveMode.Update);
                // Encrypted entries always require a password for re-encryption,
                // even when discarding existing content (write-only access).
                if (IsEncrypted && password.IsEmpty && access != FileAccess.Read)
                {
                    throw new ArgumentException(SR.PasswordRequired, nameof(password));
                }
                return access switch
                {
                    FileAccess.Read => OpenInReadModeAsync(checkOpenable: true, password, cancellationToken),
                    FileAccess.Write => usePassword
                        ? OpenInUpdateModeWithPasswordAsync(loadExistingContent: false, password, cancellationToken)
                        : CastToStreamAsync(OpenInUpdateModeAsync(loadExistingContent: false, cancellationToken)),
                    _ => usePassword
                        ? OpenInUpdateModeWithPasswordAsync(loadExistingContent: true, password, cancellationToken)
                        : CastToStreamAsync(OpenInUpdateModeAsync(loadExistingContent: true, cancellationToken)),
                };
        }
    }

    internal async Task<long> GetOffsetOfCompressedDataAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_storedOffsetOfCompressedData == null)
        {
            // Seek to local header
            _archive.ArchiveStream.Seek(_offsetOfLocalHeader, SeekOrigin.Begin);

            // Skip the local file header to get to the compressed data
            // TrySkipBlockAsync handles both AES and non-AES cases correctly
            if (!await ZipLocalFileHeader.TrySkipBlockAsync(_archive.ArchiveStream, cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidDataException(SR.LocalFileHeaderCorrupt);
            }

            _storedOffsetOfCompressedData = _archive.ArchiveStream.Position;
        }
        return _storedOffsetOfCompressedData.Value;
    }

    /// <summary>
    /// Asynchronously reads and caches the AES encryption salt from the local file data area.
    /// Called during central directory parsing for AES-encrypted entries so that
    /// the salt is available for key derivation without additional I/O at open time.
    /// </summary>
    internal async Task ReadEncryptionSaltIfNeededAsync(CancellationToken cancellationToken)
    {
        if (!IsAesEncrypted || !_originallyInArchive || OperatingSystem.IsBrowser())
        {
            return;
        }

        long savedPosition = _archive.ArchiveStream.Position;
        try
        {
            long offset = await GetOffsetOfCompressedDataAsync(cancellationToken).ConfigureAwait(false);
            _archive.ArchiveStream.Seek(offset, SeekOrigin.Begin);

            int keySizeBits = GetAesKeySizeBits(Encryption);
            int saltSize = WinZipAesStream.GetSaltSize(keySizeBits);
            _aesSalt = new byte[saltSize];
            await _archive.ArchiveStream.ReadExactlyAsync(_aesSalt, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is InvalidDataException or EndOfStreamException)
        {
            // These are the only exceptions GetOffsetOfCompressedDataAsync() and ReadExactlyAsync()
            // can throw for corrupt or truncated data. Swallow them here and defer the error
            // to when the entry is actually opened.
            _aesSalt = null;
        }
        finally
        {
            _archive.ArchiveStream.Seek(savedPosition, SeekOrigin.Begin);
        }
    }

    private Task<Stream> OpenInReadModeAsync(bool checkOpenable, ReadOnlySpan<char> password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Derive key material from the password span before entering the async core.
        WinZipAesKeyMaterial? aesKeys = null;
        ZipCryptoKeys? zipCryptoKeys = null;
        byte zipCryptoCheckByte = 0;

        if (IsEncrypted)
        {
            if (Encryption == ZipEncryptionMethod.Unknown)
            {
                throw new NotSupportedException(SR.UnsupportedEncryptionMethod);
            }

            if (password.IsEmpty)
            {
                throw new InvalidDataException(SR.PasswordRequired);
            }

            if (IsAesEncrypted)
            {
                if (OperatingSystem.IsBrowser())
                {
                    throw new PlatformNotSupportedException(SR.WinZipEncryptionNotSupportedOnBrowser);
                }

                if (_aesSalt is null)
                {
                    throw new InvalidDataException(SR.LocalFileHeaderCorrupt);
                }

                int keySizeBits = GetAesKeySizeBits(Encryption);
                aesKeys = WinZipAesStream.CreateKey(password, _aesSalt, keySizeBits);
            }
            else if (IsZipCryptoEncrypted)
            {
                zipCryptoCheckByte = CalculateZipCryptoCheckByte();
                zipCryptoKeys = ZipCryptoStream.CreateKey(password);
            }
            else
            {
                throw new NotSupportedException(SR.UnsupportedEncryptionMethod);
            }
        }

        return OpenInReadModeAsyncCore(checkOpenable, aesKeys, zipCryptoKeys, zipCryptoCheckByte, cancellationToken);

        async Task<Stream> OpenInReadModeAsyncCore(bool checkOpenable, WinZipAesKeyMaterial? aesKeys, ZipCryptoKeys? zipCryptoKeys, byte zipCryptoCheckByte, CancellationToken cancellationToken)
        {
            if (checkOpenable)
            {
                await ThrowIfNotOpenableAsync(needToUncompress: true, needToLoadIntoMemory: false, cancellationToken).ConfigureAwait(false);
            }

            long offset = await GetOffsetOfCompressedDataAsync(cancellationToken).ConfigureAwait(false);
            Stream compressedStream = new SubReadStream(_archive.ArchiveStream, offset, _compressedSize);

            Stream streamToDecompress;
            if (aesKeys is not null)
            {
                streamToDecompress = await WinZipAesStream.CreateAsync(
                    baseStream: compressedStream,
                    keyMaterial: aesKeys.Value,
                    totalStreamSize: _compressedSize,
                    encrypting: false,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else if (zipCryptoKeys is not null)
            {
                streamToDecompress = await ZipCryptoStream.CreateAsync(
                    compressedStream, zipCryptoKeys.Value, zipCryptoCheckByte,
                    encrypting: false, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                streamToDecompress = compressedStream;
            }

            return BuildDecompressionPipeline(streamToDecompress);
        }
    }

    private async Task<WrappedStream> OpenInUpdateModeAsync(bool loadExistingContent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_currentlyOpenForWrite)
        {
            throw new IOException(SR.UpdateModeOneStream);
        }

        if (Encryption == ZipEncryptionMethod.Unknown)
        {
            throw new NotSupportedException(SR.UnsupportedEncryptionMethod);
        }

        if (loadExistingContent)
        {
            await ThrowIfNotOpenableAsync(needToUncompress: true, needToLoadIntoMemory: true, cancellationToken).ConfigureAwait(false);
        }

        _currentlyOpenForWrite = true;

        if (loadExistingContent)
        {
            _storedUncompressedData = await GetUncompressedDataAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _storedUncompressedData?.Dispose();
            _storedUncompressedData = new MemoryStream();
            MarkAsModified();
        }

        _storedUncompressedData.Seek(0, SeekOrigin.Begin);

        return new WrappedStream(_storedUncompressedData, this,
            onClosed: thisRef => thisRef!._currentlyOpenForWrite = false,
            notifyEntryOnWrite: true);
    }

    private async Task<MemoryStream> GetUncompressedDataAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_storedUncompressedData is null)
        {
            // MemoryStream is backed by a single byte[] and cannot grow beyond Array.MaxLength.
            // Validate up front before attempting the (int) cast.
            if ((ulong)_uncompressedSize > (ulong)Array.MaxLength)
            {
                _currentlyOpenForWrite = false;
                throw new InvalidDataException(SR.EntryUncompressedSizeTooLargeForUpdateMode);
            }


            _storedUncompressedData = new MemoryStream((int)_uncompressedSize);

            if (_originallyInArchive)
            {
                Stream decompressor = await OpenInReadModeAsync(checkOpenable: false, default, cancellationToken).ConfigureAwait(false);

                await using (decompressor)
                {
                    try
                    {
                        await decompressor.CopyToAsync(_storedUncompressedData, cancellationToken).ConfigureAwait(false);
                    }
                    catch (InvalidDataException)
                    {
                        await _storedUncompressedData.DisposeAsync().ConfigureAwait(false);
                        _storedUncompressedData = null;
                        _currentlyOpenForWrite = false;
                        _everOpenedForWrite = false;
                        _derivedZipCryptoKeyMaterial = null;
                        _derivedAesKeyMaterial = null;
                        throw;
                    }
                }
            }
        }

        return _storedUncompressedData;
    }

    // does almost everything you need to do to forget about this entry
    // writes the local header/data, gets rid of all the data,
    // closes all of the streams except for the very outermost one that
    // the user holds on to and is responsible for closing
    //
    // after calling this, and only after calling this can we be guaranteed
    // that we are reading to write the central directory
    //
    // should only throw an exception in extremely exceptional cases because it is called from dispose
    internal async Task WriteAndFinishLocalEntryAsync(bool forceWrite, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await CloseStreamsAsync().ConfigureAwait(false);
        await WriteLocalFileHeaderAndDataIfNeededAsync(forceWrite, cancellationToken).ConfigureAwait(false);
        await UnloadStreamsAsync().ConfigureAwait(false);
    }

    // should only throw an exception in extremely exceptional cases because it is called from dispose
    internal async Task WriteCentralDirectoryFileHeaderAsync(bool forceWrite, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (WriteCentralDirectoryFileHeaderInitialize(forceWrite, out Zip64ExtraField? zip64ExtraField, out uint compressedSizeTruncated, out uint uncompressedSizeTruncated, out ushort extraFieldLength, out uint offsetOfLocalHeaderTruncated))
        {
            byte[] cdStaticHeader = new byte[ZipCentralDirectoryFileHeader.BlockConstantSectionSize];
            WriteCentralDirectoryFileHeaderPrepare(cdStaticHeader, compressedSizeTruncated, uncompressedSizeTruncated, extraFieldLength, offsetOfLocalHeaderTruncated);

            await _archive.ArchiveStream.WriteAsync(cdStaticHeader, cancellationToken).ConfigureAwait(false);
            await _archive.ArchiveStream.WriteAsync(_storedEntryNameBytes, cancellationToken).ConfigureAwait(false);

            // Write zip64ExtraField first if we decided we need it
            if (zip64ExtraField != null)
            {
                await zip64ExtraField.WriteBlockAsync(_archive.ArchiveStream, cancellationToken).ConfigureAwait(false);
            }

            // Write WinZip AES extra field AFTER Zip64 (matching sync version order)
            // Must match the exact check used in the sync version WriteCentralDirectoryFileHeader
            if (UseAesEncryption)
            {
                await CreateAesExtraField().WriteBlockAsync(_archive.ArchiveStream, cancellationToken).ConfigureAwait(false);

                // write extra fields excluding existing AES extra field (and any malformed trailing data).
                await ZipGenericExtraField.WriteAllBlocksExcludingTagAsync(_cdUnknownExtraFields, _cdTrailingExtraFieldData ?? Array.Empty<byte>(), _archive.ArchiveStream, WinZipAesExtraField.HeaderId, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // write extra fields (and any malformed trailing data).
                await ZipGenericExtraField.WriteAllBlocksAsync(_cdUnknownExtraFields, _cdTrailingExtraFieldData ?? Array.Empty<byte>(), _archive.ArchiveStream, cancellationToken).ConfigureAwait(false);
            }

            if (_fileComment.Length > 0)
            {
                await _archive.ArchiveStream.WriteAsync(_fileComment, cancellationToken).ConfigureAwait(false);
            }
        }
    }
    internal async Task LoadLocalHeaderExtraFieldIfNeededAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // we should have made this exact call in _archive.Init through ThrowIfOpenable
        Debug.Assert(await GetIsOpenableAsync(false, true, cancellationToken).ConfigureAwait(false));

        // load local header's extra fields. it will be null if we couldn't read for some reason
        if (_originallyInArchive)
        {
            _archive.ArchiveStream.Seek(_offsetOfLocalHeader, SeekOrigin.Begin);
            (_lhUnknownExtraFields, _lhTrailingExtraFieldData) = await ZipLocalFileHeader.GetExtraFieldsAsync(_archive.ArchiveStream, cancellationToken).ConfigureAwait(false);
        }
    }

    internal async Task LoadCompressedBytesIfNeededAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // we should have made this exact call in _archive.Init through ThrowIfOpenable
        Debug.Assert(await GetIsOpenableAsync(false, true, cancellationToken).ConfigureAwait(false));

        if (!_everOpenedForWrite && _originallyInArchive)
        {
            _compressedBytes = LoadCompressedBytesIfNeededInitialize(out int maxSingleBufferSize);

            _archive.ArchiveStream.Seek(await GetOffsetOfCompressedDataAsync(cancellationToken).ConfigureAwait(false), SeekOrigin.Begin);

            for (int i = 0; i < _compressedBytes.Length - 1; i++)
            {
                await _archive.ArchiveStream.ReadAtLeastAsync(_compressedBytes[i], maxSingleBufferSize, throwOnEndOfStream: true, cancellationToken).ConfigureAwait(false);
            }
            await _archive.ArchiveStream.ReadAtLeastAsync(_compressedBytes[_compressedBytes.Length - 1], (int)(_compressedSize % maxSingleBufferSize), throwOnEndOfStream: true, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<bool> GetIsOpenableAsync(bool needToUncompress, bool needToLoadIntoMemory, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        (bool result, _) = await IsOpenableAsync(needToUncompress, needToLoadIntoMemory, cancellationToken).ConfigureAwait(false);
        return result;
    }

    internal async Task ThrowIfNotOpenableAsync(bool needToUncompress, bool needToLoadIntoMemory, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        (bool openable, string? message) = await IsOpenableAsync(needToUncompress, needToLoadIntoMemory, cancellationToken).ConfigureAwait(false);
        if (!openable)
        {
            throw new InvalidDataException(message);
        }
    }

    /// <summary>
    /// Accepts a <see cref="ReadOnlySpan{T}"/> password, derives decryption and re-encryption keys (CPU-only),
    /// and delegates all I/O to fully async helper methods.
    /// </summary>
    private Task<Stream> OpenInUpdateModeWithPasswordAsync(bool loadExistingContent, ReadOnlySpan<char> password, CancellationToken cancellationToken)
    {
        if (_currentlyOpenForWrite)
        {
            throw new IOException(SR.UpdateModeOneStream);
        }

        if (Encryption == ZipEncryptionMethod.Unknown)
        {
            throw new NotSupportedException(SR.UnsupportedEncryptionMethod);
        }

        // Encrypted entries always require a password for re-encryption,
        // even when discarding existing content (write-only access).
        if (IsEncrypted && password.IsEmpty)
        {
            throw new ArgumentException(SR.PasswordRequired, nameof(password));
        }

        // Derive re-encryption key material while the password span is still valid.
        if (IsEncrypted)
        {
            SetupEncryptionKeyMaterial(password);
        }

        if (loadExistingContent && IsEncrypted)
        {
            if (IsAesEncrypted)
            {
                if (OperatingSystem.IsBrowser())
                {
                    throw new PlatformNotSupportedException(SR.WinZipEncryptionNotSupportedOnBrowser);
                }

                if (_aesSalt is null)
                {
                    throw new InvalidDataException(SR.LocalFileHeaderCorrupt);
                }

                int keySizeBits = GetAesKeySizeBits(Encryption);
                WinZipAesKeyMaterial aesKeys = WinZipAesStream.CreateKey(password, _aesSalt, keySizeBits);
                return DecryptAndStoreForUpdateWithAesAsync(aesKeys, cancellationToken);
            }

            if (IsZipCryptoEncrypted)
            {
                byte checkByte = CalculateZipCryptoCheckByte();
                ZipCryptoKeys keys = ZipCryptoStream.CreateKey(password);
                return DecryptAndStoreForUpdateWithZipCryptoAsync(keys, checkByte, cancellationToken);
            }

            throw new NotSupportedException(SR.UnsupportedEncryptionMethod);
        }

        return CastToStreamAsync(OpenInUpdateModeAsync(loadExistingContent, cancellationToken));
    }

    private static async Task<Stream> CastToStreamAsync(Task<WrappedStream> task) => await task.ConfigureAwait(false);

    private async Task<Stream> DecryptAndStoreForUpdateWithZipCryptoAsync(ZipCryptoKeys keys, byte checkByte, CancellationToken cancellationToken)
    {
        await ThrowIfNotOpenableAsync(needToUncompress: true, needToLoadIntoMemory: true, cancellationToken).ConfigureAwait(false);

        long offset = await GetOffsetOfCompressedDataAsync(cancellationToken).ConfigureAwait(false);
        Stream compressedStream = new SubReadStream(_archive.ArchiveStream, offset, _compressedSize);

        Stream decrypted = await ZipCryptoStream.CreateAsync(compressedStream, keys, checkByte, encrypting: false, cancellationToken).ConfigureAwait(false);

        return await StoreDecryptedDataForUpdateAsync(decrypted, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Stream> DecryptAndStoreForUpdateWithAesAsync(WinZipAesKeyMaterial aesKeys, CancellationToken cancellationToken)
    {
        if (OperatingSystem.IsBrowser())
        {
            throw new PlatformNotSupportedException(SR.WinZipEncryptionNotSupportedOnBrowser);
        }

        await ThrowIfNotOpenableAsync(needToUncompress: true, needToLoadIntoMemory: true, cancellationToken).ConfigureAwait(false);

        long offset = await GetOffsetOfCompressedDataAsync(cancellationToken).ConfigureAwait(false);
        Stream compressedStream = new SubReadStream(_archive.ArchiveStream, offset, _compressedSize);

        Stream decrypted = await WinZipAesStream.CreateAsync(
            baseStream: compressedStream,
            keyMaterial: aesKeys,
            totalStreamSize: _compressedSize,
            encrypting: false,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return await StoreDecryptedDataForUpdateAsync(decrypted, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Decompresses a decrypted stream and stores the result in memory for update mode.
    /// </summary>
    private async Task<Stream> StoreDecryptedDataForUpdateAsync(Stream decryptedStream, CancellationToken cancellationToken)
    {
        _currentlyOpenForWrite = true;

        if (_uncompressedSize > Array.MaxLength)
        {
            throw new InvalidDataException(SR.EntryTooLarge);
        }

        _storedUncompressedData = new MemoryStream((int)_uncompressedSize);

        Stream decompressed = BuildDecompressionPipeline(decryptedStream);

        await using (decompressed)
        {
            try
            {
                await decompressed.CopyToAsync(_storedUncompressedData, cancellationToken).ConfigureAwait(false);
            }
            catch (InvalidDataException)
            {
                await _storedUncompressedData.DisposeAsync().ConfigureAwait(false);
                _storedUncompressedData = null;
                _currentlyOpenForWrite = false;
                _everOpenedForWrite = false;
                _derivedZipCryptoKeyMaterial = null;
                _derivedAesKeyMaterial = null;
                throw;
            }
        }

        _storedUncompressedData.Seek(0, SeekOrigin.Begin);

        return new WrappedStream(_storedUncompressedData, this,
            onClosed: thisRef => thisRef!._currentlyOpenForWrite = false,
            notifyEntryOnWrite: true);
    }

    private async Task<(bool, string?)> IsOpenableAsync(bool needToUncompress, bool needToLoadIntoMemory, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string? message = null;

        if (!_originallyInArchive)
        {
            return (true, message);
        }

        if (!IsOpenableInitialVerifications(needToUncompress, out message))
        {
            return (false, message);
        }

        if (!IsEncrypted && !await ZipLocalFileHeader.TrySkipBlockAsync(_archive.ArchiveStream, cancellationToken).ConfigureAwait(false))
        {
            message = SR.LocalFileHeaderCorrupt;
            return (false, message);
        }
        else if (IsEncrypted && (ushort)_headerCompressionMethod == WinZipAesMethod)
        {
            _archive.ArchiveStream.Seek(_offsetOfLocalHeader, SeekOrigin.Begin);
            // AES case - skip the local file header and validate it exists.
            // The AES metadata (encryption strength, actual compression method) was already
            // parsed from the central directory in the constructor.
            if (!await ZipLocalFileHeader.TrySkipBlockAsync(_archive.ArchiveStream, cancellationToken).ConfigureAwait(false))
            {
                message = SR.LocalFileHeaderCorrupt;
                return (false, message);
            }
        }

        // when this property gets called, some duplicated work
        long offsetOfCompressedData = await GetOffsetOfCompressedDataAsync(cancellationToken).ConfigureAwait(false);
        if (!IsOpenableFinalVerifications(needToLoadIntoMemory, offsetOfCompressedData, out message))
        {
            return (false, message);
        }

        return (true, message);
    }

    // return value is true if we allocated an extra field for 64 bit headers, un/compressed size
    private async Task<bool> WriteLocalFileHeaderAsync(bool isEmptyFile, bool forceWrite, bool preserveDataDescriptor, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (WriteLocalFileHeaderInitialize(isEmptyFile, forceWrite, preserveDataDescriptor, out Zip64ExtraField? zip64ExtraField, out uint compressedSizeTruncated, out uint uncompressedSizeTruncated, out ushort extraFieldLength, out uint crc32ToWrite))
        {
            byte[] lfStaticHeader = new byte[ZipLocalFileHeader.SizeOfLocalHeader];
            WriteLocalFileHeaderPrepare(lfStaticHeader, crc32ToWrite, compressedSizeTruncated, uncompressedSizeTruncated, extraFieldLength);

            // write header
            await _archive.ArchiveStream.WriteAsync(lfStaticHeader, cancellationToken).ConfigureAwait(false);
            await _archive.ArchiveStream.WriteAsync(_storedEntryNameBytes, cancellationToken).ConfigureAwait(false);

            // Only when handling zip64
            if (zip64ExtraField != null)
            {
                await zip64ExtraField.WriteBlockAsync(_archive.ArchiveStream, cancellationToken).ConfigureAwait(false);
            }

            // Write WinZip AES extra field if using AES encryption
            // Must match the exact check used in the sync version WriteLocalFileHeader
            if (UseAesEncryption)
            {
                await CreateAesExtraField().WriteBlockAsync(_archive.ArchiveStream, cancellationToken).ConfigureAwait(false);

                // Write other extra fields, excluding any existing AES extra field to avoid duplication
                await ZipGenericExtraField.WriteAllBlocksExcludingTagAsync(_lhUnknownExtraFields, _lhTrailingExtraFieldData ?? Array.Empty<byte>(), _archive.ArchiveStream, WinZipAesExtraField.HeaderId, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await ZipGenericExtraField.WriteAllBlocksAsync(_lhUnknownExtraFields, _lhTrailingExtraFieldData ?? Array.Empty<byte>(), _archive.ArchiveStream, cancellationToken).ConfigureAwait(false);
            }
        }

        return zip64ExtraField != null;
    }

    private async Task WriteLocalFileHeaderAndDataIfNeededAsync(bool forceWrite, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Check if the entry's stored data was actually modified (StoredData flag is set).
        // If _storedUncompressedData is loaded but StoredData is not set, it means the entry
        // was opened for update but no writes occurred - we should use the original compressed bytes.
        bool storedDataModified = (Changes & ZipArchive.ChangeState.StoredData) != 0;

        // If _storedUncompressedData is loaded but not modified, clear it so we use _compressedBytes
        if (_storedUncompressedData != null && !storedDataModified)
        {
            await _storedUncompressedData.DisposeAsync().ConfigureAwait(false);
            _storedUncompressedData = null;
        }

        // _storedUncompressedData gets frozen here, and is what gets written to the file
        if (_storedUncompressedData != null || _compressedBytes != null)
        {
            if (_storedUncompressedData != null)
            {
                _uncompressedSize = _storedUncompressedData.Length;

                // Check if we need to re-encrypt with ZipCrypto (only if we have cached key material)
                if (Encryption == ZipEncryptionMethod.ZipCrypto && _derivedZipCryptoKeyMaterial != null)
                {
                    // Write local file header first (with encryption flag set)
                    // Pass isEmptyFile: false because even empty encrypted files have the 12-byte header
                    await WriteLocalFileHeaderAsync(isEmptyFile: false, forceWrite: true, preserveDataDescriptor: false, cancellationToken).ConfigureAwait(false);

                    // Record position before encryption data
                    long startPosition = _archive.ArchiveStream.Position;

                    ushort verifierLow2Bytes = (ushort)ZipHelper.DateTimeToDosTime(_lastModified.DateTime);

                    ZipCryptoStream encryptionStream = ZipCryptoStream.Create(
                        baseStream: _archive.ArchiveStream,
                        keys: _derivedZipCryptoKeyMaterial.Value,
                        passwordVerifierLow2Bytes: verifierLow2Bytes,
                        encrypting: true,
                        crc32: null,
                        leaveOpen: true);
                    await using (encryptionStream.ConfigureAwait(false))
                    {
                        // Use GetDataCompressor which handles CRC calculation and compression
                        CheckSumAndSizeWriteStream crcStream = GetDataCompressor(encryptionStream, leaveBackingStreamOpen: true, onClose: null, streamForPosition: _archive.ArchiveStream);
                        await using (crcStream.ConfigureAwait(false))
                        {
                            _storedUncompressedData.Seek(0, SeekOrigin.Begin);
                            await _storedUncompressedData.CopyToAsync(crcStream, cancellationToken).ConfigureAwait(false);
                        }
                        // CRC, uncompressed size are now set by GetDataCompressor callback
                        // For empty files, ZipCryptoStream.Dispose() will write the 12-byte header
                    }

                    // Calculate compressed size AFTER ZipCryptoStream is disposed
                    // (includes 12-byte encryption header + compressed data)
                    _compressedSize = _archive.ArchiveStream.Position - startPosition;

                    // Write data descriptor since we used streaming mode
                    await WriteDataDescriptorAsync(cancellationToken).ConfigureAwait(false);

                    await _storedUncompressedData.DisposeAsync().ConfigureAwait(false);
                    _storedUncompressedData = null;
                }
                else if (UseAesEncryption && _derivedAesKeyMaterial != null)
                {

                    if (OperatingSystem.IsBrowser())
                    {
                        throw new PlatformNotSupportedException(SR.WinZipEncryptionNotSupportedOnBrowser);
                    }
                    // For AES, we need to:
                    // 1. Write header with CompressionMethod = Aes (99)
                    // 2. Compress data with actual compression (Deflate/Stored)
                    // 3. Keep CompressionMethod = Aes for central directory

                    // WriteLocalFileHeaderAsync will set CompressionMethod = Aes
                    bool usedZip64InLH = await WriteLocalFileHeaderAsync(isEmptyFile: false, forceWrite: true, preserveDataDescriptor: false, cancellationToken).ConfigureAwait(false);

                    // Record position before encryption data
                    long startPosition = _archive.ArchiveStream.Position;

                    int keySizeBits = GetAesKeySizeBits(Encryption);

                    // Determine the actual compression method to use
                    // The AES extra field stores the real compression method
                    bool useDeflate = _compressionLevel != CompressionLevel.NoCompression;

                    WinZipAesStream encryptionStream = WinZipAesStream.Create(
                        baseStream: _archive.ArchiveStream,
                        keyMaterial: _derivedAesKeyMaterial.Value,
                        totalStreamSize: -1,
                        encrypting: true,
                        leaveOpen: true);

                    await using (encryptionStream.ConfigureAwait(false))
                    {
                        // Only compress/write if there's data
                        if (_storedUncompressedData.Length > 0)
                        {
                            // Temporarily set CompressionMethod for GetDataCompressor
                            ZipCompressionMethod savedMethod = CompressionMethod;
                            CompressionMethod = useDeflate ? ZipCompressionMethod.Deflate : ZipCompressionMethod.Stored;

                            try
                            {
                                CheckSumAndSizeWriteStream crcStream = GetDataCompressor(encryptionStream, leaveBackingStreamOpen: true, onClose: null, streamForPosition: _archive.ArchiveStream);
                                await using (crcStream.ConfigureAwait(false))
                                {
                                    _storedUncompressedData.Seek(0, SeekOrigin.Begin);
                                    await _storedUncompressedData.CopyToAsync(crcStream, cancellationToken).ConfigureAwait(false);
                                }
                            }
                            finally
                            {
                                // Restore CompressionMethod - AesCompressionMethodValue is used directly when writing headers
                                CompressionMethod = savedMethod;
                            }
                        }
                        else
                        {
                            // Empty file: CRC is 0, uncompressed size is 0
                            _crc32 = 0;
                            _uncompressedSize = 0;
                        }
                        // WinZipAesStream.Dispose() writes salt + verifier + HMAC even for empty files
                    }

                    // Calculate compressed size AFTER WinZipAesStream is disposed
                    // (includes salt + password verifier + encrypted data + HMAC)
                    _compressedSize = _archive.ArchiveStream.Position - startPosition;

                    // Patch CRC and sizes back into the local header
                    await WriteCrcAndSizesInLocalHeaderAsync(usedZip64InLH, cancellationToken).ConfigureAwait(false);

                    await _storedUncompressedData.DisposeAsync().ConfigureAwait(false);
                    _storedUncompressedData = null;
                }
                else
                {
                    // Non-encrypted: use standard path
                    //The compressor fills in CRC and sizes
                    //The DirectToArchiveWriterStream writes headers and such
                    DirectToArchiveWriterStream entryWriter = new(GetDataCompressor(_archive.ArchiveStream, true, null, null), this);
                    await using (entryWriter.ConfigureAwait(false))
                    {
                        _storedUncompressedData.Seek(0, SeekOrigin.Begin);
                        await _storedUncompressedData.CopyToAsync(entryWriter, cancellationToken).ConfigureAwait(false);
                    }
                    await _storedUncompressedData.DisposeAsync().ConfigureAwait(false);
                    _storedUncompressedData = null;
                }
            }
            else // _compressedBytes path - copying unchanged entry data
            {
                if (_uncompressedSize == 0)
                {
                    // reset size to ensure proper central directory size header
                    _compressedSize = 0;
                }

                // For unchanged entries, we need to write the header correctly but avoid
                // WriteLocalFileHeaderAsync creating NEW encryption structures (which would have
                // wrong compression method from _compressionLevel).
                // The original AES extra field is preserved in _lhUnknownExtraFields.
                BitFlagValues savedFlags = _generalPurposeBitFlag;
                ZipEncryptionMethod savedEncryption = Encryption;
                ZipCompressionMethod savedCompressionMethod = CompressionMethod;

                try
                {
                    // For AES entries: set CompressionMethod to Aes so header writes method 99,
                    // but clear _encryptionMethod so WriteLocalFileHeaderAsync doesn't create a new
                    // AES extra field (the original one in _lhUnknownExtraFields will be used).
                    if (savedEncryption is ZipEncryptionMethod.Aes128 or ZipEncryptionMethod.Aes192 or ZipEncryptionMethod.Aes256)
                    {
                        CompressionMethod = (ZipCompressionMethod)WinZipAesMethod;
                        Encryption = ZipEncryptionMethod.None;
                    }

                    await WriteLocalFileHeaderAsync(isEmptyFile: _uncompressedSize == 0, forceWrite: true, preserveDataDescriptor: false, cancellationToken).ConfigureAwait(false);

                    // WriteLocalFileHeaderInitialize may have cleared the DataDescriptor flag
                    // (because Encryption was temporarily set to None and the stream is seekable).
                    // If the original entry had a data descriptor, patch the general-purpose bit
                    // flags in the already-written local header to match, so the header on disk
                    // is consistent with the data descriptor we conditionally write below.
                    if ((savedFlags & BitFlagValues.DataDescriptor) != 0 &&
                        (_generalPurposeBitFlag & BitFlagValues.DataDescriptor) == 0)
                    {
                        long currentPos = _archive.ArchiveStream.Position;
                        _archive.ArchiveStream.Seek(
                            _offsetOfLocalHeader + ZipLocalFileHeader.FieldLocations.GeneralPurposeBitFlags,
                            SeekOrigin.Begin);
                        byte[] flagBytes = new byte[2];
                        BinaryPrimitives.WriteUInt16LittleEndian(flagBytes, (ushort)savedFlags);
                        await _archive.ArchiveStream.WriteAsync(flagBytes, cancellationToken).ConfigureAwait(false);
                        _archive.ArchiveStream.Seek(currentPos, SeekOrigin.Begin);
                    }
                }
                finally
                {
                    // Restore original state
                    _generalPurposeBitFlag = savedFlags;
                    Encryption = savedEncryption;
                    CompressionMethod = savedCompressionMethod;
                }

                // according to ZIP specs, zero-byte files MUST NOT include file data
                if (_uncompressedSize != 0)
                {
                    Debug.Assert(_compressedBytes != null);
                    foreach (byte[] compressedBytes in _compressedBytes)
                    {
                        await _archive.ArchiveStream.WriteAsync(compressedBytes, cancellationToken).ConfigureAwait(false);
                    }
                }

                // Write data descriptor if the original entry had one
                if ((savedFlags & BitFlagValues.DataDescriptor) != 0)
                {
                    await WriteDataDescriptorAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }
        else // there is no data in the file (or the data in the file has not been loaded), but if we are in update mode, we may still need to write a header
        {
            if (_archive.Mode == ZipArchiveMode.Update || !_everOpenedForWrite)
            {
                _everOpenedForWrite = true;
                // Preserve the data descriptor flag for entries that originally had one,
                // since the descriptor bytes remain on disk after the compressed data.
                bool preserveDataDescriptor = _originallyInArchive
                    && (_generalPurposeBitFlag & BitFlagValues.DataDescriptor) != 0;
                await WriteLocalFileHeaderAsync(isEmptyFile: _uncompressedSize == 0, forceWrite: forceWrite, preserveDataDescriptor: preserveDataDescriptor, cancellationToken).ConfigureAwait(false);

                // Advance the stream past the compressed data and any trailing data descriptor
                // by seeking to the pre-computed end-of-entry boundary.
                if (_endOfLocalEntryData > _archive.ArchiveStream.Position)
                {
                    _archive.ArchiveStream.Seek(_endOfLocalEntryData, SeekOrigin.Begin);
                }
            }
        }
    }

    // Using _offsetOfLocalHeader, seeks back to where CRC and sizes should be in the header,
    // writes them, then seeks back to where you started
    // Assumes that the stream is currently at the end of the data
    private async Task WriteCrcAndSizesInLocalHeaderAsync(bool zip64HeaderUsed, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Buffer has been sized to the largest data payload required: the 64-bit data descriptor.
        byte[] writeBuffer = new byte[Zip64DataDescriptorCrcAndSizesBufferLength];

        WriteCrcAndSizesInLocalHeaderInitialize(zip64HeaderUsed, out long finalPosition, out bool pretendStreaming, out uint compressedSizeTruncated, out uint uncompressedSizeTruncated);

        // first step is, if we need zip64, but didn't allocate it, pretend we did a stream write, because
        // we can't go back and give ourselves the space that the extra field needs.
        // we do this by setting the correct property in the bit flag to indicate we have a data descriptor
        // and setting the version to Zip64 to indicate that descriptor contains 64-bit values
        if (pretendStreaming)
        {
            WriteCrcAndSizesInLocalHeaderPrepareForZip64PretendStreaming(writeBuffer);
            await _archive.ArchiveStream.WriteAsync(writeBuffer.AsMemory(0, MetadataBufferLength), cancellationToken).ConfigureAwait(false);
        }

        // next step is fill out the 32-bit size values in the normal header. we can't assume that
        // they are correct. we also write the CRC
        WriteCrcAndSizesInLocalHeaderPrepareFor32bitValuesWriting(pretendStreaming, writeBuffer, compressedSizeTruncated, uncompressedSizeTruncated);
        await _archive.ArchiveStream.WriteAsync(writeBuffer.AsMemory(0, CrcAndSizesBufferLength), cancellationToken).ConfigureAwait(false);

        // next step: if we wrote the 64 bit header initially, a different implementation might
        // try to read it, even if the 32-bit size values aren't masked. thus, we should always put the
        // correct size information in there. note that order of uncomp/comp is switched, and these are
        // 64-bit values
        // also, note that in order for this to be correct, we have to ensure that the zip64 extra field
        // is always the first extra field that is written
        if (zip64HeaderUsed)
        {
            WriteCrcAndSizesInLocalHeaderPrepareForWritingWhenZip64HeaderUsed(writeBuffer);
            await _archive.ArchiveStream.WriteAsync(writeBuffer.AsMemory(0, Zip64SizesBufferLength), cancellationToken).ConfigureAwait(false);
        }

        // now go to the where we were. assume that this is the end of the data
        _archive.ArchiveStream.Seek(finalPosition, SeekOrigin.Begin);

        // if we are pretending we did a stream write, we want to write the data descriptor out
        // the data descriptor can have 32-bit sizes or 64-bit sizes. In this case, we always use
        // 64-bit sizes
        if (pretendStreaming)
        {
            WriteCrcAndSizesInLocalHeaderPrepareForWritingDataDescriptor(writeBuffer);
            await _archive.ArchiveStream.WriteAsync(writeBuffer.AsMemory(0, Zip64DataDescriptorCrcAndSizesBufferLength), cancellationToken).ConfigureAwait(false);
        }
    }

    private ValueTask WriteDataDescriptorAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byte[] dataDescriptor = new byte[MaxSizeOfDataDescriptor];
        int bytesToWrite = PrepareToWriteDataDescriptor(dataDescriptor);
        return _archive.ArchiveStream.WriteAsync(dataDescriptor.AsMemory(0, bytesToWrite), cancellationToken);
    }

    internal async Task UnloadStreamsAsync()
    {
        if (_storedUncompressedData != null)
        {
            await _storedUncompressedData.DisposeAsync().ConfigureAwait(false);
        }
        _compressedBytes = null;
        _outstandingWriteStream = null;
    }

    private async Task CloseStreamsAsync()
    {
        // if the user left the stream open, close the underlying stream for them
        if (_outstandingWriteStream != null)
        {
            await _outstandingWriteStream.DisposeAsync().ConfigureAwait(false);
        }
    }
}
