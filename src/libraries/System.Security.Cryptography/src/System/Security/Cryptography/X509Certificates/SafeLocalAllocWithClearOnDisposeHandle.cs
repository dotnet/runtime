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
        internal int Length { get; private set; }

        public static SafeLocalAllocWithClearOnDisposeHandle Create(int cb)
        {
            var h = new SafeLocalAllocWithClearOnDisposeHandle();
            h.SetHandle(Marshal.AllocHGlobal(cb));
            h.Length = cb;
            return h;
        }

        private unsafe Span<byte> GetSpan()
            => new Span<byte>((void*)handle, Length);

        protected sealed override unsafe bool ReleaseHandle()
        {
            CryptographicOperations.ZeroMemory(GetSpan());
            Marshal.FreeHGlobal(handle);
            return true;
        }
    }
}
