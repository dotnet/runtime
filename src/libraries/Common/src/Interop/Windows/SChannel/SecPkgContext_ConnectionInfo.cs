// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Net
{
    // TODO (Issue #3114): Investigate if this can be safely converted to a struct.
    // From Schannel.h
    [StructLayout(LayoutKind.Sequential)]
    internal class SecPkgContext_ConnectionInfo
    {
        public readonly int Protocol;
        public readonly int DataCipherAlg;
        public readonly int DataKeySize;
        public readonly int DataHashAlg;
        public readonly int DataHashKeySize;
        public readonly int KeyExchangeAlg;
        public readonly int KeyExchKeySize;

        internal unsafe SecPkgContext_ConnectionInfo(byte[] nativeBuffer)
        {
            fixed (void* voidPtr = nativeBuffer)
            {
                try
                {
                    // TODO (Issue #3114): replace with Marshal.PtrToStructure.
                    IntPtr unmanagedAddress = new IntPtr(voidPtr);
                    Protocol = Marshal.ReadInt32(unmanagedAddress);
                    DataCipherAlg = Marshal.ReadInt32(unmanagedAddress, 4);
                    DataKeySize = Marshal.ReadInt32(unmanagedAddress, 8);
                    DataHashAlg = Marshal.ReadInt32(unmanagedAddress, 12);
                    DataHashKeySize = Marshal.ReadInt32(unmanagedAddress, 16);
                    KeyExchangeAlg = Marshal.ReadInt32(unmanagedAddress, 20);
                    KeyExchKeySize = Marshal.ReadInt32(unmanagedAddress, 24);
                }
                catch (OverflowException)
                {
                    NetEventSource.Fail(this, "Negative size");
                    throw;
                }
            }
        }
    }
}
