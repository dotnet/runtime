// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.IsolatedStorage
{
    public class IsolatedStorageFileStream : FileStream
    {
        private const string BackSlash = "\\";
        private const int DefaultBufferSize = 1024;

        private readonly FileStream _fs;
        private readonly IsolatedStorageFile _isf;
        private readonly string _givenPath;
        private readonly string _fullPath;

        public IsolatedStorageFileStream(string path, FileMode mode)
            : this(path, mode, (mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite), FileShare.None, null)
        {
        }

        public IsolatedStorageFileStream(string path, FileMode mode, IsolatedStorageFile? isf)
            : this(path, mode, (mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite), FileShare.None, isf)
        {
        }

        public IsolatedStorageFileStream(string path, FileMode mode, FileAccess access)
            : this(path, mode, access, access == FileAccess.Read ? FileShare.Read : FileShare.None, DefaultBufferSize, null)
        {
        }

        public IsolatedStorageFileStream(string path, FileMode mode, FileAccess access, IsolatedStorageFile? isf)
            : this(path, mode, access, access == FileAccess.Read ? FileShare.Read : FileShare.None, DefaultBufferSize, isf)
        {
        }

        public IsolatedStorageFileStream(string path, FileMode mode, FileAccess access, FileShare share)
            : this(path, mode, access, share, DefaultBufferSize, null)
        {
        }

        public IsolatedStorageFileStream(string path, FileMode mode, FileAccess access, FileShare share, IsolatedStorageFile? isf)
            : this(path, mode, access, share, DefaultBufferSize, isf)
        {
        }

        public IsolatedStorageFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize)
            : this(path, mode, access, share, bufferSize, null)
        {
        }

        public IsolatedStorageFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, IsolatedStorageFile? isf)
            : this(path, mode, access, share, bufferSize, InitializeFileStream(path, mode, access, share, bufferSize, isf))
        {
        }

        // On .NET Framework FileStream has an internal no arg constructor that we utilize to provide the facade. We don't have access
        // to internals in .NET Core so we'll do the next best thing and contort ourselves into the SafeFileHandle constructor.
        // (A path constructor would try and create the requested file and give us two open handles.)
        //
        // We only expose our own nested FileStream so the base class having a handle doesn't matter. Passing a new SafeFileHandle
        // with ownsHandle: false avoids the parent class closing without our knowledge.
        private IsolatedStorageFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, InitialiationData initializationData)
            : base(new SafeFileHandle(initializationData.NestedStream.SafeFileHandle.DangerousGetHandle(), ownsHandle: false), access, bufferSize)
        {
            _isf = initializationData.StorageFile;
            _givenPath = path;
            _fullPath = initializationData.FullPath;
            _fs = initializationData.NestedStream;
        }

        private struct InitialiationData
        {
            public FileStream NestedStream;
            public IsolatedStorageFile StorageFile;
            public string FullPath;
        }

        // If IsolatedStorageFile is null, then we default to using a file that is scoped by user, appdomain, and assembly.
        private static InitialiationData InitializeFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, IsolatedStorageFile? isf)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if ((path.Length == 0) || path.Equals(BackSlash))
                throw new ArgumentException(
                   SR.IsolatedStorage_Path);

            bool createdStore = false;
            if (isf == null)
            {
                isf = IsolatedStorageFile.GetUserStoreForDomain();
                createdStore = true;
            }

            if (isf.Disposed)
                throw new ObjectDisposedException(null, SR.IsolatedStorage_StoreNotOpen);

            switch (mode)
            {
                case FileMode.CreateNew:        // Assume new file
                case FileMode.Create:           // Check for New file & Unreserve
                case FileMode.OpenOrCreate:     // Check for new file
                case FileMode.Truncate:         // Unreserve old file size
                case FileMode.Append:           // Check for new file
                case FileMode.Open:             // Open existing, else exception
                    break;

                default:
                    throw new ArgumentException(SR.IsolatedStorage_FileOpenMode);
            }

            InitialiationData data = new InitialiationData
            {
                FullPath = isf.GetFullPath(path),
                StorageFile = isf
            };

            try
            {
                data.NestedStream = new FileStream(data.FullPath, mode, access, share, bufferSize, FileOptions.None);
            }
            catch (Exception e)
            {
                // Make an attempt to clean up the StorageFile if we created it
                try
                {
                    if (createdStore)
                    {
                        data.StorageFile?.Dispose();
                    }
                }
                catch
                {
                }

                // Exception message might leak the IsolatedStorage path. The .NET Framework prevented this by calling an
                // internal API which made sure that the exception message was scrubbed. However since the innerException
                // is never returned to the user(GetIsolatedStorageException() does not populate the innerexception
                // in retail bits we leak the path only under the debugger via IsolatedStorageException._underlyingException which
                // they can any way look at via IsolatedStorageFile instance as well.
                throw IsolatedStorageFile.GetIsolatedStorageException(SR.IsolatedStorage_Operation_ISFS, e);
            }

            return data;
        }

        public override bool CanRead
        {
            get
            {
                return _fs.CanRead;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return _fs.CanWrite;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return _fs.CanSeek;
            }
        }

        public override long Length
        {
            get
            {
                return _fs.Length;
            }
        }

        public override long Position
        {
            get
            {
                return _fs.Position;
            }
            set
            {
                _fs.Position = value;
            }
        }

        public override bool IsAsync
        {
            get
            {
                return _fs.IsAsync;
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if (_fs != null)
                        _fs.Dispose();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public override ValueTask DisposeAsync()
        {
            return
                GetType() != typeof(IsolatedStorageFileStream) ? base.DisposeAsync() :
                _fs != null ? _fs.DisposeAsync() :
                default;
        }

        public override void Flush()
        {
            _fs.Flush();
        }

        public override void Flush(bool flushToDisk)
        {
            _fs.Flush(flushToDisk);
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _fs.FlushAsync(cancellationToken);
        }

        public override void SetLength(long value)
        {
            _fs.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _fs.Read(buffer, offset, count);
        }

        public override int Read(System.Span<byte> buffer)
        {
            return _fs.Read(buffer);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, Threading.CancellationToken cancellationToken)
        {
            return _fs.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            return _fs.ReadAsync(buffer, cancellationToken);
        }

        public override int ReadByte()
        {
            return _fs.ReadByte();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            // Desktop implementation of IsolatedStorage ensures that in case the size is increased the new memory is zero'ed out.
            // However in this implementation we simply call the FileStream.Seek APIs which have an undefined behavior.
            return _fs.Seek(offset, origin);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _fs.Write(buffer, offset, count);
        }

        public override void Write(System.ReadOnlySpan<byte> buffer)
        {
            _fs.Write(buffer);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _fs.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            return _fs.WriteAsync(buffer, cancellationToken);
        }

        public override void WriteByte(byte value)
        {
            _fs.WriteByte(value);
        }

        public override IAsyncResult BeginRead(byte[] array, int offset, int numBytes, AsyncCallback? userCallback, object? stateObject)
        {
            return _fs.BeginRead(array, offset, numBytes, userCallback, stateObject);
        }

        public override IAsyncResult BeginWrite(byte[] array, int offset, int numBytes, AsyncCallback? userCallback, object? stateObject)
        {
            return _fs.BeginWrite(array, offset, numBytes, userCallback, stateObject);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return _fs.EndRead(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            _fs.EndWrite(asyncResult);
        }

        [Obsolete("This property has been deprecated.  Please use IsolatedStorageFileStream's SafeFileHandle property instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public override IntPtr Handle
        {
            get { return _fs.Handle; }
        }

        public override void Unlock(long position, long length)
        {
            _fs.Unlock(position, length);
        }

        public override void Lock(long position, long length)
        {
            _fs.Lock(position, length);
        }

        public override SafeFileHandle SafeFileHandle
        {
            get
            {
                throw new IsolatedStorageException(SR.IsolatedStorage_Operation_ISFS);
            }
        }
    }
}
