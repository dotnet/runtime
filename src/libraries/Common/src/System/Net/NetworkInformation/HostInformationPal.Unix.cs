// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.NetworkInformation
{
    internal static class HostInformationPal
    {
        public static string GetHostName()
        {
            return Interop.Sys.GetHostName();
        }

        public static string GetDomainName()
        {
            return Interop.Sys.GetDomainName();
        }
    }
}
