// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** Purpose: Exposes a separate Stream for Console IO and 
** handles WinCE appropriately.  Also keeps us from using the
** ThreadPool for all Console output.
**
**
===========================================================*/

using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Diagnostics.Contracts;

namespace System.IO {

    internal sealed class __ConsoleStream : Stream
    {

        // We know that if we are using console APIs rather than file APIs, then the encoding
        // is Encoding.Unicode implying 2 bytes per character:                
        const int BytesPerWChar = 2;

        [System.Security.SecurityCritical] // auto-generated
        private SafeFileHandle _handle;
        private bool _canRead;
        private bool _canWrite;

        private bool _useFileAPIs;
        private bool _isPipe;  // When reading from pipes, we need to properly handle EOF cases.

        [System.Security.SecurityCritical]  // auto-generated
        internal __ConsoleStream(SafeFileHandle handle, FileAccess access, bool useFileAPIs)
        {
            Contract.Assert(handle != null && !handle.IsInvalid, "__ConsoleStream expects a valid handle!");
            _handle = handle;
            _canRead = ( (access & FileAccess.Read) == FileAccess.Read );
            _canWrite = ( (access & FileAccess.Write) == FileAccess.Write);
            _useFileAPIs = useFileAPIs;
            _isPipe = Win32Native.GetFileType(handle) == Win32Native.FILE_TYPE_PIPE;
        }
    
        public override bool CanRead {
            [Pure]
            get { return _canRead; }
        }

        public override bool CanWrite {
            [Pure]
            get { return _canWrite; }
        }

        public override bool CanSeek {
            [Pure]
            get { return false; }
        }

        public override long Length {
            get {
                __Error.SeekNotSupported();
                return 0; // compiler appeasement
            }
        }

        public override long Position {
            get { 
                __Error.SeekNotSupported();
                return 0; // compiler appeasement
            }
            set {
                __Error.SeekNotSupported();
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        protected override void Dispose(bool disposing)
        {
            // We're probably better off not closing the OS handle here.  First,
            // we allow a program to get multiple instances of __ConsoleStreams
            // around the same OS handle, so closing one handle would invalidate
            // them all.  Additionally, we want a second AppDomain to be able to 
            // write to stdout if a second AppDomain quits.
            if (_handle != null) {
                _handle = null;
            }
            _canRead = false;
            _canWrite = false;
            base.Dispose(disposing);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override void Flush()
        {
            if (_handle == null) __Error.FileNotOpen();
            if (!CanWrite) __Error.WriteNotSupported();
        }

        public override void SetLength(long value)
        {
            __Error.SeekNotSupported();
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override int Read([In, Out] byte[] buffer, int offset, int count) {
            if (buffer==null)
                throw new ArgumentNullException("buffer");
            if (offset < 0 || count < 0)
                throw new ArgumentOutOfRangeException((offset < 0 ? "offset" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - offset < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();
            if (!_canRead) __Error.ReadNotSupported();

            int bytesRead;
            int errCode = ReadFileNative(_handle, buffer, offset, count, _useFileAPIs, _isPipe, out bytesRead);

            if (Win32Native.ERROR_SUCCESS != errCode)
                __Error.WinIOError(errCode, String.Empty);

            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin) {
            __Error.SeekNotSupported();
            return 0; // compiler appeasement
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override void Write(byte[] buffer, int offset, int count) {
            if (buffer==null)
                throw new ArgumentNullException("buffer");
            if (offset < 0 || count < 0)
                throw new ArgumentOutOfRangeException((offset < 0 ? "offset" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - offset < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();
            if (!_canWrite) __Error.WriteNotSupported();

            int errCode = WriteFileNative(_handle, buffer, offset, count, _useFileAPIs);

            if (Win32Native.ERROR_SUCCESS != errCode)                 
                __Error.WinIOError(errCode, String.Empty);

            return;
        }

        // P/Invoke wrappers for writing to and from a file, nearly identical
        // to the ones on FileStream.  These are duplicated to save startup/hello
        // world working set.
        [System.Security.SecurityCritical]  // auto-generated
        private unsafe static int ReadFileNative(SafeFileHandle hFile, byte[] bytes, int offset, int count, bool useFileAPIs, bool isPipe, out int bytesRead) {

            Contract.Requires(offset >= 0, "offset >= 0");
            Contract.Requires(count >= 0, "count >= 0");
            Contract.Requires(bytes != null, "bytes != null");
            // Don't corrupt memory when multiple threads are erroneously writing
            // to this stream simultaneously.
            if (bytes.Length - offset < count)
                throw new IndexOutOfRangeException(Environment.GetResourceString("IndexOutOfRange_IORaceCondition"));
            Contract.EndContractBlock();

            // You can't use the fixed statement on an array of length 0.
            if (bytes.Length == 0) {
                bytesRead = 0;
                return Win32Native.ERROR_SUCCESS;
            }

            // First, wait bytes to become available.  This is preferable to letting ReadFile block,
            // since ReadFile is not abortable (via Thread.Abort), while WaitForAvailableConsoleInput is.

#if !FEATURE_CORESYSTEM // CoreSystem isn't signaling stdin when input is available so we can't block on it
            WaitForAvailableConsoleInput(hFile, isPipe);
#endif

            bool readSuccess;

            if (useFileAPIs) {

                fixed (byte* p = bytes) {
                    readSuccess = (0 != Win32Native.ReadFile(hFile, p + offset, count, out bytesRead, IntPtr.Zero));
                }

            } else {

                fixed (byte* p = bytes) {
                    int charsRead;
                    readSuccess = Win32Native.ReadConsoleW(hFile, p + offset, count / BytesPerWChar, out charsRead, IntPtr.Zero);
                    bytesRead = charsRead * BytesPerWChar;
                }

            }

            if (readSuccess)
                return Win32Native.ERROR_SUCCESS;

            int errorCode = Marshal.GetLastWin32Error();

            // For pipes that are closing or broken, just stop.
            // (E.g. ERROR_NO_DATA ("pipe is being closed") is returned when we write to a console that is closing;
            // ERROR_BROKEN_PIPE ("pipe was closed") is returned when stdin was closed, which is mot an error, but EOF.)
            if (errorCode == Win32Native.ERROR_NO_DATA || errorCode == Win32Native.ERROR_BROKEN_PIPE)
                return Win32Native.ERROR_SUCCESS;
            return errorCode;
        }

        [System.Security.SecurityCritical]  // auto-generated
        private static unsafe int WriteFileNative(SafeFileHandle hFile, byte[] bytes, int offset, int count, bool useFileAPIs) {

            Contract.Requires(offset >= 0, "offset >= 0");
            Contract.Requires(count >= 0, "count >= 0");
            Contract.Requires(bytes != null, "bytes != null");
            Contract.Requires(bytes.Length >= offset + count, "bytes.Length >= offset + count");

            // You can't use the fixed statement on an array of length 0.
            if (bytes.Length == 0)             
                return Win32Native.ERROR_SUCCESS;
          
            bool writeSuccess;

            if (useFileAPIs) {

                fixed (byte* p = bytes) {
                    int numBytesWritten;
                    writeSuccess = (0 != Win32Native.WriteFile(hFile, p + offset, count, out numBytesWritten, IntPtr.Zero));
                    Contract.Assert(!writeSuccess || count == numBytesWritten);
                }

            } else {

                // Note that WriteConsoleW has a max limit on num of chars to write (64K)
                // [http://msdn.microsoft.com/en-us/library/ms687401.aspx]
                // However, we do not need to worry about that becasue the StreamWriter in Console has
                // a much shorter buffer size anyway.
                fixed (byte* p = bytes) {
                    Int32 charsWritten;
                    writeSuccess = Win32Native.WriteConsoleW(hFile, p + offset, count / BytesPerWChar, out charsWritten, IntPtr.Zero);
                    Contract.Assert(!writeSuccess || count / BytesPerWChar == charsWritten);
                }
            }

            if (writeSuccess)
                return Win32Native.ERROR_SUCCESS;

            int errorCode = Marshal.GetLastWin32Error();

            // For pipes that are closing or broken, just stop.
            // (E.g. ERROR_NO_DATA ("pipe is being closed") is returned when we write to a console that is closing;
            // ERROR_BROKEN_PIPE ("pipe was closed") is returned when stdin was closed, which is mot an error, but EOF.)
            if (errorCode == Win32Native.ERROR_NO_DATA || errorCode == Win32Native.ERROR_BROKEN_PIPE)
                return Win32Native.ERROR_SUCCESS;
            return errorCode;            
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void WaitForAvailableConsoleInput(SafeFileHandle file, bool isPipe);
    }
}
