// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Net
{
    internal abstract unsafe class RequestContextBase : IDisposable
    {
        private Interop.HttpApi.HTTP_REQUEST* _memoryBlob;
        private Interop.HttpApi.HTTP_REQUEST* _originalBlobAddress;
        private IntPtr _backingBuffer = IntPtr.Zero;
        private int _backingBufferLength = 0;

        // Must call this from derived class' constructors.
        protected void BaseConstruction(Interop.HttpApi.HTTP_REQUEST* requestBlob)
        {
            if (requestBlob != null)
            {
                _memoryBlob = requestBlob;
            }
        }

        // ReleasePins() should be called exactly once.  It must be called before Dispose() is called, which means it must be called
        // before an object (HttpListenerRequest) which closes the RequestContext on demand is returned to the application.
        internal void ReleasePins()
        {
            Debug.Assert(_memoryBlob != null || _backingBuffer == IntPtr.Zero, "RequestContextBase::ReleasePins()|ReleasePins() called twice.");
            _originalBlobAddress = _memoryBlob;
            UnsetBlob();
            OnReleasePins();
        }

        protected abstract void OnReleasePins();

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            Debug.Assert(_memoryBlob == null, "RequestContextBase::Dispose()|Dispose() called before ReleasePins().");
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_backingBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_backingBuffer);
                _backingBuffer = IntPtr.Zero;
            }
        }

        ~RequestContextBase()
        {
            Dispose(false);
        }

        internal Interop.HttpApi.HTTP_REQUEST* RequestBlob
        {
            get
            {
                Debug.Assert(_memoryBlob != null || _backingBuffer == IntPtr.Zero, "RequestContextBase::Dispose()|RequestBlob requested after ReleasePins().");
                return _memoryBlob;
            }
        }

        internal IntPtr RequestBuffer
        {
            get
            {
                return _backingBuffer;
            }
        }

        internal uint Size
        {
            get
            {
                return (uint)_backingBufferLength;
            }
        }

        internal IntPtr OriginalBlobAddress
        {
            get
            {
                Interop.HttpApi.HTTP_REQUEST* blob = _memoryBlob;
                return (IntPtr)(blob == null ? _originalBlobAddress : blob);
            }
        }

        protected void SetBlob(Interop.HttpApi.HTTP_REQUEST* requestBlob)
        {
            Debug.Assert(_memoryBlob != null || _backingBuffer == IntPtr.Zero, "RequestContextBase::Dispose()|SetBlob() called after ReleasePins().");
            if (requestBlob == null)
            {
                UnsetBlob();
                return;
            }

            _memoryBlob = requestBlob;
        }

        protected void UnsetBlob()
        {
            _memoryBlob = null;
        }

        protected void SetBuffer(int size)
        {
            if (_backingBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_backingBuffer);
            }

            _backingBuffer = size == 0 ? IntPtr.Zero : Marshal.AllocHGlobal(size);
            _backingBufferLength = size;

            // Zero out the contents of the buffer.
            new Span<byte>(_backingBuffer.ToPointer(), size).Fill(0);
        }
    }
}
