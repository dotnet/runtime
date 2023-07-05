// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Packaging
{
    /// <summary>
    /// This class ignores all calls to Flush() and Close() methods
    /// depending on whether the IgnoreFlushAndClose property is set to true
    /// or false. This has been created for performance improvements for the
    /// ZipPackage.
    /// </summary>
    internal sealed class IgnoreFlushAndCloseStream : Stream
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="stream"></param>
        internal IgnoreFlushAndCloseStream(Stream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        #endregion Constructor

        #region Properties

        /// <summary>
        /// Member of the abstract Stream class
        /// </summary>
        /// <value>Bool, true if the stream can be read from, else false</value>
        public override bool CanRead
        {
            get { return !_disposed && _stream.CanRead; }
        }

        /// <summary>
        /// Member of the abstract Stream class
        /// </summary>
        /// <value>Bool, true if the stream can be seeked, else false</value>
        public override bool CanSeek
        {
            get { return !_disposed && _stream.CanSeek; }
        }

        /// <summary>
        /// Member of the abstract Stream class
        /// </summary>
        /// <value>Bool, true if the stream can be written to, else false</value>
        public override bool CanWrite
        {
            get { return !_disposed && _stream.CanWrite; }
        }

        /// <summary>
        /// Member of the abstract Stream class
        /// </summary>
        /// <value>Long value indicating the length of the stream</value>
        public override long Length
        {
            get
            {
                ThrowIfStreamDisposed();
                return _stream.Length;
            }
        }

        /// <summary>
        /// Member of the abstract Stream class
        /// </summary>
        /// <value>Long value indicating the current position in the stream</value>
        public override long Position
        {
            get
            {
                ThrowIfStreamDisposed();
                return _stream.Position;
            }
            set
            {
                ThrowIfStreamDisposed();
                _stream.Position = value;
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Member of the abstract Stream class
        /// </summary>
        /// <param name="offset">only zero is supported</param>
        /// <param name="origin">only SeekOrigin.Begin is supported</param>
        /// <returns>zero</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            ThrowIfStreamDisposed();
            return _stream.Seek(offset, origin);
        }

        /// <summary>
        /// Member of the abstract Stream class
        /// </summary>
        /// <param name="newLength"></param>
        public override void SetLength(long newLength)
        {
            ThrowIfStreamDisposed();
            _stream.SetLength(newLength);
        }

        /// <summary>
        /// Member of the abstract Stream class
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        /// <remarks>
        /// The standard Stream.Read semantics, and in particular the restoration of the current
        /// position in case of an exception, is implemented by the underlying stream.
        /// </remarks>
        public override int Read(byte[] buffer, int offset, int count)
        {
            ThrowIfStreamDisposed();
            return _stream.Read(buffer, offset, count);
        }

        /// <summary>
        /// Member of the abstract Stream class
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buf, int offset, int count)
        {
            ThrowIfStreamDisposed();
            _stream.Write(buf, offset, count);
        }

        /// <summary>
        /// Member of the abstract Stream class
        /// </summary>
        public override void Flush()
        {
            ThrowIfStreamDisposed();
        }
        #endregion Methods

        /// <summary>
        /// Dispose(bool)
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!_disposed)
                {
                    _stream = null!;
                    _disposed = true;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }


        #region Private Methods

        private void ThrowIfStreamDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(null, SR.StreamObjectDisposed);
        }

        #endregion Private Methods

        #region Private Variables

        private Stream _stream;
        private bool _disposed;

        #endregion Private Variables
    }
}
