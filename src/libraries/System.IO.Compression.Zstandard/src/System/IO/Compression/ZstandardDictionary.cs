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
        public static ZstandardDictionary Create(ReadOnlyMemory<byte> buffer) => Create(buffer, ZstandardUtils.Quality_Default);

        /// <summary>Creates a Zstandard dictionary from the specified buffer with the specified quality level.</summary>
        /// <param name="buffer">The buffer containing the dictionary data.</param>
        /// <param name="quality">The quality level for dictionary creation.</param>
        /// <returns>A new <see cref="ZstandardDictionary"/> instance.</returns>
        /// <exception cref="ArgumentException">The buffer is empty.</exception>
        public static ZstandardDictionary Create(ReadOnlyMemory<byte> buffer, int quality)
        {
            if (buffer.IsEmpty)
                throw new ArgumentException(SR.ZstandardDictionary_EmptyBuffer, nameof(buffer));

            byte[] dictionaryData = buffer.ToArray();

            unsafe
            {
                fixed (byte* dictPtr = dictionaryData)
                {
                    IntPtr dictData = (IntPtr)dictPtr;

                    // Create both compression and decompression dictionaries
                    SafeZstdCDictHandle compressionDict = Interop.Zstd.ZSTD_createCDict(dictData, (nuint)dictionaryData.Length, quality);
                    if (compressionDict.IsInvalid)
                        throw new InvalidOperationException(SR.ZstandardDictionary_CreateCompressionFailed);
                    compressionDict.Quality = quality;

                    SafeZstdDDictHandle decompressionDict = Interop.Zstd.ZSTD_createDDict(dictData, (nuint)dictionaryData.Length);
                    if (decompressionDict.IsInvalid)
                    {
                        compressionDict.Dispose();
                        throw new InvalidOperationException(SR.ZstandardDictionary_CreateDecompressionFailed);
                    }

                    return new ZstandardDictionary(compressionDict, decompressionDict, dictionaryData);
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

        /// <summary>Gets the dictionary data as a read-only span.</summary>
        internal ReadOnlySpan<byte> Data
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
