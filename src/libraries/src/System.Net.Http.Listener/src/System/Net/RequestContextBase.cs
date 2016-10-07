// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Net
{
    internal abstract unsafe class RequestContextBase : IDisposable
    {
        private Interop.HttpApi.HTTP_REQUEST* m_MemoryBlob;
        private Interop.HttpApi.HTTP_REQUEST* m_OriginalBlobAddress;
        private byte[] m_BackingBuffer;

        // Must call this from derived class' constructors.
        protected void BaseConstruction(Interop.HttpApi.HTTP_REQUEST* requestBlob)
        {
            if (requestBlob == null)
            {
                GC.SuppressFinalize(this);
            }
            else
            {
                m_MemoryBlob = requestBlob;
            }
        }

        // ReleasePins() should be called exactly once.  It must be called before Dispose() is called, which means it must be called
        // before an object (HttpListenerReqeust) which closes the RequestContext on demand is returned to the application.
        internal void ReleasePins()
        {
            Debug.Assert(m_MemoryBlob != null || m_BackingBuffer == null, "RequestContextBase::ReleasePins()|ReleasePins() called twice.");
            m_OriginalBlobAddress = m_MemoryBlob;
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
            Debug.Assert(m_MemoryBlob == null, "RequestContextBase::Dispose()|Dispose() called before ReleasePins().");
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing) { }

        ~RequestContextBase()
        {
            Dispose(false);
        }

        internal Interop.HttpApi.HTTP_REQUEST* RequestBlob
        {
            get
            {
                Debug.Assert(m_MemoryBlob != null || m_BackingBuffer == null, "RequestContextBase::Dispose()|RequestBlob requested after ReleasePins().");
                return m_MemoryBlob;
            }
        }

        internal byte[] RequestBuffer
        {
            get
            {
                return m_BackingBuffer;
            }
        }

        internal uint Size
        {
            get
            {
                return (uint)m_BackingBuffer.Length;
            }
        }

        internal IntPtr OriginalBlobAddress
        {
            get
            {
                Interop.HttpApi.HTTP_REQUEST* blob = m_MemoryBlob;
                return (IntPtr)(blob == null ? m_OriginalBlobAddress : blob);
            }
        }

        protected void SetBlob(Interop.HttpApi.HTTP_REQUEST* requestBlob)
        {
            Debug.Assert(m_MemoryBlob != null || m_BackingBuffer == null, "RequestContextBase::Dispose()|SetBlob() called after ReleasePins().");
            if (requestBlob == null)
            {
                UnsetBlob();
                return;
            }

            if (m_MemoryBlob == null)
            {
                GC.ReRegisterForFinalize(this);
            }
            m_MemoryBlob = requestBlob;
        }

        protected void UnsetBlob()
        {
            if (m_MemoryBlob != null)
            {
                GC.SuppressFinalize(this);
            }
            m_MemoryBlob = null;
        }

        protected void SetBuffer(int size)
        {
            m_BackingBuffer = size == 0 ? null : new byte[size];
        }
    }
}
