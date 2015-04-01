// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Contracts;

namespace System.IO.Compression
{
    internal class WrappedStream : Stream
    {
        #region fields

        private readonly Stream _baseStream;
        private readonly EventHandler _onClosed;
        private Boolean _canRead, _canWrite, _canSeek;
        private Boolean _isDisposed;
        private readonly Boolean _closeBaseStream;

        #endregion

        #region constructors

        internal WrappedStream(Stream baseStream, Boolean canRead, Boolean canWrite, Boolean canSeek, EventHandler onClosed)
            : this(baseStream, canRead, canWrite, canSeek, false, onClosed)
        { }

        internal WrappedStream(Stream baseStream, Boolean canRead, Boolean canWrite, Boolean canSeek, Boolean closeBaseStream, EventHandler onClosed)
        {
            _baseStream = baseStream;
            _onClosed = onClosed;
            _canRead = canRead;
            _canSeek = canSeek;
            _canWrite = canWrite;
            _isDisposed = false;
            _closeBaseStream = closeBaseStream;
        }

        internal WrappedStream(Stream baseStream, EventHandler onClosed)
            : this(baseStream, true, true, true, onClosed)
        { }

        #endregion

        #region properties

        public override long Length
        {
            get
            {
                ThrowIfDisposed();
                return _baseStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                ThrowIfDisposed();
                return _baseStream.Position;
            }
            set
            {
                ThrowIfDisposed();
                ThrowIfCantSeek();

                _baseStream.Position = value;
            }
        }

        public override bool CanRead { get { return _canRead && _baseStream.CanRead; } }

        public override bool CanSeek { get { return _canSeek && _baseStream.CanSeek; } }

        public override bool CanWrite { get { return _canWrite && _baseStream.CanWrite; } }

        #endregion

        #region methods

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().ToString(), SR.HiddenStreamName);
        }
        private void ThrowIfCantRead()
        {
            if (!CanWrite)
                throw new NotSupportedException(SR.WritingNotSupported);
        }
        private void ThrowIfCantWrite()
        {
            if (!CanWrite)
                throw new NotSupportedException(SR.WritingNotSupported);
        }
        private void ThrowIfCantSeek()
        {
            if (!CanSeek)
                throw new NotSupportedException(SR.SeekingNotSupported);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();
            ThrowIfCantRead();

            return _baseStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            ThrowIfDisposed();
            ThrowIfCantSeek();

            return _baseStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            ThrowIfDisposed();
            ThrowIfCantSeek();
            ThrowIfCantWrite();

            _baseStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();
            ThrowIfCantWrite();

            _baseStream.Write(buffer, offset, count);
        }

        public override void Flush()
        {
            ThrowIfDisposed();
            ThrowIfCantWrite();

            _baseStream.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed)
            {
                if (_onClosed != null)
                    _onClosed(this, null);

                if (_closeBaseStream)
                    _baseStream.Dispose();

                _canRead = false;
                _canWrite = false;
                _canSeek = false;
                _isDisposed = true;
            }
            base.Dispose(disposing);
        }
        #endregion
    }

    internal class SubReadStream : Stream
    {
        #region fields

        private readonly long _startInSuperStream;
        private long _positionInSuperStream;
        private readonly long _endInSuperStream;
        private readonly Stream _superStream;
        private Boolean _canRead;
        private Boolean _isDisposed;

        #endregion

        #region constructors

        public SubReadStream(Stream superStream, long startPosition, long maxLength)
        {
            _startInSuperStream = startPosition;
            _positionInSuperStream = startPosition;
            _endInSuperStream = startPosition + maxLength;
            _superStream = superStream;
            _canRead = true;
            _isDisposed = false;
        }

        #endregion

        #region properties

        public override long Length
        {
            get
            {
                Contract.Ensures(Contract.Result<Int64>() >= 0);

                ThrowIfDisposed();

                return _endInSuperStream - _startInSuperStream;
            }
        }

        public override long Position
        {
            get
            {
                Contract.Ensures(Contract.Result<Int64>() >= 0);

                ThrowIfDisposed();

                return _positionInSuperStream - _startInSuperStream;
            }
            set
            {
                ThrowIfDisposed();

                throw new NotSupportedException(SR.SeekingNotSupported);
            }
        }

        public override bool CanRead { get { return _superStream.CanRead && _canRead; } }

        public override bool CanSeek { get { return false; } }

        public override bool CanWrite { get { return false; } }

        #endregion

        #region methods

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().ToString(), SR.HiddenStreamName);
        }
        private void ThrowIfCantRead()
        {
            if (!CanRead)
                throw new NotSupportedException(SR.ReadingNotSupported);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            //parameter validation sent to _superStream.Read
            int origCount = count;

            ThrowIfDisposed();
            ThrowIfCantRead();

            if (_superStream.Position != _positionInSuperStream)
                _superStream.Seek(_positionInSuperStream, SeekOrigin.Begin);
            if (_positionInSuperStream + count > _endInSuperStream)
                count = (int)(_endInSuperStream - _positionInSuperStream);

            Contract.Assert(count >= 0);
            Contract.Assert(count <= origCount);

            int ret = _superStream.Read(buffer, offset, count);

            _positionInSuperStream += ret;
            return ret;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            ThrowIfDisposed();
            throw new NotSupportedException(SR.SeekingNotSupported);
        }

        public override void SetLength(long value)
        {
            ThrowIfDisposed();
            throw new NotSupportedException(SR.SetLengthRequiresSeekingAndWriting);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();
            throw new NotSupportedException(SR.WritingNotSupported);
        }

        public override void Flush()
        {
            ThrowIfDisposed();
            throw new NotSupportedException(SR.WritingNotSupported);
        }

        // Close the stream for reading.  Note that this does NOT close the superStream (since 
        // the substream is just 'a chunk' of the super-stream 
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed)
            {
                _canRead = false;
                _isDisposed = true;
            }
            base.Dispose(disposing);
        }
        #endregion
    }

    internal class CheckSumAndSizeWriteStream : Stream
    {
        #region fields

        private readonly Stream _baseStream;
        private readonly Stream _baseBaseStream;
        private Int64 _position;
        private UInt32 _checksum;

        private readonly Boolean _leaveOpenOnClose;
        private Boolean _canWrite;
        private Boolean _isDisposed;

        private Boolean _everWritten;

        //this is the position in BaseBaseStream
        private Int64 _initialPosition;

        // Called when the stream is closed.
        // parameters are initialPosition, currentPosition, checkSum
        private readonly Action<Int64, Int64, UInt32> _saveCrcAndSizes;

        #endregion

        #region constructors

        /* parameters to saveCrcAndSizes are
         *  initialPosition (initialPosition in baseBaseStream),
         *  currentPosition (in this CheckSumAndSizeWriteStream),
         *  checkSum (of data passed into this CheckSumAndSizeWriteStream)
        */
        public CheckSumAndSizeWriteStream(Stream baseStream, Stream baseBaseStream,
                                            Boolean leaveOpenOnClose, Action<Int64, Int64, UInt32> saveCrcAndSizes)
        {
            _baseStream = baseStream;
            _baseBaseStream = baseBaseStream;
            _position = 0;
            _checksum = 0;
            _leaveOpenOnClose = leaveOpenOnClose;
            _canWrite = true;
            _isDisposed = false;
            _initialPosition = 0;
            _saveCrcAndSizes = saveCrcAndSizes;
        }

        #endregion

        #region properties

        public override Int64 Length
        {
            get
            {
                ThrowIfDisposed();
                throw new NotSupportedException(SR.SeekingNotSupported);
            }
        }

        public override Int64 Position
        {
            get
            {
                Contract.Ensures(Contract.Result<Int64>() >= 0);
                ThrowIfDisposed();
                return _position;
            }
            set
            {
                ThrowIfDisposed();
                throw new NotSupportedException(SR.SeekingNotSupported);
            }
        }

        public override Boolean CanRead { get { return false; } }

        public override Boolean CanSeek { get { return false; } }

        public override Boolean CanWrite { get { return _canWrite; } }

        #endregion

        #region methods

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().ToString(), SR.HiddenStreamName);
        }

        public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count)
        {
            ThrowIfDisposed();
            throw new NotSupportedException(SR.ReadingNotSupported);
        }

        public override Int64 Seek(Int64 offset, SeekOrigin origin)
        {
            ThrowIfDisposed();
            throw new NotSupportedException(SR.SeekingNotSupported);
        }

        public override void SetLength(Int64 value)
        {
            ThrowIfDisposed();
            throw new NotSupportedException(SR.SetLengthRequiresSeekingAndWriting);
        }

        public override void Write(Byte[] buffer, Int32 offset, Int32 count)
        {
            //we can't pass the argument checking down a level
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", SR.ArgumentNeedNonNegative);
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", SR.ArgumentNeedNonNegative);
            if ((buffer.Length - offset) < count)
                throw new ArgumentException(SR.OffsetLengthInvalid);
            Contract.EndContractBlock();

            //if we're not actually writing anything, we don't want to trigger as if we did write something
            ThrowIfDisposed();
            Contract.Assert(CanWrite);

            if (count == 0)
                return;

            if (!_everWritten)
            {
                _initialPosition = _baseBaseStream.Position;
                _everWritten = true;
            }

            _checksum = Crc32Helper.UpdateCrc32(_checksum, buffer, offset, count);
            _baseStream.Write(buffer, offset, count);
            _position += count;
        }

        public override void Flush()
        {
            ThrowIfDisposed();

            //assume writeable if not disposed
            Contract.Assert(CanWrite);

            _baseStream.Flush();
        }

        protected override void Dispose(Boolean disposing)
        {
            if (disposing && !_isDisposed)
            {
                // if we never wrote through here, save the position
                if (!_everWritten)
                    _initialPosition = _baseBaseStream.Position;
                if (!_leaveOpenOnClose)
                    _baseStream.Dispose();        // Close my super-stream (flushes the last data)
                if (_saveCrcAndSizes != null)
                    _saveCrcAndSizes(_initialPosition, Position, _checksum);
                _isDisposed = true;
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}
