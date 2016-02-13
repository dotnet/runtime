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
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetSocketErrorOption")]
        private static extern unsafe Error DangerousGetSocketErrorOption(int socket, Error* socketError);

        internal static unsafe Error GetSocketErrorOption(SafeHandle socket, Error* socketError)
        {
            bool release = false;
            try
            {
                socket.DangerousAddRef(ref release);
                return DangerousGetSocketErrorOption((int)socket.DangerousGetHandle(), socketError);
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
