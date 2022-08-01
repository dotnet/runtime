// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Sockets
{
    public partial class Socket
    {
        [SupportedOSPlatform("windows")]
        public Socket(SocketInformation socketInformation)
        {
            // This constructor works in conjunction with DuplicateAndClose, which is not supported on Unix.
            // See comments in DuplicateAndClose.
            throw new PlatformNotSupportedException(SR.net_sockets_duplicateandclose_notsupported);
        }

        [SupportedOSPlatform("windows")]
        public SocketInformation DuplicateAndClose(int targetProcessId)
        {
            // DuplicateAndClose is not supported on Unix, since passing file descriptors between processes
            // requires Unix Domain Sockets. The programming model is fundamentally different,
            // and incompatible with the design of SocketInformation-related methods.
            throw new PlatformNotSupportedException(SR.net_sockets_duplicateandclose_notsupported);
        }

        internal bool PreferInlineCompletions
        {
            get => _handle.PreferInlineCompletions;
            set => _handle.PreferInlineCompletions = value;
        }

        partial void ValidateForMultiConnect(bool isMultiEndpoint)
        {
            // ValidateForMultiConnect is called before any {Begin}Connect{Async} call,
            // regardless of whether it's targeting an endpoint with multiple addresses.
            // If it is targeting such an endpoint, then any exposure of the socket's handle
            // or configuration of the socket we haven't tracked would prevent us from
            // replicating the socket's file descriptor appropriately.  Similarly, if it's
            // only targeting a single address, but it already experienced a failure in a
            // previous connect call, then this is logically part of a multi endpoint connect,
            // and the same logic applies.  Either way, in such a situation we throw.
            if (_handle.ExposedHandleOrUntrackedConfiguration && (isMultiEndpoint || _handle.LastConnectFailed))
            {
                ThrowMultiConnectNotSupported();
            }

            // If the socket was already used for a failed connect attempt, replace it
            // with a fresh one, copying over all of the state we've tracked.
            ReplaceHandleIfNecessaryAfterFailedConnect();
            Debug.Assert(!_handle.LastConnectFailed);
        }

        private static unsafe void LoadSocketTypeFromHandle(
            SafeSocketHandle handle, out AddressFamily addressFamily, out SocketType socketType, out ProtocolType protocolType, out bool blocking, out bool isListening, out bool isSocket)
        {
            if (Interop.Sys.FStat(handle, out Interop.Sys.FileStatus stat) == -1)
            {
                throw new SocketException((int)SocketError.NotSocket);
            }
            isSocket = (stat.Mode & Interop.Sys.FileTypes.S_IFSOCK) == Interop.Sys.FileTypes.S_IFSOCK;

            handle.IsSocket = isSocket;

            if (isSocket)
            {
                // On Linux, GetSocketType will be able to query SO_DOMAIN, SO_TYPE, and SO_PROTOCOL to get the
                // address family, socket type, and protocol type, respectively.  On macOS, this will only succeed
                // in getting the socket type, and the others will be unknown.  Subsequently the Socket ctor
                // can use getsockname to retrieve the address family as part of trying to get the local end point.
                Interop.Error e = Interop.Sys.GetSocketType(handle, out addressFamily, out socketType, out protocolType, out isListening);
                Debug.Assert(e == Interop.Error.SUCCESS, e.ToString());
            }
            else
            {
                addressFamily = AddressFamily.Unknown;
                socketType = SocketType.Unknown;
                protocolType = ProtocolType.Unknown;
                isListening = false;
            }

            // Get whether the socket is in non-blocking mode.  On Unix, we automatically put the underlying
            // Socket into non-blocking mode whenever an async method is first invoked on the instance, but we
            // maintain a shadow bool that maintains the Socket.Blocking value set by the developer.  Because
            // we're querying the underlying socket here, and don't have access to the original Socket instance
            // (if there even was one... the Socket(SafeSocketHandle) ctor is likely being used because there
            // wasn't one, Socket.Blocking will end up reflecting the actual state of the socket even if the
            // developer didn't set Blocking = false.
            bool nonBlocking;
            int rv = Interop.Sys.Fcntl.GetIsNonBlocking(handle, out nonBlocking);
            blocking = !nonBlocking;
            Debug.Assert(rv == 0 || blocking); // ignore failures
        }

        internal void ReplaceHandleIfNecessaryAfterFailedConnect()
        {
            if (!_handle.LastConnectFailed)
            {
                return;
            }

            SocketError errorCode = ReplaceHandle();
            if (errorCode != SocketError.Success)
            {
                throw new SocketException((int) errorCode);
            }

            _handle.LastConnectFailed = false;
        }

        internal SocketError ReplaceHandle()
        {
            // Copy out values from key options. The copied values should be kept in sync with the
            // handling in SafeSocketHandle.TrackOption.  Note that we copy these values out first, before
            // we change _handle, so that we can use the helpers on Socket which internally access _handle.
            // Then once _handle is switched to the new one, we can call the setters to propagate the retrieved
            // values back out to the new underlying socket.
            bool broadcast = false, dontFragment = false, noDelay = false;
            int receiveSize = -1, receiveTimeout = -1, sendSize = -1, sendTimeout = -1;
            short ttl = -1;
            LingerOption? linger = null;
            if (_handle.IsTrackedOption(TrackedSocketOptions.DontFragment)) dontFragment = DontFragment;
            if (_handle.IsTrackedOption(TrackedSocketOptions.EnableBroadcast)) broadcast = EnableBroadcast;
            if (_handle.IsTrackedOption(TrackedSocketOptions.LingerState)) linger = LingerState;
            if (_handle.IsTrackedOption(TrackedSocketOptions.NoDelay)) noDelay = NoDelay;
            if (_handle.IsTrackedOption(TrackedSocketOptions.ReceiveBufferSize)) receiveSize = ReceiveBufferSize;
            if (_handle.IsTrackedOption(TrackedSocketOptions.ReceiveTimeout)) receiveTimeout = ReceiveTimeout;
            if (_handle.IsTrackedOption(TrackedSocketOptions.SendBufferSize)) sendSize = SendBufferSize;
            if (_handle.IsTrackedOption(TrackedSocketOptions.SendTimeout)) sendTimeout = SendTimeout;
            if (_handle.IsTrackedOption(TrackedSocketOptions.Ttl)) ttl = Ttl;

            // Then replace the handle with a new one
            SafeSocketHandle oldHandle = _handle;
            SocketError errorCode = SocketPal.CreateSocket(_addressFamily, _socketType, _protocolType, out _handle);
            oldHandle.TransferTrackedState(_handle);
            oldHandle.Dispose();
            if (errorCode != SocketError.Success)
            {
                return errorCode;
            }

            // And put back the copied settings.  For DualMode, we use the value stored in the _handle
            // rather than querying the socket itself, as on Unix stacks binding a dual-mode socket to
            // an IPv6 address may cause the IPv6Only setting to revert to true.
            if (_handle.IsTrackedOption(TrackedSocketOptions.DualMode)) DualMode = _handle.DualMode;
            if (_handle.IsTrackedOption(TrackedSocketOptions.DontFragment)) DontFragment = dontFragment;
            if (_handle.IsTrackedOption(TrackedSocketOptions.EnableBroadcast)) EnableBroadcast = broadcast;
            if (_handle.IsTrackedOption(TrackedSocketOptions.LingerState)) LingerState = linger!;
            if (_handle.IsTrackedOption(TrackedSocketOptions.NoDelay)) NoDelay = noDelay;
            if (_handle.IsTrackedOption(TrackedSocketOptions.ReceiveBufferSize)) ReceiveBufferSize = receiveSize;
            if (_handle.IsTrackedOption(TrackedSocketOptions.ReceiveTimeout)) ReceiveTimeout = receiveTimeout;
            if (_handle.IsTrackedOption(TrackedSocketOptions.SendBufferSize)) SendBufferSize = sendSize;
            if (_handle.IsTrackedOption(TrackedSocketOptions.SendTimeout)) SendTimeout = sendTimeout;
            if (_handle.IsTrackedOption(TrackedSocketOptions.Ttl)) Ttl = ttl;

            return SocketError.Success;
        }

        private static void ThrowMultiConnectNotSupported()
        {
            throw new PlatformNotSupportedException(SR.net_sockets_connect_multiconnect_notsupported);
        }

#pragma warning disable CA1822
        private Socket? GetOrCreateAcceptSocket(Socket? acceptSocket, bool unused, string propertyName, out SafeSocketHandle? handle)
        {
            // AcceptSocket is not supported on Unix.
            if (acceptSocket != null)
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_AcceptSocket);
            }

            handle = null;
            return null;
        }
#pragma warning restore CA1822

        private static void CheckTransmitFileOptions(TransmitFileOptions flags)
        {
            // Note, UseDefaultWorkerThread is the default and is == 0.
            // Unfortunately there is no TransmitFileOptions.None.
            if (flags != TransmitFileOptions.UseDefaultWorkerThread)
            {
                throw new PlatformNotSupportedException(SR.net_sockets_transmitfileoptions_notsupported);
            }
        }

        private void SendFileInternal(string? fileName, ReadOnlySpan<byte> preBuffer, ReadOnlySpan<byte> postBuffer, TransmitFileOptions flags)
        {
            CheckTransmitFileOptions(flags);

            SocketError errorCode = SocketError.Success;

            // Open the file, if any
            // Open it before we send the preBuffer so that any exception happens first
            using (SafeFileHandle? fileHandle = OpenFileHandle(fileName))
            {
                // Send the preBuffer, if any
                // This will throw on error
                if (!preBuffer.IsEmpty)
                {
                    Send(preBuffer);
                }

                // Send the file, if any
                if (fileHandle != null)
                {
                    // This can throw ObjectDisposedException.
                    errorCode = SocketPal.SendFile(_handle, fileHandle);
                }
            }

            if (errorCode != SocketError.Success)
            {
                UpdateSendSocketErrorForDisposed(ref errorCode);

                UpdateStatusAfterSocketErrorAndThrowException(errorCode);
            }

            // Send the postBuffer, if any
            // This will throw on error
            if (!postBuffer.IsEmpty)
            {
                Send(postBuffer);
            }
        }
    }
}
