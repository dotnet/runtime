// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetSockOpt")]
        internal static extern unsafe Error GetSockOpt(SafeHandle socket, SocketOptionLevel optionLevel, SocketOptionName optionName, byte* optionValue, int* optionLen);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetSockOpt")]
        internal static extern unsafe Error GetSockOpt(IntPtr socket, SocketOptionLevel optionLevel, SocketOptionName optionName, byte* optionValue, int* optionLen);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetRawSockOpt")]
        internal static extern unsafe Error GetRawSockOpt(SafeHandle socket, int optionLevel, int optionName, byte* optionValue, int* optionLen);
    }
}
