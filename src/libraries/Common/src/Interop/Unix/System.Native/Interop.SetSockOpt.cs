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
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetSockOpt")]
        internal static extern unsafe Error SetSockOpt(SafeHandle socket, SocketOptionLevel optionLevel, SocketOptionName optionName, byte* optionValue, int optionLen);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetSockOpt")]
        internal static extern unsafe Error SetSockOpt(IntPtr socket, SocketOptionLevel optionLevel, SocketOptionName optionName, byte* optionValue, int optionLen);
    }
}
