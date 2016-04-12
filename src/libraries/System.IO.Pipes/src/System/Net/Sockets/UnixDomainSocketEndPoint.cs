// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;

namespace System.Net.Sockets
{
    /// <summary>Represents a Unix Domain Socket endpoint as a path.</summary>
    internal sealed class UnixDomainSocketEndPoint : EndPoint
    {
        private const AddressFamily EndPointAddressFamily = AddressFamily.Unix;

        private static readonly Encoding s_pathEncoding = Encoding.UTF8;
        private static readonly int s_nativePathOffset;
        private static readonly int s_nativePathLength;
        private static readonly int s_nativeAddressSize;

        private readonly string _path;
        private readonly byte[] _encodedPath;

        static UnixDomainSocketEndPoint()
        {
            Interop.Sys.GetDomainSocketSizes(out s_nativePathOffset, out s_nativePathLength, out s_nativeAddressSize);

            Debug.Assert(s_nativePathOffset >= 0, "Expected path offset to be positive");
            Debug.Assert(s_nativePathOffset + s_nativePathLength <= s_nativeAddressSize, "Expected address size to include all of the path length");
            Debug.Assert(s_nativePathLength >= 92, "Expected max path length to be at least 92"); // per http://pubs.opengroup.org/onlinepubs/9699919799/basedefs/sys_un.h.html

            s_nativePathLength -= 1; // to account for null terminator within the allotted space
        }

        public UnixDomainSocketEndPoint(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            _path = path;
            _encodedPath = s_pathEncoding.GetBytes(_path);

            if (path.Length == 0 || _encodedPath.Length > s_nativePathLength)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(path), path, 
                    SR.Format(SR.ArgumentOutOfRange_PathLengthInvalid, path, s_nativePathLength));
            }
        }

        internal UnixDomainSocketEndPoint(SocketAddress socketAddress)
        {
            if (socketAddress == null)
            {
                throw new ArgumentNullException(nameof(socketAddress));
            }

            if (socketAddress.Family != EndPointAddressFamily || 
                socketAddress.Size > s_nativeAddressSize)
            {
                throw new ArgumentOutOfRangeException(nameof(socketAddress));
            }

            if (socketAddress.Size > s_nativePathOffset)
            {
                _encodedPath = new byte[socketAddress.Size - s_nativePathOffset];
                for (int i = 0; i < _encodedPath.Length; i++)
                {
                    _encodedPath[i] = socketAddress[s_nativePathOffset + i];
                }

                _path = s_pathEncoding.GetString(_encodedPath, 0, _encodedPath.Length);
            }
            else
            {
                _encodedPath = Array.Empty<byte>();
                _path = string.Empty;
            }
        }

        public override SocketAddress Serialize()
        {
            var result = new SocketAddress(AddressFamily.Unix, s_nativeAddressSize);
            Debug.Assert(_encodedPath.Length + s_nativePathOffset <= result.Size, "Expected path to fit in address");

            for (int index = 0; index < _encodedPath.Length; index++)
            {
                result[s_nativePathOffset + index] = _encodedPath[index];
            }
            result[s_nativePathOffset + _encodedPath.Length] = 0; // path must be null-terminated

            return result;
        }

        public override EndPoint Create(SocketAddress socketAddress) => new UnixDomainSocketEndPoint(socketAddress);

        public override AddressFamily AddressFamily => EndPointAddressFamily;

        public override string ToString() => _path;
    }
}
