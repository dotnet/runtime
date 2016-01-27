// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Net
{
    // _SecPkgContext_Sizes in sspi.h.
    [StructLayout(LayoutKind.Sequential)]
    internal class SecSizes
    {
        public readonly int MaxToken;
        public readonly int MaxSignature;
        public readonly int BlockSize;
        public readonly int SecurityTrailer;

        internal unsafe SecSizes(byte[] memory)
        {
            fixed (void* voidPtr = memory)
            {
                IntPtr unmanagedAddress = new IntPtr(voidPtr);
                try
                {
                    // TODO (Issue #3114): replace with Marshal.PtrToStructure.
                    MaxToken = (int)checked((uint)Marshal.ReadInt32(unmanagedAddress));
                    MaxSignature = (int)checked((uint)Marshal.ReadInt32(unmanagedAddress, 4));
                    BlockSize = (int)checked((uint)Marshal.ReadInt32(unmanagedAddress, 8));
                    SecurityTrailer = (int)checked((uint)Marshal.ReadInt32(unmanagedAddress, 12));
                }
                catch (OverflowException)
                {
                    if (GlobalLog.IsEnabled)
                    {
                        GlobalLog.Assert("SecSizes::.ctor", "Negative size.");
                    }

                    Debug.Fail("SecSizes::.ctor", "Negative size.");
                    throw;
                }
            }
        }
        public static readonly int SizeOf = Marshal.SizeOf<SecSizes>();
    }
}
