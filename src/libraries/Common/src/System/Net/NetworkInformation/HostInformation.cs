// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.NetworkInformation
{
    internal static class HostInformation
    {
        // Specifies the host name for the local computer.
        internal static string HostName
        {
            get
            {
                return HostInformationPal.GetHostName();
            }
        }

        // Specifies the domain in which the local computer is registered.
        internal static string DomainName
        {
            get
            {
                return HostInformationPal.GetDomainName();
            }
        }
    }
}
