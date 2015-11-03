// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Net.NetworkInformation;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct IPEndPointInfo
        {
            public fixed byte AddressBytes[16];
            public uint NumAddressBytes;
            public uint Port;
            private uint __padding; // For native struct-size padding. Does not contain useful data.
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NativeTcpConnectionInformation
        {
            public IPEndPointInfo LocalEndPoint;
            public IPEndPointInfo RemoteEndPoint;
            public TcpState State;
        }

        [DllImport(Libraries.SystemNative)]
        public unsafe static extern int GetEstimatedTcpConnectionCount();

        [DllImport(Libraries.SystemNative)]
        public unsafe static extern int GetActiveTcpConnectionInfos(NativeTcpConnectionInformation* infos, int* infoCount);

        [DllImport(Libraries.SystemNative)]
        public unsafe static extern int GetEstimatedUdpListenerCount();

        [DllImport(Libraries.SystemNative)]
        public unsafe static extern int GetActiveUdpListeners(IPEndPointInfo* infos, int* infoCount);
    }    
}
