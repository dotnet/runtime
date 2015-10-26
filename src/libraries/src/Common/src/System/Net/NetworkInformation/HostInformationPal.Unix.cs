// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
