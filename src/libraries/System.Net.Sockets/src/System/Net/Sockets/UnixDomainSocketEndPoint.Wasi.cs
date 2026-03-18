// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;

namespace System.Net.Sockets
{
#pragma warning disable CA1822
    public sealed partial class UnixDomainSocketEndPoint : System.Net.EndPoint
    {
        public UnixDomainSocketEndPoint(string path)
        {
            throw new PlatformNotSupportedException(); // TODO remove with https://github.com/dotnet/runtime/pull/107185
        }

        internal UnixDomainSocketEndPoint(ReadOnlySpan<byte> socketAddress)
        {
            throw new PlatformNotSupportedException(); // TODO remove with https://github.com/dotnet/runtime/pull/107185
        }

        internal string? BoundFileName
        {
            get
            {
                throw new PlatformNotSupportedException(); // TODO remove with https://github.com/dotnet/runtime/pull/107185
            }
        }

        public override System.Net.Sockets.AddressFamily AddressFamily
        {
            get
            {
                throw new PlatformNotSupportedException(); // TODO remove with https://github.com/dotnet/runtime/pull/107185
            }
        }

        public override System.Net.EndPoint Create(System.Net.SocketAddress socketAddress)
        {
            throw new PlatformNotSupportedException(); // TODO remove with https://github.com/dotnet/runtime/pull/107185
        }

        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj)
        {
            throw new PlatformNotSupportedException(); // TODO remove with https://github.com/dotnet/runtime/pull/107185
        }

        public override int GetHashCode()
        {
            throw new PlatformNotSupportedException(); // TODO remove with https://github.com/dotnet/runtime/pull/107185
        }

        public override System.Net.SocketAddress Serialize()
        {
            throw new PlatformNotSupportedException(); // TODO remove with https://github.com/dotnet/runtime/pull/107185
        }

        public override string ToString()
        {
            throw new PlatformNotSupportedException(); // TODO remove with https://github.com/dotnet/runtime/pull/107185
        }

        internal UnixDomainSocketEndPoint CreateBoundEndPoint()
        {
            throw new PlatformNotSupportedException(); // TODO remove with https://github.com/dotnet/runtime/pull/107185
        }

        internal UnixDomainSocketEndPoint CreateUnboundEndPoint()
        {
            throw new PlatformNotSupportedException(); // TODO remove with https://github.com/dotnet/runtime/pull/107185
        }
    }
}
