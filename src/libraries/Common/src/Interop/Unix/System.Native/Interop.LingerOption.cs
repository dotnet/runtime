// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        internal static extern unsafe Error GetLingerOption(int socket, LingerOption* option);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetLingerOption")]
        internal static extern unsafe Error SetLingerOption(int socket, LingerOption* option);
    }
}
