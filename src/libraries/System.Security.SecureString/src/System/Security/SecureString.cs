// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Security
{
    public sealed partial class SecureString : IDisposable
    {
        private const int MaxLength = 65536;
        private readonly object _methodLock = new object();
        private bool _readOnly;
        private int _decryptedLength;

        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe SecureString()
        {
            InitializeSecureString(null, 0);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [CLSCompliant(false)]
        public unsafe SecureString(char* value, int length)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length", SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (length > MaxLength)
            {
                throw new ArgumentOutOfRangeException("length", SR.ArgumentOutOfRange_Length);
            }

            // Refactored since HandleProcessCorruptedStateExceptionsAttribute applies to methods only (yet).
            InitializeSecureString(value, length);
        }

        public int Length
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                lock (_methodLock)
                {
                    EnsureNotDisposed();
                    return _decryptedLength;
                }
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public void AppendChar(char c)
        {
            lock (_methodLock)
            {
                EnsureNotDisposed();
                EnsureNotReadOnly();
                AppendCharCore(c);
            }
        }

        // clears the current contents. Only available if writable
        [System.Security.SecuritySafeCritical]  // auto-generated
        public void Clear()
        {
            lock (_methodLock)
            {
                EnsureNotDisposed();
                EnsureNotReadOnly();
                ClearCore();
            }
        }

        // Do a deep-copy of the SecureString 
        [System.Security.SecuritySafeCritical]  // auto-generated
        public SecureString Copy()
        {
            lock (_methodLock)
            {
                EnsureNotDisposed();
                return new SecureString(this);
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public void Dispose()
        {
            lock (_methodLock)
            {
                DisposeCore();
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public void InsertAt(int index, char c)
        {
            lock (_methodLock)
            {
                if (index < 0 || index > _decryptedLength)
                {
                    throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_IndexString);
                }

                EnsureNotDisposed();
                EnsureNotReadOnly();

                InsertAtCore(index, c);
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public bool IsReadOnly()
        {
            lock (_methodLock)
            {
                EnsureNotDisposed();
                return _readOnly;
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public void MakeReadOnly()
        {
            lock (_methodLock)
            {
                EnsureNotDisposed();
                _readOnly = true;
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public void RemoveAt(int index)
        {
            lock (_methodLock)
            {
                EnsureNotDisposed();
                EnsureNotReadOnly();

                if (index < 0 || index >= _decryptedLength)
                {
                    throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_IndexString);
                }

                RemoveAtCore(index);
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public void SetAt(int index, char c)
        {
            lock (_methodLock)
            {
                if (index < 0 || index >= _decryptedLength)
                {
                    throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_IndexString);
                }
                Debug.Assert(index <= Int32.MaxValue / sizeof(char));

                EnsureNotDisposed();
                EnsureNotReadOnly();

                SetAtCore(index, c);
            }
        }

        private void EnsureNotReadOnly()
        {
            if (_readOnly)
            {
                throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal unsafe IntPtr ToUniStr()
        {
            lock (_methodLock)
            {
                EnsureNotDisposed();
                return ToUniStrCore();
            }
        }
    }
}
