// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.NetworkInformation
{
    public partial class Ping
    {
        // Any exceptions that escape synchronously will be caught by the caller and wrapped in a PingException.
        // We do not need to or want to capture such exceptions into the returned task.
        private Task<PingReply> SendPingAsyncCore(IPAddress address, byte[] buffer, int timeout, PingOptions options)
        {
            // Win32 Icmp* APIs fail with E_ACCESSDENIED when called from UWP due to Windows OS limitations.
            throw new PlatformNotSupportedException(string.Format(CultureInfo.InvariantCulture,
                        SR.net_ping_not_supported_uwp));
        }
    }
}
