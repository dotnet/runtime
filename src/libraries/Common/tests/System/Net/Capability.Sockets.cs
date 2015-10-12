// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Sockets;

namespace System.Net.Test.Common
{
    public static partial class Capability
    {
        public static bool IPv6Support()
        {
            return Socket.OSSupportsIPv6;
        }

        public static bool IPv4Support()
        {
            return Socket.OSSupportsIPv4;
        }
    }
}
