// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;

namespace System.Net
{
    internal static partial class RawSocketPermissions
    {
        private static readonly Lazy<bool> s_canUseRawIPv4Sockets = new Lazy<bool>(() => CheckRawSocketPermissions(AddressFamily.InterNetwork));
        private static readonly Lazy<bool> s_canUseRawIPv6Sockets = new Lazy<bool>(() => CheckRawSocketPermissions(AddressFamily.InterNetworkV6));

        /// <summary>
        /// Returns whether or not the current user has the necessary permission to open raw sockets.
        /// </summary>
        public static bool CanUseRawSockets(AddressFamily addressFamily) =>
            addressFamily == AddressFamily.InterNetworkV6 ?
                s_canUseRawIPv6Sockets.Value :
                s_canUseRawIPv4Sockets.Value;

        private static bool CheckRawSocketPermissions(AddressFamily addressFamily)
        {
            try
            {
                new Socket(addressFamily, SocketType.Raw, ProtocolType.Icmp).Dispose();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
