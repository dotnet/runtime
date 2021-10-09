// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Authentication;

namespace System.Net
{
    internal static class SslProtocolsValidation
    {
        public static (int MinIndex, int MaxIndex) ValidateContiguous(this SslProtocols protocols, SslProtocols[] orderedSslProtocols)
        {
            // A contiguous range of protocols is required.  Find the min and max of the range,
            // or throw if it's non-contiguous or if no protocols are specified.

            // First, mark all of the specified protocols.
            Span<bool> protocolSet = stackalloc bool[orderedSslProtocols.Length];
            for (int i = 0; i < orderedSslProtocols.Length; i++)
            {
                protocolSet[i] = (protocols & orderedSslProtocols[i]) != 0;
            }

            int minIndex = -1;
            int maxIndex = -1;

            // Loop through them, starting from the lowest.
            for (int min = 0; min < protocolSet.Length; min++)
            {
                if (protocolSet[min])
                {
                    // We found the first one that's set; that's the bottom of the range.
                    minIndex = min;

                    // Now loop from there to look for the max of the range.
                    for (int max = min + 1; max < protocolSet.Length; max++)
                    {
                        if (!protocolSet[max])
                        {
                            // We found the first one after the min that's not set; the top of the range
                            // is the one before this (which might be the same as the min).
                            maxIndex = max - 1;

                            // Finally, verify that nothing beyond this one is set, as that would be
                            // a discontiguous set of protocols.
                            for (int verifyNotSet = max + 1; verifyNotSet < protocolSet.Length; verifyNotSet++)
                            {
                                if (protocolSet[verifyNotSet])
                                {
                                    throw new PlatformNotSupportedException(SR.Format(SR.net_security_sslprotocol_contiguous, protocols));
                                }
                            }

                            break;
                        }
                    }

                    break;
                }
            }

            // If no protocols were set, throw.
            if (minIndex == -1)
            {
                throw new PlatformNotSupportedException(SR.net_securityprotocolnotsupported);
            }

            // If we didn't find an unset protocol after the min, go all the way to the last one.
            if (maxIndex == -1)
            {
                maxIndex = orderedSslProtocols.Length - 1;
            }

            return (minIndex, maxIndex);
        }
    }
}
