// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace System.IO
{
    internal sealed class WinRTFileStream : FileStreamBase
    {
        private readonly FileAccess _access;
        private bool _disposed;
        private StorageFile _file;
        private readonly Stream _innerStream;
        private readonly FileOptions _options;
        private static readonly SafeFileHandle s_invalidHandle = new SafeFileHandle(IntPtr.Zero, false);

        internal WinRTFileStream(Stream innerStream, StorageFile file, FileAccess access, FileOptions options, FileStream parent) 
            : base(parent)
        {
            Debug.Assert(innerStream != null);
            Debug.Assert(file != null);

            this._access = access;
            this._disposed = false;
            this._file = file;
            this._innerStream = innerStream;
            this._options = options;
        }

        ~WinRTFileStream()
        {
            Dispose(false);
        }

        #region FileStream members
        public override bool IsAsync { get { return true; } }
        public override string Name { get { return _file.Name; } }
        public override Microsoft.Win32.SafeHandles.SafeFileHandle SafeFileHandle { get { return s_invalidHandle; } }
        internal override bool IsClosed => false;

        public override void Flush(bool flushToDisk)
        {
            // WinRT streams are not buffered, however the WinRT stream will be wrapped in a BufferedStream
            // Flush & FlushAsync will flush the internal managed buffer of the BufferedStream wrapper
            // The WinRT stream only exposes FlushAsync which flushes to disk.
            // The managed Stream adapter does nothing for Flush() and forwards to WinRT for FlushAsync (flushing to disk).
            if (flushToDisk)
            {
                // FlushAsync() will do the write to disk when it hits the WinRT->NetFx adapter
                Task flushTask = _innerStream.FlushAsync();
                flushTask.Wait();
            }
            else
            {
                // Flush doesn't write to disk
                _innerStream.Flush();
            }
        }

        public override void Lock(long position, long length)
        {
            throw new PlatformNotSupportedException();
        }

        public override void Unlock(long position, long length)
        {
            throw new PlatformNotSupportedException();
        }
        #endregion

        #region Stream members
        #region Properties

        public override bool CanRead
        {
            // WinRT doesn't support write-only streams, override what the stream tells us 
            // with what was passed in when creating it.
            get { return _innerStream.CanRead && (_access & FileAccess.Read) != 0; }
        }

        public override bool CanSeek
        {
            get { return _innerStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return _innerStream.CanWrite; }
        }

        public override long Length
        {
            get { return _innerStream.Length; }
        }

        public override long Position
        {
            get { return _innerStream.Position; }
            set { _innerStream.Position = value; }
        }

        public override int ReadTimeout
        {
            get { return _innerStream.ReadTimeout; }
            set { _innerStream.ReadTimeout = value; }
        }

        public override bool CanTimeout
        {
            get { return _innerStream.CanTimeout; }
        }

        public override int WriteTimeout
        {
            get { return _innerStream.WriteTimeout; }
            set { _innerStream.WriteTimeout = value; }
        }

        #endregion Properties

        #region Methods
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            return _innerStream.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                    _innerStream.Dispose();

                if ((_options & FileOptions.DeleteOnClose) != 0 && _file != null)
                {
                    // WinRT doesn't directly support DeleteOnClose but we can mimic it
                    // There are a few reasons that this will fail
                    //   1) the file may not allow delete permissions for the current user
                    //   2) the storage file RCW may have already been disconnected
                    try
                    {
                        _file.DeleteAsync().AsTask().Wait();
                    }
                    catch { }
                }

                _disposed = true;
                _file = null;
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public override void Flush()
        {
            _parent.Flush(false);
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _innerStream.FlushAsync(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!_disposed && !CanRead)
                throw Error.GetReadNotSupported();

            return _innerStream.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (!_disposed && !CanRead)
                throw Error.GetReadNotSupported();

            return _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override int ReadByte()
        {
            if (!_disposed && !CanRead)
                throw Error.GetReadNotSupported();

            return _innerStream.ReadByte();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin && offset < 0)
                throw Win32Marshal.GetExceptionForWin32Error(Interop.Errors.ERROR_NEGATIVE_SEEK);

            return _innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _innerStream.SetLength(value);

            // WinRT ignores all errors when setting length, check after setting

            if (_innerStream.Length < value)
            {
                throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_FileLengthTooBig);
            }
            else if (_innerStream.Length != value)
            {
                throw new ArgumentException(SR.Argument_FileNotResized, nameof(value));
            }

            // WinRT doesn't update the position when truncating a file
            if (value < _innerStream.Position)
                _innerStream.Position = value;
        }

        public override string ToString()
        {
            return _innerStream.ToString();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _innerStream.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override void WriteByte(byte value)
        {
            _innerStream.WriteByte(value);
        }
        #endregion Methods
        #endregion Stream members
    }
}
