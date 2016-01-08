// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace System.Net
{
    // _SecPkgContext_StreamSizes in sspi.h.
    [StructLayout(LayoutKind.Sequential)]
    internal class StreamSizes
    {
        public int header;
        public int trailer;
        public int maximumMessage;
        public int buffersCount;
        public int blockSize;

        internal unsafe StreamSizes(byte[] memory)
        {
            fixed (void* voidPtr = memory)
            {
                IntPtr unmanagedAddress = new IntPtr(voidPtr);
                try
                {
                    header = (int)checked((uint)Marshal.ReadInt32(unmanagedAddress));
                    trailer = (int)checked((uint)Marshal.ReadInt32(unmanagedAddress, 4));
                    maximumMessage = (int)checked((uint)Marshal.ReadInt32(unmanagedAddress, 8));
                    buffersCount = (int)checked((uint)Marshal.ReadInt32(unmanagedAddress, 12));
                    blockSize = (int)checked((uint)Marshal.ReadInt32(unmanagedAddress, 16));
                }
                catch (OverflowException)
                {
                    if (GlobalLog.IsEnabled)
                    {
                        GlobalLog.Assert("StreamSizes::.ctor", "Negative size.");
                    }
                    throw;
                }
            }
        }
        public static readonly int SizeOf = Marshal.SizeOf<StreamSizes>();
    }
}