// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Test.Common
{
    public static partial class Configuration
    {
        public static partial class Sockets
        {
           public static Uri SocketServer => GetUriValue("COREFX_NET_SOCKETS_SERVERURI", new Uri("http://" + DefaultAzureServer));
        }
    }
}
