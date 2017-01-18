// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetEstimatedTcpConnectionCount")]
        public static extern unsafe int GetEstimatedTcpConnectionCount();

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetActiveTcpConnectionInfos")]
        public static extern unsafe int GetActiveTcpConnectionInfos(NativeTcpConnectionInformation* infos, int* infoCount);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetEstimatedUdpListenerCount")]
        public static extern unsafe int GetEstimatedUdpListenerCount();

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetActiveUdpListeners")]
        public static extern unsafe int GetActiveUdpListeners(IPEndPointInfo* infos, int* infoCount);
    }    
}
