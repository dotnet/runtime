// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Compression
{
    /// <summary>Represents a ZStandard compression dictionary.</summary>
    public sealed class ZStandardDictionary : IDisposable
    {
        private readonly SafeZStdCDictHandle? _compressionDictionary;
        private readonly SafeZStdDDictHandle _decompressionDictionary;
        private readonly byte[] _dictionaryData;
        private bool _disposed;

        private ZStandardDictionary(SafeZStdCDictHandle? compressionDict, SafeZStdDDictHandle decompressionDict, byte[] data)
        {
            _compressionDictionary = compressionDict;
            _decompressionDictionary = decompressionDict;
            _dictionaryData = data;
        }

        /// <summary>Creates a ZStandard dictionary from the specified buffer.</summary>
        /// <param name="buffer">The buffer containing the dictionary data.</param>
        /// <returns>A new <see cref="ZStandardDictionary"/> instance.</returns>
        /// <exception cref="ArgumentException">The buffer is empty.</exception>
        public static ZStandardDictionary Create(ReadOnlyMemory<byte> buffer)
        {
            if (buffer.IsEmpty)
                throw new ArgumentException("Buffer cannot be empty.", nameof(buffer));

            byte[] dictionaryData = buffer.ToArray();

            unsafe
            {
                fixed (byte* dictPtr = dictionaryData)
                {
                    IntPtr dictData = (IntPtr)dictPtr;

                    // Create only decompression dictionary since no quality level is specified
                    SafeZStdDDictHandle decompressionDict = Interop.Zstd.ZSTD_createDDict(dictData, (nuint)dictionaryData.Length);
                    if (decompressionDict.IsInvalid)
                        throw new InvalidOperationException("Failed to create ZStandard decompression dictionary.");

                    return new ZStandardDictionary(null, decompressionDict, dictionaryData);
                }
            }
        }

        /// <summary>Creates a ZStandard dictionary from the specified buffer with the specified quality level.</summary>
        /// <param name="buffer">The buffer containing the dictionary data.</param>
        /// <param name="quality">The quality level for dictionary creation.</param>
        /// <returns>A new <see cref="ZStandardDictionary"/> instance.</returns>
        /// <exception cref="ArgumentException">The buffer is empty.</exception>
        public static ZStandardDictionary Create(ReadOnlyMemory<byte> buffer, int quality)
        {
            if (buffer.IsEmpty)
                throw new ArgumentException("Buffer cannot be empty.", nameof(buffer));

            byte[] dictionaryData = buffer.ToArray();

            unsafe
            {
                fixed (byte* dictPtr = dictionaryData)
                {
                    IntPtr dictData = (IntPtr)dictPtr;

                    // Create both compression and decompression dictionaries
                    SafeZStdCDictHandle compressionDict = Interop.Zstd.ZSTD_createCDict(dictData, (nuint)dictionaryData.Length, quality);
                    if (compressionDict.IsInvalid)
                        throw new InvalidOperationException("Failed to create ZStandard compression dictionary.");

                    SafeZStdDDictHandle decompressionDict = Interop.Zstd.ZSTD_createDDict(dictData, (nuint)dictionaryData.Length);
                    if (decompressionDict.IsInvalid)
                    {
                        compressionDict.Dispose();
                        throw new InvalidOperationException("Failed to create ZStandard decompression dictionary.");
                    }

                    return new ZStandardDictionary(compressionDict, decompressionDict, dictionaryData);
                }
            }
        }

        /// <summary>Gets the compression dictionary handle.</summary>
        internal SafeZStdCDictHandle? CompressionDictionary
        {
            get
            {
                ThrowIfDisposed();
                return _compressionDictionary;
            }
        }

        /// <summary>Gets the decompression dictionary handle.</summary>
        internal SafeZStdDDictHandle DecompressionDictionary
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

        /// <summary>Releases all resources used by the <see cref="ZStandardDictionary"/>.</summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _compressionDictionary?.Dispose();
                _decompressionDictionary?.Dispose();
                _disposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }
    }
}
