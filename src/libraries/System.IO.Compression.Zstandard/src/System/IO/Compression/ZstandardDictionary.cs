// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Compression
{
    /// <summary>Represents a Zstandard compression dictionary.</summary>
    public sealed class ZstandardDictionary : IDisposable
    {
        private readonly SafeZstdCDictHandle _compressionDictionary;
        private readonly SafeZstdDDictHandle _decompressionDictionary;
        private readonly byte[] _dictionaryData;
        private bool _disposed;

        private ZstandardDictionary(SafeZstdCDictHandle compressionDict, SafeZstdDDictHandle decompressionDict, byte[] data)
        {
            _compressionDictionary = compressionDict;
            _decompressionDictionary = decompressionDict;
            _dictionaryData = data;
        }

        /// <summary>Creates a Zstandard dictionary from the specified buffer.</summary>
        /// <param name="buffer">The buffer containing the dictionary data.</param>
        /// <returns>A new <see cref="ZstandardDictionary"/> instance.</returns>
        /// <exception cref="ArgumentException">The buffer is empty.</exception>
        public static ZstandardDictionary Create(ReadOnlySpan<byte> buffer) => Create(buffer, ZstandardUtils.Quality_Default);

        /// <summary>Creates a Zstandard dictionary from the specified buffer with the specified quality level and dictionary type.</summary>
        /// <param name="buffer">The buffer containing the dictionary data.</param>
        /// <param name="quality">The quality level for dictionary creation.</param>
        /// <returns>A new <see cref="ZstandardDictionary"/> instance.</returns>
        /// <exception cref="ArgumentException">The buffer is empty.</exception>
        public static ZstandardDictionary Create(ReadOnlySpan<byte> buffer, int quality)
        {
            if (buffer.IsEmpty)
                throw new ArgumentException(SR.ZstandardDictionary_EmptyBuffer, nameof(buffer));

            // TODO: make this data being referenced by the native dict structures
            // and avoid them having their own copies
            byte[] data = GC.AllocateArray<byte>(buffer.Length, pinned: true);
            buffer.CopyTo(data);

            unsafe
            {
                fixed (byte* dictPtr = data)
                {
                    IntPtr dictData = (IntPtr)dictPtr;

                    SafeZstdCDictHandle compressionDict = Interop.Zstd.ZSTD_createCDict_byReference(dictData, (nuint)data.Length, quality);

                    if (compressionDict.IsInvalid)
                        throw new IOException(SR.ZstandardDictionary_CreateCompressionFailed);
                    compressionDict._pinnedData = GCHandle.Alloc(data, GCHandleType.Pinned);

                    SafeZstdDDictHandle decompressionDict = Interop.Zstd.ZSTD_createDDict_byReference(dictData, (nuint)data.Length);

                    if (decompressionDict.IsInvalid)
                    {
                        compressionDict.Dispose();
                        throw new IOException(SR.ZstandardDictionary_CreateDecompressionFailed);
                    }
                    decompressionDict._pinnedData = GCHandle.Alloc(data, GCHandleType.Pinned);

                    return new ZstandardDictionary(compressionDict, decompressionDict, data);
                }
            }
        }

        /// <summary>Creates a dictionary by training on the provided samples.</summary>
        /// <param name="samples">All training samples concatenated in one large buffer.</param>
        /// <param name="sampleLengths">The lengths of the individual samples.</param>
        /// <param name="maxDictionarySize">The maximum size of the dictionary to create.</param>
        /// <returns>A new <see cref="ZstandardDictionary"/> instance.</returns>
        /// <exception cref="ArgumentException">Invalid sample data or lengths.</exception>
        /// <exception cref="IOException">Failed to train the dictionary.</exception>
        /// <remarks>
        /// Recommended maximum dictionary size is 100KB, and that the size of the training data
        /// should be approximately 100 times the size of the resulting dictionary.
        /// </remarks>
        public static ZstandardDictionary Train(ReadOnlySpan<byte> samples, ReadOnlySpan<long> sampleLengths, int maxDictionarySize)
        {
            if (samples.IsEmpty)
                throw new ArgumentException(SR.ZstandardDictionary_EmptyBuffer, nameof(samples));
            if (sampleLengths.IsEmpty)
                throw new ArgumentException("Sample lengths cannot be empty.", nameof(sampleLengths));
            if (maxDictionarySize <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxDictionarySize), "Dictionary size must be positive.");

            byte[] dictionaryBuffer = new byte[maxDictionarySize];

            unsafe
            {
                fixed (byte* samplesPtr = &MemoryMarshal.GetReference(samples))
                fixed (long* lengthsPtr = &MemoryMarshal.GetReference(sampleLengths))
                fixed (byte* dictPtr = dictionaryBuffer)
                {
                    nuint dictSize = Interop.Zstd.ZDICT_trainFromBuffer(
                        (IntPtr)dictPtr, (nuint)maxDictionarySize,
                        (IntPtr)samplesPtr, (IntPtr)lengthsPtr, (uint)sampleLengths.Length);

                    if (Interop.Zstd.ZSTD_isError(dictSize) != 0)
                        throw new IOException("Failed to train dictionary from samples.");

                    if (dictSize == 0)
                        throw new IOException("Dictionary training produced empty dictionary.");

                    // Resize dictionary to actual size
                    if (dictSize < (nuint)maxDictionarySize)
                    {
                        Array.Resize(ref dictionaryBuffer, (int)dictSize);
                    }

                    return Create(dictionaryBuffer);
                }
            }
        }

        /// <summary>Gets the compression dictionary handle.</summary>
        internal SafeZstdCDictHandle CompressionDictionary
        {
            get
            {
                ThrowIfDisposed();
                return _compressionDictionary;
            }
        }

        /// <summary>Gets the decompression dictionary handle.</summary>
        internal SafeZstdDDictHandle DecompressionDictionary
        {
            get
            {
                ThrowIfDisposed();
                return _decompressionDictionary;
            }
        }

        /// <summary>Gets the dictionary data.</summary>
        /// <value>The raw dictionary bytes.</value>
        public ReadOnlyMemory<byte> Data
        {
            get
            {
                ThrowIfDisposed();
                return _dictionaryData;
            }
        }

        /// <summary>Releases all resources used by the <see cref="ZstandardDictionary"/>.</summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _compressionDictionary.Dispose();
                _decompressionDictionary.Dispose();
                _disposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }
    }
}
