// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;

namespace System.Net.Sockets
{
    /// <summary>Represents a Unix Domain Socket endpoint as a path.</summary>
    public sealed partial class UnixDomainSocketEndPoint : EndPoint
    {
        private static readonly int s_nativePathOffset;
        private static readonly int s_nativePathLength;
        private static readonly int s_nativeAddressSize;

        static UnixDomainSocketEndPoint()
        {
            Interop.Sys.GetDomainSocketSizes(out s_nativePathOffset, out s_nativePathLength, out s_nativeAddressSize);

            Debug.Assert(s_nativePathOffset >= 0, "Expected path offset to be positive");
            Debug.Assert(s_nativePathOffset + s_nativePathLength <= s_nativeAddressSize, "Expected address size to include all of the path length");
            Debug.Assert(s_nativePathLength >= 92, "Expected max path length to be at least 92"); // per http://pubs.opengroup.org/onlinepubs/9699919799/basedefs/sys_un.h.html
        }

        private SocketAddress CreateSocketAddressForSerialize() =>
            new SocketAddress(AddressFamily.Unix, s_nativePathOffset + _encodedPath.Length);
    }
}
