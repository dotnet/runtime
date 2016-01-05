// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_PlatformSupportsMultipleConnectAttempts")]
        internal static extern bool PlatformSupportsMultipleConnectAttempts();

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_PlatformSupportsDualModeIPv4PacketInfo")]
        internal static extern bool PlatformSupportsDualModeIPv4PacketInfo();
    }
}
