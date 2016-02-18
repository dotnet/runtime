// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace System.Net.Sockets
{
    /// <summary>Represents a Unix Domain Socket endpoint as a path.</summary>
    public sealed class UnixDomainSocketEndPoint : EndPoint
    {
        private const int MaxPathLength = 92;   // sockaddr_un.sun_path at http://pubs.opengroup.org/onlinepubs/9699919799/basedefs/sys_un.h.html
        private const int PathOffset = 2;       // = offsetof(struct sockaddr_un, sun_path). It's the same on Linux and OSX
        private const int MaxSocketAddressSize = PathOffset + MaxPathLength;
        private const int MinSocketAddressSize = PathOffset + 2; // +1 for one character and +1 for \0 ending
        private const AddressFamily EndPointAddressFamily = AddressFamily.Unix;

        private static readonly Encoding s_pathEncoding = Encoding.UTF8;
        private readonly string _path;
        private readonly byte[] _encodedPath;

        public UnixDomainSocketEndPoint(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (path.Length == 0 || s_pathEncoding.GetByteCount(path) >= MaxPathLength)
            {
                throw new ArgumentOutOfRangeException(nameof(path));
            }

            _path = path;
            _encodedPath = s_pathEncoding.GetBytes(_path);
        }

        internal UnixDomainSocketEndPoint(SocketAddress socketAddress)
        {
            if (socketAddress == null)
            {
                throw new ArgumentNullException(nameof(socketAddress));
            }

            if (socketAddress.Family != EndPointAddressFamily || 
                socketAddress.Size > MaxSocketAddressSize)
            {
                throw new ArgumentOutOfRangeException(nameof(socketAddress));
            }

            if (socketAddress.Size >= MinSocketAddressSize)
            {
                _encodedPath = new byte[socketAddress.Size - PathOffset];
                for (int index = 0; index < socketAddress.Size - PathOffset; index++)
                {
                    _encodedPath[index] = socketAddress[PathOffset + index];
                }

                _path = s_pathEncoding.GetString(_encodedPath, 0, _encodedPath.Length);
            }
            else
            {
                // Empty path may be used by System.Net.Socket logging.
                _encodedPath = Array.Empty<byte>();
                _path = string.Empty;
            }
        }

        public string Path { get { return _path; } }

        public override AddressFamily AddressFamily { get { return EndPointAddressFamily; } }

        public override SocketAddress Serialize()
        {
            var result = new SocketAddress(AddressFamily.Unix, MaxSocketAddressSize);

            // Ctor has already checked that PathOffset + _encodedPath.Length < MaxSocketAddressSize
            for (int index = 0; index < _encodedPath.Length; index++)
            {
                result[PathOffset + index] = _encodedPath[index];
            }
            result[PathOffset + _encodedPath.Length] = 0; // The path must be ending with \0

            return result;
        }

        public override EndPoint Create(SocketAddress socketAddress)
        {
            return new UnixDomainSocketEndPoint(socketAddress);
        }

        public override string ToString()
        {
            return Path;
        }
    }
}
