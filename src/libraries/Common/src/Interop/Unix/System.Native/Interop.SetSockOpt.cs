// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetSockOpt")]
        internal static unsafe partial Error SetSockOpt(SafeHandle socket, SocketOptionLevel optionLevel, SocketOptionName optionName, byte* optionValue, int optionLen);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetSockOpt")]
        internal static unsafe partial Error SetSockOpt(IntPtr socket, SocketOptionLevel optionLevel, SocketOptionName optionName, byte* optionValue, int optionLen);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetRawSockOpt")]
        internal static unsafe partial Error SetRawSockOpt(SafeHandle socket, int optionLevel, int optionName, byte* optionValue, int optionLen);
    }
}
