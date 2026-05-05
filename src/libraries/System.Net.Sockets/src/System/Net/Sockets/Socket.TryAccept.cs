// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Net.Sockets
{
    public partial class Socket
    {
        /// <summary>
        /// Attempts to accept a pending connection without blocking.
        /// </summary>
        /// <param name="acceptedHandle">
        /// On success, receives the accepted connection's handle. The handle is
        /// non-blocking and CLOEXEC. The caller owns the handle and is responsible
        /// for disposing it.
        /// On would-block, receives <see langword="null"/>.
        /// </param>
        /// <param name="remoteEndPoint">
        /// On success, receives the peer's address, captured in the same syscall
        /// as the accept (no separate <c>getpeername</c> call).
        /// On would-block, receives <see langword="null"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if a connection was accepted;
        /// <see langword="false"/> if no connection was pending (would block / EAGAIN).
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The socket is not bound and listening.
        /// </exception>
        /// <exception cref="SocketException">
        /// An error other than would-block occurred during the accept.
        /// </exception>
        /// <remarks>
        /// <para>
        /// This method is designed for high-performance accept loops driven by a
        /// readiness poll mechanism (e.g., <see cref="Threading.SafePollHandle"/>).
        /// It avoids the overhead of creating a full <see cref="Socket"/> wrapper
        /// for the accepted connection.
        /// </para>
        /// <para>
        /// On Linux, uses <c>accept4(SOCK_NONBLOCK | SOCK_CLOEXEC)</c> under the hood.
        /// On macOS/FreeBSD, falls back to <c>accept()</c> followed by
        /// <c>fcntl(O_NONBLOCK | FD_CLOEXEC)</c>.
        /// </para>
        /// </remarks>
        [UnsupportedOSPlatform("windows")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("wasi")]
        public bool TryAccept(
            out SafeSocketHandle? acceptedHandle,
            out EndPoint? remoteEndPoint)
        {
            ThrowIfDisposed();

            if (_rightEndPoint is null)
            {
                throw new InvalidOperationException(SR.net_sockets_mustbind);
            }

            if (!_isListening)
            {
                throw new InvalidOperationException(SR.net_sockets_mustlisten);
            }

            SocketAddress socketAddress = new SocketAddress(_addressFamily);

            SocketError errorCode = SocketPal.Accept(
                _handle,
                socketAddress.Buffer,
                out int socketAddressLen,
                out SafeSocketHandle accepted);

            if (errorCode == SocketError.WouldBlock)
            {
                accepted.Dispose();
                acceptedHandle = null;
                remoteEndPoint = null;
                return false;
            }

            if (errorCode != SocketError.Success)
            {
                accepted.Dispose();
                throw new SocketException((int)errorCode);
            }

            socketAddress.Size = socketAddressLen;
            acceptedHandle = accepted;
            remoteEndPoint = _rightEndPoint.Create(socketAddress);
            return true;
        }
    }
}
