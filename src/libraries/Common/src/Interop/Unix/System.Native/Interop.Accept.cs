// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_Accept")]
        private static extern unsafe Error DangerousAccept(int socket, byte* socketAddress, int* socketAddressLen, int* acceptedFd);

        internal static unsafe Error Accept(SafeHandle socket, byte* socketAddress, int* socketAddressLen, int* acceptedFd)
        {
            bool release = false;
            try
            {
                socket.DangerousAddRef(ref release);
                return DangerousAccept((int)socket.DangerousGetHandle(), socketAddress, socketAddressLen, acceptedFd);
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
