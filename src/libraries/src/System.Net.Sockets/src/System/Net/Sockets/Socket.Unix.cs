// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    public partial class Socket
    {
        partial void ValidateForMultiConnect(bool isMultiEndpoint)
        {
            // ValidateForMultiConnect is called before any {Begin}Connect{Async} call,
            // regardless of whether it's targeting an endpoint with multiple addresses.
            // If it is targeting such an endpoint, then any exposure of the socket's handle
            // or configuration of the socket we haven't tracked would prevent us from
            // replicating the socket's file descriptor appropriately.  Similarly, if it's
            // only targeting a single address, but it already experienced a failure in a
            // previous connect call, then this is logically part of a multi endpoint connect,
            // and the same logic applies.  Either wya, in such a situation we throw.
            if (_handle.ExposedHandleOrUntrackedConfiguration && (isMultiEndpoint || _handle.LastConnectFailed))
            {
                ThrowMultiConnectNotSupported();
            }

            // If the socket was already used for a failed connect attempt, replace it
            // with a fresh one, copying over all of the state we've tracked.
            ReplaceHandleIfNecessaryAfterFailedConnect();
            Debug.Assert(!_handle.LastConnectFailed);
        }

        internal void ReplaceHandleIfNecessaryAfterFailedConnect()
        {
            if (!_handle.LastConnectFailed)
            {
                return;
            }

            // Copy out values from key options. The copied values should
            // be kept in sync with the list in SafeCloseSocket.TrackOption.
            bool broadcast = false, dontFragment = false, noDelay = false;
            int receiveSize = -1, receiveTimeout = -1, sendSize = -1, sendTimeout = -1;
            short ttl = -1;
            LingerOption linger = null;
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
            SafeCloseSocket oldHandle = _handle;
            SocketError errorCode = SocketPal.CreateSocket(_addressFamily, _socketType, _protocolType, out _handle);
            oldHandle.TransferTrackedState(_handle);
            oldHandle.Dispose();
            if (errorCode != SocketError.Success)
            {
                throw new SocketException((int)errorCode);
            }

            // And put back the copied settings.  For DualMode, we use the value stored in the _handle
            // rather than querying the socket itself, as on Unix stacks binding a dual-mode socket to
            // an IPv6 address may cause the IPv6Only setting to revert to true.
            if (_handle.IsTrackedOption(TrackedSocketOptions.DualMode)) DualMode = _handle.DualMode;
            if (_handle.IsTrackedOption(TrackedSocketOptions.DontFragment)) DontFragment = dontFragment;
            if (_handle.IsTrackedOption(TrackedSocketOptions.EnableBroadcast)) EnableBroadcast = broadcast;
            if (_handle.IsTrackedOption(TrackedSocketOptions.LingerState)) LingerState = linger;
            if (_handle.IsTrackedOption(TrackedSocketOptions.NoDelay)) NoDelay = noDelay;
            if (_handle.IsTrackedOption(TrackedSocketOptions.ReceiveBufferSize)) ReceiveBufferSize = receiveSize;
            if (_handle.IsTrackedOption(TrackedSocketOptions.ReceiveTimeout)) ReceiveTimeout = receiveTimeout;
            if (_handle.IsTrackedOption(TrackedSocketOptions.SendBufferSize)) SendBufferSize = sendSize;
            if (_handle.IsTrackedOption(TrackedSocketOptions.SendTimeout)) SendTimeout = sendTimeout;
            if (_handle.IsTrackedOption(TrackedSocketOptions.Ttl)) Ttl = ttl;

            _handle.LastConnectFailed = false;
        }

        private static void ThrowMultiConnectNotSupported()
        {
            throw new PlatformNotSupportedException(SR.net_sockets_connect_multiconnect_notsupported);
        }

        private Socket GetOrCreateAcceptSocket(Socket acceptSocket, bool unused, string propertyName, out SafeCloseSocket handle)
        {
            // AcceptSocket is not supported on Unix.
            if (acceptSocket != null)
            {
                throw new PlatformNotSupportedException();
            }

            handle = null;
            return null;
        }

        private static void CheckTransmitFileOptions(TransmitFileOptions flags)
        {
            // Note, UseDefaultWorkerThread is the default and is == 0.
            // Unfortunately there is no TransmitFileOptions.None.
            if (flags != TransmitFileOptions.UseDefaultWorkerThread)
            {
                throw new PlatformNotSupportedException(SR.net_sockets_transmitfileoptions_notsupported);
            }
        }

        private void SendFileInternal(string fileName, byte[] preBuffer, byte[] postBuffer, TransmitFileOptions flags)
        {
            CheckTransmitFileOptions(flags);

            // Open the file, if any
            // Open it before we send the preBuffer so that any exception happens first
            FileStream fileStream = OpenFile(fileName);

            SocketError errorCode = SocketError.Success;
            using (fileStream)
            {
                // Send the preBuffer, if any
                // This will throw on error
                if (preBuffer != null && preBuffer.Length > 0)
                {
                    Send(preBuffer);
                }

                // Send the file, if any
                if (fileStream != null)
                {
                    // This can throw ObjectDisposedException.
                    errorCode = SocketPal.SendFile(_handle, fileStream);
                }
            }

            if (errorCode != SocketError.Success)
            {
                SocketException socketException = new SocketException((int)errorCode);
                UpdateStatusAfterSocketError(socketException);
                if (NetEventSource.IsEnabled) NetEventSource.Error(this, socketException);
                throw socketException;
            }

            // Send the postBuffer, if any
            // This will throw on error
            if (postBuffer != null && postBuffer.Length > 0)
            {
                Send(postBuffer);
            }
        }

        private async Task SendFileInternalAsync(FileStream fileStream, byte[] preBuffer, byte[] postBuffer)
        {
            SocketError errorCode = SocketError.Success;
            using (fileStream)
            {
                // Send the preBuffer, if any
                // This will throw on error
                if (preBuffer != null && preBuffer.Length > 0)
                {
                    // Using "this." makes the extension method kick in
                    await this.SendAsync(new ArraySegment<byte>(preBuffer), SocketFlags.None).ConfigureAwait(false);
                }

                // Send the file, if any
                if (fileStream != null)
                {
                    var tcs = new TaskCompletionSource<bool>();

                    // This can throw ObjectDisposedException.
                    errorCode = SocketPal.SendFileAsync(_handle, fileStream, (bytesTransferred, socketError) => 
                    {
                        if (socketError != SocketError.Success)
                        {
                            // Synchronous exception from SendFileAsync
                            SocketException socketException = new SocketException((int)errorCode);
                            UpdateStatusAfterSocketError(socketException);
                            if (NetEventSource.IsEnabled) NetEventSource.Error(this, socketException);
                            tcs.SetException(socketException);
                        }

                        tcs.SetResult(true);
                    });

                    await tcs.Task.ConfigureAwait(false);
                }
            }

            if (errorCode != SocketError.Success)
            {
                // Synchronous exception from SendFileAsync
                SocketException socketException = new SocketException((int)errorCode);
                UpdateStatusAfterSocketError(socketException);
                if (NetEventSource.IsEnabled) NetEventSource.Error(this, socketException);
                throw socketException;
            }

            // Send the postBuffer, if any
            // This will throw on error
            if (postBuffer != null && postBuffer.Length > 0)
            {
                // Using "this." makes the extension method kick in
                await this.SendAsync(new ArraySegment<byte>(postBuffer), SocketFlags.None).ConfigureAwait(false);
            }
        }

        private IAsyncResult BeginSendFileInternal(string fileName, byte[] preBuffer, byte[] postBuffer, TransmitFileOptions flags, AsyncCallback callback, object state)
        {
            CheckTransmitFileOptions(flags);

            // Open the file, if any
            // Open it before we send the preBuffer so that any exception happens first
            FileStream fileStream = OpenFile(fileName);

            return TaskToApm.Begin(SendFileInternalAsync(fileStream, preBuffer, postBuffer), callback, state);
        }

        private void EndSendFileInternal(IAsyncResult asyncResult)
        {
            TaskToApm.End(asyncResult);
        }
    }
}
