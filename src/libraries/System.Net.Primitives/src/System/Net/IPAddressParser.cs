// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;
using System.Text;

namespace System.Net
{
    internal class IPAddressParser
    {
        internal const int INET_ADDRSTRLEN = 22;
        internal const int INET6_ADDRSTRLEN = 65;

        internal static unsafe IPAddress Parse(string ipString, bool tryParse)
        {
            if (ipString == null)
            {
                if (tryParse)
                {
                    return null;
                }
                throw new ArgumentNullException(nameof(ipString));
            }

            uint error = 0;

            // IPv6 Changes: Detect probable IPv6 addresses and use separate parse method.
            if (ipString.IndexOf(':') != -1)
            {
                // If the address string contains the colon character
                // then it can only be an IPv6 address. Use a separate
                // parse method to unpick it all. Note: we don't support
                // port specification at the end of address and so can
                // make this decision.
                uint scope;
                byte* bytes = stackalloc byte[IPAddressParserStatics.IPv6AddressBytes];
                error = IPAddressPal.Ipv6StringToAddress(ipString, bytes, IPAddressParserStatics.IPv6AddressBytes, out scope);

                if (error == IPAddressPal.SuccessErrorCode)
                {
                    // AppCompat: .Net 4.5 ignores a correct port if the address was specified in brackets.
                    // Will still throw for an incorrect port.
                    return new IPAddress(bytes, IPAddressParserStatics.IPv6AddressBytes, (long)scope);
                }
            }
            else
            {
                ushort port;
                byte* bytes = stackalloc byte[IPAddressParserStatics.IPv4AddressBytes];
                error = IPAddressPal.Ipv4StringToAddress(ipString, bytes, IPAddressParserStatics.IPv4AddressBytes, out port);

                if (error == IPAddressPal.SuccessErrorCode)
                {
                    if (port != 0)
                    {
                        throw new FormatException(SR.dns_bad_ip_address);
                    }

                    return new IPAddress(bytes, IPAddressParserStatics.IPv4AddressBytes);
                }
            }

            if (tryParse)
            {
                return null;
            }

            Exception e = new SocketException(IPAddressPal.GetSocketErrorForErrorCode(error), error);
            throw new FormatException(SR.dns_bad_ip_address, e);
        }

        internal static string IPv4AddressToString(byte[] numbers)
        {
            StringBuilder sb = new StringBuilder(INET_ADDRSTRLEN);
            uint errorCode = IPAddressPal.Ipv4AddressToString(numbers, sb);

            if (errorCode == IPAddressPal.SuccessErrorCode)
            {
                return sb.ToString();
            }
            else
            {
                throw new SocketException(IPAddressPal.GetSocketErrorForErrorCode(errorCode), errorCode);
            }
        }

        internal static string IPv6AddressToString(byte[] numbers, uint scopeId)
        {
            StringBuilder sb = new StringBuilder(INET6_ADDRSTRLEN);
            uint errorCode = IPAddressPal.Ipv6AddressToString(numbers, scopeId, sb);

            if (errorCode == IPAddressPal.SuccessErrorCode)
            {
                return sb.ToString();
            }
            else
            {
                throw new SocketException(IPAddressPal.GetSocketErrorForErrorCode(errorCode), errorCode);
            }
        }
    }
}
