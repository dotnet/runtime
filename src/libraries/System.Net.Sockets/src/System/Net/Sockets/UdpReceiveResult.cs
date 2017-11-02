// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Sockets
{
    /// <summary>
    /// Presents UDP receive result information from a call to the <see cref="UdpClient.ReceiveAsync"/> method
    /// </summary>
    public struct UdpReceiveResult : IEquatable<UdpReceiveResult> 
    {
        private byte[] _buffer;
        private IPEndPoint _remoteEndPoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpReceiveResult"/> class
        /// </summary>
        /// <param name="buffer">A buffer for data to receive in the UDP packet</param>
        /// <param name="remoteEndPoint">The remote endpoint of the UDP packet</param>
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

        /// <summary>
        /// Gets a buffer with the data received in the UDP packet
        /// </summary>
        public byte[] Buffer
        {
            get
            {
                return _buffer;
            }
        }

        /// <summary>
        /// Gets the remote endpoint from which the UDP packet was received
        /// </summary>
        public IPEndPoint RemoteEndPoint
        {
            get
            {
                return _remoteEndPoint;
            }
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code</returns>
        public override int GetHashCode()
        {
            return (_buffer != null) ? (_buffer.GetHashCode() ^ _remoteEndPoint.GetHashCode()) : 0;
        }

        /// <summary>
        /// Returns a value that indicates whether this instance is equal to a specified object
        /// </summary>
        /// <param name="obj">The object to compare with this instance</param>
        /// <returns>true if obj is an instance of <see cref="UdpReceiveResult"/> and equals the value of the instance; otherwise, false</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is UdpReceiveResult))
            {
                return false;
            }

            return Equals((UdpReceiveResult)obj);
        }

        /// <summary>
        /// Returns a value that indicates whether this instance is equal to a specified object
        /// </summary>
        /// <param name="other">The object to compare with this instance</param>
        /// <returns>true if other is an instance of <see cref="UdpReceiveResult"/> and equals the value of the instance; otherwise, false</returns>
        public bool Equals(UdpReceiveResult other)
        {
            return object.Equals(_buffer, other._buffer) && object.Equals(_remoteEndPoint, other._remoteEndPoint);
        }

        /// <summary>
        /// Tests whether two specified <see cref="UdpReceiveResult"/> instances are equivalent
        /// </summary>
        /// <param name="left">The <see cref="UdpReceiveResult"/> instance that is to the left of the equality operator</param>
        /// <param name="right">The <see cref="UdpReceiveResult"/> instance that is to the right of the equality operator</param>
        /// <returns>true if left and right are equal; otherwise, false</returns>
        public static bool operator ==(UdpReceiveResult left, UdpReceiveResult right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Tests whether two specified <see cref="UdpReceiveResult"/> instances are not equal
        /// </summary>
        /// <param name="left">The <see cref="UdpReceiveResult"/> instance that is to the left of the not equal operator</param>
        /// <param name="right">The <see cref="UdpReceiveResult"/> instance that is to the right of the not equal operator</param>
        /// <returns>true if left and right are unequal; otherwise, false</returns>
        public static bool operator !=(UdpReceiveResult left, UdpReceiveResult right)
        {
            return !left.Equals(right);
        }
    }
}
