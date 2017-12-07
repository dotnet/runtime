// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Sockets
{
    /// <summary>Represents a Unix Domain Socket endpoint as a path.</summary>
    public sealed class UnixDomainSocketEndPoint : EndPoint
    {
        public UnixDomainSocketEndPoint(string path)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
