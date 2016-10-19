// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    public partial class FileStream : FileStreamBase
    {
        private const FileShare DefaultShare = FileShare.Read;
        private const bool DefaultIsAsync = false;
        internal const int DefaultBufferSize = 4096;

        public FileStream(SafeFileHandle handle, FileAccess access)
            : this(handle, access, DefaultBufferSize)
        {
        }

        public FileStream(SafeFileHandle handle, FileAccess access, int bufferSize)
            : this(handle, access, bufferSize, GetDefaultIsAsync(handle))
        {
        }

        public FileStream(SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync)
        {
            InitFromHandle(handle, access, bufferSize, isAsync);
        }

        public FileStream(string path, FileMode mode) :
            this(path, mode, (mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite), DefaultShare, DefaultBufferSize, DefaultIsAsync)
        { }

        public FileStream(string path, FileMode mode, FileAccess access) :
            this(path, mode, access, DefaultShare, DefaultBufferSize, DefaultIsAsync)
        { }

        public FileStream(string path, FileMode mode, FileAccess access, FileShare share) :
            this(path, mode, access, share, DefaultBufferSize, DefaultIsAsync)
        { }

        public FileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize) :
            this(path, mode, access, share, bufferSize, DefaultIsAsync)
        { }

        public FileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, bool useAsync) :
            this(path, mode, access, share, bufferSize, useAsync ? FileOptions.Asynchronous : FileOptions.None)
        { }

        public FileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
        {
            Init(path, mode, access, share, bufferSize, options);
        }

        private static bool GetDefaultIsAsync(SafeFileHandle handle)
        {
            // This will eventually get more complicated as we can actually check the underlying handle type on Windows
            return handle.IsAsync.HasValue ? handle.IsAsync.Value : false;
        }

        private void Init(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path), SR.ArgumentNull_Path);
            if (path.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(path));

            // don't include inheritable in our bounds check for share
            FileShare tempshare = share & ~FileShare.Inheritable;
            string badArg = null;

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

            if ((access & FileAccess.Read) != 0 && mode == FileMode.Append)
                throw new ArgumentException(SR.Argument_InvalidAppendMode, nameof(access));

            InitInternal(fullPath, mode, access, share, bufferSize, options);
        }

        private void InitFromHandle(SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync)
        {
            if (handle.IsInvalid)
                throw new ArgumentException(SR.Arg_InvalidHandle, nameof(handle));

            if (access < FileAccess.Read || access > FileAccess.ReadWrite)
                throw new ArgumentOutOfRangeException(nameof(access), SR.ArgumentOutOfRange_Enum);
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), SR.ArgumentOutOfRange_NeedPosNum);

            InitFromHandleInternal(handle, access, bufferSize, isAsync);
        }

        // InternalOpen, InternalCreate, and InternalAppend:
        // Factory methods for FileStream used by File, FileInfo, and ReadLinesIterator
        // Specifies default access and sharing options for FileStreams created by those classes
        internal static FileStream InternalOpen(string path, int bufferSize = DefaultBufferSize, bool useAsync = DefaultIsAsync)
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync);
        }

        internal static FileStream InternalCreate(string path, int bufferSize = DefaultBufferSize, bool useAsync = DefaultIsAsync)
        {
            return new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize, useAsync);
        }

        internal static FileStream InternalAppend(string path, int bufferSize = DefaultBufferSize, bool useAsync = DefaultIsAsync)
        {
            return new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read, bufferSize, useAsync);
        }

        public virtual IntPtr Handle { get { return SafeFileHandle.DangerousGetHandle(); } }

        public override void Lock(long position, long length)
        {
            if (position < 0 || length < 0)
            {
                throw new ArgumentOutOfRangeException(position < 0 ? nameof(position) : nameof(length), SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            LockInternal(position, length);
        }

        public override void Unlock(long position, long length)
        {
            if (position < 0 || length < 0)
            {
                throw new ArgumentOutOfRangeException(position < 0 ? nameof(position) : nameof(length), SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            UnlockInternal(position, length);
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Flush() which a subclass might have overridden.  To be safe 
            // we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Flush) when we are not sure.
            if (GetType() != typeof(FileStream))
                return base.FlushAsync(cancellationToken);

            return FlushAsyncInternal(cancellationToken);
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

            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Read() or ReadAsync() which a subclass might have overridden.  
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Read/ReadAsync) when we are not sure.
            if (GetType() != typeof(FileStream))
                return base.ReadAsync(buffer, offset, count, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<int>(cancellationToken);

            if (IsClosed)
                throw Error.GetFileNotOpen();

            return ReadAsyncInternal(buffer, offset, count, cancellationToken);
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

            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Write() or WriteAsync() which a subclass might have overridden.  
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Write/WriteAsync) when we are not sure.
            if (GetType() != typeof(FileStream))
                return base.WriteAsync(buffer, offset, count, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            if (IsClosed)
                throw Error.GetFileNotOpen();

            return WriteAsyncInternal(buffer, offset, count, cancellationToken);
        }

        /// <summary>
        /// Clears buffers for this stream and causes any buffered data to be written to the file.
        /// </summary>
        public override void Flush()
        {
            // Make sure that we call through the public virtual API
            Flush(flushToDisk: false);
        }

        /// <summary>
        /// Clears buffers for this stream, and if <param name="flushToDisk"/> is true, 
        /// causes any buffered data to be written to the file.
        /// </summary>
        public override void Flush(bool flushToDisk)
        {
            if (IsClosed) throw Error.GetFileNotOpen();

            FlushInternalBuffer();

            if (flushToDisk && CanWrite)
            {
                FlushOSBuffer();
            }
        }

        ~FileStream()
        {
            // Preserved for compatibility since FileStream has defined a 
            // finalizer in past releases and derived classes may depend
            // on Dispose(false) call.
            Dispose(false);
        }
    }
}
