// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    /// <summary>
    /// SafeHandle for LocalAlloc'd memory which calls ZeroMemory when releasing handle.
    /// </summary>
    internal sealed class SafeLocalAllocWithClearOnDisposeHandle : SafeCrypt32Handle<SafeLocalAllocWithClearOnDisposeHandle>
    {
        private int _length;

        public static SafeLocalAllocWithClearOnDisposeHandle Create(int cb)
        {
            var h = new SafeLocalAllocWithClearOnDisposeHandle();
            h.SetHandle(Marshal.AllocHGlobal(cb));
            h._length = cb;
            return h;
        }

        public unsafe PointerMemoryManager<byte> GetMemoryManager()
            => new PointerMemoryManager<byte>((byte*)handle, _length);

        private unsafe Span<byte> GetSpan()
            => new Span<byte>((void*)handle, _length);

        protected sealed override unsafe bool ReleaseHandle()
        {
            CryptographicOperations.ZeroMemory(GetSpan());
            Marshal.FreeHGlobal(handle);
            return true;
        }
    }
}
