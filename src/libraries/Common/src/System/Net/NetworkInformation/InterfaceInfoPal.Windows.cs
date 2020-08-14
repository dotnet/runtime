// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.NetworkInformation
{
    internal static class InterfaceInfoPal
    {
        public static uint InterfaceNameToIndex(string interfaceName)
        {
            return Interop.IpHlpApi.if_nametoindex(interfaceName);
        }
    }
}
