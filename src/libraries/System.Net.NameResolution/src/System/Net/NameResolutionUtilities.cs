// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
