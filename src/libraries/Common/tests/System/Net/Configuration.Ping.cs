// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Test.Common
{
    public static partial class Configuration
    {
        public static partial class Ping
        {
           // Host not on same network with ability to respond to ICMP Echo
           public static string PingHost => GetValue("COREFX_NET_PING_HOST", "www.microsoft.com");
        }
    }
}
