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
        private const AddressFamily EndPointAddressFamily = AddressFamily.Unix;

        private static readonly Encoding s_pathEncoding = Encoding.UTF8;
        private static readonly Lazy<bool> s_udsSupported = new Lazy<bool>(() =>
        {
            try
            {
                new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified).Dispose();
                return true;
            }
            catch
            {
                return false;
            }
        });

        private readonly string _path;
        private readonly byte[] _encodedPath;

        public UnixDomainSocketEndPoint(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            // Pathname socket addresses should be null-terminated.
            // Linux abstract socket addresses start with a zero byte, they must not be null-terminated.
            bool isAbstract = IsAbstract(path);
            int bufferLength = s_pathEncoding.GetByteCount(path);
            if (!isAbstract)
            {
                // for null terminator
                bufferLength++;
            }

            if (path.Length == 0 || bufferLength > s_nativePathLength)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(path), path,
                    SR.Format(SR.ArgumentOutOfRange_PathLengthInvalid, path, s_nativePathLength));
            }

            _path = path;
            _encodedPath = new byte[bufferLength];
            int bytesEncoded = s_pathEncoding.GetBytes(path, 0, path.Length, _encodedPath, 0);
            Debug.Assert(bufferLength - (isAbstract ? 0 : 1) == bytesEncoded);

            if (!s_udsSupported.Value)
            {
                throw new PlatformNotSupportedException();
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

                // Strip trailing null of pathname socket addresses.
                int length = _encodedPath.Length;
                if (!IsAbstract(_encodedPath))
                {
                    // Since this isn't an abstract path, we're sure our first byte isn't 0.
                    while (_encodedPath[length - 1] == 0)
                    {
                        length--;
                    }
                }
                _path = s_pathEncoding.GetString(_encodedPath, 0, length);
            }
            else
            {
                _encodedPath = Array.Empty<byte>();
                _path = string.Empty;
            }
        }

        public override SocketAddress Serialize()
        {
            SocketAddress result = CreateSocketAddressForSerialize();

            for (int index = 0; index < _encodedPath.Length; index++)
            {
                result[s_nativePathOffset + index] = _encodedPath[index];
            }

            return result;
        }

        public override EndPoint Create(SocketAddress socketAddress) => new UnixDomainSocketEndPoint(socketAddress);

        public override AddressFamily AddressFamily => EndPointAddressFamily;

        public override string ToString()
        {
            bool isAbstract = IsAbstract(_path);
            if (isAbstract)
            {
                return string.Concat("@", _path.AsSpan(1));
            }
            else
            {
                return _path;
            }
        }

        private static bool IsAbstract(string path) => path.Length > 0 && path[0] == '\0';

        private static bool IsAbstract(byte[] encodedPath) => encodedPath.Length > 0 && encodedPath[0] == 0;
    }
}
