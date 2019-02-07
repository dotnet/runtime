// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;
using System.Buffers;

namespace System.Data.SqlTypes
{
    public sealed partial class SqlFileStream : System.IO.Stream
    {
        // NOTE: if we ever unseal this class, be sure to specify the Name, SafeFileHandle, and 
        // TransactionContext accessors as virtual methods. Doing so now on a sealed class
        // generates a compiler error (CS0549)

        // from System.IO.FileStream implementation
        // DefaultBufferSize = 4096;
        // SQLBUVSTS# 193123 - disable lazy flushing of written data in order to prevent
        // potential exceptions during Close/Finalization. Since System.IO.FileStream will
        // not allow for a zero byte buffer, we'll create a one byte buffer which, in normal
        // usage, will not be used and the user buffer will automatically flush directly to 
        // the disk cache. In pathological scenarios where the client is writing a single 
        // byte at a time, we'll explicitly call flush ourselves.
        internal const int DefaultBufferSize = 1;

        private const ushort IoControlCodeFunctionCode = 2392;
        private const int ERROR_MR_MID_NOT_FOUND = 317;
        #region Definitions from devioctl.h
        private const ushort FILE_DEVICE_FILE_SYSTEM = 0x0009;
        #endregion

        private System.IO.FileStream _m_fs;
        private string _m_path;
        private byte[] _m_txn;
        private bool _m_disposed;
        private static byte[] s_eaNameString = new byte[]
        {
            (byte)'F', (byte)'i', (byte)'l', (byte)'e', (byte)'s', (byte)'t', (byte)'r', (byte)'e', (byte)'a', (byte)'m', (byte)'_',
            (byte)'T', (byte)'r', (byte)'a', (byte)'n', (byte)'s', (byte)'a', (byte)'c', (byte)'t', (byte)'i', (byte)'o', (byte)'n', (byte)'_',
            (byte)'T', (byte)'a', (byte)'g', (byte) '\0'
        };

        public SqlFileStream(string path, byte[] transactionContext, FileAccess access) :
            this(path, transactionContext, access, FileOptions.None, 0)
        { }

        public SqlFileStream(string path, byte[] transactionContext, FileAccess access, FileOptions options, long allocationSize)
        {
            //-----------------------------------------------------------------
            // precondition validation

            if (transactionContext == null)
                throw ADP.ArgumentNull("transactionContext");

            if (path == null)
                throw ADP.ArgumentNull("path");

            //-----------------------------------------------------------------

            _m_disposed = false;
            _m_fs = null;

            OpenSqlFileStream(path, transactionContext, access, options, allocationSize);

            // only set internal state once the file has actually been successfully opened
            Name = path;
            TransactionContext = transactionContext;
        }

        #region destructor/dispose code

        // NOTE: this destructor will only be called only if the Dispose
        // method is not called by a client, giving the class a chance
        // to finalize properly (i.e., free unmanaged resources)
        ~SqlFileStream()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!_m_disposed)
                {
                    try
                    {
                        if (disposing)
                        {
                            if (_m_fs != null)
                            {
                                _m_fs.Close();
                                _m_fs = null;
                            }
                        }
                    }
                    finally
                    {
                        _m_disposed = true;
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
        #endregion

        public string Name
        {
            get
            {
                // assert that path has been properly processed via GetFullPathInternal
                // (e.g. m_path hasn't been set directly)
                AssertPathFormat(_m_path);
                return _m_path;
            }
            private set
            {
                // should be validated by callers of this method
                Debug.Assert(value != null);
                Debug.Assert(!_m_disposed);

                _m_path = GetFullPathInternal(value);
            }
        }

        public byte[] TransactionContext
        {
            get
            {
                if (_m_txn == null)
                    return null;

                return (byte[])_m_txn.Clone();
            }
            private set
            {
                // should be validated by callers of this method
                Debug.Assert(value != null);
                Debug.Assert(!_m_disposed);

                _m_txn = (byte[])value.Clone();
            }
        }

        #region System.IO.Stream methods

        public override bool CanRead
        {
            get
            {
                if (_m_disposed)
                    throw ADP.ObjectDisposed(this);

                return _m_fs.CanRead;
            }
        }

        // If CanSeek is false, Position, Seek, Length, and SetLength should throw.
        public override bool CanSeek
        {
            get
            {
                if (_m_disposed)
                    throw ADP.ObjectDisposed(this);

                return _m_fs.CanSeek;
            }
        }

        public override bool CanTimeout
        {
            get
            {
                if (_m_disposed)
                    throw ADP.ObjectDisposed(this);

                return _m_fs.CanTimeout;
            }
        }

        public override bool CanWrite
        {
            get
            {
                if (_m_disposed)
                    throw ADP.ObjectDisposed(this);

                return _m_fs.CanWrite;
            }
        }

        public override long Length
        {
            get
            {
                if (_m_disposed)
                    throw ADP.ObjectDisposed(this);

                return _m_fs.Length;
            }
        }

        public override long Position
        {
            get
            {
                if (_m_disposed)
                    throw ADP.ObjectDisposed(this);

                return _m_fs.Position;
            }
            set
            {
                if (_m_disposed)
                    throw ADP.ObjectDisposed(this);

                _m_fs.Position = value;
            }
        }

        public override int ReadTimeout
        {
            get
            {
                if (_m_disposed)
                    throw ADP.ObjectDisposed(this);

                return _m_fs.ReadTimeout;
            }
            set
            {
                if (_m_disposed)
                    throw ADP.ObjectDisposed(this);

                _m_fs.ReadTimeout = value;
            }
        }

        public override int WriteTimeout
        {
            get
            {
                if (_m_disposed)
                    throw ADP.ObjectDisposed(this);

                return _m_fs.WriteTimeout;
            }
            set
            {
                if (_m_disposed)
                    throw ADP.ObjectDisposed(this);

                _m_fs.WriteTimeout = value;
            }
        }

        public override void Flush()
        {
            if (_m_disposed)
                throw ADP.ObjectDisposed(this);

            _m_fs.Flush();
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            if (_m_disposed)
                throw ADP.ObjectDisposed(this);

            return _m_fs.BeginRead(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            if (_m_disposed)
                throw ADP.ObjectDisposed(this);

            return _m_fs.EndRead(asyncResult);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            if (_m_disposed)
                throw ADP.ObjectDisposed(this);

            IAsyncResult asyncResult = _m_fs.BeginWrite(buffer, offset, count, callback, state);

            // SQLBUVSTS# 193123 - disable lazy flushing of written data in order to prevent
            // potential exceptions during Close/Finalization. Since System.IO.FileStream will
            // not allow for a zero byte buffer, we'll create a one byte buffer which, in normal
            // usage, will not be used and the user buffer will automatically flush directly to 
            // the disk cache. In pathological scenarios where the client is writing a single 
            // byte at a time, we'll explicitly call flush ourselves.
            if (count == 1)
            {
                // calling flush here will mimic the internal control flow of System.IO.FileStream
                _m_fs.Flush();
            }

            return asyncResult;
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            if (_m_disposed)
                throw ADP.ObjectDisposed(this);

            _m_fs.EndWrite(asyncResult);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (_m_disposed)
                throw ADP.ObjectDisposed(this);

            return _m_fs.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            if (_m_disposed)
                throw ADP.ObjectDisposed(this);

            _m_fs.SetLength(value);
        }

        public override int Read([In, Out] byte[] buffer, int offset, int count)
        {
            if (_m_disposed)
                throw ADP.ObjectDisposed(this);

            return _m_fs.Read(buffer, offset, count);
        }

        public override int ReadByte()
        {
            if (_m_disposed)
                throw ADP.ObjectDisposed(this);

            return _m_fs.ReadByte();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_m_disposed)
                throw ADP.ObjectDisposed(this);

            _m_fs.Write(buffer, offset, count);

            // SQLBUVSTS# 193123 - disable lazy flushing of written data in order to prevent
            // potential exceptions during Close/Finalization. Since System.IO.FileStream will
            // not allow for a zero byte buffer, we'll create a one byte buffer which, in normal
            // usage, will cause System.IO.FileStream to utilize the user-supplied buffer and
            // automatically flush the data directly to the disk cache. In pathological scenarios 
            // where the user is writing a single byte at a time, we'll explicitly call flush ourselves.
            if (count == 1)
            {
                // calling flush here will mimic the internal control flow of System.IO.FileStream
                _m_fs.Flush();
            }
        }

        public override void WriteByte(byte value)
        {
            if (_m_disposed)
                throw ADP.ObjectDisposed(this);

            _m_fs.WriteByte(value);

            // SQLBUVSTS# 193123 - disable lazy flushing of written data in order to prevent
            // potential exceptions during Close/Finalization. Since our internal buffer is
            // only a single byte in length, the provided user data will always be cached.
            // As a result, we need to be sure to flush the data to disk ourselves.

            // calling flush here will mimic the internal control flow of System.IO.FileStream
            _m_fs.Flush();
        }

        #endregion

        [Conditional("DEBUG")]
        static private void AssertPathFormat(string path)
        {
            Debug.Assert(path != null);
            Debug.Assert(path == path.Trim());
            Debug.Assert(path.Length > 0);
            Debug.Assert(path.StartsWith(@"\\", StringComparison.OrdinalIgnoreCase));
        }

        static private string GetFullPathInternal(string path)
        {
            //-----------------------------------------------------------------
            // precondition validation should be validated by callers of this method
            // NOTE: if this method moves elsewhere, this assert should become an actual runtime check
            // as the implicit assumptions here cannot be relied upon in an inter-class context
            Debug.Assert(path != null);

            // remove leading and trailing whitespace
            path = path.Trim();
            if (path.Length == 0)
            {
                throw ADP.Argument(SR.GetString(SR.SqlFileStream_InvalidPath), "path");
            }

            // make sure path is not DOS device path
            if (!path.StartsWith(@"\\") && !System.IO.PathInternal.IsDevice(path.AsSpan()))
            {
                throw ADP.Argument(SR.GetString(SR.SqlFileStream_InvalidPath), "path");
            }

            // normalize the path
            path = System.IO.Path.GetFullPath(path);

            // make sure path is a UNC path
            if (System.IO.PathInternal.IsDeviceUNC(path.AsSpan()))
            {
                throw ADP.Argument(SR.GetString(SR.SqlFileStream_PathNotValidDiskResource), "path");
            }

            return path;
        }

        private unsafe void OpenSqlFileStream
            (
                string sPath,
                byte[] transactionContext,
                System.IO.FileAccess access,
                System.IO.FileOptions options,
                long allocationSize
            )
        {
            //-----------------------------------------------------------------
            // precondition validation
            // these should be checked by any caller of this method
            // ensure we have validated and normalized the path before
            Debug.Assert(sPath != null);
            Debug.Assert(transactionContext != null);

            if (access != System.IO.FileAccess.Read && access != System.IO.FileAccess.Write && access != System.IO.FileAccess.ReadWrite)
                throw ADP.ArgumentOutOfRange("access");

            // FileOptions is a set of flags, so AND the given value against the set of values we do not support
            if ((options & ~(System.IO.FileOptions.WriteThrough | System.IO.FileOptions.Asynchronous | System.IO.FileOptions.RandomAccess | System.IO.FileOptions.SequentialScan)) != 0)
                throw ADP.ArgumentOutOfRange("options");

            //-----------------------------------------------------------------
            // normalize the provided path
            // * compress path to remove any occurrences of '.' or '..'
            // * trim whitespace from the beginning and end of the path
            // * ensure that the path starts with '\\'
            // * ensure that the path does not start with '\\.\'
            sPath = GetFullPathInternal(sPath);

            Microsoft.Win32.SafeHandles.SafeFileHandle hFile = null;
            Interop.NtDll.DesiredAccess nDesiredAccess = Interop.NtDll.DesiredAccess.FILE_READ_ATTRIBUTES | Interop.NtDll.DesiredAccess.SYNCHRONIZE;
            Interop.NtDll.CreateOptions dwCreateOptions = 0;
            Interop.NtDll.CreateDisposition dwCreateDisposition = 0;
            System.IO.FileShare nShareAccess = System.IO.FileShare.None;

            switch (access)
            {
                case System.IO.FileAccess.Read:

                    nDesiredAccess |= Interop.NtDll.DesiredAccess.FILE_READ_DATA;
                    nShareAccess = System.IO.FileShare.Delete | System.IO.FileShare.ReadWrite;
                    dwCreateDisposition = Interop.NtDll.CreateDisposition.FILE_OPEN;
                    break;

                case System.IO.FileAccess.Write:
                    nDesiredAccess |= Interop.NtDll.DesiredAccess.FILE_WRITE_DATA;
                    nShareAccess = System.IO.FileShare.Delete | System.IO.FileShare.Read;
                    dwCreateDisposition = Interop.NtDll.CreateDisposition.FILE_OVERWRITE;
                    break;

                case System.IO.FileAccess.ReadWrite:
                default:
                    // we validate the value of 'access' parameter in the beginning of this method
                    Debug.Assert(access == System.IO.FileAccess.ReadWrite);

                    nDesiredAccess |= Interop.NtDll.DesiredAccess.FILE_READ_DATA | Interop.NtDll.DesiredAccess.FILE_WRITE_DATA;
                    nShareAccess = System.IO.FileShare.Delete | System.IO.FileShare.Read;
                    dwCreateDisposition = Interop.NtDll.CreateDisposition.FILE_OVERWRITE;
                    break;
            }

            if ((options & System.IO.FileOptions.WriteThrough) != 0)
            {
                dwCreateOptions |= Interop.NtDll.CreateOptions.FILE_WRITE_THROUGH;
            }

            if ((options & System.IO.FileOptions.Asynchronous) == 0)
            {
                dwCreateOptions |= Interop.NtDll.CreateOptions.FILE_SYNCHRONOUS_IO_NONALERT;
            }

            if ((options & System.IO.FileOptions.SequentialScan) != 0)
            {
                dwCreateOptions |= Interop.NtDll.CreateOptions.FILE_SEQUENTIAL_ONLY;
            }

            if ((options & System.IO.FileOptions.RandomAccess) != 0)
            {
                dwCreateOptions |= Interop.NtDll.CreateOptions.FILE_RANDOM_ACCESS;
            }

            try
            {
                // NOTE: the Name property is intended to reveal the publicly available moniker for the
                // FILESTREAM attributed column data. We will not surface the internal processing that
                // takes place to create the mappedPath.
                string mappedPath = InitializeNtPath(sPath);
                int retval = 0;
                Interop.Kernel32.SetThreadErrorMode(Interop.Kernel32.SEM_FAILCRITICALERRORS, out uint oldMode);

                try
                {
                    if (transactionContext.Length >= ushort.MaxValue)
                        throw ADP.ArgumentOutOfRange("transactionContext");

                    int headerSize = sizeof(Interop.NtDll.FILE_FULL_EA_INFORMATION);
                    int fullSize = headerSize + transactionContext.Length + s_eaNameString.Length;

                    byte[] buffer = ArrayPool<byte>.Shared.Rent(fullSize);

                    fixed (byte* b = buffer)
                    {
                        Interop.NtDll.FILE_FULL_EA_INFORMATION* ea = (Interop.NtDll.FILE_FULL_EA_INFORMATION*)b;
                        ea->NextEntryOffset = 0;
                        ea->Flags = 0;
                        ea->EaNameLength = (byte)(s_eaNameString.Length - 1); // Length does not include terminating null character.
                        ea->EaValueLength = (ushort)transactionContext.Length;

                        // We could continue to do pointer math here, chose to use Span for convenience to 
                        // make sure we get the other members in the right place.
                        Span<byte> data = buffer.AsSpan(headerSize);
                        s_eaNameString.AsSpan().CopyTo(data);
                        data = data.Slice(s_eaNameString.Length);
                        transactionContext.AsSpan().CopyTo(data);

                        (int status, IntPtr handle) = Interop.NtDll.CreateFile(
                                                                                path: mappedPath.AsSpan(),
                                                                                rootDirectory: IntPtr.Zero,
                                                                                createDisposition: dwCreateDisposition,
                                                                                desiredAccess: nDesiredAccess,
                                                                                shareAccess: nShareAccess,
                                                                                fileAttributes: 0,
                                                                                createOptions: dwCreateOptions,
                                                                                eaBuffer: b,
                                                                                eaLength: (uint)fullSize);
                        retval = status;
                        hFile = new SafeFileHandle(handle, true);
                    }

                    ArrayPool<byte>.Shared.Return(buffer);
                }
                finally
                {
                    Interop.Kernel32.SetThreadErrorMode(oldMode, out oldMode);
                }

                switch (retval)
                    {
                        case 0:
                            break;

                        case Interop.Errors.ERROR_SHARING_VIOLATION:
                            throw ADP.InvalidOperation(SR.GetString(SR.SqlFileStream_FileAlreadyInTransaction));

                        case Interop.Errors.ERROR_INVALID_PARAMETER:
                            throw ADP.Argument(SR.GetString(SR.SqlFileStream_InvalidParameter));

                        case Interop.Errors.ERROR_FILE_NOT_FOUND:
                            {
                                System.IO.DirectoryNotFoundException e = new System.IO.DirectoryNotFoundException();
                                ADP.TraceExceptionAsReturnValue(e);
                                throw e;
                            }
                        default:
                            {
                                uint error = Interop.NtDll.RtlNtStatusToDosError(retval);
                                if (error == ERROR_MR_MID_NOT_FOUND)
                                {
                                    // status code could not be mapped to a Win32 error code 
                                    error = (uint)retval;
                                }

                                System.ComponentModel.Win32Exception e = new System.ComponentModel.Win32Exception(unchecked((int)error));
                                ADP.TraceExceptionAsReturnValue(e);
                                throw e;
                            }
                    }

                    if (hFile.IsInvalid)
                    {
                        System.ComponentModel.Win32Exception e = new System.ComponentModel.Win32Exception(Interop.Errors.ERROR_INVALID_HANDLE);
                        ADP.TraceExceptionAsReturnValue(e);
                        throw e;
                    }

                    if (Interop.Kernel32.GetFileType(hFile) != Interop.Kernel32.FileTypes.FILE_TYPE_DISK)
                    {
                        hFile.Dispose();
                        throw ADP.Argument(SR.GetString(SR.SqlFileStream_PathNotValidDiskResource));
                    }

                    // if the user is opening the SQL FileStream in read/write mode, we assume that they want to scan
                    // through current data and then append new data to the end, so we need to tell SQL Server to preserve
                    // the existing file contents.
                    if (access == System.IO.FileAccess.ReadWrite)
                    {
                        uint ioControlCode = Interop.Kernel32.CTL_CODE(FILE_DEVICE_FILE_SYSTEM,
                            IoControlCodeFunctionCode, (byte)Interop.Kernel32.IoControlTransferType.METHOD_BUFFERED,
                            (byte)Interop.Kernel32.IoControlCodeAccess.FILE_ANY_ACCESS);

                        if (!Interop.Kernel32.DeviceIoControl(hFile, ioControlCode, IntPtr.Zero, 0, IntPtr.Zero, 0, out uint cbBytesReturned, IntPtr.Zero))
                        {
                            System.ComponentModel.Win32Exception e = new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                            ADP.TraceExceptionAsReturnValue(e);
                            throw e;
                        }
                    }

                    // now that we've successfully opened a handle on the path and verified that it is a file,
                    // use the SafeFileHandle to initialize our internal System.IO.FileStream instance
                    System.Diagnostics.Debug.Assert(_m_fs == null);
                    _m_fs = new System.IO.FileStream(hFile, access, DefaultBufferSize, ((options & System.IO.FileOptions.Asynchronous) != 0));
            }
            catch
            {
                if (hFile != null && !hFile.IsInvalid)
                    hFile.Dispose();

                throw;
            }
        }
        // This method exists to ensure that the requested path name is unique so that SMB/DNS is prevented
        // from collapsing a file open request to a file handle opened previously. In the SQL FILESTREAM case,
        // this would likely be a file open in another transaction, so this mechanism ensures isolation.
        static private string InitializeNtPath(string path)
        {
            // Ensure we have validated and normalized the path before
            AssertPathFormat(path);
            string uniqueId = Guid.NewGuid().ToString("N");
            return System.IO.PathInternal.IsDeviceUNC(path) ? string.Format(CultureInfo.InvariantCulture, @"{0}\{1}", path.Replace(@"\\.", @"\??"), uniqueId)
                                                            : string.Format(CultureInfo.InvariantCulture, @"\??\UNC\{0}\{1}", path.Trim('\\'), uniqueId);
        }
    }
}


