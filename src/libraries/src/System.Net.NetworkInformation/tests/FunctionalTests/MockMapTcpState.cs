// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/* 
    This class is just meant to mock out the "MapTcpState" method in the shim.
    For the tests that parse strings, we need to have a single definition for these mappings
    in order for the tests to run on different platforms against the same standard test files.
*/

using System.Net.NetworkInformation;

internal static class Interop
{
    internal static class Sys
    {
        public static TcpState MapTcpState(int stateAsInt)
        {
            LinuxTcpState state = (LinuxTcpState)stateAsInt;
            switch (state)
            {
                case LinuxTcpState.TCP_ESTABLISHED:
                    return TcpState.Established;
                case LinuxTcpState.TCP_SYN_SENT:
                    return TcpState.SynSent;
                case LinuxTcpState.TCP_SYN_RECV:
                case LinuxTcpState.TCP_NEW_SYN_RECV:
                    return TcpState.SynReceived;
                case LinuxTcpState.TCP_FIN_WAIT1:
                    return TcpState.FinWait1;
                case LinuxTcpState.TCP_FIN_WAIT2:
                    return TcpState.FinWait2;
                case LinuxTcpState.TCP_TIME_WAIT:
                    return TcpState.TimeWait;
                case LinuxTcpState.TCP_CLOSE:
                    return TcpState.Closing;
                case LinuxTcpState.TCP_CLOSE_WAIT:
                    return TcpState.CloseWait;
                case LinuxTcpState.TCP_LAST_ACK:
                    return TcpState.LastAck;
                case LinuxTcpState.TCP_LISTEN:
                    return TcpState.Listen;
                case LinuxTcpState.TCP_CLOSING:
                    return TcpState.Closing;
                default:
                    return TcpState.Unknown;
            }
        }

        /// <summary>
        /// Represents the values of the TCP States from the test files.
        /// </summary>
        private enum LinuxTcpState
        {
            TCP_ESTABLISHED = 1,
            TCP_SYN_SENT,
            TCP_SYN_RECV,
            TCP_FIN_WAIT1,
            TCP_FIN_WAIT2,
            TCP_TIME_WAIT,
            TCP_CLOSE,
            TCP_CLOSE_WAIT,
            TCP_LAST_ACK,
            TCP_LISTEN,
            TCP_CLOSING,
            TCP_NEW_SYN_RECV,
        }
    }
}
