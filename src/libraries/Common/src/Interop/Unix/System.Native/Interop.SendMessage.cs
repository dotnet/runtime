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
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_SendMessage")]
        private static extern unsafe Error DangerousSendMessage(int socket, MessageHeader* messageHeader, SocketFlags flags, long* sent);

        internal static unsafe Error SendMessage(SafeHandle socket, MessageHeader* messageHeader, SocketFlags flags, long* sent)
        {
            bool release = false;
            try
            {
                socket.DangerousAddRef(ref release);
                return DangerousSendMessage((int)socket.DangerousGetHandle(), messageHeader, flags, sent);
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
