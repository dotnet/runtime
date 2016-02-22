// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal struct LingerOption
        {
            public int OnOff;   // Non-zero to enable linger
            public int Seconds; // Number of seconds to linger for
        }

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetLingerOption")]
        private static extern unsafe Error DangerousGetLingerOption(int socket, LingerOption* option);

        internal static unsafe Error GetLingerOption(SafeHandle socket, LingerOption* option)
        {
            bool release = false;
            try
            {
                socket.DangerousAddRef(ref release);
                return DangerousGetLingerOption((int)socket.DangerousGetHandle(), option);
            }
            finally
            {
                if (release)
                {
                    socket.DangerousRelease();
                }
            }
        }

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetLingerOption")]
        internal static extern unsafe Error DangerousSetLingerOption(int socket, LingerOption* option);

        internal static unsafe Error SetLingerOption(SafeHandle socket, LingerOption* option)
        {
            bool release = false;
            try
            {
                socket.DangerousAddRef(ref release);
                return DangerousSetLingerOption((int)socket.DangerousGetHandle(), option);
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
