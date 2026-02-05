// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http
{
    internal static partial class HttpUtilities
    {
        public static string ParseHostNameFromHeader(string hostHeader)
        {
            // See if we need to trim off a port.
            int colonPos = hostHeader.IndexOf(':');
            if (colonPos >= 0)
            {
                // There is colon, which could either be a port separator or a separator in
                // an IPv6 address.  See if this is an IPv6 address; if it's not, use everything
                // before the colon as the host name, and if it is, use everything before the last
                // colon iff the last colon is after the end of the IPv6 address (otherwise it's a
                // part of the address).
                int ipV6AddressEnd = hostHeader.IndexOf(']');
                if (ipV6AddressEnd == -1)
                {
                    return hostHeader.Substring(0, colonPos);
                }
                else
                {
                    colonPos = hostHeader.LastIndexOf(':');
                    if (colonPos > ipV6AddressEnd)
                    {
                        return hostHeader.Substring(0, colonPos);
                    }
                }
            }

            return hostHeader;
        }
    }
}
