// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.NetworkInformation
{
    internal static class NetworkInterfacePal
    {
        public static NetworkInterface[] GetAllNetworkInterfaces() => throw new PlatformNotSupportedException();
        public static int IPv6LoopbackInterfaceIndex => throw new PlatformNotSupportedException();
        public static int LoopbackInterfaceIndex => throw new PlatformNotSupportedException();

        public static bool GetIsNetworkAvailable() => BrowserNetworkInterfaceInterop.IsOnline();
    }
}
