// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_Shutdown")]
        private static extern Error DangerousShutdown(int socket, SocketShutdown how);

        internal static Error Shutdown(SafeHandle socket, SocketShutdown how)
        {
            bool release = false;
            try
            {
                socket.DangerousAddRef(ref release);
                return DangerousShutdown((int)socket.DangerousGetHandle(), how);
            }
            finally
            {
                if (release)
                {
                    socket.DangerousRelease();
                }
            }
        }
    }
}
