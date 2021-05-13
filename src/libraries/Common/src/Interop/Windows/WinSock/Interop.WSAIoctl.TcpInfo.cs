// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        private const int SioTcpInfo = unchecked((int)3623878695L);

        [DllImport(Interop.Libraries.Ws2_32, SetLastError = true, EntryPoint = "WSAIoctl")]
        private static extern SocketError WSAIoctl_Blocking(
            SafeSocketHandle socketHandle,
            [In] int ioControlCode,
            [In] ref int inBuffer,
            [In] int inBufferSize,
            [Out] out _TCP_INFO_v0 outBuffer,
            [In] int outBufferSize,
            [Out] out int bytesTransferred,
            [In] IntPtr overlapped,
            [In] IntPtr completionRoutine);

        internal static unsafe SocketError GetTcpInfoV0(SafeSocketHandle socketHandle, out _TCP_INFO_v0 tcpInfo)
        {
            int input = 0;
            return WSAIoctl_Blocking(socketHandle, SioTcpInfo, ref input, sizeof(int), out tcpInfo, sizeof(_TCP_INFO_v0), out _, IntPtr.Zero, IntPtr.Zero);
        }

        internal struct _TCP_INFO_v0
        {
            internal System.Net.NetworkInformation.TcpState State;
            internal uint Mss;
            internal ulong ConnectionTimeMs;
            internal byte TimestampsEnabled;
            internal uint RttUs;
            internal uint MinRttUs;
            internal uint BytesInFlight;
            internal uint Cwnd;
            internal uint SndWnd;
            internal uint RcvWnd;
            internal uint RcvBuf;
            internal ulong BytesOut;
            internal ulong BytesIn;
            internal uint BytesReordered;
            internal uint BytesRetrans;
            internal uint FastRetrans;
            internal uint DupAcksIn;
            internal uint TimeoutEpisodes;
            internal byte SynRetrans;
        }
    }
}
