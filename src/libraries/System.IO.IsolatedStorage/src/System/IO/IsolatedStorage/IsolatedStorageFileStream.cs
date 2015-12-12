// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.Win32.SafeHandles;
using System.Security;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.IsolatedStorage
{
    public class IsolatedStorageFileStream : Stream
    {
        private const String s_BackSlash = "\\";

        private FileStream m_fs;
        private IsolatedStorageFile m_isf;
        private String m_GivenPath;
        private String m_FullPath;

        private IsolatedStorageFileStream() { }

        public IsolatedStorageFileStream(String path, FileMode mode,
                IsolatedStorageFile isf)
            : this(path, mode, (mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite), FileShare.None, isf)
        {
        }
        public IsolatedStorageFileStream(String path, FileMode mode,
                FileAccess access, IsolatedStorageFile isf)
            : this(path, mode, access, access == FileAccess.Read ?
                FileShare.Read : FileShare.None, DefaultBufferSize, isf)
        {
        }
        public IsolatedStorageFileStream(String path, FileMode mode,
                FileAccess access, FileShare share, IsolatedStorageFile isf)
            : this(path, mode, access, share, DefaultBufferSize, isf)
        {
        }

        private const int DefaultBufferSize = 1024;

        // If the isolated storage file is null, then we default to using a file
        // that is scoped by user, appdomain, and assembly.
        public IsolatedStorageFileStream(String path, FileMode mode,
            FileAccess access, FileShare share, int bufferSize,
            IsolatedStorageFile isf)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            Contract.EndContractBlock();

            if ((path.Length == 0) || path.Equals(s_BackSlash))
                throw new ArgumentException(
                   SR.IsolatedStorage_Path);

            if (isf == null)
            {
                throw new ArgumentNullException("isf");
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

            m_isf = isf;
            m_GivenPath = path;
            m_FullPath = m_isf.GetFullPath(m_GivenPath);

            try
            {
                m_fs = new
                   FileStream(m_FullPath, mode, access, share, bufferSize,
                       FileOptions.None);
            }
            catch (Exception e)
            {
                // Exception message might leak the IsolatedStorage path. The desktop prevented this by calling an
                // internal API which made sure that the exception message was scrubbed. However since the innerException
                // is never returned to the user(GetIsolatedStorageException() does not populate the innerexception
                // in retail bits we leak the path only under the debugger via IsolatedStorageException.m_underlyingException which
                // they can any way look at via IsolatedStorageFile instance as well.
                throw IsolatedStorageFile.GetIsolatedStorageException("IsolatedStorage_Operation_ISFS", e);
            }
        }

        public override bool CanRead
        {
            [Pure]
            get
            {
                return m_fs.CanRead;
            }
        }

        public override bool CanWrite
        {
            [Pure]
            get
            {
                return m_fs.CanWrite;
            }
        }

        public override bool CanSeek
        {
            [Pure]
            get
            {
                return m_fs.CanSeek;
            }
        }

        public override long Length
        {
            get
            {
                return m_fs.Length;
            }
        }

        public override long Position
        {
            get
            {
                return m_fs.Position;
            }

            set
            {
                m_fs.Position = value;
            }
        }

        public string Name
        {
            [SecurityCritical]
            get
            {
                return m_FullPath;
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if (m_fs != null)
                        m_fs.Dispose();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public override void Flush()
        {
            m_fs.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return m_fs.FlushAsync();
        }

        public override void SetLength(long value)
        {
            m_fs.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return m_fs.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, Threading.CancellationToken cancellationToken)
        {
            return m_fs.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override int ReadByte()
        {
            return m_fs.ReadByte();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            // Desktop implementation of IsolatedStorage ensures that in case the size is increased the new memory is zero'ed out.
            // However in this implementation we simply call the FileStream.Seek APIs which have an undefined behavior.
            return m_fs.Seek(offset, origin);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            m_fs.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return m_fs.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override void WriteByte(byte value)
        {
            m_fs.WriteByte(value);
        }
    }
}
