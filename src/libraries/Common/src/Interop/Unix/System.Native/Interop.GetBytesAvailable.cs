// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetBytesAvailable")]
        private static extern unsafe Error DangerousGetBytesAvailable(int socket, int* available);

        internal static unsafe Error GetBytesAvailable(SafeHandle socket, int* available)
        {
            bool release = false;
            try
            {
                socket.DangerousAddRef(ref release);
                return DangerousGetBytesAvailable((int)socket.DangerousGetHandle(), available);
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
