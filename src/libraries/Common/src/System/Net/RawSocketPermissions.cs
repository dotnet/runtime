// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Sockets;

namespace System.Net
{
    internal static partial class RawSocketPermissions
    {
        private static readonly Lazy<bool> s_canUseRawSockets = new Lazy<bool>(CheckRawSocketPermissions);

        /// <summary>
        /// Returns whether or not the current user has the necessary permission to open raw sockets.
        /// </summary>
        public static bool CanUseRawSockets()
        {
            return s_canUseRawSockets.Value;
        }

        private static bool CheckRawSocketPermissions()
        {
            try
            {
                Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
                s.Dispose();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
