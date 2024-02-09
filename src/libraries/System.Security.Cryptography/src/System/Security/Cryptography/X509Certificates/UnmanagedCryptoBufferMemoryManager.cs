// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace System.Security.Cryptography.X509Certificates
{
    /// <summary>
    /// Unmanaged memory manager backed by buffer allocated with Marshal.AllocHGlobal ensuring memory is cleared on Dispose.
    /// </summary>
    internal sealed unsafe class UnmanagedCryptoBufferMemoryManager : MemoryManager<byte>
    {
        private readonly SafeLocalAllocWithClearOnDisposeHandle _handle;

        internal UnmanagedCryptoBufferMemoryManager(int length)
        {
            _handle = SafeLocalAllocWithClearOnDisposeHandle.Create(length);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _handle.Dispose();
            }
        }

        public override Span<byte> GetSpan()
            => _handle.DangerousGetSpan();

        public override MemoryHandle Pin(int elementIndex = 0)
        {
            throw new NotSupportedException();
        }

        public override void Unpin()
        {
        }
    }
}
