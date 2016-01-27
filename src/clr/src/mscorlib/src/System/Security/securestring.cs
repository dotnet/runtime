// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
namespace System.Security {
    using System.Security.Cryptography;
    using System.Runtime.InteropServices;
#if FEATURE_CORRUPTING_EXCEPTIONS
    using System.Runtime.ExceptionServices;
#endif // FEATURE_CORRUPTING_EXCEPTIONS
    using System.Text;
    using Microsoft.Win32;
    using System.Runtime.CompilerServices;
    using System.Security.Permissions;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
    using Microsoft.Win32.SafeHandles;
    using System.Diagnostics.Contracts;

    public sealed class SecureString: IDisposable {
        [System.Security.SecurityCritical] // auto-generated
        private SafeBSTRHandle m_buffer; 
        [ContractPublicPropertyName("Length")]
        private int       m_length;
        private bool     m_readOnly;
        private bool     m_encrypted; 
        
        static bool supportedOnCurrentPlatform = EncryptionSupported();

        const int BlockSize = (int)Win32Native.CRYPTPROTECTMEMORY_BLOCK_SIZE /2;  // a char is two bytes
        const int MaxLength = 65536;
        const uint ProtectionScope = Win32Native.CRYPTPROTECTMEMORY_SAME_PROCESS;
                
        [System.Security.SecuritySafeCritical]  // auto-generated
        static SecureString()
        {
        }

        [System.Security.SecurityCritical]  // auto-generated
        unsafe static bool EncryptionSupported() {
            // check if the enrypt/decrypt function is supported on current OS
            bool supported = true;                        
            try {
                Win32Native.SystemFunction041(
                    SafeBSTRHandle.Allocate(null , (int)Win32Native.CRYPTPROTECTMEMORY_BLOCK_SIZE),
                    Win32Native.CRYPTPROTECTMEMORY_BLOCK_SIZE, 
                    Win32Native.CRYPTPROTECTMEMORY_SAME_PROCESS);
            }
            catch (EntryPointNotFoundException) {
                supported = false;
            }            
            return supported;
        }
        
        [System.Security.SecurityCritical]  // auto-generated
        internal SecureString(SecureString str) {
            AllocateBuffer(str.BufferLength); 
            SafeBSTRHandle.Copy(str.m_buffer, this.m_buffer);
            m_length = str.m_length;
            m_encrypted = str.m_encrypted;
        }

        
        [System.Security.SecuritySafeCritical]  // auto-generated
        public SecureString() {
            CheckSupportedOnCurrentPlatform();            
            
            // allocate the minimum block size for calling protectMemory
            AllocateBuffer(BlockSize);  
            m_length = 0;
        }
         

        [System.Security.SecurityCritical]  // auto-generated
#if FEATURE_CORRUPTING_EXCEPTIONS
        [HandleProcessCorruptedStateExceptions] 
#endif // FEATURE_CORRUPTING_EXCEPTIONS
        private unsafe void InitializeSecureString(char* value, int length)
        {
            CheckSupportedOnCurrentPlatform();

            AllocateBuffer(length);
            m_length = length;

            byte* bufferPtr = null;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                m_buffer.AcquirePointer(ref bufferPtr);
                Buffer.Memcpy(bufferPtr, (byte*)value, length * 2);
            }
            catch (Exception) {
                ProtectMemory();
                throw;
            }
            finally
            {
                if (bufferPtr != null)
                    m_buffer.ReleasePointer();
            }

            ProtectMemory();
        }

        [System.Security.SecurityCritical]  // auto-generated
        [CLSCompliant(false)]
        public unsafe SecureString(char* value, int length) {
            if( value == null) {
                throw new ArgumentNullException("value");
            }

            if( length < 0) {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            if( length > MaxLength) {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_Length"));
            }
            Contract.EndContractBlock();

            // Refactored since HandleProcessCorruptedStateExceptionsAttribute applies to methods only (yet).
            InitializeSecureString(value, length);
        }
  
        public int Length { 
            [System.Security.SecuritySafeCritical]  // auto-generated
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            get  { 
                EnsureNotDisposed();
                return m_length;
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.Synchronized)]
#if FEATURE_CORRUPTING_EXCEPTIONS
        [HandleProcessCorruptedStateExceptions] 
#endif // FEATURE_CORRUPTING_EXCEPTIONS
        public void AppendChar(char c) {
            EnsureNotDisposed();
            EnsureNotReadOnly();

            EnsureCapacity(m_length + 1);

            RuntimeHelpers.PrepareConstrainedRegions();
            try {
                UnProtectMemory();            
                m_buffer.Write<char>((uint)m_length * sizeof(char), c);
                m_length++;
            }
            catch (Exception) {
                ProtectMemory();
                throw;
            }
            finally {
                ProtectMemory();            
            }
        }

        // clears the current contents. Only available if writable
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        public void Clear() {
            EnsureNotDisposed();
            EnsureNotReadOnly();

            m_length = 0;
            m_buffer.ClearBuffer();
            m_encrypted = false;
        }
                
        // Do a deep-copy of the SecureString 
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        public SecureString Copy() {
            EnsureNotDisposed();
            return new SecureString(this);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        public void Dispose() {
            if(m_buffer != null && !m_buffer.IsInvalid) {
                m_buffer.Close();
                m_buffer = null;   
            }            
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.Synchronized)]
#if FEATURE_CORRUPTING_EXCEPTIONS
        [HandleProcessCorruptedStateExceptions] 
#endif // FEATURE_CORRUPTING_EXCEPTIONS
        public void InsertAt( int index, char c ) {
            if( index < 0 || index > m_length) {
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_IndexString"));
            }
            Contract.EndContractBlock();

            EnsureNotDisposed();
            EnsureNotReadOnly();

            EnsureCapacity(m_length + 1);

            unsafe {
                byte* bufferPtr = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try {
                    UnProtectMemory();
                    m_buffer.AcquirePointer(ref bufferPtr);
                    char* pBuffer = (char*)bufferPtr;

                    for (int i = m_length; i > index; i--) {
                        pBuffer[i] = pBuffer[i - 1];
                    }
                    pBuffer[index] = c;
                    ++m_length;
                }
                catch (Exception) {
                    ProtectMemory();
                    throw;
                }
                finally {
                    ProtectMemory();
                    if (bufferPtr != null)
                        m_buffer.ReleasePointer();
                }
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        public bool IsReadOnly() {
            EnsureNotDisposed();
            return m_readOnly; 
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        public void MakeReadOnly() {
            EnsureNotDisposed();
            m_readOnly = true;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.Synchronized)]
#if FEATURE_CORRUPTING_EXCEPTIONS
        [HandleProcessCorruptedStateExceptions]
#endif // FEATURE_CORRUPTING_EXCEPTIONS
        public void RemoveAt( int index ) {
            EnsureNotDisposed();
            EnsureNotReadOnly();

            if( index < 0 || index >= m_length) {
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_IndexString"));
            }

            unsafe
            {
                byte* bufferPtr = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    UnProtectMemory();
                    m_buffer.AcquirePointer(ref bufferPtr);
                    char* pBuffer = (char*)bufferPtr;

                    for (int i = index; i < m_length - 1; i++)
                    {
                        pBuffer[i] = pBuffer[i + 1];
                    }
                    pBuffer[--m_length] = (char)0;
                }
                catch (Exception) {
                    ProtectMemory();
                    throw;
                }
                finally
                {
                    ProtectMemory();
                    if (bufferPtr != null)
                        m_buffer.ReleasePointer();
                }
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.Synchronized)]
#if FEATURE_CORRUPTING_EXCEPTIONS
        [HandleProcessCorruptedStateExceptions] 
#endif // FEATURE_CORRUPTING_EXCEPTIONS
        public void SetAt( int index, char c ) {
            if( index < 0 || index >= m_length) {
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_IndexString"));
            }
            Contract.EndContractBlock();
            Contract.Assert(index <= Int32.MaxValue / sizeof(char));

            EnsureNotDisposed();
            EnsureNotReadOnly();

            RuntimeHelpers.PrepareConstrainedRegions();
            try {
                UnProtectMemory();            
                m_buffer.Write<char>((uint)index * sizeof(char), c);
            }
            catch (Exception) {
                ProtectMemory();
                throw;
            }
            finally {
                ProtectMemory();            
            }
        }

        private int BufferLength {
            [System.Security.SecurityCritical]  // auto-generated
            get {
                Contract.Assert(m_buffer != null, "Buffer is not initialized!");   
                return m_buffer.Length;
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        private void AllocateBuffer(int size) {
            uint alignedSize = GetAlignedSize(size);

            m_buffer = SafeBSTRHandle.Allocate(null, alignedSize);
            if (m_buffer.IsInvalid) {
                throw new OutOfMemoryException();
            }
        }
        
        private void CheckSupportedOnCurrentPlatform() {
            if( !supportedOnCurrentPlatform) {
                throw new NotSupportedException(Environment.GetResourceString("Arg_PlatformSecureString"));
            }                            
            Contract.EndContractBlock();
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void EnsureCapacity(int capacity) {            
            if( capacity > MaxLength) {
                throw new ArgumentOutOfRangeException("capacity", Environment.GetResourceString("ArgumentOutOfRange_Capacity"));
            }
            Contract.EndContractBlock();

            if( capacity <= m_buffer.Length) {
                return;
            }

            SafeBSTRHandle newBuffer = SafeBSTRHandle.Allocate(null, GetAlignedSize(capacity));

            if (newBuffer.IsInvalid) {
                throw new OutOfMemoryException();
            }                

            SafeBSTRHandle.Copy(m_buffer, newBuffer);
            m_buffer.Close();
            m_buffer = newBuffer;                
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void EnsureNotDisposed() {
            if( m_buffer == null) {
                throw new ObjectDisposedException(null);
            }
            Contract.EndContractBlock();
        }

        private void EnsureNotReadOnly() {
            if( m_readOnly) {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
            }
            Contract.EndContractBlock();
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        private static uint GetAlignedSize( int size) {
            Contract.Assert(size >= 0, "size must be non-negative");

            uint alignedSize = ((uint)size / BlockSize) * BlockSize;
            if( (size % BlockSize != 0) || size == 0) {  // if size is 0, set allocated size to blocksize
                alignedSize += BlockSize;
            }
            return alignedSize;
        }

        [System.Security.SecurityCritical]  // auto-generated
        private unsafe int GetAnsiByteCount() {
            const uint CP_ACP               = 0;
            const uint WC_NO_BEST_FIT_CHARS = 0x00000400;

            uint flgs = WC_NO_BEST_FIT_CHARS;
            uint DefaultCharUsed = (uint)'?';            
            
            byte* bufferPtr = null;
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
                m_buffer.AcquirePointer(ref bufferPtr);

                return Win32Native.WideCharToMultiByte(
                    CP_ACP,
                    flgs,
                    (char*) bufferPtr,
                    m_length,
                    null,
                    0,
                    IntPtr.Zero,
                    new IntPtr((void*)&DefaultCharUsed));
            }
            finally {
                if (bufferPtr != null)
                    m_buffer.ReleasePointer();
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private unsafe void GetAnsiBytes( byte * ansiStrPtr, int byteCount) {
            const uint CP_ACP               = 0;
            const uint WC_NO_BEST_FIT_CHARS = 0x00000400;

            uint flgs = WC_NO_BEST_FIT_CHARS;
            uint DefaultCharUsed = (uint)'?';

            byte* bufferPtr = null;
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
                m_buffer.AcquirePointer(ref bufferPtr);

                Win32Native.WideCharToMultiByte(
                    CP_ACP,
                    flgs,
                    (char*) bufferPtr,
                    m_length,
                    ansiStrPtr,
                    byteCount - 1,
                    IntPtr.Zero,
                    new IntPtr((void*)&DefaultCharUsed));

                *(ansiStrPtr + byteCount - 1) = (byte)0;
            }
            finally {
                if (bufferPtr != null)
                    m_buffer.ReleasePointer();
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        private void ProtectMemory() {            
            Contract.Assert(!m_buffer.IsInvalid && m_buffer.Length != 0, "Invalid buffer!");
            Contract.Assert(m_buffer.Length % BlockSize == 0, "buffer length must be multiple of blocksize!");

            if( m_length == 0 || m_encrypted) {
                return;
            }

            RuntimeHelpers.PrepareConstrainedRegions();
            try {
            }
            finally {
                // RtlEncryptMemory return an NTSTATUS
                int status = Win32Native.SystemFunction040(m_buffer, (uint)m_buffer.Length * 2, ProtectionScope);
                if (status < 0)  { // non-negative numbers indicate success
#if FEATURE_CORECLR
                    throw new CryptographicException(Win32Native.RtlNtStatusToDosError(status));
#else
                    throw new CryptographicException(Win32Native.LsaNtStatusToWinError(status));
#endif
                }
                m_encrypted = true;
            }            
        }
        
        [System.Security.SecurityCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [MethodImplAttribute(MethodImplOptions.Synchronized)]
#if FEATURE_CORRUPTING_EXCEPTIONS
        [HandleProcessCorruptedStateExceptions] 
#endif // FEATURE_CORRUPTING_EXCEPTIONS
        internal unsafe IntPtr ToBSTR() {
            EnsureNotDisposed();
            int length = m_length;        
            IntPtr ptr = IntPtr.Zero;
            IntPtr result = IntPtr.Zero;
            byte* bufferPtr = null;

            RuntimeHelpers.PrepareConstrainedRegions();
            try {            
                RuntimeHelpers.PrepareConstrainedRegions();            
                try {
                }
                finally {
                    ptr = Win32Native.SysAllocStringLen(null, length);
                }
                
                if (ptr == IntPtr.Zero) {
                    throw new OutOfMemoryException();
                }
                
                UnProtectMemory();
                m_buffer.AcquirePointer(ref bufferPtr);
                Buffer.Memcpy((byte*) ptr.ToPointer(), bufferPtr, length *2); 
                result = ptr;
            }
            catch (Exception) {
                ProtectMemory();
                throw;
            }
            finally {                
                ProtectMemory();                
                if( result == IntPtr.Zero) { 
                    // If we failed for any reason, free the new buffer
                    if (ptr != IntPtr.Zero) {
                        Win32Native.ZeroMemory(ptr, (UIntPtr)(length * 2));
                        Win32Native.SysFreeString(ptr);
                    }                    
                }
                if (bufferPtr != null)
                    m_buffer.ReleasePointer();
            }
            return result;        
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.Synchronized)]
#if FEATURE_CORRUPTING_EXCEPTIONS
        [HandleProcessCorruptedStateExceptions] 
#endif // FEATURE_CORRUPTING_EXCEPTIONS
        internal unsafe IntPtr ToUniStr(bool allocateFromHeap) {
            EnsureNotDisposed();
            int length = m_length;
            IntPtr ptr = IntPtr.Zero;
            IntPtr result = IntPtr.Zero;
            byte* bufferPtr = null;

            RuntimeHelpers.PrepareConstrainedRegions();            
            try {
                RuntimeHelpers.PrepareConstrainedRegions();            
                try {
                }
                finally {
                    if( allocateFromHeap) {
                        ptr = Marshal.AllocHGlobal((length + 1) * 2);
                    }
                    else {
                        ptr = Marshal.AllocCoTaskMem((length + 1) * 2);
                    }
                }

                if (ptr == IntPtr.Zero) {
                    throw new OutOfMemoryException();
                }
            
                UnProtectMemory();
                m_buffer.AcquirePointer(ref bufferPtr);
                Buffer.Memcpy((byte*) ptr.ToPointer(), bufferPtr, length *2); 
                char * endptr = (char *) ptr.ToPointer();
                *(endptr + length) = '\0';
                result = ptr;
            }
            catch (Exception) {
                ProtectMemory();
                throw;
            }
            finally {
                ProtectMemory();

                if( result == IntPtr.Zero) { 
                    // If we failed for any reason, free the new buffer
                    if (ptr != IntPtr.Zero) {
                        Win32Native.ZeroMemory(ptr, (UIntPtr)(length * 2));
                        if( allocateFromHeap) {                                                    
                            Marshal.FreeHGlobal(ptr);
                        }
                        else {
                            Marshal.FreeCoTaskMem(ptr);
                        }
                    }                    
                }

                if (bufferPtr != null)
                    m_buffer.ReleasePointer();
            }
            return result;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.Synchronized)]
#if FEATURE_CORRUPTING_EXCEPTIONS
        [HandleProcessCorruptedStateExceptions] 
#endif // FEATURE_CORRUPTING_EXCEPTIONS
        internal unsafe IntPtr ToAnsiStr(bool allocateFromHeap) {
            EnsureNotDisposed();
            
            IntPtr ptr = IntPtr.Zero;
            IntPtr result = IntPtr.Zero;          
            int byteCount = 0;
            RuntimeHelpers.PrepareConstrainedRegions();                        
            try {
                // GetAnsiByteCount uses the string data, so the calculation must happen after we are decrypted.
                UnProtectMemory();
                
                // allocating an extra char for terminating zero
                byteCount = GetAnsiByteCount() + 1; 
                
                RuntimeHelpers.PrepareConstrainedRegions();
                try {
                }
                finally {
                    if( allocateFromHeap) {
                        ptr = Marshal.AllocHGlobal(byteCount);
                    }
                    else {
                        ptr = Marshal.AllocCoTaskMem(byteCount);
                   }                    
                }

                if (ptr == IntPtr.Zero) {
                    throw new OutOfMemoryException();
                }
                
                GetAnsiBytes((byte *)ptr.ToPointer(), byteCount);
                result = ptr;                
            }
            catch (Exception) {
                ProtectMemory();
                throw;
            }
            finally {
                ProtectMemory();
                if( result == IntPtr.Zero) { 
                    // If we failed for any reason, free the new buffer
                    if (ptr != IntPtr.Zero) {
                        Win32Native.ZeroMemory(ptr, (UIntPtr)byteCount);
                        if( allocateFromHeap) {                                                    
                            Marshal.FreeHGlobal(ptr);
                        }
                        else {
                            Marshal.FreeCoTaskMem(ptr);                            
                        }
                    }                    
                }                
                
            }
            return result;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        private void UnProtectMemory() {
            Contract.Assert(!m_buffer.IsInvalid && m_buffer.Length != 0, "Invalid buffer!");
            Contract.Assert(m_buffer.Length % BlockSize == 0, "buffer length must be multiple of blocksize!");

            if( m_length == 0) {
                return;
            }
            
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
            }
            finally {
                if (m_encrypted) {
                    // RtlEncryptMemory return an NTSTATUS
                    int status = Win32Native.SystemFunction041(m_buffer, (uint)m_buffer.Length * 2, ProtectionScope);
                    if (status < 0)
                    { // non-negative numbers indicate success
#if FEATURE_CORECLR
                        throw new CryptographicException(Win32Native.RtlNtStatusToDosError(status));
#else
                        throw new CryptographicException(Win32Native.LsaNtStatusToWinError(status));
#endif
                    }
                    m_encrypted = false;
                }
            }
        }        
    }

    [System.Security.SecurityCritical]  // auto-generated
    [SuppressUnmanagedCodeSecurityAttribute()]
    internal sealed class SafeBSTRHandle : SafeBuffer {
        internal SafeBSTRHandle () : base(true) {}

        internal static SafeBSTRHandle Allocate(String src, uint len)
        {
            SafeBSTRHandle bstr = SysAllocStringLen(src, len);
            bstr.Initialize(len * sizeof(char));
            return bstr;
        }

        [DllImport(Win32Native.OLEAUT32, CharSet = CharSet.Unicode)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]            
        private static extern SafeBSTRHandle SysAllocStringLen(String src, uint len);  // BSTR

        [System.Security.SecurityCritical]
        override protected bool ReleaseHandle()
        {
            Win32Native.ZeroMemory(handle, (UIntPtr) (Win32Native.SysStringLen(handle) * 2));
            Win32Native.SysFreeString(handle);
            return true;
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal unsafe void ClearBuffer() {
            byte* bufferPtr = null;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                AcquirePointer(ref bufferPtr);
                Win32Native.ZeroMemory((IntPtr)bufferPtr, (UIntPtr) (Win32Native.SysStringLen((IntPtr)bufferPtr) * 2));
            }
            finally
            {
                if (bufferPtr != null)
                    ReleasePointer();
            }
        }


        internal unsafe int Length {
            get {
                return (int) Win32Native.SysStringLen(this);
            }
        }

        internal unsafe static void Copy(SafeBSTRHandle source, SafeBSTRHandle target) {
            byte* sourcePtr = null, targetPtr = null;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                source.AcquirePointer(ref sourcePtr);
                target.AcquirePointer(ref targetPtr);

                Contract.Assert(Win32Native.SysStringLen((IntPtr)targetPtr) >= Win32Native.SysStringLen((IntPtr)sourcePtr), "Target buffer is not large enough!");

                Buffer.Memcpy(targetPtr, sourcePtr, (int) Win32Native.SysStringLen((IntPtr)sourcePtr) * 2);
            }
            finally
            {
                if (sourcePtr != null)
                    source.ReleasePointer();
                if (targetPtr != null)
                    target.ReleasePointer();
            }
        }
    }
}

