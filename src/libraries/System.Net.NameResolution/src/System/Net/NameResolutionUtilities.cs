// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net
{
    internal static class NameResolutionUtilities
    {
        public static IPHostEntry GetUnresolvedAnswer(IPAddress address)
        {
            return new IPHostEntry
            {
                HostName = address.ToString(),
                Aliases = Array.Empty<string>(),
                AddressList = new IPAddress[] { address }
            };
        }

        public static IPHostEntry GetUnresolvedAnswer(string name)
        {
            return new IPHostEntry
            {
                HostName = name,
                Aliases = Array.Empty<string>(),
                AddressList = Array.Empty<IPAddress>()
            };
        }
    }
}
