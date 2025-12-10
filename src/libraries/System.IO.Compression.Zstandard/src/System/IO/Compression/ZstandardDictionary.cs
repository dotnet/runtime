// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
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
            {
                throw new ArgumentException(SR.ZstandardDictionary_EmptyBuffer, nameof(buffer));
            }

            byte[] data = buffer.ToArray();
            buffer.CopyTo(data);

            unsafe
            {
                fixed (byte* dictPtr = data)
                {
                    SafeZstdCDictHandle compressionDict = Interop.Zstd.ZSTD_createCDict_byReference(dictPtr, (nuint)data.Length, quality);

                    if (compressionDict.IsInvalid)
                    {
                        throw new IOException(SR.ZstandardDictionary_CreateCompressionFailed);
                    }
                    compressionDict._pinnedData = new PinnedGCHandle<byte[]>(data);

                    SafeZstdDDictHandle decompressionDict = Interop.Zstd.ZSTD_createDDict_byReference(dictPtr, (nuint)data.Length);

                    if (decompressionDict.IsInvalid)
                    {
                        compressionDict.Dispose();
                        throw new IOException(SR.ZstandardDictionary_CreateDecompressionFailed);
                    }
                    decompressionDict._pinnedData = new PinnedGCHandle<byte[]>(data);

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
        public static ZstandardDictionary Train(ReadOnlySpan<byte> samples, ReadOnlySpan<int> sampleLengths, int maxDictionarySize)
        {
            if (samples.IsEmpty)
            {
                throw new ArgumentException(SR.ZstandardDictionary_EmptyBuffer, nameof(samples));
            }

            // this requirement is enforced by zstd native library, probably due to the underlying algorithm design
            if (sampleLengths.Length < 5)
            {
                throw new ArgumentException(SR.Format(SR.ZstandardDictionary_Train_MinimumSampleCount, 5), nameof(sampleLengths));
            }

            long totalLength = 0;
            foreach (int length in sampleLengths)
            {
                totalLength += length;
            }
            if (totalLength != samples.Length)
            {
                throw new ArgumentException(SR.ZstandardDictionary_SampleLengthsMismatch, nameof(sampleLengths));
            }

            ArgumentOutOfRangeException.ThrowIfLessThan(maxDictionarySize, 256, nameof(maxDictionarySize));

            byte[] dictionaryBuffer = new byte[maxDictionarySize];

            nuint dictSize;

            unsafe
            {
                if (sizeof(nuint) == sizeof(int))
                {
                    ReadOnlySpan<nuint> lengthsAsNuint = MemoryMarshal.Cast<int, nuint>(sampleLengths);

                    fixed (byte* samplesPtr = &MemoryMarshal.GetReference(samples))
                    fixed (byte* dictPtr = dictionaryBuffer)
                    fixed (nuint* lengthsAsNuintPtr = &MemoryMarshal.GetReference(lengthsAsNuint))
                    {
                        dictSize = Interop.Zstd.ZDICT_trainFromBuffer(
                                dictPtr, (nuint)maxDictionarySize,
                                samplesPtr, lengthsAsNuintPtr, (uint)sampleLengths.Length);
                    }
                }
                else
                {
                    // on 64-bit platforms, we need to convert ints to nuints
                    const int maxStackAlloc = 1024; // 8 kB
                    Span<nuint> lengthsAsNuint = sampleLengths.Length <= maxStackAlloc ? stackalloc nuint[maxStackAlloc] : new nuint[sampleLengths.Length];

                    for (int i = 0; i < sampleLengths.Length; i++)
                    {
                        lengthsAsNuint[i] = (nuint)sampleLengths[i];
                    }

                    lengthsAsNuint = lengthsAsNuint.Slice(0, sampleLengths.Length);

                    fixed (byte* samplesPtr = &MemoryMarshal.GetReference(samples))
                    fixed (byte* dictPtr = dictionaryBuffer)
                    fixed (nuint* lengthsAsNuintPtr = &MemoryMarshal.GetReference(lengthsAsNuint))
                    {
                        dictSize = Interop.Zstd.ZDICT_trainFromBuffer(
                                dictPtr, (nuint)maxDictionarySize,
                                samplesPtr, lengthsAsNuintPtr, (uint)sampleLengths.Length);
                    }
                }

                if (ZstandardUtils.IsError(dictSize))
                {
                    throw new IOException(SR.ZstandardDictionary_Train_Failure, ZstandardUtils.CreateExceptionForError(dictSize));
                }

                return Create(dictionaryBuffer.AsSpan(0, (int)dictSize));
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
