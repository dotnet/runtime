// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net
{
    internal static class IPEndPointStatics
    {
        internal const int AnyPort = IPEndPoint.MinPort;
        internal readonly static IPEndPoint Any = new IPEndPoint(IPAddress.Any, AnyPort);
        internal readonly static IPEndPoint IPv6Any = new IPEndPoint(IPAddress.IPv6Any, AnyPort);
    }
}
