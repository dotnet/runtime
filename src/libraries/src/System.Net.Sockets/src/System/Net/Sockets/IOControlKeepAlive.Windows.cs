// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Net.Sockets
{
    // Allow hiding keep-alive time and interval handling behind *SocketOption
    // on Windows < 10 v1709 that only supports set via IOControl
    internal sealed class IOControlKeepAlive
    {
        private const uint WindowsDefaultTimeMs = 7200000u;
        private const uint WindowsDefaultIntervalMs = 1000u;
        private static readonly bool s_supportsKeepAliveViaSocketOption = SupportsKeepAliveViaSocketOption();
        private static readonly ConditionalWeakTable<SafeSocketHandle, IOControlKeepAlive> s_socketKeepAliveTable = new ConditionalWeakTable<SafeSocketHandle, IOControlKeepAlive>();
        [ThreadStatic]
        private static byte[] s_keepAliveValuesBuffer;

        private uint _timeMs = WindowsDefaultTimeMs;
        private uint _intervalMs = WindowsDefaultIntervalMs;

        public static bool IsNeeded => !s_supportsKeepAliveViaSocketOption;

        public static SocketError Get(SafeSocketHandle handle, SocketOptionName optionName, byte[] optionValueSeconds, ref int optionLength)
        {
            if (optionValueSeconds == null ||
                !BitConverter.TryWriteBytes(optionValueSeconds.AsSpan(), Get(handle, optionName)))
            {
                return SocketError.Fault;
            }

            optionLength = optionValueSeconds.Length;
            return SocketError.Success;
        }

        public static int Get(SafeSocketHandle handle, SocketOptionName optionName)
        {
            if (s_socketKeepAliveTable.TryGetValue(handle, out IOControlKeepAlive ioControlKeepAlive))
            {
                return optionName == SocketOptionName.TcpKeepAliveTime ?
                    MillisecondsToSeconds(ioControlKeepAlive._timeMs) :
                    MillisecondsToSeconds(ioControlKeepAlive._intervalMs);
            }

            return optionName == SocketOptionName.TcpKeepAliveTime ?
                MillisecondsToSeconds(WindowsDefaultTimeMs) :
                MillisecondsToSeconds(WindowsDefaultIntervalMs);
        }

        public static SocketError Set(SafeSocketHandle handle, SocketOptionName optionName, byte[] optionValueSeconds)
        {
            if (optionValueSeconds == null ||
                optionValueSeconds.Length < sizeof(int))
            {
                return SocketError.Fault;
            }

            return Set(handle, optionName, BitConverter.ToInt32(optionValueSeconds, 0));
        }

        public static SocketError Set(SafeSocketHandle handle, SocketOptionName optionName, int optionValueSeconds)
        {
            IOControlKeepAlive ioControlKeepAlive = s_socketKeepAliveTable.GetOrCreateValue(handle);
            if (optionName == SocketOptionName.TcpKeepAliveTime)
            {
                ioControlKeepAlive._timeMs = SecondsToMilliseconds(optionValueSeconds);
            }
            else
            {
                ioControlKeepAlive._intervalMs = SecondsToMilliseconds(optionValueSeconds);
            }

            byte[] buffer = s_keepAliveValuesBuffer ?? (s_keepAliveValuesBuffer = new byte[3 * sizeof(uint)]);
            ioControlKeepAlive.Fill(buffer);
            int realOptionLength = 0;
            return SocketPal.WindowsIoctl(handle, unchecked((int)IOControlCode.KeepAliveValues), buffer, null, out realOptionLength);
        }

        private static bool SupportsKeepAliveViaSocketOption()
        {
            AddressFamily addressFamily = Socket.OSSupportsIPv4 ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6;
            using (Socket socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                int time = MillisecondsToSeconds(WindowsDefaultTimeMs);
                SocketError timeErrCode = Interop.Winsock.setsockopt(
                    socket.SafeHandle,
                    SocketOptionLevel.Tcp,
                    SocketOptionName.TcpKeepAliveTime,
                    ref time,
                    sizeof(int));

                int interval = MillisecondsToSeconds(WindowsDefaultIntervalMs);
                SocketError intervalErrCode = Interop.Winsock.setsockopt(
                    socket.SafeHandle,
                    SocketOptionLevel.Tcp,
                    SocketOptionName.TcpKeepAliveInterval,
                    ref interval,
                    sizeof(int));

                return
                    timeErrCode == SocketError.Success &&
                    intervalErrCode == SocketError.Success;
            }
        }

        private static int MillisecondsToSeconds(uint milliseconds) => (int)(milliseconds / 1000u);

        private static uint SecondsToMilliseconds(int seconds) => (uint)seconds * 1000u;

        private void Fill(byte[] buffer)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(buffer.Length == 3 * sizeof(uint));

            const uint OnOff = 1u;
            bool written =
                BitConverter.TryWriteBytes(buffer.AsSpan(), OnOff) &
                BitConverter.TryWriteBytes(buffer.AsSpan(sizeof(uint)), _timeMs) &
                BitConverter.TryWriteBytes(buffer.AsSpan(sizeof(uint) * 2), _intervalMs);
            Debug.Assert(written);
        }
    }
}
