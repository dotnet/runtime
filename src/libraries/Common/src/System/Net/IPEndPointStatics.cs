// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net
{
    internal static class IPEndPointStatics
    {
        internal const int AnyPort = IPEndPoint.MinPort;
        internal readonly static IPEndPoint Any = new IPEndPoint(IPAddress.Any, AnyPort);
        internal readonly static IPEndPoint IPv6Any = new IPEndPoint(IPAddress.IPv6Any, AnyPort);
    }
}
