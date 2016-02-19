// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    public partial class FileStream : Stream
    {
        internal const int DefaultBufferSize = 4096;
        private const FileShare DefaultShare = FileShare.Read;
        private const bool DefaultUseAsync = true;
        private const bool DefaultIsAsync = false;

        private FileStreamBase _innerStream;

        internal FileStream(FileStreamBase innerStream)
        {
            if (innerStream == null)
            {
                throw new ArgumentNullException(nameof(innerStream));
            }

            this._innerStream = innerStream;
        }

        public FileStream(Microsoft.Win32.SafeHandles.SafeFileHandle handle, FileAccess access) :
            this(handle, access, DefaultBufferSize)
        {
        }

        public FileStream(string path, System.IO.FileMode mode) :
            this(path, mode, (mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite), DefaultShare, DefaultBufferSize, DefaultUseAsync)
        { }

        public FileStream(string path, System.IO.FileMode mode, FileAccess access) :
            this(path, mode, access, DefaultShare, DefaultBufferSize, DefaultUseAsync)
        { }

        public FileStream(string path, System.IO.FileMode mode, FileAccess access, FileShare share) :
            this(path, mode, access, share, DefaultBufferSize, DefaultUseAsync)
        { }

        public FileStream(string path, System.IO.FileMode mode, FileAccess access, FileShare share, int bufferSize) :
            this(path, mode, access, share, bufferSize, DefaultUseAsync)
        { }

        public FileStream(string path, System.IO.FileMode mode, FileAccess access, FileShare share, int bufferSize, bool useAsync) :
            this(path, mode, access, share, bufferSize, useAsync ? FileOptions.Asynchronous : FileOptions.None)
        { }

        public FileStream(string path, System.IO.FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
        {
            Init(path, mode, access, share, bufferSize, options);
        }

        private void Init(String path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path), SR.ArgumentNull_Path);
            if (path.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(path));

            // don't include inheritable in our bounds check for share
            FileShare tempshare = share & ~FileShare.Inheritable;
            String badArg = null;

            if (mode < FileMode.CreateNew || mode > FileMode.Append)
                badArg = "mode";
            else if (access < FileAccess.Read || access > FileAccess.ReadWrite)
                badArg = "access";
            else if (tempshare < FileShare.None || tempshare > (FileShare.ReadWrite | FileShare.Delete))
                badArg = "share";

            if (badArg != null)
                throw new ArgumentOutOfRangeException(badArg, SR.ArgumentOutOfRange_Enum);

            // NOTE: any change to FileOptions enum needs to be matched here in the error validation
            if (options != FileOptions.None && (options & ~(FileOptions.WriteThrough | FileOptions.Asynchronous | FileOptions.RandomAccess | FileOptions.DeleteOnClose | FileOptions.SequentialScan | FileOptions.Encrypted | (FileOptions)0x20000000 /* NoBuffering */)) != 0)
                throw new ArgumentOutOfRangeException(nameof(options), SR.ArgumentOutOfRange_Enum);

            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), SR.ArgumentOutOfRange_NeedPosNum);

            // Write access validation
            if ((access & FileAccess.Write) == 0)
            {
                if (mode == FileMode.Truncate || mode == FileMode.CreateNew || mode == FileMode.Create || mode == FileMode.Append)
                {
                    // No write access, mode and access disagree but flag access since mode comes first
                    throw new ArgumentException(SR.Format(SR.Argument_InvalidFileModeAndAccessCombo, mode, access), nameof(access));
                }
            }

            string fullPath = Path.GetFullPath(path);

            ValidatePath(fullPath, "path");

            if ((access & FileAccess.Read) != 0 && mode == FileMode.Append)
                throw new ArgumentException(SR.Argument_InvalidAppendMode, nameof(access));

            this._innerStream = FileSystem.Current.Open(fullPath, mode, access, share, bufferSize, options, this);
        }

        static partial void ValidatePath(string fullPath, string paramName);

        // InternalOpen, InternalCreate, and InternalAppend:
        // Factory methods for FileStream used by File, FileInfo, and ReadLinesIterator
        // Specifies default access and sharing options for FileStreams created by those classes
        internal static FileStream InternalOpen(String path, int bufferSize = DefaultBufferSize, bool useAsync = DefaultUseAsync)
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync);
        }

        internal static FileStream InternalCreate(String path, int bufferSize = DefaultBufferSize, bool useAsync = DefaultUseAsync)
        {
            return new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize, useAsync);
        }

        internal static FileStream InternalAppend(String path, int bufferSize = DefaultBufferSize, bool useAsync = DefaultUseAsync)
        {
            return new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read, bufferSize, useAsync);
        }

        #region FileStream members
        public virtual bool IsAsync { get { return this._innerStream.IsAsync; } }
        public string Name { get { return this._innerStream.Name; } }
        public virtual Microsoft.Win32.SafeHandles.SafeFileHandle SafeFileHandle { get { return this._innerStream.SafeFileHandle; } }

        public virtual void Flush(bool flushToDisk)
        {
            this._innerStream.Flush(flushToDisk);
        }
        #endregion

        #region Stream members
        #region Properties

        public override bool CanRead
        {
            get { return _innerStream.CanRead; }
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
            if (_innerStream != null)
            {
                // called even during finalization
                _innerStream.DisposeInternal(disposing);
            }
            base.Dispose(disposing);
        }

        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Flush() which a subclass might have overridden.  To be safe 
            // we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Flush) when we are not sure.
            if (this.GetType() != typeof(FileStream))
                return base.FlushAsync(cancellationToken);

            return _innerStream.FlushAsync(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _innerStream.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), SR.ArgumentNull_Buffer);
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (buffer.Length - offset < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen /*, no good single parameter name to pass*/);
            Contract.EndContractBlock();

            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Read() or ReadAsync() which a subclass might have overridden.  
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Read/ReadAsync) when we are not sure.
            if (this.GetType() != typeof(FileStream))
                return base.ReadAsync(buffer, offset, count, cancellationToken);

            return _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override int ReadByte()
        {
            return _innerStream.ReadByte();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _innerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _innerStream.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), SR.ArgumentNull_Buffer);
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (buffer.Length - offset < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen /*, no good single parameter name to pass*/);
            Contract.EndContractBlock();

            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Write() or WriteAsync() which a subclass might have overridden.  
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Write/WriteAsync) when we are not sure.
            if (this.GetType() != typeof(FileStream))
                return base.WriteAsync(buffer, offset, count, cancellationToken);

            return _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override void WriteByte(byte value)
        {
            _innerStream.WriteByte(value);
        }
        #endregion Methods
        #endregion Stream members

        [Security.SecuritySafeCritical]
        ~FileStream()
        {
            // Preserved for compatibility since FileStream has defined a 
            // finalizer in past releases and derived classes may depend
            // on Dispose(false) call.
            Dispose(false);
        }
    }
}
