// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.NETCore.Client
{
    internal class IpcSocket : Socket
    {
        public IpcSocket(SocketType socketType, ProtocolType protocolType)
            : base(socketType, protocolType)
        {
        }

        public IpcSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
            : base(addressFamily, socketType, protocolType)
        {
        }

// .NET 6 implements this method directly on Socket, but for earlier runtimes we need a polyfill
#if !NET6_0_OR_GREATER
        public async Task<Socket> AcceptAsync(CancellationToken token)
        {
            using (token.Register(() => Close(0)))
            {
                try
                {
                    return await Task.Factory.FromAsync(BeginAccept, EndAccept, this).ConfigureAwait(false);
                }
                // When the socket is closed, the FromAsync logic will try to call EndAccept on the socket,
                // but that will throw an ObjectDisposedException. Only catch the exception if due to cancellation.
                catch (ObjectDisposedException) when (token.IsCancellationRequested)
                {
                    // First check if the cancellation token caused the closing of the socket,
                    // then rethrow the exception if it did not.
                    token.ThrowIfCancellationRequested();

                    Debug.Fail("Token should have thrown cancellation exception.");
                    return null;
                }
            }
        }
#endif

        public virtual void Connect(EndPoint remoteEP, TimeSpan timeout)
        {
            IAsyncResult result = BeginConnect(remoteEP, null, null);

            if (result.AsyncWaitHandle.WaitOne(timeout))
            {
                EndConnect(result);
            }
            else
            {
                Close(0);
                throw new TimeoutException();
            }
        }

// .NET 6 implements this method directly on Socket, but for earlier runtimes we need a polyfill
#if !NET6_0_OR_GREATER
        public async Task ConnectAsync(EndPoint remoteEP, CancellationToken token)
        {
            using (token.Register(() => Close(0)))
            {
                try
                {
                    Func<AsyncCallback, object, IAsyncResult> beginConnect = (callback, state) =>
                    {
                        return BeginConnect(remoteEP, callback, state);
                    };
                    await Task.Factory.FromAsync(beginConnect, EndConnect, this).ConfigureAwait(false);
                }
                // When the socket is closed, the FromAsync logic will try to call EndAccept on the socket,
                // but that will throw an ObjectDisposedException. Only catch the exception if due to cancellation.
                catch (ObjectDisposedException) when (token.IsCancellationRequested)
                {
                    // First check if the cancellation token caused the closing of the socket,
                    // then rethrow the exception if it did not.
                    token.ThrowIfCancellationRequested();
                }
            }
        }
#endif
    }
}
