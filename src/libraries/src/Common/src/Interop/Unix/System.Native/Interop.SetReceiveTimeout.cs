// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetReceiveTimeout")]
        private static extern unsafe Error DangerousSetReceiveTimeout(int socket, int millisecondsTimeout);

        internal static unsafe Error SetReceiveTimeout(SafeHandle socket, int millisecondsTimeout)
        {
            bool release = false;
            try
            {
                socket.DangerousAddRef(ref release);
                return DangerousSetReceiveTimeout((int)socket.DangerousGetHandle(), millisecondsTimeout);
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
