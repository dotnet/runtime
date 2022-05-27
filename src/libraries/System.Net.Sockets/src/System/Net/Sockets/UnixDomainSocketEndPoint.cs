// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using System.IO;

namespace System.Net.Sockets
{
    /// <summary>Represents a Unix Domain Socket endpoint as a path.</summary>
    public sealed partial class UnixDomainSocketEndPoint : EndPoint
    {
        private const AddressFamily EndPointAddressFamily = AddressFamily.Unix;

        private readonly string _path;
        private readonly byte[] _encodedPath;

        // Tracks the file Socket should delete on Dispose.
        internal string? BoundFileName { get; }

        public UnixDomainSocketEndPoint(string path)
            : this(path, null)
        { }

        private UnixDomainSocketEndPoint(string path, string? boundFileName)
        {
            ArgumentNullException.ThrowIfNull(path);

            BoundFileName = boundFileName;

            // Pathname socket addresses should be null-terminated.
            // Linux abstract socket addresses start with a zero byte, they must not be null-terminated.
            bool isAbstract = IsAbstract(path);
            int bufferLength = Encoding.UTF8.GetByteCount(path);
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
            int bytesEncoded = Encoding.UTF8.GetBytes(path, 0, path.Length, _encodedPath, 0);
            Debug.Assert(bufferLength - (isAbstract ? 0 : 1) == bytesEncoded);

            if (!Socket.OSSupportsUnixDomainSockets)
            {
                throw new PlatformNotSupportedException();
            }
        }

        internal static int MaxAddressSize => s_nativeAddressSize;

        internal UnixDomainSocketEndPoint(SocketAddress socketAddress)
        {
            ArgumentNullException.ThrowIfNull(socketAddress);

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
                _path = Encoding.UTF8.GetString(_encodedPath, 0, length);
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

        internal UnixDomainSocketEndPoint CreateBoundEndPoint()
        {
            if (IsAbstract(_path))
            {
                return this;
            }
            return new UnixDomainSocketEndPoint(_path, Path.GetFullPath(_path));
        }

        internal UnixDomainSocketEndPoint CreateUnboundEndPoint()
        {
            if (IsAbstract(_path) || BoundFileName is null)
            {
                return this;
            }
            return new UnixDomainSocketEndPoint(_path, null);
        }

        private static bool IsAbstract(string path) => path.Length > 0 && path[0] == '\0';

        private static bool IsAbstract(byte[] encodedPath) => encodedPath.Length > 0 && encodedPath[0] == 0;
    }
}
