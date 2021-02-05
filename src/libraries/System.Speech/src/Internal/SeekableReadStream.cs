// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace System.Speech.Internal
{
#pragma warning disable 56528 // Override of Dispose(bool) not needed as base stream should not be closed.

    // Class that is used to wrap a stream that does not support Seek into one that does.
    // While CacheDataForSeeking is true then Read data is buffered so that Seeking can be done later back into the buffer.
    // The Read call will first use the buffer and then the actual data once the buffer is read.
    // After CacheDataForSeeking is set to false data can be read from the buffer but no more Seeking can be done.
    internal class SeekableReadStream : Stream
    {
        #region Constructors

        internal SeekableReadStream(Stream baseStream)
        {
            Debug.Assert(baseStream.CanRead);

            _canSeek = baseStream.CanSeek; // If the stream is already seekable then don't need to do anything special
            _baseStream = baseStream;
        }

        #endregion

        #region Internal Properties

        internal bool CacheDataForSeeking
        {
            set
            {
                // Currently we can switch the caching off, but not back on again. Not needed for current scenarios.
                Debug.Assert(!value || _cacheDataForSeeking);
                _cacheDataForSeeking = value;
            }
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get
            {
                // Can do seeking only if we are caching data or underlying stream supports it.
                return (_canSeek || _cacheDataForSeeking);
            }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            // Non Seekable streams may not implement this, but we don't have much choice as we can't calculate the Stream length any other way.
            get { return _baseStream.Length; }
        }

        public override long Position
        {
            get
            {
                if (_canSeek)
                {
                    // Delegate to underlying Stream:
                    return _baseStream.Position;
                }
                else
                {
                    return _virtualPosition;
                }
            }
            set
            {
                if (_canSeek)
                {
                    // Delegate to underlying Stream:
                    _baseStream.Position = value;
                }
                else if (value != _virtualPosition)
                {
                    if (value < 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), SR.Get(SRID.MustBeGreaterThanZero));
                    }
                    // We can't check the length here so you can Seek beyond the end of the Stream. This will error later though.

                    if (_cacheDataForSeeking)
                    {
                        if (value < _buffer.Count)
                        {
                            // We're moving within the already buffered data so just move the position:
                            _virtualPosition = value;
                        }
                        else
                        {
                            // We're moving beyond current position.
                            // Thus Read the new data and buffer it.

                            // Read until at new position:
                            long bytesToReadLong = value - _buffer.Count;
                            if (bytesToReadLong > int.MaxValue)
                            {
                                throw new NotSupportedException(SR.Get(SRID.SeekNotSupported));
                            }
                            byte[] readBuffer = new byte[bytesToReadLong];
                            Helpers.BlockingRead(_baseStream, readBuffer, 0, (int)bytesToReadLong);

                            // Copy from readBuffer into cache:
                            _buffer.AddRange(readBuffer);
                            _virtualPosition = value;
                        }
                    }
                    else
                    {
                        // No longer caching data so we can't seek around.
                        // Limited cases of this could be supported if needed.
                        throw new NotSupportedException(SR.Get(SRID.SeekNotSupported));
                    }
                }
            }
        }

        #endregion

        #region Internal Methods

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_canSeek)
            {
                // Delegate to underlying Stream:
                return _baseStream.Read(buffer, offset, count);
            }
            else
            {
                int bytesRead = 0;
                if (_virtualPosition < _buffer.Count)
                {
                    // if new position inside buffer then read until at end of buffer
                    int toCopy = (int)(_buffer.Count - _virtualPosition);
                    if (toCopy > count)
                    {
                        toCopy = count;
                    }
                    _buffer.CopyTo((int)_virtualPosition, buffer, offset, toCopy);
                    count -= toCopy;
                    _virtualPosition += toCopy;
                    offset += toCopy;
                    bytesRead += toCopy;
                    if (!_cacheDataForSeeking && _virtualPosition >= _buffer.Count)
                    {
                        // Used up all the buffer, free.
                        _buffer.Clear();
                    }
                }
                if (count > 0)
                {
                    // Still data to Read so read it from the base Stream:
                    int localBytesRead = _baseStream.Read(buffer, offset, count);
                    bytesRead += localBytesRead;
                    _virtualPosition += localBytesRead;
                    if (_cacheDataForSeeking)
                    {
                        // if caching then extend Stream.
                        _buffer.Capacity += localBytesRead;
                        // Copy from buffer + offset for bytesRead
                        for (int i = 0; i < localBytesRead; i++)
                        {
                            _buffer.Add(buffer[offset + i]);
                        }
                    }
                    // Even if we didn't read every requested byte we can return - that's the contract on Stream.Read.
                }
                return bytesRead;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long position;

            checked // Check for integer overflow
            {
                switch (origin)
                {
                    case SeekOrigin.Begin:
                        position = offset;
                        break;

                    case SeekOrigin.Current:
                        position = Position + offset;
                        break;

                    case SeekOrigin.End:
                        position = Length + offset;
                        break;

                    default:
                        throw new ArgumentException(SR.Get(SRID.EnumInvalid, "SeekOrigin"), nameof(origin));
                }
            }

            Position = position; // Actually update position, checks for out of range
            return position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException(SR.Get(SRID.SeekNotSupported));
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException(SR.Get(SRID.StreamMustBeWriteable));
        }

        public override void Flush()
        {
            _baseStream.Flush();
        }

        #endregion

        #region Private Fields

        private long _virtualPosition;
        private List<byte> _buffer = new(); // Data cached from start of stream onwards.

        private Stream _baseStream;
        private bool _cacheDataForSeeking = true;
        private bool _canSeek;

        #endregion
    }
#pragma warning restore 56528
}
