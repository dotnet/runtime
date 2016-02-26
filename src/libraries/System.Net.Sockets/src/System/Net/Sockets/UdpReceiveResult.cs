// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Sockets
{
    public struct UdpReceiveResult : IEquatable<UdpReceiveResult>
    {
        private byte[] _buffer;
        private IPEndPoint _remoteEndPoint;

        public UdpReceiveResult(byte[] buffer, IPEndPoint remoteEndPoint)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (remoteEndPoint == null)
            {
                throw new ArgumentNullException(nameof(remoteEndPoint));
            }

            _buffer = buffer;
            _remoteEndPoint = remoteEndPoint;
        }

        public byte[] Buffer
        {
            get
            {
                return _buffer;
            }
        }

        public IPEndPoint RemoteEndPoint
        {
            get
            {
                return _remoteEndPoint;
            }
        }

        public override int GetHashCode()
        {
            return (_buffer != null) ? (_buffer.GetHashCode() ^ _remoteEndPoint.GetHashCode()) : 0;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is UdpReceiveResult))
            {
                return false;
            }

            return Equals((UdpReceiveResult)obj);
        }

        public bool Equals(UdpReceiveResult other)
        {
            return object.Equals(_buffer, other._buffer) && object.Equals(_remoteEndPoint, other._remoteEndPoint);
        }

        public static bool operator ==(UdpReceiveResult left, UdpReceiveResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UdpReceiveResult left, UdpReceiveResult right)
        {
            return !left.Equals(right);
        }
    }
}
