// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
 *
// 
// 
 *
 *
 * Purpose: Provides access to files using the same interface as FileStream
 *
 *
 ===========================================================*/
namespace System.IO.IsolatedStorage {
    using System;
    using System.IO;
    using Microsoft.Win32;
    using Microsoft.Win32.SafeHandles;
    using System.Security;
    using System.Security.Permissions;
    using System.Threading;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

[System.Runtime.InteropServices.ComVisible(true)]
    public class IsolatedStorageFileStream : FileStream
    {
        private const int    s_BlockSize = 1024;    // Should be a power of 2!
                                                    // see usage before 
                                                    // changing this constant
#if !FEATURE_PAL
        private const String s_BackSlash = "\\";
#else
        // s_BackSlash is initialized in the contructor with Path.DirectorySeparatorChar
        private readonly String s_BackSlash;
#endif // !FEATURE_PAL

        private FileStream m_fs;
        private IsolatedStorageFile m_isf;
        private String m_GivenPath;
        private String m_FullPath;
        private bool   m_OwnedStore;

        private IsolatedStorageFileStream() {}

#if !FEATURE_ISOSTORE_LIGHT
        public IsolatedStorageFileStream(String path, FileMode mode) 
            : this(path, mode, (mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite), FileShare.None, null) {
        }
#endif // !FEATURE_ISOSTORE_LIGHT    
        public IsolatedStorageFileStream(String path, FileMode mode,
                IsolatedStorageFile isf)
            : this(path, mode, (mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite), FileShare.None, isf)
        {
        }
    
#if !FEATURE_ISOSTORE_LIGHT    
        public IsolatedStorageFileStream(String path, FileMode mode, 
                FileAccess access) 
            : this(path, mode, access, access == FileAccess.Read?
                FileShare.Read: FileShare.None, DefaultBufferSize, null) {
        }
#endif // !FEATURE_ISOSTORE_LIGHT    

        public IsolatedStorageFileStream(String path, FileMode mode, 
                FileAccess access, IsolatedStorageFile isf) 
            : this(path, mode, access, access == FileAccess.Read?
                FileShare.Read: FileShare.None, DefaultBufferSize, isf) {
        }

#if !FEATURE_ISOSTORE_LIGHT    
        public IsolatedStorageFileStream(String path, FileMode mode, 
                FileAccess access, FileShare share) 
            : this(path, mode, access, share, DefaultBufferSize, null) {
        }
#endif // !FEATURE_ISOSTORE_LIGHT    

        public IsolatedStorageFileStream(String path, FileMode mode, 
                FileAccess access, FileShare share, IsolatedStorageFile isf) 
            : this(path, mode, access, share, DefaultBufferSize, isf) {
        }

#if !FEATURE_ISOSTORE_LIGHT            
        public IsolatedStorageFileStream(String path, FileMode mode, 
                FileAccess access, FileShare share, int bufferSize) 
            : this(path, mode, access, share, bufferSize, null) {
        }
#endif // !FEATURE_ISOSTORE_LIGHT    

        // If the isolated storage file is null, then we default to using a file 
        // that is scoped by user, appdomain, and assembly.
        [System.Security.SecuritySafeCritical]  // auto-generated
        public IsolatedStorageFileStream(String path, FileMode mode, 
            FileAccess access, FileShare share, int bufferSize,  
            IsolatedStorageFile isf) 
        {
            if (path == null)
                throw new ArgumentNullException("path");
            Contract.EndContractBlock();

#if FEATURE_PAL
            if (s_BackSlash == null)
                s_BackSlash = new String(System.IO.Path.DirectorySeparatorChar,1);
#endif // FEATURE_PAL           

            if ((path.Length == 0) || path.Equals(s_BackSlash))
                throw new ArgumentException(
                    Environment.GetResourceString(
                        "IsolatedStorage_Path"));
 
            if (isf == null)
            {
#if FEATURE_ISOSTORE_LIGHT
                throw new ArgumentNullException("isf");
#else // !FEATURE_ISOSTORE_LIGHT
                m_OwnedStore = true;
                isf = IsolatedStorageFile.GetUserStoreForDomain();
#endif // !FEATURE_ISOSTORE_LIGHT                    
            }

            if (isf.Disposed)
                throw new ObjectDisposedException(null, Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));

            switch (mode) {

                case FileMode.CreateNew:        // Assume new file   
                case FileMode.Create:           // Check for New file & Unreserve
                case FileMode.OpenOrCreate:     // Check for new file
                case FileMode.Truncate:         // Unreserve old file size
                case FileMode.Append:           // Check for new file
                case FileMode.Open:             // Open existing, else exception
                    break;
    
                default:
                    throw new ArgumentException(Environment.GetResourceString("IsolatedStorage_FileOpenMode"));
            }

            m_isf = isf;

#if !FEATURE_CORECLR
            FileIOPermission fiop = 
                new FileIOPermission(FileIOPermissionAccess.AllAccess,
                    m_isf.RootDirectory);

            fiop.Assert();
            fiop.PermitOnly();
#endif

            m_GivenPath = path;
            m_FullPath  = m_isf.GetFullPath(m_GivenPath);

#if FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
            ulong oldFileSize=0, newFileSize;
            bool fNewFile = false, fLock=false;

            RuntimeHelpers.PrepareConstrainedRegions();
            try { // for finally Unlocking locked store

                // Cache the old file size if the file size could change
                // Also find if we are going to create a new file.

                switch (mode) {
                    case FileMode.CreateNew:        // Assume new file
#if FEATURE_ISOSTORE_LIGHT
                        // We are going to call Reserve so we need to lock the store.
                        m_isf.Lock(ref fLock);
#endif
                        fNewFile = true;
                        break;
    
                    case FileMode.Create:           // Check for New file & Unreserve
                    case FileMode.OpenOrCreate:     // Check for new file
                    case FileMode.Truncate:         // Unreserve old file size
                    case FileMode.Append:           // Check for new file
    
                        m_isf.Lock(ref fLock);      // oldFileSize needs to be 
                                                // protected

                        try {
#if FEATURE_ISOSTORE_LIGHT
                            oldFileSize = IsolatedStorageFile.RoundToBlockSize((ulong)(FileInfo.UnsafeCreateFileInfo(m_FullPath).Length));
#else
                            oldFileSize = IsolatedStorageFile.RoundToBlockSize((ulong)LongPathFile.GetLength(m_FullPath));
#endif
                        } catch (FileNotFoundException) {
                            fNewFile = true;
                        } catch {
    
                        }
    
                        break;
    
                    case FileMode.Open:             // Open existing, else exception
                        break;
    
                }
    
                if (fNewFile)
                    m_isf.ReserveOneBlock();

#endif // FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
    
                try {

#if FEATURE_CORECLR
                    // Since FileStream's .ctor won't do a demand, we need to do our access check here.
                    m_isf.Demand(m_FullPath);
#endif

#if FEATURE_ISOSTORE_LIGHT
                    m_fs = new
                        FileStream(m_FullPath, mode, access, share, bufferSize, 
                            FileOptions.None, m_GivenPath, true);

                } catch (Exception e) {

#else
                        m_fs = new
                        FileStream(m_FullPath, mode, access, share, bufferSize,
                            FileOptions.None, m_GivenPath, true, true);

                } catch {

#endif
#if FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT

                    if (fNewFile)
                        m_isf.UnreserveOneBlock();
#endif // FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
#if FEATURE_ISOSTORE_LIGHT
                    // IsoStore generally does not let arbitrary exceptions flow out: a
                    // IsolatedStorageException is thrown instead (see examples in IsolatedStorageFile.cs
                    // Keeping this scoped to coreclr just because changing the exception type thrown is a
                    // breaking change and that should not be introduced into the desktop without deliberation.
                    //
                    // Note that GetIsolatedStorageException may set InnerException. To the real exception
                    // Today it always does this, for debug and chk builds, and for other builds asks the host 
                    // if it is okay to do so.
                    throw IsolatedStorageFile.GetIsolatedStorageException("IsolatedStorage_Operation_ISFS", e);
#else
                    throw;
#endif // FEATURE_ISOSTORE_LIGHT
                }

#if FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT    
                // make adjustment to the Reserve / Unreserve state
    
                if ((fNewFile == false) &&
                    ((mode == FileMode.Truncate) || (mode == FileMode.Create)))
                {
                    newFileSize = IsolatedStorageFile.RoundToBlockSize((ulong)m_fs.Length);
        
                    if (oldFileSize > newFileSize)
                        m_isf.Unreserve(oldFileSize - newFileSize);
                    else if (newFileSize > oldFileSize)     // Can this happen ?
                        m_isf.Reserve(newFileSize - oldFileSize);
                }

            } finally {
                if (fLock)
                    m_isf.Unlock();
            }
#endif // FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT

#if !FEATURE_CORECLR
            CodeAccessPermission.RevertAll();
#endif
            
        }

        public override bool CanRead {
            [Pure]
            get {
                return m_fs.CanRead; 
            }
        }

        public override bool CanWrite {
            [Pure]
            get {
                return m_fs.CanWrite; 
            }
        }

        public override bool CanSeek {
            [Pure]
            get {
                return m_fs.CanSeek; 
            }
        }

        public override bool IsAsync {
            get {
                return m_fs.IsAsync; 
            }
        }

        public override long Length {
            get {
                return m_fs.Length; 
            }
        }

        public override long Position {

            get {
                return m_fs.Position; 
            }

            set {

                if (value < 0) 
                {
                    throw new ArgumentOutOfRangeException("value", 
                        Environment.GetResourceString(
                            "ArgumentOutOfRange_NeedNonNegNum"));
                }
                Contract.EndContractBlock();

                Seek(value, SeekOrigin.Begin);
            }
        }

#if FEATURE_LEGACYNETCF
        public new string Name {
            [SecurityCritical]
            get {
                return m_FullPath;
            }
        }
#endif

#if false
        unsafe private static void AsyncFSCallback(uint errorCode, 
                uint numBytes, NativeOverlapped* pOverlapped) {
            NotPermittedError();
        }
#endif

        protected override void Dispose(bool disposing)
        {
            try {
                if (disposing) {
                    try {
                        if (m_fs != null)
                            m_fs.Close();
                    }
                    finally {
                        if (m_OwnedStore && m_isf != null)
                            m_isf.Close();
                    }
                }
            }
            finally {
                base.Dispose(disposing);
            }
        }

        public override void Flush() {
            m_fs.Flush();
        }

        public override void Flush(Boolean flushToDisk) {
            m_fs.Flush(flushToDisk);
        }

        [Obsolete("This property has been deprecated.  Please use IsolatedStorageFileStream's SafeFileHandle property instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public override IntPtr Handle {
            [System.Security.SecurityCritical]  // auto-generated_required
#if !FEATURE_CORECLR
            [SecurityPermissionAttribute(SecurityAction.InheritanceDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
#endif
            get {
                NotPermittedError();
                return Win32Native.INVALID_HANDLE_VALUE;
            }
        }

        public override SafeFileHandle SafeFileHandle {
            [System.Security.SecurityCritical]  // auto-generated_required
#if !FEATURE_CORECLR
            [SecurityPermissionAttribute(SecurityAction.InheritanceDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
#endif
            get {
                NotPermittedError();
                return null;
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override void SetLength(long value) 
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();

#if FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
            bool locked = false;

            RuntimeHelpers.PrepareConstrainedRegions();
            try {
                m_isf.Lock(ref locked); // oldLen needs to be protected
#endif // FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
                
                ulong oldLen = (ulong)m_fs.Length;
                ulong newLen = (ulong)value;
    
#if FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
                // Reserve before the operation.
                m_isf.Reserve(oldLen, newLen);
    
                try {
#endif // FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
    
                    ZeroInit(oldLen, newLen);
    
                    m_fs.SetLength(value);

#if FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT    
                } catch {
    
                    // Undo the reserve
                    m_isf.UndoReserveOperation(oldLen, newLen);
    
                    throw;
                }
    
                // Unreserve if this operation reduced the file size.
                if (oldLen > newLen)
                {
                    // params oldlen, newlength reversed on purpose.
                    m_isf.UndoReserveOperation(newLen, oldLen);
                }

            } finally {
                if (locked)
                m_isf.Unlock();
            }
#endif // FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
        }

        public override void Lock(long position, long length)
        {
            if (position < 0 || length < 0)
                throw new ArgumentOutOfRangeException((position < 0 ? "position" : "length"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();

            m_fs.Lock(position, length);
        }

        public override void Unlock(long position, long length)
        {
            if (position < 0 || length < 0)
                throw new ArgumentOutOfRangeException((position < 0 ? "position" : "length"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();

            m_fs.Unlock(position, length);
        }

        // 0 out the allocated disk so that 
        // untrusted apps won't be able to read garbage, which
        // is a security  hole, if allowed.
        // This may not be necessary in some file systems ?
        private void ZeroInit(ulong oldLen, ulong newLen)
        {
            if (oldLen >= newLen)
                return;

            ulong    rem  = newLen - oldLen;
            byte[] buffer = new byte[s_BlockSize];  // buffer is zero inited 
                                                    // here by the runtime 
                                                    // memory allocator.

            // back up the current position.
            long pos      = m_fs.Position;

            m_fs.Seek((long)oldLen, SeekOrigin.Begin);

            // If we have a small number of bytes to write, do that and
            // we are done.
            if (rem <= (ulong)s_BlockSize)
            {
                m_fs.Write(buffer, 0, (int)rem);
                m_fs.Position = pos;
                return;
            }

            // Block write is better than writing a byte in a loop
            // or all bytes. The number of bytes to write could
            // be very large.

            // Align to block size
            // allign = s_BlockSize - (int)(oldLen % s_BlockSize);
            // Converting % to & operation since s_BlockSize is a power of 2

            int allign = s_BlockSize - (int)(oldLen & ((ulong)s_BlockSize - 1));

            /* 
                this will never happen since we already handled this case
                leaving this code here for documentation
            if ((ulong)allign > rem)
                allign = (int)rem;
            */

            m_fs.Write(buffer, 0, allign);
            rem -= (ulong)allign;

            int nBlocks = (int)(rem / s_BlockSize);

            // Write out one block at a time.
            for (int i=0; i<nBlocks; ++i)
                m_fs.Write(buffer, 0, s_BlockSize);

            // Write out the remaining bytes.
            // m_fs.Write(buffer, 0, (int) (rem % s_BlockSize));
            // Converting % to & operation since s_BlockSize is a power of 2
            m_fs.Write(buffer, 0, (int) (rem & ((ulong)s_BlockSize - 1)));

            // restore the current position
            m_fs.Position = pos;
        }

        public override int Read(byte[] buffer, int offset, int count) {
            return m_fs.Read(buffer, offset, count);
        }

        public override int ReadByte() {
            return m_fs.ReadByte();
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override long Seek(long offset, SeekOrigin origin) 
        {
            long  ret;

#if FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
            bool locked = false;

            RuntimeHelpers.PrepareConstrainedRegions();
            try {
                m_isf.Lock(ref locked); // oldLen needs to be protected
#endif // FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
                    
                // Seek operation could increase the file size, make sure
                // that the quota is updated, and file is zeroed out

                ulong oldLen;
                ulong newLen;
                oldLen = (ulong) m_fs.Length;
                // Note that offset can be negative too.

                switch (origin) {
                case SeekOrigin.Begin:
                    newLen = (ulong)((offset < 0)?0:offset);
                    break;
                case SeekOrigin.Current:
                    newLen = (ulong) ((m_fs.Position + offset) < 0 ? 0 : (m_fs.Position + offset));
                    break;
                case SeekOrigin.End:
                    newLen = (ulong)((m_fs.Length + offset) < 0 ? 0 : (m_fs.Length + offset));
                    break;
                default:
                    throw new ArgumentException(
                        Environment.GetResourceString(
                            "IsolatedStorage_SeekOrigin"));
                }
#if FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT

                m_isf.Reserve(oldLen, newLen);

                try {
#endif // FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT

                    ZeroInit(oldLen, newLen);

                    ret = m_fs.Seek(offset, origin);

#if FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
                } catch {

                    m_isf.UndoReserveOperation(oldLen, newLen);

                    throw;
                }
            }
            finally
            {
                if (locked)
                m_isf.Unlock();
            }
#endif // FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT

            return ret;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override void Write(byte[] buffer, int offset, int count) 
        {

#if FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
            bool locked = false;

            RuntimeHelpers.PrepareConstrainedRegions();
            try {
                m_isf.Lock(ref locked); // oldLen needs to be protected
                    
                ulong oldLen = (ulong)m_fs.Length;
                ulong newLen = (ulong)(m_fs.Position + count);

                m_isf.Reserve(oldLen, newLen);

                try {
#endif // FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT

                    m_fs.Write(buffer, offset, count);

#if FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT

                } catch {

                    m_isf.UndoReserveOperation(oldLen, newLen);

                    throw;
                }
            }
            finally
            {
                if (locked)
                m_isf.Unlock();
            }
#endif
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override void WriteByte(byte value)
        {

#if FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
            bool locked = false;

            RuntimeHelpers.PrepareConstrainedRegions();
            try {
                m_isf.Lock(ref locked); // oldLen needs to be protected
                
                ulong oldLen = (ulong)m_fs.Length;
                ulong newLen = (ulong)m_fs.Position + 1;

                m_isf.Reserve(oldLen, newLen);

                try {
#endif // FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
                   
                    m_fs.WriteByte(value);

#if FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
                } catch {

                    m_isf.UndoReserveOperation(oldLen, newLen);

                    throw;
                }
            }
            finally {
                if (locked)
                m_isf.Unlock(); 
            }
#endif // FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT

        }

        [HostProtection(ExternalThreading=true)]
        public override IAsyncResult BeginRead(byte[] buffer, int offset, 
            int numBytes, AsyncCallback userCallback, Object stateObject) {

            return m_fs.BeginRead(buffer, offset, numBytes, userCallback, stateObject);
        }

        public override int EndRead(IAsyncResult asyncResult) {
            if (asyncResult == null)
                throw new ArgumentNullException("asyncResult");
            Contract.EndContractBlock();

            // try-catch to avoid leaking path info
            return m_fs.EndRead(asyncResult);
                
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [HostProtection(ExternalThreading=true)]
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, 
            int numBytes, AsyncCallback userCallback, Object stateObject) {

#if FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
            bool locked = false;

            RuntimeHelpers.PrepareConstrainedRegions();
            try {
                m_isf.Lock(ref locked); // oldLen needs to be protected
                    
                ulong oldLen = (ulong)m_fs.Length;
                ulong newLen = (ulong)m_fs.Position + (ulong)numBytes;
                m_isf.Reserve(oldLen, newLen);

                try {
#endif // FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT                    
                    return m_fs.BeginWrite(buffer, offset, numBytes, userCallback, stateObject);
#if FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
                } catch {

                    m_isf.UndoReserveOperation(oldLen, newLen);

                    throw;
                }
            }
            finally
            {
                if(locked)
                m_isf.Unlock();
            }
#endif // FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
        }

        public override void EndWrite(IAsyncResult asyncResult) {
            if (asyncResult == null)
                throw new ArgumentNullException("asyncResult");
            Contract.EndContractBlock();

            m_fs.EndWrite(asyncResult);
        }

        internal void NotPermittedError(String str) {
            throw new IsolatedStorageException(str);
        }

        internal void NotPermittedError() {
            NotPermittedError(Environment.GetResourceString(
                "IsolatedStorage_Operation_ISFS"));
        }

    }
}

