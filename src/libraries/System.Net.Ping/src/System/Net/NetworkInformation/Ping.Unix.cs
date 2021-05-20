// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.NetworkInformation
{
    public partial class Ping
    {
        private static bool SendIpHeader => false;
        private static bool NeedsConnect => OperatingSystem.IsLinux();
        private static bool SupportsDualMode => true;

        private PingReply SendPingCore(IPAddress address, byte[] buffer, int timeout, PingOptions? options)
        {
            PingReply reply = RawSocketPermissions.CanUseRawSockets(address.AddressFamily) ?
                    SendIcmpEchoRequestOverRawSocket(address, buffer, timeout, options) :
                    SendWithPingUtility(address, buffer, timeout, options);
            return reply;
        }

        private async Task<PingReply> SendPingAsyncCore(IPAddress address, byte[] buffer, int timeout, PingOptions? options)
        {
            Task<PingReply> t = RawSocketPermissions.CanUseRawSockets(address.AddressFamily) ?
                    SendIcmpEchoRequestOverRawSocketAsync(address, buffer, timeout, options) :
                    SendWithPingUtilityAsync(address, buffer, timeout, options);

            PingReply reply = await t.ConfigureAwait(false);

            if (_canceled)
            {
                throw new OperationCanceledException();
            }

            return reply;
        }
    }
}
