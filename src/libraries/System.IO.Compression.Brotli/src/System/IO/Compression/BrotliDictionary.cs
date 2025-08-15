// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Compression
{
    /// <summary>
    /// Represents a Brotli dictionary used for compression and decompression.
    /// </summary>
    public sealed class BrotliDictionary : IDisposable
    {
        private readonly SafeBrotliPreparedDictionaryHandle _preparedDictionary;
        private bool _disposed;

        private BrotliDictionary(SafeBrotliPreparedDictionaryHandle preparedDictionary)
        {
            _preparedDictionary = preparedDictionary ?? throw new ArgumentNullException(nameof(preparedDictionary));
            _disposed = false;
        }

        /// <summary>
        /// Creates a new <see cref="BrotliDictionary"/> from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer containing the dictionary data.</param>
        /// <returns>A new <see cref="BrotliDictionary"/> instance.</returns>
        /// <exception cref="ArgumentException">Thrown when the buffer is empty.</exception>
        public static BrotliDictionary CreateFromBuffer(ReadOnlySpan<byte> buffer) => CreateFromBuffer(buffer, BrotliUtils.Quality_Max);

        /// <summary>
        /// Creates a new prepared <see cref="BrotliDictionary"/> from a buffer for use with an encoder.
        /// </summary>
        /// <param name="buffer">The buffer containing the dictionary data.</param>
        /// <param name="quality">The quality level used for preparing the dictionary.</param>
        /// <returns>A new <see cref="BrotliDictionary"/> instance.</returns>
        /// <exception cref="ArgumentException">Thrown when the buffer is empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when quality is not between 0 and 11.</exception>
        public static BrotliDictionary CreateFromBuffer(ReadOnlySpan<byte> buffer, int quality)
        {
            if (buffer.IsEmpty)
            {
                throw new ArgumentException(SR.BrotliDictionary_EmptyBuffer, nameof(buffer));
            }

            if (quality < BrotliUtils.Quality_Min || quality > BrotliUtils.Quality_Max)
            {
                throw new ArgumentOutOfRangeException(nameof(quality), SR.Format(SR.BrotliEncoder_Quality, quality, BrotliUtils.Quality_Min, BrotliUtils.Quality_Max));
            }

            SafeBrotliPreparedDictionaryHandle? preparedDictionary;

            unsafe
            {
                // BrotliPreparedDictionary references the memory used to create the dictionary,
                // so we make a copy of it on the native heap.

                IntPtr nativeMemory = (IntPtr)NativeMemory.Alloc((nuint)buffer.Length, 1);
                buffer.CopyTo(new Span<byte>(nativeMemory.ToPointer(), buffer.Length));

                preparedDictionary = Interop.Brotli.BrotliEncoderPrepareDictionary(
                    Interop.Brotli.BrotliSharedDictionaryType.RAW,
                    (nuint)buffer.Length,
                    (byte*)nativeMemory.ToPointer(),
                    quality,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero);

                if (preparedDictionary == null || preparedDictionary.IsInvalid)
                {
                    NativeMemory.Free(nativeMemory.ToPointer());
                    throw new IOException(SR.BrotliDictionary_Create);
                }

                preparedDictionary.SetDictionaryBytes(nativeMemory, buffer.Length);
            }

            return new BrotliDictionary(preparedDictionary);
        }

        internal bool AttachToEncoder(SafeBrotliEncoderHandle encoderHandle)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            return Interop.Brotli.BrotliEncoderAttachPreparedDictionary(encoderHandle, _preparedDictionary) != Interop.BOOL.FALSE;
        }

        internal bool AttachToDecoder(SafeBrotliDecoderHandle decoderHandle)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            unsafe
            {
                return Interop.Brotli.BrotliDecoderAttachDictionary(
                    decoderHandle,
                    Interop.Brotli.BrotliSharedDictionaryType.RAW,
                    (nuint)_preparedDictionary.DictionaryLength,
                    _preparedDictionary.DictionaryBytes) != Interop.BOOL.FALSE;
            }
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="BrotliDictionary"/> class.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _preparedDictionary.Dispose();
                _disposed = true;
            }
        }
    }
}
