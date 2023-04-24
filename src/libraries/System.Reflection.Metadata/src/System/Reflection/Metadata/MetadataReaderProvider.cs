// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection.Internal;
using System.Text;
using System.Threading;

namespace System.Reflection.Metadata
{
    /// <summary>
    /// Provides a <see cref="MetadataReader"/> metadata stored in an array of bytes, a memory block, or a stream.
    /// </summary>
    /// <remarks>
    /// Supported formats:
    /// - ECMA-335 CLI (Common Language Infrastructure) metadata (<see cref="FromMetadataImage(byte*, int)"/>)
    /// - Edit and Continue metadata delta (<see cref="FromMetadataImage(byte*, int)"/>)
    /// - Portable PDB metadata (<see cref="FromPortablePdbImage(byte*, int)"/>)
    /// </remarks>
    public sealed class MetadataReaderProvider : IDisposable
    {
        // Either we have a provider and create metadata block lazily or
        // we have no provider and metadata block is created in the ctor.
        private MemoryBlockProvider? _blockProviderOpt;
        private AbstractMemoryBlock? _lazyMetadataBlock;

        // cached reader
        private MetadataReader? _lazyMetadataReader;
        private readonly object _metadataReaderGuard = new object();

        internal MetadataReaderProvider(AbstractMemoryBlock metadataBlock)
        {
            Debug.Assert(metadataBlock != null);
            _lazyMetadataBlock = metadataBlock;
        }

        private MetadataReaderProvider(MemoryBlockProvider blockProvider)
        {
            Debug.Assert(blockProvider != null);
            _blockProviderOpt = blockProvider;
        }

        /// <summary>
        /// Creates a Portable PDB metadata provider over a blob stored in memory.
        /// </summary>
        /// <param name="start">Pointer to the start of the Portable PDB blob.</param>
        /// <param name="size">The size of the Portable PDB blob.</param>
        /// <exception cref="ArgumentNullException"><paramref name="start"/> is <see cref="IntPtr.Zero"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is negative.</exception>
        /// <remarks>
        /// The memory is owned by the caller and not released on disposal of the <see cref="MetadataReaderProvider"/>.
        /// The caller is responsible for keeping the memory alive and unmodified throughout the lifetime of the <see cref="MetadataReaderProvider"/>.
        /// The content of the blob is not read during the construction of the <see cref="MetadataReaderProvider"/>
        /// </remarks>
        public static unsafe MetadataReaderProvider FromPortablePdbImage(byte* start, int size) => FromMetadataImage(start, size);

        /// <summary>
        /// Creates a metadata provider over an image stored in memory.
        /// </summary>
        /// <param name="start">Pointer to the start of the metadata blob.</param>
        /// <param name="size">The size of the metadata blob.</param>
        /// <exception cref="ArgumentNullException"><paramref name="start"/> is <see cref="IntPtr.Zero"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is negative.</exception>
        /// <remarks>
        /// The memory is owned by the caller and not released on disposal of the <see cref="MetadataReaderProvider"/>.
        /// The caller is responsible for keeping the memory alive and unmodified throughout the lifetime of the <see cref="MetadataReaderProvider"/>.
        /// The content of the blob is not read during the construction of the <see cref="MetadataReaderProvider"/>
        /// </remarks>
        public static unsafe MetadataReaderProvider FromMetadataImage(byte* start, int size)
        {
            if (start is null)
            {
                Throw.ArgumentNull(nameof(start));
            }

            if (size < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            return new MetadataReaderProvider(new ExternalMemoryBlockProvider(start, size));
        }

        /// <summary>
        /// Creates a Portable PDB metadata provider over a byte array.
        /// </summary>
        /// <param name="image">Portable PDB image.</param>
        /// <remarks>
        /// The content of the image is not read during the construction of the <see cref="MetadataReaderProvider"/>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
        public static MetadataReaderProvider FromPortablePdbImage(ImmutableArray<byte> image) => FromMetadataImage(image);

        /// <summary>
        /// Creates a provider over a byte array.
        /// </summary>
        /// <param name="image">Metadata image.</param>
        /// <remarks>
        /// The content of the image is not read during the construction of the <see cref="MetadataReaderProvider"/>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
        public static MetadataReaderProvider FromMetadataImage(ImmutableArray<byte> image)
        {
            if (image.IsDefault)
            {
                Throw.ArgumentNull(nameof(image));
            }

            return new MetadataReaderProvider(new ByteArrayMemoryProvider(image));
        }

        /// <summary>
        /// Creates a provider for a stream of the specified size beginning at its current position.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <param name="size">Size of the metadata blob in the stream. If not specified the metadata blob is assumed to span to the end of the stream.</param>
        /// <param name="options">
        /// Options specifying how sections of the image are read from the stream.
        ///
        /// Unless <see cref="MetadataStreamOptions.LeaveOpen"/> is specified, ownership of the stream is transferred to the <see cref="MetadataReaderProvider"/>
        /// upon successful argument validation. It will be disposed by the <see cref="MetadataReaderProvider"/> and the caller must not manipulate it.
        ///
        /// Unless <see cref="MetadataStreamOptions.PrefetchMetadata"/> is specified no data
        /// is read from the stream during the construction of the <see cref="MetadataReaderProvider"/>. Furthermore, the stream must not be manipulated
        /// by caller while the <see cref="MetadataReaderProvider"/> is alive and undisposed.
        ///
        /// If <see cref="MetadataStreamOptions.PrefetchMetadata"/>, the <see cref="MetadataReaderProvider"/>
        /// will have read all of the data requested during construction. As such, if <see cref="MetadataStreamOptions.LeaveOpen"/> is also
        /// specified, the caller retains full ownership of the stream and is assured that it will not be manipulated by the <see cref="MetadataReaderProvider"/>
        /// after construction.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="stream"/> doesn't support read and seek operations.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Size is negative or extends past the end of the stream.</exception>
        public static MetadataReaderProvider FromPortablePdbStream(Stream stream, MetadataStreamOptions options = MetadataStreamOptions.Default, int size = 0) => FromMetadataStream(stream, options, size);

        /// <summary>
        /// Creates a provider for a stream of the specified size beginning at its current position.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <param name="size">Size of the metadata blob in the stream. If not specified the metadata blob is assumed to span to the end of the stream.</param>
        /// <param name="options">
        /// Options specifying how sections of the image are read from the stream.
        ///
        /// Unless <see cref="MetadataStreamOptions.LeaveOpen"/> is specified, ownership of the stream is transferred to the <see cref="MetadataReaderProvider"/>
        /// upon successful argument validation. It will be disposed by the <see cref="MetadataReaderProvider"/> and the caller must not manipulate it.
        ///
        /// Unless <see cref="MetadataStreamOptions.PrefetchMetadata"/> is specified no data
        /// is read from the stream during the construction of the <see cref="MetadataReaderProvider"/>. Furthermore, the stream must not be manipulated
        /// by caller while the <see cref="MetadataReaderProvider"/> is alive and undisposed.
        ///
        /// If <see cref="MetadataStreamOptions.PrefetchMetadata"/>, the <see cref="MetadataReaderProvider"/>
        /// will have read all of the data requested during construction. As such, if <see cref="MetadataStreamOptions.LeaveOpen"/> is also
        /// specified, the caller retains full ownership of the stream and is assured that it will not be manipulated by the <see cref="MetadataReaderProvider"/>
        /// after construction.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="stream"/> doesn't support read and seek operations.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Size is negative or extends past the end of the stream.</exception>
        /// <exception cref="IOException">Error reading from the stream (only when <see cref="MetadataStreamOptions.PrefetchMetadata"/> is specified).</exception>
        public static MetadataReaderProvider FromMetadataStream(Stream stream, MetadataStreamOptions options = MetadataStreamOptions.Default, int size = 0)
        {
            if (stream is null)
            {
                Throw.ArgumentNull(nameof(stream));
            }

            if (!stream.CanRead || !stream.CanSeek)
            {
                throw new ArgumentException(SR.StreamMustSupportReadAndSeek, nameof(stream));
            }

            if (!options.IsValid())
            {
                throw new ArgumentOutOfRangeException(nameof(options));
            }

            long start = stream.Position;
            int actualSize = StreamExtensions.GetAndValidateSize(stream, size, nameof(stream));

            MetadataReaderProvider result;
            bool closeStream = true;
            try
            {
                if ((options & MetadataStreamOptions.PrefetchMetadata) == 0)
                {
                    result = new MetadataReaderProvider(new StreamMemoryBlockProvider(stream, start, actualSize, (options & MetadataStreamOptions.LeaveOpen) != 0));
                    closeStream = false;
                }
                else
                {
                    // Read in the entire image or metadata blob:
                    result = new MetadataReaderProvider(StreamMemoryBlockProvider.ReadMemoryBlockNoLock(stream, start, actualSize));

                    // We read all we need, the stream is going to be closed.
                }
            }
            finally
            {
                if (closeStream && (options & MetadataStreamOptions.LeaveOpen) == 0)
                {
                    stream.Dispose();
                }
            }

            return result;
        }

        /// <summary>
        /// Disposes all memory allocated by the reader.
        /// </summary>
        /// <remarks>
        /// <see cref="Dispose"/>  can be called multiple times (but not in parallel).
        /// It is not safe to call <see cref="Dispose"/> in parallel with any other operation on the <see cref="MetadataReaderProvider"/>
        /// or reading from the underlying memory.
        /// </remarks>
        public void Dispose()
        {
            _blockProviderOpt?.Dispose();
            _blockProviderOpt = null;

            _lazyMetadataBlock?.Dispose();
            _lazyMetadataBlock = null;

            _lazyMetadataReader = null;
        }

        /// <summary>
        /// Gets a <see cref="MetadataReader"/> from a <see cref="MetadataReaderProvider"/>.
        /// </summary>
        /// <remarks>
        /// The caller must keep the <see cref="MetadataReaderProvider"/> undisposed throughout the lifetime of the metadata reader.
        ///
        /// If this method is called multiple times each call with arguments equal to the arguments passed to the previous successful call
        /// returns the same instance of <see cref="MetadataReader"/> as the previous call.
        /// </remarks>
        /// <exception cref="ArgumentException">The encoding of <paramref name="utf8Decoder"/> is not <see cref="UTF8Encoding"/>.</exception>
        /// <exception cref="PlatformNotSupportedException">The current platform is big-endian.</exception>
        /// <exception cref="IOException">IO error while reading from the underlying stream.</exception>
        /// <exception cref="ObjectDisposedException">Provider has been disposed.</exception>
        public unsafe MetadataReader GetMetadataReader(MetadataReaderOptions options = MetadataReaderOptions.Default, MetadataStringDecoder? utf8Decoder = null)
        {
            var cachedReader = _lazyMetadataReader;

            if (CanReuseReader(cachedReader, options, utf8Decoder))
            {
                return cachedReader!;
            }

            // If multiple threads attempt to open a metadata reader with the same options and decoder
            // it's cheaper to wait for the other thread to finish initializing the reader than to open
            // two readers and discard one.
            // Note that it's rare to reader the same metadata using different options.
            lock (_metadataReaderGuard)
            {
                cachedReader = _lazyMetadataReader;

                if (CanReuseReader(cachedReader, options, utf8Decoder))
                {
                    return cachedReader!;
                }

                AbstractMemoryBlock metadata = GetMetadataBlock();
                var newReader = new MetadataReader(metadata.Pointer, metadata.Size, options, utf8Decoder, memoryOwner: this);
                _lazyMetadataReader = newReader;
                return newReader;
            }
        }

        private static bool CanReuseReader(MetadataReader? reader, MetadataReaderOptions options, MetadataStringDecoder? utf8DecoderOpt)
        {
            return reader != null && reader.Options == options && ReferenceEquals(reader.UTF8Decoder, utf8DecoderOpt ?? MetadataStringDecoder.DefaultUTF8);
        }

        /// <exception cref="IOException">IO error while reading from the underlying stream.</exception>
        /// <exception cref="ObjectDisposedException">Provider has been disposed.</exception>
        internal AbstractMemoryBlock GetMetadataBlock()
        {
            if (_lazyMetadataBlock == null)
            {
                if (_blockProviderOpt == null)
                {
                    throw new ObjectDisposedException(nameof(MetadataReaderProvider));
                }

                var newBlock = _blockProviderOpt.GetMemoryBlock(0, _blockProviderOpt.Size);
                if (Interlocked.CompareExchange(ref _lazyMetadataBlock, newBlock, null) != null)
                {
                    // another thread created the block already, we need to dispose ours:
                    newBlock.Dispose();
                }
            }

            return _lazyMetadataBlock;
        }
    }
}
