// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.JavaScript;

namespace System.Net.NetworkInformation
{
    internal static partial class BrowserNetworkInterfaceInterop
    {
        [JSImport("INTERNAL.network_wasm_online")]
        public static partial bool IsOnline();

        [JSImport("INTERNAL.network_wasm_add_change_listener")]
        public static partial void AddChangeListener([JSMarshalAs<JSType.Function<JSType.Boolean>>] Action<bool> handler);
    }

    internal static class NetworkInterfacePal
    {
        public static NetworkInterface[] GetAllNetworkInterfaces() => throw new PlatformNotSupportedException();
        public static int IPv6LoopbackInterfaceIndex => throw new PlatformNotSupportedException();
        public static int LoopbackInterfaceIndex => throw new PlatformNotSupportedException();

        public static bool GetIsNetworkAvailable()
        {
            return BrowserNetworkInterfaceInterop.IsOnline();
        }
    }
}
