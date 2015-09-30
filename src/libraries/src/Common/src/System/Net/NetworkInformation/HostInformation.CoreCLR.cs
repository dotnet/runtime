// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    internal class HostInformation
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
