// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Security
{
    public sealed partial class SecureString : IDisposable
    {
        private const int MaxLength = 65536;
        private readonly object _methodLock = new object();
        private bool _readOnly;
        private int _decryptedLength;

        public unsafe SecureString()
        {
            InitializeSecureString(null, 0);
        }

        [CLSCompliant(false)]
        public unsafe SecureString(char* value, int length)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (length > MaxLength)
            {
                throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_Length);
            }

            InitializeSecureString(value, length);
        }

        public int Length
        {
            get
            {
                lock (_methodLock)
                {
                    EnsureNotDisposed();
                    return _decryptedLength;
                }
            }
        }

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
        public SecureString Copy()
        {
            lock (_methodLock)
            {
                EnsureNotDisposed();
                return new SecureString(this);
            }
        }

        public void Dispose()
        {
            lock (_methodLock)
            {
                DisposeCore();
            }
        }

        public void InsertAt(int index, char c)
        {
            lock (_methodLock)
            {
                if (index < 0 || index > _decryptedLength)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_IndexString);
                }

                EnsureNotDisposed();
                EnsureNotReadOnly();

                InsertAtCore(index, c);
            }
        }

        public bool IsReadOnly()
        {
            lock (_methodLock)
            {
                EnsureNotDisposed();
                return _readOnly;
            }
        }

        public void MakeReadOnly()
        {
            lock (_methodLock)
            {
                EnsureNotDisposed();
                _readOnly = true;
            }
        }

        public void RemoveAt(int index)
        {
            lock (_methodLock)
            {
                EnsureNotDisposed();
                EnsureNotReadOnly();

                if (index < 0 || index >= _decryptedLength)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_IndexString);
                }

                RemoveAtCore(index);
            }
        }

        public void SetAt(int index, char c)
        {
            lock (_methodLock)
            {
                if (index < 0 || index >= _decryptedLength)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_IndexString);
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

        internal unsafe IntPtr MarshalToString(bool globalAlloc, bool unicode)
        {
            lock (_methodLock)
            {
                EnsureNotDisposed();
                return MarshalToStringCore(globalAlloc, unicode);
            }
        }

        private static void MarshalFree(IntPtr ptr, bool globalAlloc)
        {
            if (globalAlloc)
            {
                Marshal.FreeHGlobal(ptr);
            }
            else
            {
                Marshal.FreeCoTaskMem(ptr);
            }
        }
    }
}
